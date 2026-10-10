# Mud.Feishu.AI.Tools

把 `Mud.Feishu` 的强类型接口以**编译期 Schema** 暴露为模型可调用的 FunctionCall 工具，
并内置多租户授权执行链与出站净化。

> 本文档聚焦"工具面"这一件事：**当前有哪些工具、怎么加一个新工具、模型看不到的能力怎么办、
> 安全边界在哪**。Agent 运行时（会话、流式、知识检索装配）见 `Mud.Feishu.AI` 的 Readme。
>
> **⚠️ R-9（2026-10-10）：飞书 Agent 集成面已并入 `Mud.Feishu.AI`。** 本包现在**只**承载工具面
> （策展 + 执行链 + 编译期契约），下列内容换了落点，改代码前先看：
>
> | 内容 | 现落点（命名空间 / 入口） |
> | --- | --- |
> | 流式消息通道（`EditMessageChannel` / `CardStreamMessageChannel` / `StreamingChannelChain`） | `Mud.Feishu.AI.Channels`（`Mud.Feishu.AI` 程序集） |
> | 会话事件处理器与装配器（`ImMessage…` / `ApprovalTask…` / `TaskUpdated…` / `BitableRecordChanged…` / `SenderInfo…` / `QuoteMessage…`） | `Mud.Feishu.AI.Events` |
> | 事件外桥与事件目录（`FeishuEventNdjsonBridge` / `FeishuEventCatalog` / `FeishuEventEnvelope`） | `Mud.Feishu.AI.Events` |
> | Aily 托管知识问答（`AilyKnowledgeProvider`） | `Mud.Feishu.AI.Knowledge` |
> | 装配入口 `AddFeishuEditMessageChannel` / `AddFeishuStreamingChannel` / `AddFeishuImConversationHandler` / `AddFeishuKnowledgeContext` / `AddFeishuAilyKnowledge` | `Mud.Feishu.AI.Extensions`（`AddFeishuAgent` 所在文件） |
>
> 收益：本包**不再引用** `Mud.Feishu.EventCallback`——"只接工具面"的宿主不必再背事件 DTO 面
> （由守卫 `AgentContractGuards.ToolPackage_ShouldNotReferenceEventCallback` 机械锁定）。

---

## 1. 工具面现状（162 个：88 只读 + 74 写类）

| 域 | 只读 | 写类 | 小计 |
| --- | ---: | ---: | ---: |
| Bitable | 6 | 3 | 9 |
| 云文档 Docx | 8 | 9 | 17 |
| Wiki | 2 | 3 | 5 |
| 搜索 Search | 1 | — | 1 |
| IM | 8 | 8 | 16 |
| 云空间 Drive | 3 | 12 | 15 |
| 电子表格 Sheets | 2 | 2 | 4 |
| 通讯录 Contact | 6 | — | 6 |
| 审批 Approval | 2 | 4 | 6 |
| 日历 Calendar | 3 | 4 | 7 |
| 任务 Task | 1 | 7 | 8 |
| 邮件 Mail | 5 | 2 | 7 |
| 妙记 Minutes | 5 | — | 5 |
| OKR | 9 | 6 | 15 |
| 视频会议 VC | 6 | 4 | 10 |
| 知识库 Knowledge | 1 | — | 1 |
| **画板 Board**（R7/A4） | 2 | 4 | 6 |
| **考勤 Attendance**（R7/A5） | 7 | 1 | 8 |
| **妙搭 Spark**（R7/A6） | 5 | 5 | 10 |
| **AI 文本**（R7/C3） | 2 | — | 2 |
| 元工具 | 4 | — | 4 |
| **合计** | **88** | **74** | **162** |

> **R-12（2026-10-10）**：万能兜底通道 `feishu.api_call` 已**整条删除**（双轨调用路径 + 全工具面风险最高的面
> + 零外部消费）。未策展能力的正确处置是「先用 `feishu.capability_lookup` 判断是不存在还是未策展，
> 再如实告知用户」，而不是提供一条绕开策展审查的调用通道。

> **逐工具清单**：以《工具权限对照表》（`documents/AIAgent/工具权限对照表.md`）为准——
> 本表只给域级汇总，不再逐工具列举（逐工具列举会与契约表漂移）。

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
    "Tools": ["bitable.list_tables", "contact.search_user"], // 只读白名单
    "WriteAllowList": ["im.send_message"], // 写工具单独键控（默认空 = 不启用任何写工具）
    "MaxToolRisk": "high-risk-write", // 策略轴：风险上限（默认值 = 不额外收紧）
    "AllowedIdentities": ["tenant", "user"], // 策略轴：身份闭集（默认 ["tenant"]）
    "ContentSafetyMode": "warn", // off | warn（默认） | block
  },
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
                      NeedsUserConfirmation → 通知宿主批准通道（IFeishuToolApprovalChannel）
                      并**中性拒绝**；SDK 不签发、不校验任何凭据（WP3 / R4-1，见下）
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

