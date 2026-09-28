# Mud.Feishu.AI.FeishuTools

把 `Mud.Feishu` 的强类型接口以**编译期 Schema** 暴露为模型可调用的 FunctionCall 工具，
并内置多租户授权执行链、出站净化、内容安全与流式回复通道。

> 本文档聚焦"工具面"这一件事：**当前有哪些工具、怎么加一个新工具、模型看不到的能力怎么办、
> 安全边界在哪**。Agent 运行时（会话、流式、知识检索装配）见 `Mud.Feishu.AI` 的 Readme。

---

## 1. 工具面现状（24 个：21 只读 + 3 写类）

| 域 | 工具 |
| --- | --- |
| Bitable（4 只读 + 1 写） | `bitable.list_tables` / `list_fields` / `query_records` / `get_records_by_ids` / `add_record`（写） |
| 云文档 Docx（2 只读） | `docx.get_raw_content` / `docx.get_document_blocks` |
| Wiki（2 只读） | `wiki.get_node` / `wiki.list_nodes` |
| 搜索（1 只读） | `search.doc_wiki` |
| IM（2 只读 + 1 写） | `im.get_history_messages` / `im.get_message_content` / `send_message`（写） |
| 云空间 Drive（2 只读） | `drive.list_folder_files` / `drive.get_file_metas` |
| 电子表格 Sheets（2 只读） | `sheets.list_sheets` / `sheets.get_range_values` |
| 通讯录 Contact（4 只读） | `contact.resolve_user`（邮箱/手机号→ID）/ **`search_user`（姓名/关键字→ID）** / `get_user` / `batch_get` |
| 审批 Approval（1 写） | `approval.create_instance`（写） |
| 知识库（1 只读） | `knowledge.search`（绑定宿主 `IRetriever`） |
| 元工具（1 只读） | **`feishu.capability_lookup`**（能力出处，见 §4） |

**权威清单以编译期产物为准**：`FeishuToolNames.All`（由源生成器从 `[FeishuTool]` 派生）、
`FeishuToolSchemas.SchemaByToolName`、以及《工具权限对照表》（`documents/AIAgent/工具权限对照表.md`）。
三者的相等由契约守卫机械断言——**本文档中的数字只是说明，不是断言依据**。

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
    "AllowedIdentities": ["tenant"],                                // 策略轴：身份闭集
    "ContentSafetyMode": "warn"                                     // off | warn（默认） | block
  }
}
```

- 名单放错类别（写工具进 `Tools`、只读工具进 `WriteAllowList`）在**注册期 fail-fast**。
- 写工具在 `EnforceToolAuthorization=true`（默认）且未注册 `IToolExecutionAuthorizer` 时**默认拒绝**。

---

## 3. 执行链与安全边界

工具调用依次经过（顺序不可变，由行为断言锁定）：

```
① appKey 上下文校验
①' 入站净化        —— 控制字符/危险 Unicode/独立 CR → 拒绝（invalid_args），零调用下游
② 策略轴           —— MaxToolRisk / AllowedIdentities → 拒绝（policy_denied: reason_code）
③ 授权门禁         —— IToolExecutionAuthorizer → 拒绝（authorization_denied: ...）
④ 租户上下文切换    —— BeginScope(appKey)
⑤ 内容安全         —— 4 条注入规则扫描原始结果（off | warn | block）
⑥ 出站净化（强制）  —— ANSI/控制字符剥离 + 凭据与手机号脱敏；无开关、不可绕过
⑦ 整形钩子         —— IToolResultShaper（可空）
   审计            —— 允许/拒绝/错误三类都投递 IToolExecutionAuditSink
