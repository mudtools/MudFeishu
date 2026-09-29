# Mud.Feishu.AI.FeishuTools

把 `Mud.Feishu` 的强类型接口以**编译期 Schema** 暴露为模型可调用的 FunctionCall 工具，
并内置多租户授权执行链、出站净化、内容安全与流式回复通道。

> 本文档聚焦"工具面"这一件事：**当前有哪些工具、怎么加一个新工具、模型看不到的能力怎么办、
> 安全边界在哪**。Agent 运行时（会话、流式、知识检索装配）见 `Mud.Feishu.AI` 的 Readme。

---

## 1. 工具面现状（31 个：24 只读 + 7 写类）

| 域 | 工具 |
| --- | --- |
| Bitable（4 只读 + 1 写） | `bitable.list_tables` / `list_fields` / `query_records` / `get_records_by_ids` / `add_record`（写） |
| 云文档 Docx（2 只读） | `docx.get_raw_content` / `docx.get_document_blocks` |
| Wiki（2 只读） | `wiki.get_node` / `wiki.list_nodes` |
| 搜索（1 只读） | `search.doc_wiki` |
| IM（2 只读 + 3 写） | `im.get_history_messages` / `im.get_message_content` / `send_message`（写）/ **`send_image` / `send_file`（写，需宿主落盘器，见 §7）** |
| 云空间 Drive（2 只读） | `drive.list_folder_files` / `drive.get_file_metas` |
| 电子表格 Sheets（2 只读） | `sheets.list_sheets` / `sheets.get_range_values` |
| 通讯录 Contact（4 只读） | `contact.resolve_user`（邮箱/手机号→ID）/ **`search_user`（姓名/关键字→ID）** / `get_user` / `batch_get` |
| 审批 Approval（1 写） | `approval.create_instance`（写） |
| 日历 Calendar（2 只读 + 1 写） | **`calendar.find_free_slots` / `list_events` / `create_event`（写）** |
| 任务 Task（1 只读 + 1 写） | **`task.create_task`（写）/ `task.list_my_tasks`（只读，`identity=user`）** |
| 知识库（1 只读） | `knowledge.search`（绑定宿主 `IRetriever`） |
| 元工具（1 只读） | **`feishu.capability_lookup`**（能力出处，见 §4） |

**权威清单以编译期产物为准**：`FeishuToolNames.All`、`FeishuToolContracts.ByToolName`（生成器发射的
**类型化契约表**，见 §8）、`FeishuToolSchemas.SchemaByToolName`、以及《工具权限对照表》
（`documents/AIAgent/工具权限对照表.md`，与契约表逐行交叉验证）。
四者的相等由契约守卫机械断言——**本文档中的数字只是说明，不是断言依据**。

**时间语义铁律**：模型侧一律**带时区的 RFC3339**（如 `2026-10-01T14:00:00+08:00`）；
平台侧形态（日历 `date_time`、任务毫秒时间戳）由工具层确定性转换——
把两种格式暴露给模型必然出现"毫秒当秒"的 1970 年静默错误。

---

## 2. 启用方式（默认全部不启用）

```csharp
services.AddFeishuTools();                 // 引入全部域（含元工具）
// 或按域装配：AddFeishuBitableTools() / AddFeishuImTools() / AddFeishuContactTools() / ...
```

```jsonc
{
  "FeishuAgent": {
    "Tools": ["bitable.list_tables", "contact.search_user"],       // 只读白名单
    "WriteAllowList": ["im.send_message"],                          // 写工具单独键控（默认空 = 不启用任何写工具）
    "MaxToolRisk": "high-risk-write",                               // 策略轴：风险上限（默认值 = 不额外收紧）
    "AllowedIdentities": ["tenant", "user"],                        // 策略轴：身份闭集（默认 ["tenant"]）
    "ContentSafetyMode": "warn"                                     // off | warn（默认） | block
  }
}
```

- 名单放错类别（写工具进 `Tools`、只读工具进 `WriteAllowList`）在**注册期 fail-fast**。
- 写工具在 `EnforceToolAuthorization=true`（默认）且未注册 `IToolExecutionAuthorizer` 时**默认拒绝**。
- **启用 `identity=user` 的工具**（如 `task.list_my_tasks`）时，`AllowedIdentities` 必须显式放行 `user`：
  白名单映射完成后集中校验，**装配期 fail-fast 并列出工具名与身份**——
  而不是等运行期被策略轴逐请求拒绝（那会被宿主误判为权限问题）。
  默认值 `["tenant"]` 不变（不削弱默认最小权限）。