### 人工确认（HITL）语义（WP3 后：**无令牌版**；R4-1 起读写工具同路径）

批准状态的**唯一所有者**是宿主授权器 `IToolExecutionAuthorizer`。SDK 侧不签发、不校验任何凭据
（旧的 HMAC 确认令牌 / `confirm_token` 已**整条删除**，源码中不再存在）。

| 角色                       | 职责                                                                                                                                                                                                                                        |
| -------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| SDK（`FeishuToolBinding`） | 授权器返回 `NeedsUserConfirmation` → 构造 `ToolApprovalRequest`（含工具名/参数摘要/appKey/userId/原因）→ 调 `IFeishuToolApprovalChannel.RequestApprovalAsync` → 回填模型的是**中性文案**（`needs_confirmation` + 宿主关联号），**恒不放行** |
| 宿主授权器                 | 记为挂起；用户批准后建立"已批准"上下文；**下次同 `(tool, argsDigest, appKey, userId)` 调用返回 `Allowed`** ⇒ 执行链放行                                                                                                                     |
| 未注册通道                 | HITL **降级为纯提示**（fail-closed）：模型只会收到"需要用户确认"，拿不到任何凭据                                                                                                                                                            |

**待确认快照（R7 / C4a）**：通道是"通知宿主"的同步点，通知完即返回；进程重启后宿主无从得知还有哪些写操作
停在等待确认。SDK 因此提供 `IFeishuPendingApprovalStore`（宿主契约 + 进程内默认实现 `InMemoryPendingApprovalStore`）：

- 执行链在通知通道**之前**把 `PendingApprovalSnapshot`（请求标识/工具名/appKey/userId/会话键/入参摘要/过期时间，
  **不含任何凭据**）落库；落库失败**不阻断**通知（best-effort，写工具仍保持未执行）；
- 宿主用 `ListPendingAsync(appKey)` 列出未过期待办、`FindAsync` 查单条、`RemoveAsync` 取消（放弃该次写操作）；
- `RunApprovalContinuationAsync(..., pendingApprovalStore: store)` 启用**幂等消费**：同一 `RequestId` 的批准
  只生效一次；不存在 / 已过期 / 已被消费三者一律 fail-closed（**丢弃迟到批准，绝不重放**）；
- 默认有效期 10 分钟（`PendingApprovalSnapshot.DefaultTtl`）；多实例部署由宿主替换为分布式实现
  （键必须含 `AppKey` 以隔离租户）。

### 审批时序：MAF 管线在前，执行链在后（**R4-1 订正**）

写类工具会被 MEAI `ApprovalRequiredAIFunction` 包装，**写调用到达拦截点的时序前移**：

| 角色                                      | 职责                                                                                                                                          |
| ----------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| MAF（`FunctionInvokingChatClient`）       | 在调用**之前**把包装工具的调用转成 `ToolApprovalRequestContent`（工具此刻**未执行**）                                                         |
| SDK（`ConversationalFeishuEventHandler`） | 识别该内容 → `IFeishuToolApprovalChannel.RequestFrameworkApprovalAsync` 提交宿主 → 向用户回一条「等待人工确认」；**未注册通道即 fail-closed** |
| 宿主                                      | 在自有界面完成批准后，调用 **`FeishuAgent.RunApprovalContinuationAsync`** 回灌续跑（见下节）；`ApprovalResponseBindingChatClient` 只接受与框架请求绑定的响应 |

### 批准之后：续跑轮必须显式重建租户上下文（**R5-2 / R5-11**）

批准**不等于**放行：续跑轮**不在事件流内**，`IFeishuToolContextAccessor` 的 `AsyncLocal` 执行上下文
（唯一建立点在 `ConversationalFeishuEventHandler`）并不存在。缺上下文时工具会 fail-closed 结构化拒绝
（多租户隔离禁止默认 appKey 兜底，TMA2-20），表现为「批准了却不生效」。

**推荐路径**（SDK 闭环，一次调用完成加载会话 → 取框架记录的原始审批请求 → 重建工具上下文 → 续跑 → 落库）：

```csharp
var response = await agent.RunApprovalContinuationAsync(
    toolContextAccessor,          // 容器解析的 IFeishuToolContextAccessor（AddFeishuTools* 已 Singleton 注册）
    approval,                     // 批准通道收到的 FrameworkToolApprovalRequest（原样回传，RequestId 必须带回）
    approved: true,
    reason: "管理员已确认",
    conversationGate: gate,       // 建议：与事件层同款闸门，保证同会话串行
    approvalChannel: channel);    // 建议：续跑轮若再产出新的写工具审批，SDK 会再次提交宿主

// 宿主仍需自己把 response.Text 投递给用户；若续跑又产出审批请求，文本为空（已提交通道）。
```