```

**边界说明（有意为之）**：

- **邮箱与标识类字段不脱敏**：邮箱是平台寻址货币（`receive_id_type=email`），
  `open_id`/`chat_id`/`page_token` 是多步链路的必需凭据。
- **入站是"拒绝"而非"剥离"**：剥离会静默改写用户内容（语义污染）；拒绝让模型立刻可自愈。
  换行/RFC 换行/Tab/Emoji（含 ZWJ 序列）**不误伤**。
- **内容安全默认 `warn` 而非 `block`**：命中即标注 `[untrusted_content: 规则]`，不阻断——
  工具结果里合法出现"忽略上一段"这类字面文本是可能的（例如一份评审文档）。
- **`dry_run`（写工具）**：只回 `method`/`path` 与请求体字段**长度**摘要，不回原文，也不调用下游。

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

以 `contact.search_user` 为样本，实测 6 步：

1. **核对 SDK 签名与 DTO**（`Mud.Feishu/Interfaces/{Module}/`）→ 定下 `Source` 字符串
   （形如 `"IFeishuTenantV3User.GetUsersByKeywordAsync"`）。
   ⚠️ 双令牌派生接口（`IFeishuTenantV*`/`IFeishuUserV*`）是**空**接口，方法在基接口上。
2. **写工具接口声明**：`Tools/Feishu{Tool}ToolInterfaces.cs` ——
   `[FeishuTool("域.动作", Description=…, RequiredScopes=[…], IsWrite=…, Source=…)]` +
   `[ToolParameter("名", "说明", Required=…)]` 扁平参数。
   分页尺寸/排序/容器类型等**运维参数不进 Schema**（绑定层补齐并钳制）；`page_token` 例外保留。
3. **写执行器**：`Internal/{Tool}Tools.cs` —— `ToolArgs` 取参 → SDK 调用 →
   `FeishuApiResultReader.Read` 解包 → **白名单投影** → `ToolResultText.TruncateJson` 截断。
   参数非法抛 `ArgumentException`（执行链会转成结构化错误回填模型）。
4. **注册**：`Registration/FeishuToolDomainRegistrars.cs` 增/改域注册器，
   一行 `FeishuToolRegistration.RegisterExecution(registry, FeishuToolNames.X, binding, (args, ct) => executor.YAsync(args, ct))`
   （缺编译期 Schema 即 fail-fast）。
5. **DI 装配**：`Extensions/FeishuToolsServiceCollectionExtensions.cs` 增 `Add…Core` 并串入入口。
6. **重固化与守卫**：golden 重固化 → `FeishuToolContractGuards` 期望表 → 《工具权限对照表》→
   调用链用例（断言 method/path/body/token 类型）。

**DoD**：① golden diff 已评审；② 守卫表与对照表同批更新；③ 有**真实调用链路**用例
（不是"工具存在"断言）；④ `Source` 可解析；⑤ 写工具默认不启用且过授权门禁；
⑥ `risk`/`is_write`/`identity` 与 Schema 一致。

> **golden 重固化的循环依赖**：漂移会让 `MUDFT014`（Error）中断构建，而重固化要靠构建出的程序集跑测试。
> 可行流程：**先把 `FeishuToolSchemas.golden.txt` 移开** → 构建（无 `AdditionalFiles`，不比对）→
> `$env:FeishuToolGoldenUpdate='true'; dotnet test Tests/Mud.Feishu.AI.FeishuTools.Tests --filter "FullyQualifiedName~FeishuToolGoldenTests"`
> → 删除旧备份文件。

---

## 6. 不变量（改动本包前请先读）

| # | 不变量 | 锁定方式 |
| --- | --- | --- |
| A1 | `SchemaByToolName.Keys == FeishuToolNames.All == registry.AllTools == 守卫表键集` | 契约守卫 |
| A2 | 每个零容忍诊断有上报点**且**有可触发反例 | 元守卫 + 负例登记表（**债务 11 项待补 driver 级负例**，见方案 §13.3） |
| A3 | `risk >= write` 的工具必经授权钩子；无授权器时默认拒绝 | 执行链 + 用例 |
| A4 | 出站净化在整形钩子之前 | `Execute_ShouldSanitizeBeforeResultShaper` |
| A5 | 入站净化对参数必经且**先于授权门禁** | `Execute_InboundSanitization_ShouldRejectControlChars_BeforeAuthorizer` |
| A6 | 新工具必须能通过 `Source` 锚定到 SDK 符号 | `MUDFT019` |
| A7 | 模型可见能力面的任何变化必须产生 golden diff | `MUDFT014` 中断构建 + golden 用例 |
| A8 | 能力目录覆盖数字精确锁定 | `GeneratorCapabilityCatalogTests` |
| A9 | 出站顺序固定为 `内容安全 → 净化 → 整形 → 审计标记` | `Execute_ContentSafety_ShouldAnnotate_AndStillSanitize` |

**防假绿铁律**：「工具存在」不算通过（必须有断言 method/path/body 的链路用例）；
断言诊断为 0 必须同时断言构建成功与产物非空；「必经」类性质不得用源码扫描验证（改用运行时行为断言）。