- `dry_run=true` 的写工具**不消耗幂等键**（不下发请求），摘要显式回显 `idempotency_key` 的 provided/omitted 状态。

---

## 3. 执行链与安全边界

工具调用依次经过（顺序不可变，由行为断言锁定）：

```
① appKey 上下文校验
①' 入站净化        —— 控制字符/危险 Unicode/独立 CR → 拒绝（invalid_args），零调用下游
② 策略轴           —— MaxToolRisk / AllowedIdentities → 拒绝（policy_denied: reason_code）
③ 授权门禁         —— IToolExecutionAuthorizer → 拒绝（authorization_denied: ...）；
                      NeedsUserConfirmation → 签发无状态确认令牌，并**只**投递给宿主批准通道
                      （IFeishuToolApprovalChannel）；令牌**不进入模型上下文**（R2-1，见下）
④ 租户上下文切换    —— BeginScope(appKey)
④' 用户上下文       —— 仅 identity=user 工具：写入 IFeishuCurrentUserContext（AsyncLocal，
                      用户令牌缓存查找键）并在 finally 清理；tenant 路径不触碰
                       （泄漏 = 跨用户令牌误用，用例成对断言设置/清理）
⑤ 内容安全         —— 4 条注入规则扫描原始结果（off | warn | block）
⑥ 出站净化（强制）  —— ANSI/控制字符剥离 + 凭据与手机号脱敏；无开关、不可绕过
⑦ 整形钩子         —— IToolResultShaper（可空）
   审计            —— 允许/拒绝/错误三类都投递 IToolExecutionAuditSink
```

**执行器骨架（WP3，R4）**：`if (!outcome.Ok)` / `catch (ArgumentException)` / `TruncateJson` 等机械骨架
全部收敛到 `Internal/ToolExecutor.cs`（`RunAsync` / `FromApi` / `FromPlainText` / `FromApiUntruncated` /
多步链路的 `FailIfError`）；每个执行器方法内 `FeishuToolNames.X` 只出现 1 次（守卫机械断言）。
**投影（`ProjectXxx` 的字段点选）保留在各执行器**——那是逐字段的业务意图，不是骨架。

**边界说明（有意为之）**：

- **邮箱与标识类字段不脱敏**：邮箱是平台寻址货币（`receive_id_type=email`），
  `open_id`/`chat_id`/`page_token` 是多步链路的必需凭据。
- **入站是"拒绝"而非"剥离"**：剥离会静默改写用户内容（语义污染）；拒绝让模型立刻可自愈。
  换行/RFC 换行/Tab/Emoji（含 ZWJ 序列）**不误伤**。
- **内容安全默认 `warn` 而非 `block`**：命中即标注 `[untrusted_content: 规则]`，不阻断——
  工具结果里合法出现"忽略上一段"这类字面文本是可能的（例如一份评审文档）。
- **`dry_run`（写工具）**：只回 `method`/`path` 与请求体字段**长度**摘要，不回原文，也不调用下游。

### 人工确认（HITL）语义（R2-1，**宿主可见的行为变更**）

`IToolExecutionAuthorizer` 返回 `NeedsUserConfirmation` 时，执行链签发一枚无状态确认令牌
（HMAC，绑定 `toolName` + 参数摘要 + `appKey` + `userId`，默认 10 分钟有效）。

**令牌只交给宿主，绝不进入模型上下文。**

| 角色 | 职责 |
| --- | --- |
| SDK（`FeishuToolBinding`） | 签发令牌 → 构造 `ToolApprovalRequest` → 调 `IFeishuToolApprovalChannel.RequestApprovalAsync` → 回填模型的是**中性文案**（`needs_confirmation` + 宿主关联号） |
| 宿主 | 实现 `IFeishuToolApprovalChannel`，在自有界面（飞书卡片/工单/审批单）展示待确认项；用户批准后把令牌作为工具参数 `confirm_token` 回灌，重新发起调用 |
| 未注册通道 | HITL **降级为纯提示**（fail-closed）：模型只会收到"需要用户确认"，拿不到令牌 |