**未续跑 ≠ 无害（R5-12）**：发起确认的那一轮会把待应答审批请求写进会话历史，而 MEAI
`FunctionInvokingChatClient` 对**整段入站历史**做审批配对校验——残留的未应答审批会让该会话
**此后每一轮**直接抛 `InvalidOperationException`。SDK 已在事件层对该形态自愈（新的用户轮次会放弃
该待确认项、摘除孤儿审批内容并清空框架记录），因此「宿主从不续跑」只会退化为「该次写操作被放弃」，
不会毒化会话；**迟到的批准**也会被框架绑定层丢弃（fail-closed，绝不把已放弃的写操作重新放行）。

**执行链仍会二次把关（R4-1）**：`ApprovalRequiredAIFunction` 是 MEAI **纯标记类型**，
其拦截只在 `FunctionInvokingChatClient` 内部生效——宿主直接 `InvokeAsync`、或调用方绕开该管线时，
包装**不会**阻止写工具执行。因此执行链**不得**据"框架已批准"放行：

- 授权器未返回 `Allowed` 的 `NeedsUserConfirmation`，**读、写工具一律**走同一条挂起解析
  （通知宿主通道 + 中性拒绝）⇒ fail-closed；
- 宿主在 MAF 批准回调中**必须同步更新授权器状态**（置为已批准），否则会出现
  「框架已批准、执行链仍拒绝」的表现。这是 WP3「批准状态单一所有权」的必然结果：
  批准事实只有一个来源，框架的回执不构成执行链可验证的证据。

> **R4-1 之前的行为**（已废弃）：写工具在授权门禁被无条件放行（`Pass()`），依据是注释里的假设
> 「框架不批准则本方法根本不会被调用」。该假设运行期无任何校验，是一条静默 fail-open 路径。

**关键安全收益**：批准事实由宿主授权器持有并逐次咨询，模型**无法自批复**；
写工具的直调路径也不再绕过人工确认。

---

## 4. 模型看不到的能力，出路在哪

本包刻意**不**做"每个 SDK 方法一个工具"（1228 无差别暴露），而是按高频工作流**策展**为 162 个工具。
对于未策展的方法，提供两层兜底（而非变相暴露全部 1228 方法）：

| 层 | 内容 | 模型可见？ |
| --- | --- | --- |
| L1 能力目录 | 编译期聚合事实（SDK 方法总数 / 分组分布 / 策展计数），`build_property.FeishuToolCatalog=true` 时产出 | ❌（`internal`） |
| L2 暴露策展 | 标注了 `[FeishuTool]` 的 162 个工具 | ✅（白名单启用后） |
| **L3 能力出路** | **`feishu.capability_lookup`**：按关键字回答"这个能力在 SDK 里有几个分组 / 是否已策展成工具" | ✅（默认不启用） |
| **L3.1 方法签名** | **`feishu.schema_read`**：查任意 SDK 方法的签名事实（HTTP/路由/参数/令牌/风险/是否已策展） | ✅（只读，`feishu:base` scope） |
| **L3.2 guidance** | **`feishu.guidance_read`**：按需读取 L2 references（`{域}/{主题}`），键不存在时列出全部候选 | ✅（只读） |

所以模型遇到不认识的域时，正确动作是**先问 `feishu.capability_lookup`**，据此判断
"是不存在（放弃）"还是"存在但宿主没启用（如实告知用户）"，而不是臆造一次调用。
若需查方法签名细节用 `feishu.schema_read`——但查到**不等于能调用**：未策展的方法
没有执行通道，必须**如实告知用户该能力暂不可用**（可建议宿主策展）。

> **R-12（2026-10-10）**：原先的 **L3.2 兜底调用 `feishu.api_call`**（方法名 + 参数 → HTTP 动态调度）
> 已**整条删除**。删除理由：① 与策展工具构成**双轨调用路径**（同一能力两条路径，
> 投影/幂等/风险语义不同，是长期维护负担）；② 它是全工具面**风险最高的面**（任意已登记方法的调度）；
> ③ **零外部消费**（Demos 未启用，仅测试覆盖）。删除后"未策展能力"的模型侧语义从
> 「可兜底」收敛为「如实告知」——这正是"不妄称能力"的最小权限形态。
> 连带清理：`Guidance/feishu.md` 与 `Guidance/spark/publish.md` 的兜底指引、
> `schema_read` 的 Description 与 note 文案、`schema_read` 的 `IFeishuAppManager` 依赖；
> 并由新增守卫 `GuidanceToolReferenceContractGuards` 机械锁定
> 「guidance 里的 `feishu.*` 引用必须存在于 `FeishuToolNames.All`」防同类复发。

---

## 5. 新增一个域工具（标准作业模板）

以 `calendar.create_event` / `task.list_my_tasks`（R4/WP5）为样本，实测 **4 处手改 + 2 处机械**：

1. **核对 SDK 签名与 DTO**（`Mud.Feishu/Interfaces/{Module}/`）→ 定下 `Source` 锚点，形如
   **`Source = nameof(IFeishuTenantV4CalendarEvent) + "." + nameof(IFeishuTenantV4CalendarEvent.CreateCalendarEventAsync)`**
   （R-1 起为 `nameof` 常量拼接：编译期求值结果与旧字面量 `"IFeishuTenantV4CalendarEvent.CreateCalendarEventAsync"` 逐字节相同，
   但 SDK 改名可随 IDE 重命名联动。⚠️ **单段 `nameof(接口.方法)` 只产出方法名**（丢接口名），会被判为 `MUDFT019`）。
   ⚠️ 双令牌派生接口（`IFeishuTenantV*`/`IFeishuUserV*`）是**空**接口，方法在基接口上。
2. **写工具接口声明**（手写）：`Curation/Feishu{Domain}ToolInterfaces.cs` ——
   `[FeishuTool("域.动作", Description=…, RequiredScopes=[…], IsWrite=…, Source=…)]` +
   `[ToolParameter("名", "说明", Required=…)]` 扁平参数。
   分页尺寸/排序/容器类型等**运维参数不进 Schema**（绑定层补齐并钳制）；`page_token` 例外保留。
   ⚠️ **工具接口名必须携带令牌标记**：租户令牌用 `IFeishuTenant{域}{动作}Tool`，用户令牌用
   `IFeishuUser{域}{动作}Tool`（`MUDFT016` 会把「工具身份」与「承载接口名推导出的令牌类型」比对，
   缺标记即构建失败——R-1+2c 起 Tenant 侧也强制，此前只有 User 侧强制）。
3. **写执行器**（手写，投影独占）：`Internal/{Tool}Tools.cs` —— `new ToolExecutor(FeishuToolNames.X, maxLength)`
   → `RunAsync(async () => { ToolArgs 取参 → SDK 调用 → FeishuApiResultReader.Read → executor.FromApi(...) })`；
   **不要**手写 `if (!outcome.Ok)` / `catch (ArgumentException)`（WP3 守卫会红）。
4. **注册 + 装配**（生成器自动产出）：`ToolRegistrarEmitter` 按执行器类自动生成域注册器
   （`FeishuToolDomainRegistrars/*.g.cs`）与逐域 DI 核心方法（`AddFeishu{Tools}Core`，
   `FeishuToolsServiceCollectionCoreExtensions.g.cs`）。手写侧仅保留公开入口编排
   （`Extensions/FeishuToolsServiceCollectionExtensions.cs` 的 `AddFeishuReadonlyToolCores` /
   `AddFeishuWriteToolCores` 链中增一行 `AddFeishu{NewDomain}ToolsCore()`）。
5. **golden 重固化**（机械）：见下方流程。
6. **守卫与文档**（机械）：`documents/AIAgent/scope-authority.json` 回填新 scope →
   《工具权限对照表》由守卫逐行交叉验证 → 调用链用例（断言 method/path/body/query 实参）。

**不再需要**：手抄 scope 期望表（已删，WP2 起契约表就是唯一真相源）、手抄对照表工具名
（`PermissionMappingDocContractGuards` 机械比对）、运行期解析 Schema（`FeishuToolContracts` 是编译期常量）。

**DoD**：① golden diff 已评审；② 新 scope 已回填权威清单且对照表同批更新；
③ 有**真实调用链路**用例（不是"工具存在"断言）+ 一条参数非法的结构化错误负例；
④ `Source` 可解析；⑤ 写工具默认不启用且过授权门禁；
⑥ `risk`/`is_write`/`identity` 与 Schema 一致；⑦ 新域 guidance 资产（可选，见 §8）已补。