> **为什么改**：旧实现把令牌内插进回填模型的拒绝文案，使批准所需的全部要素都进入模型上下文，
> 而令牌校验只校验签名/有效期/绑定、**不校验批准是否来自人** ⇒ 模型可自行带令牌重试放行写操作，
> 一次成功的提示注入即可绕过人工确认。

### P4-1：改由 MAF 审批管线把关（**当前形态**）

写类工具现在会用 MEAI `ApprovalRequiredAIFunction` 包装，因此**写调用到达拦截点的时序前移**：

| 角色 | 职责 |
| --- | --- |
| MAF（`FunctionInvokingChatClient`） | 在调用**之前**把包装工具的调用转成 `ToolApprovalRequestContent`（工具此刻**未执行**） |
| SDK（`ConversationalFeishuEventHandler`） | 识别该内容 → `IFeishuToolApprovalChannel.RequestFrameworkApprovalAsync` 提交宿主 → 向用户回一条「等待人工确认」；**未注册通道即 fail-closed** |
| 宿主 | 在自有界面完成批准后，由宿主侧回灌批准响应继续本轮；`ApprovalResponseBindingChatClient` 只接受与框架请求绑定的响应 |
| 自研 `confirm_token` 路径 | **已标 `[Obsolete]`（P4-3）**，仅剩「非写类工具的动态选择性确认」一种用途，计划 next-major 移除 |

**执行链对写类工具不再二次拦截（P4-3）**：写工具能被执行链看到，就说明框架已经批准过——
若此时授权器仍返回 `NeedsUserConfirmation` 而执行链又走自研令牌，宿主无法把 `confirm_token`
注入模型工具参数 ⇒ 写工具会卡死在「框架已批准、执行链仍拒绝」的**死胡同**。
故 `FeishuToolBinding` 对写类工具的待确认判定直接放行（记 Information 日志），
`NeedsUserConfirmation` 对写工具退化为「由框架承载」的语义。
**非写类工具**不进入框架审批，自研令牌仍是其唯一 HITL 机制（已废弃但可用）。

**关键安全收益**：批准资格由框架绑定到「框架发出的请求」，模型**无法自批复**——
这补上了上述 R2-1 方案里"仍需依赖模型自律"的最后一环。

> **迁移**：若宿主此前按旧文案实现「把令牌抄回去重试」，须改为实现 `IFeishuToolApprovalChannel`、
> 由宿主侧回灌令牌；旧路径的令牌从未真正证明"人已批准"，应予废弃。

---

## 4. 模型看不到的能力，出路在哪

本包刻意**不**做"每个 SDK 方法一个工具"（1155 无差别暴露）也不做通用裸 `api` 工具。
三层结构如下：

| 层 | 内容 | 模型可见？ |
| --- | --- | --- |
| L1 能力目录 | 编译期聚合事实（SDK 方法总数 / 分组分布 / 策展计数），`build_property.FeishuToolCatalog=true` 时产出 | ❌（`internal`） |
| L2 暴露策展 | 标注了 `[FeishuTool]` 的 24 个工具 | ✅（白名单启用后） |
| **L3 能力出路** | **`feishu.capability_lookup`**：按关键字回答"这个能力在 SDK 里有几个分组 / 是否已策展成工具" | ✅（默认不启用） |

所以模型遇到不认识的域时，正确动作是**先问 `feishu.capability_lookup`**，据此判断
"是不存在（放弃）"还是"存在但宿主没启用（如实告知用户）"，而不是臆造一次调用。
该工具的**边界**：只返回分组级元数据，**不返回方法名、不返回请求构造**（方法名不进编译期产物，
且返回它等于变相提供通用调用能力）。

---

## 5. 新增一个域工具（标准作业模板）

以 `calendar.create_event` / `task.list_my_tasks`（R4/WP5）为样本，实测 **4 处手改 + 2 处机械**：

1. **核对 SDK 签名与 DTO**（`Mud.Feishu/Interfaces/{Module}/`）→ 定下 `Source` 字符串
   （形如 `"IFeishuTenantV4CalendarEvent.CreateCalendarEventAsync"`）。
   ⚠️ 双令牌派生接口（`IFeishuTenantV*`/`IFeishuUserV*`）是**空**接口，方法在基接口上。