> **golden 重固化（R-1+2c 起有一键通道）**：漂移会让 `MUDFT014`（Error）中断构建，而重固化要靠构建出的
> 程序集跑测试——为解开这个自锁，csproj 提供了显式重固化窗口：
>
> ```powershell
> $env:FeishuToolGoldenUpdate='true'
> dotnet test Tests/Mud.Feishu.AI.Tools.Tests -p:FeishuToolRefreeze=true `
>   --filter "FullyQualifiedName~FeishuToolGoldenTests"
> ```
>
> `-p:FeishuToolRefreeze=true` 让构建期 `AdditionalFiles` 暂时不引入快照（从而不比对），测试则用运行时
> 真实 Schema 重写快照。固化后**必须**：评审 diff（媒体可见面变更）→ 运行
> `pwsh ./scripts/sync-publicapi.ps1`（同步 `PublicAPI.Unshipped.txt` 里的 Schema 常量载荷）→ 记录 CHANGELOG。

---

## 6. 不变量（改动本包前请先读）

| #   | 不变量                                                                                                            | 锁定方式                                                                                                                |
| --- | ----------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| A1  | `SchemaByToolName.Keys == FeishuToolNames.All == FeishuToolContracts.AllNames == registry.AllTools == 对照表键集` | 契约守卫                                                                                                                |
| A2  | 每个零容忍诊断有上报点**且**有可触发反例                                                                          | `Mud.Feishu.AI.Tools.Tests` 的 driver 负例（**11/11 已落地**，R4/WP1）+ 元守卫（登记缺失即红）；R3 的"债务登记表"已删除 |
| A3  | `risk >= write` 的工具必经授权钩子；无授权器时默认拒绝                                                            | 执行链 + 用例                                                                                                           |
| A4  | 出站净化在整形钩子之前                                                                                            | `Execute_ShouldSanitizeBeforeResultShaper`                                                                              |
| A5  | 入站净化对参数必经且**先于授权门禁**                                                                              | `Execute_InboundSanitization_ShouldRejectControlChars_BeforeAuthorizer`                                                 |
| A6  | 新工具必须能通过 `Source` 锚定到 SDK 符号                                                                         | `MUDFT019`                                                                                                              |
| A7  | 模型可见能力面的任何变化必须产生 golden diff                                                                      | `MUDFT014` 中断构建 + golden 用例                                                                                       |
| A8  | 能力目录覆盖数字精确锁定                                                                                          | `GeneratorCapabilityCatalogTests`                                                                                       |
| A9  | 出站顺序固定为 `内容安全 → 净化 → 整形 → 审计标记`                                                                | `Execute_ContentSafety_ShouldAnnotate_AndStillSanitize`                                                                 |

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

| 安全项   | Demo 默认值                          | 生产宿主应                     |
| -------- | ------------------------------------ | ------------------------------ |
| 协议     | 只允许 http/https（防 SSRF）         | 收紧为 HTTPS-only + 域名白名单 |
| 大小上限 | 25 MB                                | 按业务调整                     |
| 扩展名   | 白名单（图片/文档/压缩/文本/音视频） | 按业务收窄                     |
| 临时目录 | `Path.GetTempPath()` 下唯一子目录    | 按存储策略调整                 |
| 清理     | `Cleanup` 在 `finally` 中删除文件    | 同（生命周期显式）             |

**软缺席语义（宿主未实现 stager 时）**：`im.send_image` / `im.send_file` 不注册（模型看不到这两个工具），
不报错、不静默失败——与"域客户端缺席 → 该域工具不注册"同一机制。

### 7.2 二进制边界：**字节两侧都不进工具面**（R7 / §3.B6 + DP-R7-2 修订）

工具面禁止二进制穿越（A10，机械守卫）。**本轮（DP-R7-2，2026-10-10）裁定的结论是：这条边界只需要一个
宿主接缝** —— `IFeishuAttachmentStager`（既有、已被 `im.send_image`/`im.send_file` 与受控下载出口使用，
且由守卫核验"豁免必须挣得"）：

| 方向 | 做法 | 约束 |
| --- | --- | --- |
| **入向**（模型要发文件/图片） | 模型只给 **URL**；宿主 `IFeishuAttachmentStager` 落盘 → SDK 上传 → 发送 | 未注册 stager ⇒ 相关工具**不注册**（软缺席）；模型给本地路径 ⇒ `invalid_args` |
| **出向**（模型要拿产物：画板缩略图 / 文件下载 / 妙记附件） | **受控下载出口**：执行器取字节 → `DownloadedContentGuard.ShouldRejectForJsonErrorBody` 拦平台错误体 → stager 落盘 → 结果只回**路径 + 大小 + Content-Type**（`no-bytes-in-context`） | 豁免**必须挣得**：守卫核验"注入非可空 stager + 调用错误体防线"，光改描述无效（`BinaryDownloadToolExposureContractTests.FindUnearnedExemptions`） |
| **宿主自用**（宿主自己调 OCR / 文档识别 / STT） | 宿主把 URL/本地文件直接交给 SDK 的 `[FormContent]`/base64 参数，再把**文本**结果放进模型上下文 | 不需要任何 SDK 侧契约 —— 这些能力**不策展**（DP-C3-1） |

> **撤销记录（DP-R7-2）**：R7 曾新增两个"出入成对"契约（`IFeishuBinaryArtifactSink` 出向 /
> `IFeishuBinaryArtifactSource` 入向）。本轮代码级调研发现二者**零消费方且无可行路径**：出向已由
> stager 覆盖（受控出口已选定 stager，与 sink 职责重叠），入向的对手是"宿主手搓"而非真实缺口；
> 且所有字节型工具还卡在下面这个**更根本**的前置上。因此**发布前删除两者**，设计与依据留存于
> §3.B6 的撤销记录 —— 避免发布面出现"不可能被调用"的公开接口，也避免宿主面对三条路的选择困难。

- **出向工具尚未落地的真实阻塞点（DP-R7-1）**：`output_schema` 由 **Source 方法的返回类型**推导，
  而字节型方法的返回类型是 `Task<byte[]?>` ⇒ 生成的 Schema 会告诉模型"本工具返回二进制"，
  与该工具实际返回的"路径/大小/类型"**互相矛盾（假事实）**；`[FeishuTool]` 目前没有 output_schema
  覆盖能力。该能力（`OutputSchema` 覆盖，或对二进制 Source 跳过推导）落在**组件仓
  `Mud.HttpUtils.Generator`**，是本仓 `drive.download_file` / `board.download_image` 等出向工具的**共同前置**。
- **SDK 不提供落盘实现**：域名白名单（防 SSRF）、大小上限、扩展名/MIME 校验、落盘目录**全是宿主策略**。
- **生命周期显式**：`StagedAttachment.Cleanup` 由调用方/执行链在 `finally` 中释放（成功与失败路径都清理）。

---

### 7.3 Skills 产物导出（R7 / C6a · 生态位）

仓外 Agent（Claude Code / Cursor / 自建 harness）**接不进本仓进程**，只能读文件。为此提供第二个导出方言：

```csharp
IToolSchemaExporter.Export(ToolSchemaDialect.Skills);
// => {"dialect":"skills","files":[{"path":"skills/feishu-im/SKILL.md","content":"…"}, …]}
```

- **产物形态**（已入库，可用 `npx skills add` 之类的机制直接加载）：
  `skills/feishu-{domain}/SKILL.md`（frontmatter `name/description/version` + **L1 guidance 原样嵌入** +
  命令清单〔工具名 + 只读/写 + Schema 描述〕+ references 索引）与
  `skills/feishu-{domain}/references/{topic}.md`（**L2 资产逐字落地**）。
- **为什么是清单 JSON 而不是"一个路径"**：`IToolSchemaExporter.Export` 的返回契约是 `string`
  （PublicAPI 已锁定），而 Skills 是**目录树**。清单让导出结果可序列化、可逐字节比对（守卫），
  落盘由宿主 / 脚本 / 测试完成——**库不做文件 IO** 是既有纪律。
- **数据源全是编译期常量**（`FeishuToolGuidance.ByDomain` / `References` / `FeishuToolSchemas` /
  `FeishuToolNames`）⇒ 零反射、零 IO、确定性（同输入必然同产物）。
- **guidance 是唯一真相源**：导出层**原样嵌入**正文，不做摘要/改写——加工一次就多一份真相源。
- **守卫**（`SkillsExportContractTests`）：产物树**逐字节**一致（陈旧文件也会被逮住）、工具清单与
  `FeishuToolNames.All` **双向**一致（只查单向会漏掉"新增工具没更新产物"）、references 逐字一致、
  frontmatter 形态、确定性、**打包路径存在**。重固化：`FeishuSkillsUpdate=true` 后重跑该测试类。
- **随包分发**：`skills/` 在仓库根（对标官方布局的默认扫描路径），并由 csproj 的
  `<None Include="..\skills\**\*" Pack="true" PackagePath="skills\" />` **打进 NuGet 包** ⇒
  只装包的用户也能拿到技能包（库本身不做文件 IO，这一行是"生态位"落到用户手里的唯一通道）。
  ⚠️ `PackagePath` 末尾的**反斜杠不能省**：省掉（或写成 `skills\%(RecursiveDir)`）会让 NuGet 与递归目录
  叠加成 `skills/feishu-ai/feishu-ai/SKILL.md`；反过来写成 `skills\%(Filename)%(Extension)` 则会把 21 个域的
  同名 `SKILL.md` 拍平覆盖。两种坏法都由 `Package_ShouldShipSkillsTree` 守卫挡下。

### 7.4 事件外桥：NDJSON（R7 / C7 · 生态位）

> **⚠️ R-9**：`FeishuEventNdjsonBridge` / `RegexEventFileRouter` / `FeishuEventEnvelope` /
> `IFeishuEventSink` / `IFeishuEventFileRouter` / `FeishuEventCatalog` 已迁到
> **`Mud.Feishu.AI.Events`**（程序集 `Mud.Feishu.AI`）。语义不变，只改命名空间与程序集引用。

事件此前只能进**本进程内模型**；外桥把同一条事件以 NDJSON 交给仓外任意进程：

```csharp
var bridge = new FeishuEventNdjsonBridge(
    output: Console.Out,
    router: new RegexEventFileRouter("./events", ("^im\\.", "im"), ("^approval\\.", "approval")));

await bridge.WriteAsync(new FeishuEventEnvelope(
    eventKey: "im.message.receive_v1",
    payloadJson: rawJson,      // ← 传输层原始文本，原样嵌入
    appKey: appKey, eventId: eventId));
```

- **一行一事件**：`{"event_key":…,"app_key":…,"event_id":…,"received_at":…,"payload":{…}}`。
  `payload` **原样嵌入**（不再序列化）——避免字段丢失、AOT 反射序列化破口，以及"本仓 JSON 上下文
  成了外桥的格式瓶颈"。