2. **写工具接口声明**（手写）：`Tools/Feishu{Tool}ToolInterfaces.cs` ——
   `[FeishuTool("域.动作", Description=…, RequiredScopes=[…], IsWrite=…, Source=…)]` +
   `[ToolParameter("名", "说明", Required=…)]` 扁平参数。
   分页尺寸/排序/容器类型等**运维参数不进 Schema**（绑定层补齐并钳制）；`page_token` 例外保留。
   ⚠️ `identity=user` 的工具，其接口名**必须以 `IFeishuUser` 开头**（`MUDFT016` 跨字段校验）。
3. **写执行器**（手写，投影独占）：`Internal/{Tool}Tools.cs` —— `new ToolExecutor(FeishuToolNames.X, maxLength)`
   → `RunAsync(async () => { ToolArgs 取参 → SDK 调用 → FeishuApiResultReader.Read → executor.FromApi(...) })`；
   **不要**手写 `if (!outcome.Ok)` / `catch (ArgumentException)`（WP3 守卫会红）。
4. **注册 + 装配**（手写，一行/工具 + 一行/域）：`Registration/FeishuToolDomainRegistrars.cs` 增域注册器；
   `Extensions/FeishuToolsServiceCollectionExtensions.cs` 增 `Add…Core` 并串入入口。
5. **golden 重固化**（机械）：见下方流程。
6. **守卫与文档**（机械）：`documents/AIAgent/scope-authority.json` 回填新 scope →
   《工具权限对照表》由守卫逐行交叉验证 → 调用链用例（断言 method/path/body/query 实参）。

**不再需要**：手抄 scope 期望表（已删，WP2 起契约表就是唯一真相源）、手抄对照表工具名
（`PermissionMappingDocContractGuards` 机械比对）、运行期解析 Schema（`FeishuToolContracts` 是编译期常量）。

**DoD**：① golden diff 已评审；② 新 scope 已回填权威清单且对照表同批更新；
③ 有**真实调用链路**用例（不是"工具存在"断言）+ 一条参数非法的结构化错误负例；
④ `Source` 可解析；⑤ 写工具默认不启用且过授权门禁；
⑥ `risk`/`is_write`/`identity` 与 Schema 一致；⑦ 新域 guidance 资产（可选，见 §8）已补。

> **golden 重固化的循环依赖**：漂移会让 `MUDFT014`（Error）中断构建，而重固化要靠构建出的程序集跑测试。
> 可行流程：**先把 `FeishuToolSchemas.golden.txt` 移开** → 构建（无 `AdditionalFiles`，不比对）→
> `$env:FeishuToolGoldenUpdate='true'; dotnet test Tests/Mud.Feishu.AI.FeishuTools.Tests --filter "FullyQualifiedName~FeishuToolGoldenTests"`
> → 删除旧备份文件。

---

## 6. 不变量（改动本包前请先读）

| # | 不变量 | 锁定方式 |
| --- | --- | --- |
| A1 | `SchemaByToolName.Keys == FeishuToolNames.All == FeishuToolContracts.AllNames == registry.AllTools == 对照表键集` | 契约守卫 |
| A2 | 每个零容忍诊断有上报点**且**有可触发反例 | `Mud.Feishu.AI.Tools.Tests` 的 driver 负例（**11/11 已落地**，R4/WP1）+ 元守卫（登记缺失即红）；R3 的"债务登记表"已删除 |
| A3 | `risk >= write` 的工具必经授权钩子；无授权器时默认拒绝 | 执行链 + 用例 |
| A4 | 出站净化在整形钩子之前 | `Execute_ShouldSanitizeBeforeResultShaper` |
| A5 | 入站净化对参数必经且**先于授权门禁** | `Execute_InboundSanitization_ShouldRejectControlChars_BeforeAuthorizer` |
| A6 | 新工具必须能通过 `Source` 锚定到 SDK 符号 | `MUDFT019` |
| A7 | 模型可见能力面的任何变化必须产生 golden diff | `MUDFT014` 中断构建 + golden 用例 |
| A8 | 能力目录覆盖数字精确锁定 | `GeneratorCapabilityCatalogTests` |
| A9 | 出站顺序固定为 `内容安全 → 净化 → 整形 → 审计标记` | `Execute_ContentSafety_ShouldAnnotate_AndStillSanitize` |

**防假绿铁律**：「工具存在」不算通过（必须有断言 method/path/body 的链路用例）；
断言诊断为 0 必须同时断言构建成功与产物非空；「必经」类性质不得用源码扫描验证（改用运行时行为断言）。

---

## 7. 多模态附件：落盘是宿主的责任（R4/WP7）

`im.send_image` / `im.send_file` 走**三步链路**：URL 落盘 → 上传取 key → 以 key 发消息。

```csharp
public interface IFeishuAttachmentStager   // 宿主实现（Mud.Feishu.AI.Tools）
{
    Task<StagedAttachment?> StageAsync(AttachmentSource source, CancellationToken cancellationToken);
}
public readonly record struct StagedAttachment(string LocalPath, long Size, string? ContentType, Func<ValueTask> Cleanup);
```

- **SDK 不实现下载/落盘**：域名白名单（防 SSRF）、大小上限、扩展名/MIME 校验、落盘目录
  **全是宿主策略**（SDK 既不知道宿主的网络边界，也不该替它决定写哪里）。
- **软缺席**：宿主未注册该接口 → 两个上传工具**不注册**（与"域客户端缺席 → 该域工具不注册"同一语义）；
  宿主返回 `null` → 工具层回填结构化 `invalid_args`（不降级为"跳过校验"）。
- **生命周期显式**：`Cleanup` 由执行链在 `finally` 中调用（不进 GC/finalizer），
  成功与失败路径**都**清理（用例断言执行后临时文件不存在）。
- **参数只收 URL**：模型给出本地路径是危险信号（无从得知宿主磁盘布局）→ 直接 `invalid_args`。

### 7.1 参考实现（WP1 / R5）

`Demos/Mud.Feishu.Agent.Demo/DemoAttachmentStager.cs` 提供 **Demo 级参考实现**：

```csharp
services.AddHttpClient<DemoAttachmentStager>();
services.AddSingleton<IFeishuAttachmentStager>(sp => sp.GetRequiredService<DemoAttachmentStager>());
```

| 安全项 | Demo 默认值 | 生产宿主应 |
| --- | --- | --- |
| 协议 | 只允许 http/https（防 SSRF） | 收紧为 HTTPS-only + 域名白名单 |
| 大小上限 | 25 MB | 按业务调整 |
| 扩展名 | 白名单（图片/文档/压缩/文本/音视频） | 按业务收窄 |
| 临时目录 | `Path.GetTempPath()` 下唯一子目录 | 按存储策略调整 |
| 清理 | `Cleanup` 在 `finally` 中删除文件 | 同（生命周期显式） |

**软缺席语义（宿主未实现 stager 时）**：`im.send_image` / `im.send_file` 不注册（模型看不到这两个工具），
不报错、不静默失败——与"域客户端缺席 → 该域工具不注册"同一机制。

---

## 8. 编译期契约出口与域 guidance（R4/WP2/WP6）

生成器在**同一 pass** 发射（都只进本程序集）：

| 产物 | 内容 | 消费方 |
| --- | --- | --- |
| `FeishuToolSchemas` | 模型侧 JSON 载荷（参数 + `x-feishu` 元数据） | 工具桥 + golden 门禁 |
| `FeishuToolNames` | 工具名契约表（只读/写分离） | 白名单、守卫 |
| **`FeishuToolContracts`** | **类型化契约**（`Risk`/`Identity`/`IsWrite`/`RequiredScopes`/`SdkSource`/`HttpMethod`/`Route`） | 注册器（直接构造定义，**零运行期解析**）、目录、守卫、权威 scope 清单校验 |
| **`FeishuToolGuidance`** | 域 guidance（素材 `Guidance/{domain}.md`，AdditionalFiles） | `FeishuGuidanceComposer`（宿主指令之后追加，2 KB 上限，超限按域截断并在返回值上给 `Truncated` 信号） |

**scope 权威性**：`documents/AIAgent/scope-authority.json`（人工从控制台核对回填）与契约表构成
**双向守卫**——契约里的 scope 必须在清单中（缺口即红），清单里未标 `⚠️` 的必须被至少一个工具使用
（防僵尸权限），`⚠️` 项必须持续可见。