- **非法载荷 fail-fast**：外桥丢事件是最难发现的故障（表现为"某个订阅一直没数据"），宁可接入时就崩。
- **互斥闸门**：同一事件既进模型又外桥 = 双重副作用。`EnsureNotConversational(注册数)` 会显式抛错，
  确认事件面互斥后用 `AllowConcurrentWithConversational("IM 进模型、审批外桥")` 放行（必须给理由）。
- **路由落盘**：每（目录 × 事件键）一个稳定 `.ndjson`、**追加**写（可 tail）；未命中不落盘也不建空目录。
- **事件目录自省**：`FeishuEventCatalog.ListEventKeys()` 给出全部事件键（反射 `FeishuEventTypes` 常量，
  唯一真相源），供配置路由正则时对齐命名。**偏差登记**：只提供事件键，不提供"载荷 schema 摘要"
  ——载荷↔事件键的映射只存在于源生成阶段，运行期要么手抄（会漂移）要么反射实例化（更糟）。

### 7.5 多维表格记录变更 → 会话（R7 / C2 配套）

> **⚠️ R-9**：`BitableRecordChangedConversationalEventHandler` 与 `BitableRecordContextAssembler`
> 现同处 `Mud.Feishu.AI.Events`（处理器）/ `Mud.Feishu.AI.Events`（装配器）——即**两者已同程序集**，
> 不再需要"工具包 → 集成面"的跨包引用来让装配器与处理器配对。

`BitableRecordChangedConversationalEventHandler` 把 `drive.file.bitable_record_changed_v1`
变成"模型可用的上下文 + 单聊回复"：

- **精确路由**：覆写 `SupportedEventType`（记录变更是高频事件，其它事件不进本处理器）；
- **只登记变化字段**：`EventFacts` 里逐字段给 `diff`（前后值），未变字段不进 prompt；
- **单聊投递给操作人**（行级事件无 `chat_id`）；缺消息客户端 / 解析不出操作人 ⇒ **模型调用前短路**；
- **高频提醒**：宿主不注册即完全不生效；注册即"每条订阅表变更跑一轮模型"——请据此收窄平台订阅面；
- 配套装配器 `BitableRecordContextAssembler`（Order = 200）在 `Mud.Feishu.AI` 包内。

### 7.6 进程外 Agent：MCP server（R7 / C6b · 可选包）

工具面除了进程内模型，还可经 **MCP（stdio JSON-RPC 2.0）**交给进程外 Agent，
实现在**独立可选包** `Mud.Feishu.AI.Mcp`（不引用 SDK 强类型客户端、不被任何核心包引用）：

```csharp
builder.Services.AddFeishuTools();                       // 工具面（白名单经 FeishuAgent:Tools/WriteAllowList）
builder.Services.AddFeishuMcpServer(o => o.AppKey = "cli_xxx");  // 进程级租户（必填）
await app.Services.RunFeishuMcpStdioAsync();             // 阻塞到客户端关闭 stdin
```

- **白名单同源**：MCP 暴露的工具 == `FeishuAgent:Tools` + `WriteAllowList`，**不新增开关**；
- **执行链零旁路**：每个 tool call 走同一个 `FeishuToolBinding`（授权 / 租户切换 / 净化 / 审计不变）；
- **租户边界 = 进程**：appKey 只来自配置（协议层传 appKey 等于把切租户交给客户端），
  跨租户宿主请**每租户一个 stdio 进程**；缺 appKey 启动期即抛（fail-closed）；
- **写工具 HITL**：授权器返回"需人工确认" ⇒ `isError` + `needs_user_confirmation` + **零调用下游**
  （MCP 不走 MAF 审批管线，故不依赖 `ApprovalRequiredAIFunction` 这个**仅标记**的类型）；
- **名字映射**：契约名 `.` → `_`（严格客户端只接受 `[a-zA-Z0-9_-]{1,64}`），契约名经 `title` 透出；
- **stdio 注意事项**：stdout 是协议通道，日志必须写 stderr/文件。

> **依赖选型**：本包 MCP 协议层为**自研**（不引官方 `ModelContextProtocol` SDK），仅覆盖
> `tools` 能力 + stdio。裁决依据、**已证伪的常见理由**（TFM 兼容性 / AOT / 预览版三条均不成立）
> 与**翻转触发条件**（HTTP-SSE / 多能力 / 客户端侧 / 新修订差异化语义）见
> `.docs/AI/MudFeishu-AI-Mcp-协议层依赖选型-ADR.md`。

---

## 8. 编译期契约出口与域 guidance（R4/WP2/WP6）

生成器在**同一 pass** 发射（都只进本程序集）：

| 产物                      | 内容                                                                                            | 消费方                                                                                               |
| ------------------------- | ----------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| `FeishuToolSchemas`       | 模型侧 JSON 载荷（参数 + `x-feishu` 元数据）                                                    | 工具桥 + golden 门禁                                                                                 |
| `FeishuToolNames`         | 工具名契约表（只读/写分离）                                                                     | 白名单、守卫                                                                                         |
| **`FeishuToolContracts`** | **类型化契约**（`Risk`/`Identity`/`IsWrite`/`RequiredScopes`/`SdkSource`/`HttpMethod`/`Route`） | 注册器（直接构造定义，**零运行期解析**）、目录、守卫、权威 scope 清单校验                            |
| **`FeishuToolGuidance`**  | 域 guidance（素材 `Guidance/{domain}.md`，AdditionalFiles）                                     | `FeishuGuidanceComposer`（宿主指令之后追加，2 KB 上限，超限按域截断并在返回值上给 `Truncated` 信号） |

**scope 权威性**：`documents/AIAgent/scope-authority.json`（人工从控制台核对回填）与契约表构成
**双向守卫**——契约里的 scope 必须在清单中（缺口即红），清单里未标 `⚠️` 的必须被至少一个工具使用
（防僵尸权限），`⚠️` 项必须持续可见。

### 8.1 写工具幂等能力表（R7/WP2-T2-4）

| 工具                           | 暴露 `idempotency_key` | 底层支持 `client_token` | 说明                                                                                                                        |
| ------------------------------ | ---------------------- | ----------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| `im.send_message`              | ✅                     | ✅                      | 平台侧 1 小时去重                                                                                                           |
| `im.reply_message`             | ✅                     | ✅                      | 平台侧 1 小时去重                                                                                                           |
| `im.send_image`                | ❌                     | —                       | 三步链路，幂等由落盘器保证                                                                                                  |
| `im.send_file`                 | ❌                     | —                       | 同上                                                                                                                        |
| `bitable.add_record`           | ✅                     | ✅                      | 相同键返回同一条记录                                                                                                        |
| `bitable.update_record`        | ✅                     | ✅                      | 相同键不产生副作用                                                                                                          |
| `bitable.delete_record`        | ❌                     | —                       | 删除天然幂等                                                                                                                |
| `approval.create_instance`     | ✅                     | ✅                      | 相同键返回错误码 60012                                                                                                      |
| `approval.approve_task`        | ❌                     | —                       | 同意操作天然幂等                                                                                                            |
| `docx.create_document`         | ❌                     | ❌                      | 平台端点与 SDK 请求模型**均不支持**幂等键（`CreateDocumentRequest` 仅 `FolderToken`/`Title`，无 `client_token`；R4-5 订正） |
| `docx.append_blocks`           | ✅                     | ✅                      | 24 小时去重                                                                                                                 |
| `sheets.update_range`          | ❌                     | —                       | 覆盖写天然幂等                                                                                                              |
| `sheets.append_rows`           | ❌                     | —                       | 追加操作非幂等                                                                                                              |
| `drive.create_folder`          | ❌                     | —                       | 非幂等                                                                                                                      |
| `drive.move_file`              | ❌                     | —                       | 异步操作，非幂等                                                                                                            |
| `drive.upload_file`            | ❌                     | —                       | 非幂等                                                                                                                      |
| `calendar.create_event`        | ✅                     | ✅                      | 平台原生幂等                                                                                                                |
| `calendar.update_event`        | ❌                     | —                       | PATCH 按字段更新天然幂等                                                                                                    |
| `calendar.delete_event`        | ❌                     | —                       | 删除天然幂等                                                                                                                |
| `calendar.add_event_attendees` | ❌                     | —                       | 非幂等（重复添加同一用户无效）                                                                                              |
| `task.create_task`             | ✅                     | ✅                      | 平台原生幂等                                                                                                                |
| `task.update_task`             | ❌                     | —                       | PATCH 按字段更新天然幂等                                                                                                    |
| `task.complete_task`           | ❌                     | —                       | 通过 update 实现，幂等                                                                                                      |
| `task.delete_task`             | ❌                     | —                       | 删除天然幂等                                                                                                                |
| `task.create_subtask`          | ✅                     | ✅                      | 平台原生幂等                                                                                                                |
| `task.add_comment`             | ❌                     | —                       | 非幂等                                                                                                                      |
| `task.add_members`             | ✅                     | ✅                      | 平台原生幂等                                                                                                                |
| `mail.send_message`            | ❌                     | —                       | 两步合一，非幂等                                                                                                            |

**汇总**：28 个写工具中，10 个暴露 `idempotency_key`，18 个不暴露（其中 8 个天然幂等，10 个非幂等）。
