# Mud.Feishu 更新日志

## [Unreleased] - AI 工具面架构重构 R1（阶段 0/1/1B/2/3/4/6 + 阶段 5）：出站唯一出口 + 删 api_call + 执行层收敛 + 公开面收敛 + 集成面并入 AI（2026-10-10）

> 方案与多维评审见 `.docs/AI/MudFeishu-AI-Tools-架构审查与重构方案-R1.md`（§13 评审结论、§14/§15 落地核验）。
> **AI 模块尚未发布，无兼容负担**；工具面 **163 → 162**（删除 `feishu.api_call`）。

### 💥 破坏性变更

- **R-9：飞书 Agent 集成面并入 `Mud.Feishu.AI`（阶段 5，最高价值项）**：
  - **迁移**：`Channels/`（4 类：`BufferedMessageChannel` / `CardStreamMessageChannel` / `EditMessageChannel` /
    `StreamingChannelChain`）、`Events/`（8 类：三个领域会话处理器 + `ImMessageConversationalEventHandler` +
    `ContextAssemblers` + `ImConversationOptions` + 事件外桥与事件目录）、`Knowledge/AilyKnowledgeProvider.cs`
    ——共 13 个文件，命名空间 `Mud.Feishu.AI.Tools.{Channels,Events,Knowledge}` → **`Mud.Feishu.AI.{Channels,Events,Knowledge}`**。
  - **入口迁移**：`AddFeishuEditMessageChannel` / `AddFeishuStreamingChannel` / `AddFeishuImConversationHandler` /
    `AddFeishuKnowledgeContext` / `AddFeishuAilyKnowledge` 从 `Mud.Feishu.AI.Tools` 迁到
    **`Mud.Feishu.AI.Extensions`**（`AddFeishuAgent` 所在文件）。调用方只需换 `using`，签名逐字不变。
  - **5.0 下沉批**（先于搬迁，否则「AI 不得引用 AI.Tools」与迁移后编译直接冲突）：
    `FeishuApiOutcome<T>` + `FeishuApiResultReader`、**`FeishuToolDiagnostics`**、**`FeishuAppContextScopeFactory`**
    三个 internal 类型从工具面下沉到 `Mud.Feishu.AI`（`InternalsVisibleTo` 回 AI.Tools，不公开化）。
    后两个是**本轮实测新发现的阻塞点**（方案原文只列了第一个）：通道实现要记降级指标、IM 入口要注册作用域工厂实现。
  - **依赖面变化**：① `Mud.Feishu.AI` 新增 `Mud.Feishu`（core）+ `Mud.Feishu.DataModels` + `Mud.Feishu.EventCallback`
    + `Mud.HttpUtils` 引用（**代价明示**：只用 Agent 运行时、自建工具面的宿主会被拉入这些程序集）；
    ② `Mud.Feishu.AI.Tools` **摘除** `Mud.Feishu.EventCallback` 引用——"只接工具面"的宿主不再背事件 DTO 面（880 源文件）。
  - **架构纪律修订（C7）**：`AgentContractGuards` 的"实现包仅允许引用 Abstractions"改为**白名单形态**
    （AI = {Abstractions, core, DataModels, EventCallback}；Redis 维持仅 Abstractions），
    并新增两条**负向不变量**：AI 不得引用 AI.Tools/AI.Mcp（防环）、AI.Tools 不得引用 EventCallback（防回流）。
  - **连带**：`Mud.Feishu.AI.Mcp` 的 csproj 注释与守卫文案由"**不引用** SDK 客户端"改为"**不直接引用且不暴露**"
    （core/DataModels/EventCallback 现为其传递依赖，写"不引用"即半假事实；签名级判据不变）。
  - **公开面**：AI.Tools **998 → 894**，AI **741 → 845**（总量不变、归属变清晰）。
- **R-7 公开面收敛**：`FeishuToolAIFunction` / `FeishuToolBinding` / `FeishuToolCatalog` / `FeishuToolSchemaExporter`
  退出公开面（实现细节，此前仅被 MCP 与测试消费）；新增两条**窄接缝** `IFeishuToolFunctionFactory`（构桥）
  与 `IToolErrorTextFormatter`（错误文案），MCP 改经接缝消费。
- **R-8 删除同义入口 `AddFeishuReadonlyTools`**：它是 `AddFeishuTools` 的转发，但名字承诺"只读"而实现注册含写工具链。
  全域入口只保留 `AddFeishuTools`；"写工具是否可用"由 `FeishuAgent:WriteAllowList` 表达（安全默认不变）。
- **删除万能兜底通道 `feishu.api_call`（R-12）**：删执行器 `Internal/GenericApiTools.cs`（384 行）、
  Curation 声明 `IFeishuTenantApiCallTool`、写链注册行、5 个 `[ToolCall]` 测试用例。
  删除理由（代码级复核）：① 与策展工具构成**双轨调用路径**（同一能力两条路径，投影/幂等/风险语义不同）；
  ② 它是全工具面**风险最高的面**（任意已登记方法的 HTTP 动态调度）；③ **零外部消费**（Demos 未启用，仅测试覆盖）。
  未策展能力的模型侧语义从「可兜底」收敛为「如实告知用户」。
  **连带清理**（同批，否则模型会照指引臆造调用）：`Guidance/feishu.md`（重写五节，删 `api_call` 四条边界）、
  `Guidance/spark/publish.md`、`feishu.schema_read` 的 `Description` 与 note 文案、
  `documents/AIAgent/工具权限对照表.md`（删行）、`Readme.md`（§1 表 / §4 L3 层表 / 数字 163→162）、
  golden 重固化 + `sync-publicapi.ps1` + `skills/` 重生成。
- **`feishu.schema_read` 的语义收窄**：不再指向任何调用通道（原文案"再用 feishu.api_call 发起调用"已删）；
  `curated=false` 现在明确表达「本工具集没有调用它的通道」。

### 🐛 修复

- **B-1（P1）出站截断对模型完全不可见**：8 处手拼信封在把**已截断**文本回填模型时
  `FeishuToolResult.Truncated` 恒为 `false`、`TruncationReason` 恒为 `null`——模型把不完整 JSON
  当完整事实（假事实），且 `tag_truncated` 恒 false 使监控对截断率失明。
  涉及 `SchemaReadTools` / `CapabilityLookupTools`（×2）/ `ToolSearchTools` / `KnowledgeSearchTools` /
  `DocxSheetsDriveWriteTools`（`FromEnvelope`）。**根因是守卫盲区**：原
  `TruncationVisibilityContractGuards` 只扫 `ToolExecutor.cs` 单文件，这 8 个"绕过助手"的执行器不在视野内。
- **B-1 二类**：12 处写工具回执信封**完全无预算意识**（`Board` / `DocxSheetsDriveWrite` / `Mail` /
  `MessageAndApproval` / `Spark` / `DriveCollab`），且同一文件内 `FromEnvelope`（截断留痕）与
  `Untouched`（不截断）态度自相矛盾。
- **B-3/B-6**：`PaginationContractGuards` 文案指向生产零调用的 `ToolPagination.AggregateAsync`；
  该"仅测试使用的适配包装"已删除，测试直连唯一翻页实现 `AggregateOutcomesAsync`。
- **截断判据修正**：`ToolExecutor.FromApi/FromPlainText` 原用「截断后长度 < 原文长度」判定，
  而两个截断器都会**附加**标记文本（101 字符截到 100 再附约 80 字符提示 ⇒ 判据为假）——
  新出口改用「是否超预算」判定（`maxLength > 0 && text.Length > maxLength`）。

### ♻️ 重构

- **R-2 形态认知单源（阶段 3）**：`JsonValueKind` 形态判定白名单由 **6 个文件收缩为单文件**
  （`ToolArgumentNormalizer` 收敛为 `IsJsonString` / `IsJsonArray` 两条原语）；`ToolArgs`（取值门面）、
  `ToolArgsDigester`（审计摘要）、`ToolPagination` / `ToolDryRun` / 执行器改为薄委托。
  ⚠️ **有意行为变更**：`ToolPagination.ReadMaxItems` 对"提供但无法解析为整数"的入参**改为抛错**
  （原为静默降级为"未提供"——那是把模型的传参错误变成一次看似成功的默认行为）；经执行链转结构化错误回填模型。
- **R-3 分页信封单源（阶段 3）**：新增 `Tools/ToolResultJsons`（`PageEnvelope` / `ItemsEnvelope` /
  `WithPageToken`），**31 处 / 18 个文件**的信封构造收敛为单源（含 `BitableTools` 的私有副本）。
  ⚠️ **有意行为变更**：`MailTools.search_messages` 与 `MinutesReadTools` 原先**无条件**写出 `page_token`
  （空值即 `""`），空游标会被模型当成"可续页"照抄回传、平台按"从头再来"处理（重复拉第一页且模型无法自查）——
  统一为"非空才写"。
- **R-4 执行器构造样板收敛（阶段 3）**：`ToolExecutor.Require(IOptions<FeishuAgentOptions>)` 成为
  `IOptions` 空值守卫的**唯一地点**，**28 个文件 / 约 40 行** `(options ?? throw …).Value.MaxXxx` 样板消除。
  ⚠️ 未引入方案原文的 `ToolExecutor.For(name, options)`：执行器预算字段在多个出口点被复用，
  改签名只增加热路径解析而不改变语义；收敛目标已由 `Require` 达成并被守卫锁定。
- **R-1 出站出口唯一化**：新增 `Tools/ToolResultPipeline.cs`（无状态静态工厂 + 显式长度参数）
  = `Ok` / `OkJson` / `OkReceipt` / `Error` / `FromErrorText`；20 处手拼信封全部改走它，
  截断/标记成为**出口的性质**而非"记得写的一步"。⚠️ **净化仍留在 `FeishuToolBinding` 的
  「模型可见」边界**（成功路径第 ⑥ 步 + `EgressResult`），不在 Pipeline 内——两条边界职责不同。
- **B-2 错误载荷单一构造点**：新增 `ToolErrorFactory`，**4 处**同构拷贝（`ToolExecutor.FromStructuredError`、
  `FeishuToolBinding.EgressResult`、catch 路径、`InvokeWithRetryAsync` 的 transient 载荷）归一；
  `new ToolError(...)` 现只允许出现在工厂内（守卫机械拦截）。
- **R-5 风险/身份字面量单源**：`FeishuToolRiskNames.ToLiteral(int)` 新增（目录的 risk 字段是 int），
  删除 `SchemaReadTools` 的私有 `RiskLabel`（其与 `GenericApiTools` 的副本逐字重复，且与
  词汇表构成第三份同源映射）；`FeishuToolBinding.IdentityUser` 改引 `FeishuToolIdentityNames.User`。
- **R-6 模块边界归位**：`FeishuApiResultReader.cs` 拆出 `Tools/ToolArgs.cs`（入参读取）与
  `Tools/ToolResultText.cs`（出站截断）。⚠️ 评审订正：原文称 `ToolResultText` 与 `ToolResultJson`
  构成"循环协作"——**不成立**（依赖单向），本项只是归属与命名整理。

### 🔒 契约/守卫

- **Q-1 既有守卫扩面（先红后绿）**：`TruncationVisibilityContractGuards` 从"扫单文件"改为
  **① 行为断言**（`ToolResultPipeline.Ok/OkJson` 超预算必须回填标记 + 边界回归 + 无预算语义）
  **② 结构断言**（`Internal/` **全目录**不得出现「裸 `Truncate*` + `FromText`」或
  「`FromText(ToolResultJson.ToText(...))`」，白名单为空）+ **③ 反向自证金丝雀**。
- 新增 **`GuidanceToolReferenceContractGuards`**（R-12 防复发）：`Guidance/**/*.md` 里的
  `feishu.{name}` 引用必须存在于 `FeishuToolNames.All`——"删了工具却留下指引"这一缺陷形态
  在编译期与既有 44 个守卫里原本**没有任何信号**。
- 新增 **`ToolError_ShouldBeConstructedOnlyAtTheSingleFactory`** + **`ToolErrorFactory_ShouldProduceTheFrozenPayloadShape`**
  （Q-4 三处归一的等价性基线：字段语义漂移即红）。
- 「出站唯一出口」的既有守卫同步上移判据（`ErrorRecoverabilityTests` 的序列化点判据从
  `ToolExecutor.cs` → `ToolResultPipeline.cs`，否则会盯一个已不负责序列化的文件 = 假门禁）；
  `ToolErrorContractGuards` 守卫 ⑤ 改为"执行器委派出口 + 出口自身序列化"；
  `ToolArgumentShapeContractGuards` 白名单随 R-6 拆分同步（`ToolArgs.cs` / `ToolResultText.cs`）。
- **R-2/R-3/R-4 三条新守卫**：① `ToolArgumentShapeContractGuards` 判据收紧为"形态认知只允许出现在
  `ToolArgumentNormalizer`"（白名单由 6 项收缩为 **1 项**），并**把扫描面扩到 `Curation/`**——
  原守卫只扫 `Tools/`，而声明面已迁到 `Curation/`，等于迁移后留下监控盲区（本轮实测发现并修掉）；
  ② 新增 `PaginationContractGuards.PageEnvelopeFields_ShouldOnlyComeFromSharedHelper`（信封形态只能来自
  `ToolResultJsons`，白名单为空）；③ 新增 `ToolExecutorSkeletonGuards.OptionsNullGuard_ShouldLiveOnlyInToolExecutor`
  （`IOptions` 空值守卫只在 `ToolExecutor`，含反向自证）。
- **R-7 窄接缝 + 公开面内化**：`McpServerContractTests` 判据不变（签名级），但 MCP 改经
  `IFeishuToolFunctionFactory` / `IToolErrorTextFormatter` 消费；两条接缝在 `AddFeishuToolInfrastructure`
  注册（缺注册会让 MCP `tools/list` 静默变空）。
- **R-8 Q-2 方向 B 守卫**：`ToolDomainCoresWiringContractTests.GeneratedCores_ShouldBeMutuallyClosedWithTheAggregator`
  ——判据改用**反射**（生成产物 `AddFeishu*Core` 集合 ↔ 聚合清单双向闭合）。既有方向 A 的期望集由
  "执行器类名正则"推断，命名规则一变就失效（漏聚合仍报绿）。
- **R-13 Demo 链路守卫（4 条，`DomainEventsDemoContractGuards`）**：三个领域处理器 + 两个装配器 +
  流式通道降级链从"仅测试消费"升级为"有真实调用点 + 有链路断言"（行为面反射 + 调用面源码 +
  **零增量落点必须抛错**）；新增 Demo 模式 `FeishuDomainEventsDemo` 与 `DemoAppKeyAccessor`
  （事件处理器在"缺 appKey + 已装配工具链"时 fail-closed，Demo 必须提供应用键事实）。
- **R-9 纪律修订与扫描面跟进**：`AgentContractGuards` 的依赖治理改白名单 + 两条负向不变量；
  `FeishuToolContractGuards.GetFeishuToolsSources()` 扫描面由"仅工具包"扩为"工具包 + 集成面"
  （否则 `FeishuToolDiagnostics` 与 `MaxStreamChunkLength` 两条断言会因"文件不在扫描面"而**假绿**——
  与 R2 那处扫描面盲区同类）。

### 📊 测试基线

- `Tests/Mud.Feishu.AI.Tools.Tests`：**800 × 2 TFM 全绿**（R-9 后：集成面 9 个测试文件迁出至 `Mud.Feishu.AI.Tests`；
  本轮新增 35 例形态等价 + 3 条守卫）。
- `Tests/Mud.Feishu.AI.Tests`：**434 × 2 TFM 全绿**（原 357 + 迁入的通道/事件/知识用例 77）。
- `Tests/Mud.Feishu.Agent.Demo.Tests`：**119 全绿**（新增 R-13 的 4 条链路守卫 + 领域事件模式配置键断言）。
- 构建：`Mud.Feishu.AI` / `Mud.Feishu.AI.Tools` / `Mud.Feishu.AI.Mcp` 四 TFM **0 错误**；
  两包 `RS0016/RS0017` 均为 **0**（公开面基线已同步：AI.Tools 894、AI 845）。


## [Unreleased] - R7 收尾：PM 最终裁定（§8.5）+ 两个重叠契约清理 + 错误子类闭集诚实化（2026-10-10）

> 决策记录见 `.docs/MudFeishu-AI工具面功能完善方案-六域补齐与Agent可用性硬伤及遗留任务-R7.md` §8.5。
> **AI 模块尚未发布，无兼容负担**；工具面**工具数不变（163）**——本批是**契约清理 + 错误契约收敛**，不新增能力。

### 🗑️ 移除

- **`IFeishuBinaryArtifactSink` / `IFeishuBinaryArtifactSource`（R7 §3.B6 的两个"出入成对"宿主契约）**：
  代码级调研结论是二者**零消费方且无可行路径**——出向（字节 → 宿主）已由既有 `IFeishuAttachmentStager`
  覆盖（且"受控下载出口"的豁免**必须挣得**：非可空 stager 注入 + `DownloadedContentGuard.ShouldRejectForJsonErrorBody`），
  入向的对手是"宿主手搓"而非真实缺口；且所有字节型工具还卡在**组件仓的 `output_schema` 表达能力**上
  （字节型 Source 会推导出 `format=binary` 的 Schema，与实现自相矛盾 ⇒ 对模型是假事实）。
  未发布窗口清理 ⇒ 零兼容成本；设计与依据留档于 §3.B6。守卫改判为 `ByteBearingSurfaceContractGuards`
  （锁"参数侧不得出现承载字节的类型"，不再依赖被删类型）。
- **`ToolErrorSubtype.MissingScope` / `SanitizerRejected`**（幽灵子类：声明但全仓无产出点）：
  99991663/99991661 在三层被解释成三种不同语义（工具层=权限类 / WebSocket=机器人被禁用 / 令牌层=令牌错误），
  据此认定"控制台未开通权限"属**猜测**，与"不得编造"的纪律冲突 ⇒ 删除；拿到实证样本后再新增。

### 🐞 修复

- **策略拒绝的原因前缀改用常量**：`FeishuToolBinding.EvaluatePolicy` 的 `risk_exceeded:` /
  `identity_mismatch:` / `tool_not_allowed:` 由字面量改为引用 `ToolErrorSubtype.*`（消除"文案与闭集"双源漂移）。
- **MCP 包仓库注释引用了不存在的守卫名**（`Mcp_ShouldNotReferenceSdkClientTypes` →
  `Mcp_ShouldNotDependOnSdkClientTypes`）——注释里的假事实与代码里的假事实同样有害。

### 🔒 契约/守卫

- 新增守卫 **`ToolErrorSubtype_Constants_ShouldAllHaveProducers`**：每个错误子类常量必须至少有一个引用
  （`Subtype` 实参或文案前缀皆可），否则报红——把"错误闭集不得有幽灵成员"从约定升级为机械约束；
  含自证用例（判据用 `\b` 词边界，防 `GhostSubtypeX` 冒名顶替 `GhostSubtype`）。
- 新增守卫 **`ToolParameters_ShouldNeverBeByteBearing`**（A10 参数侧）：工具契约面不得出现
  `byte[]` / `Memory<byte>` / `Stream` 参数 + 判据自证。
- 测试基线：`AI.Tools.Tests` 836 → **835**（删 5 例契约用例、增 2 例新守卫；net8.0 全绿）。

### 📌 裁定（不改代码的部分）

| 事项 | 裁定 |
| --- | --- |
| `board.download_image`（出向产物） | **维持不策展**；前置 = 组件仓补齐 `output_schema` 表达力，前置完成后与 `drive.download_file`（F-10）同批落地（通道 = stager 挣得豁免） |
| 新 scope 控制台核对（10 项 ⚠️） | **发布硬门禁**：逐项核对并回写 `scope-authority.json`；不通过按平台真名修正，禁止别名兼容 |
| C4b 卡片回灌 / C7 载荷 schema 摘要 / PII 单一真相源 | 确认既有裁定（延后 / 接受偏差 / 批准） |
| MCP 包发布面 | 随 AI 三包同版本发布、默认不启用、不入核心依赖；**发布前**三包统一补 `PackageReadmeFile` + README |
| 工具面扩张治理 | 新增工具三前置继续有效；新增"域"须证明 ≥2 个真实高频场景；任何情况下不得为过构建而放宽守卫 |

## [Unreleased] - R7 Batch-8：MCP server 可选包 + 妙记逐字稿分窗口修复 + 主线 A 链路用例补齐（2026-10-10）

> 方案与落地核验见 `.docs/MudFeishu-AI工具面功能完善方案-六域补齐与Agent可用性硬伤及遗留任务-R7.md` §10.9。
> **AI 模块尚未发布，无兼容负担**；工具面**工具数不变（163）**——本批新增的是**可选包**、一处行为修复与用例。

### ✨ 新增

- **`Mud.Feishu.AI.Mcp`（可选包，R7 / C6b · Batch-8）**：把**白名单已启用**的飞书工具面经
  stdio JSON-RPC 2.0（MCP 2024-11-05 / 2025-03-26 / 2025-06-18）暴露给进程外 Agent。
  - **工具集同源**：== `FeishuAgent:Tools` + `FeishuAgent:WriteAllowList`，**不新增开关**；未启用工具在
    `tools/list` 不可见，调用只回"不存在或未启用"（不回显已启用清单）。
  - **执行链零旁路**：`tools/call` → `FeishuToolAIFunction` → 既有 `FeishuToolBinding`
    （授权门禁 / `BeginScope` 租户切换 / 出站净化 / 内容安全 / 审计 / 截断全部不变）。本包**不引用 SDK 强类型客户端**，
    由反射守卫断言"包内不存在来自 `Mud.Feishu` 程序集的成员签名"（含金丝雀自证）。
  - **租户上下文**：每次调用前 `IFeishuToolContextAccessor.Begin` 重建并成对释放；`appKey` **只来自配置**
    （协议层传 appKey 等于把切租户交给客户端），缺失即启动期 fail-fast；跨租户宿主应每租户一个 stdio 进程。
  - **工具名映射**：契约名 `bitable.query_records` → MCP 名 `bitable_query_records`
    （严格客户端只接受 `[a-zA-Z0-9_-]{1,64}`）；契约名经 `title` 原样透出；映射冲突/超长构造期 fail-fast。
  - **生态位**：`initialize.instructions` 下发已启用域的 guidance（与进程内 Agent 同一份编译期常量与预算装配器），
    `annotations`（`readOnlyHint` / `destructiveHint`）由 `is_write` / `risk` 派生。
  - **写工具 HITL**：不套用 MAF 的 `ApprovalRequiredAIFunction`（经核实它**只是标记、不强制**，强制点在
    `FunctionInvokingChatClient`，而 MCP 不走该管线）；真实门禁是授权器三态——`NeedsUserConfirmation` ⇒
    `isError=true` + `needs_user_confirmation` + **零调用下游**；未注册授权器 ⇒ `authorization_denied`。

### 🐞 修复

- **`minutes.get_artifacts` 的逐字稿窗口"声明了但没人读"**：`transcript_offset` / `transcript_limit` 早已进 Schema
  （描述还承诺返回总长与 `has_more`），但执行器**只用了 `minute_token`**——逐字稿仍按 200 字符预览截断，
  模型传参被静默丢弃、后半段逐字稿**永远取不到**。改为真分窗口：默认 10,000 / 上限 50,000 字符，
  返回 `transcript_total_length` + `has_more` + `next_offset`（续读游标）+ `offset_out_of_range`，
  移除 `transcript_truncated`；**越界参数在下游调用之前拒绝**（不消耗 5 次/秒限速）。
  连带更新 golden、`Guidance/minutes.md`（同时修掉"本域没有列出妙记工具"的过时表述——`minutes.search` 早已策展）、
  `skills/feishu-minutes/SKILL.md`。

### 🔒 契约/守卫/用例

- 新增用例 **73 条**：Minutes 15（窗口切片逐字比对 / 续读游标 / 三条负例零调用下游 / search 实参 / media 只回 URL）、
  MCP 30（白名单同源、版本协商、初始化前 fail-closed、**审计 `allowed` 证明走了执行链**、上下文重建、
  写工具三条 fail-closed 路径、stdio 顺序与 EOF、包边界反射守卫）、
  Drive 协作面 10（权限域 6 工具全部写面 + high-risk、后果句、`fetch_all` 真翻页、扁平文本组装请求体、三条负例）、
  Board 12（`dsl_type→SyntaxType` 映射、幂等键 → `client_token`、三条负例）、Docx 群公告 6（广播面纪律 + 负例）。
- `SilentCatchContractGuards` 扫描面扩入 `Mud.Feishu.AI.Mcp`（协议面静默 catch 的后果更重）。
- 测试基线：`AI.Tools.Tests` 763 → **836**（net8.0 与 net10.0 双 TFM 全绿）、`AI.Tests` 357、`Agent.Demo.Tests` 114 全绿；
  Release 全 TFM 构建 0 错误，三个工程 AOT 严格模式 `IL2026` / `IL3050` / `AOT00x` = **0**。

## [Unreleased] - R7 Batch-7：Bitable 事件上下文 + RAG-B 自建检索 + Skills 导出 + NDJSON 事件外桥（2026-10-10）

> 方案与落地核验见 `.docs/MudFeishu-AI工具面功能完善方案-六域补齐与Agent可用性硬伤及遗留任务-R7.md` §10.8。
> **AI 模块尚未发布，无兼容负担**；工具面**工具数不变（163）**，本批新增的是宿主契约、装配器与生态产物。

### ✨ 新增

- **多维表格记录变更 → 会话链路（R7 / C2 补齐）**：核实结论是**官方 SDK 事件侧本就完整**
  （`BitableRecordChangedResult` + `FeishuEventTypes.BitableRecordChanged` + 源生成 JSON 上下文 + 生成的
  处理器基类），真实缺口在 AI 侧没有生产者。补齐：`BitableRecordContextAssembler`（Order=200，
  **仅变化字段**前后 diff + untrusted 标注 + 逐行预算扣减）与
  `BitableRecordChangedConversationalEventHandler`（**覆写 `SupportedEventType` 精确路由**；单聊投递给操作人；
  缺消息客户端/缺操作人 ⇒ 模型调用前短路；幂等键 `feishu.agent.bitable_record:{EventId}`）。
- **事件事实载体改为有序列表**：`ConversationRequest.EventFacts` 由 `Dictionary` 改为
  `IReadOnlyList<FeishuEventFact>`（新增 `FeishuEventFact` 与 `FeishuEventKeys` 单一源）——
  `Dictionary` 枚举顺序在契约上未定义，会让同一事件两次装配产出不同 prompt。
- **RAG-B 自建检索（R7 / C5）**：`ICorpusSource` / `IVectorStore`（宿主契约）、`DocumentChunker`
  （标题层级 → 空行块边界 → 定长窗口含重叠；回链三件套 + `ScopeKey` 逐块携带）、`TokenEstimator`
  （可解释近似，不引入分词器依赖）、`VectorRetriever`（`IRetriever` 第二实现，空查询不触达向量库）、
  `CorpusIndexer`（拉取→切片→写入，空语料不写库）、DI 入口 `AddFeishuVectorKnowledge` /
  `AddFeishuCorpusIndexing`。与 RAG-A（Aily）**并存**（`TryAddSingleton`，先注册者生效）。
- **`ToolSchemaDialect.Skills`（R7 / C6a）**：把 `Guidance/` 资产 + 工具面导出为
  `skills/feishu-{domain}/SKILL.md` + `references/*.md`（**35 个文件已入库**，仓外 Agent 可直接加载）。
  数据源全为编译期常量（零反射零 IO），guidance 正文**原样嵌入**（唯一真相源不加工）；
  `Export` 返回**清单 JSON**（返回契约是 `string`，而 Skills 是目录树）。
  **随 NuGet 包分发**（`PackagePath="skills\"`）⇒ 只装包的用户也能拿到技能包；打包路径由守卫钉住
  （`PackagePath` 少写末尾反斜杠会让 NuGet 与递归目录叠加成 `skills/x/x/SKILL.md`，写成拍平形式则 21 个域的
  同名 `SKILL.md` 互相覆盖）。
- **NDJSON 事件外桥（R7 / C7）**：`FeishuEventNdjsonBridge`（payload **原始文本原样嵌入**、非法载荷
  fail-fast、写入串行化）+ `RegexEventFileRouter`（每（目录 × 事件键）一个稳定文件、追加写）+ 互斥闸门
  `EnsureNotConversational` / `AllowConcurrentWithConversational` + `FeishuEventCatalog`（事件键清单）。

### 🐞 修复

- **多 TFM 编译破口（本轮新增代码触发，已修）**：`File.AppendAllLinesAsync`、`string.Replace(a,b,StringComparison)`、
  `IReadOnlySet<T>`、`ArgumentNullException.ThrowIfNull`、`KeyValuePair` 解构在 **netstandard2.0** 目标上不可用。
- **AOT 破口**：`JsonArray.Add<T>(T)` 标注了 `RequiresDynamicCode/RequiresUnreferencedCode`，且泛型重载会被
  C# 优先选中 ⇒ 严格模式直接 **IL2026 + IL3050**；必须显式转 `JsonNode` 走非泛型重载。
- **新增守卫钉住的既有异常**：`FeishuEventTypes` 中 `ChatUpdated` 与 `GroupUpdated` 指向**同一事件键**
  （历史别名）——已登记白名单，并锁"事件键 ↔ 常量名一对一"，新增别名必须先合并。

### 🔒 契约/守卫

- 新增用例 **67 条**：Bitable 装配器 12 + Bitable 事件处理器 7（含"变化字段进 prompt / 未变字段不进"端到端）、
  RAG-B 34（切片边界/标题层级/重叠/预算/确定性/token 估算 + 检索裁剪 + 索引编排 + 装配互斥）、
  Skills 导出守卫 8、NDJSON 外桥 17；SDK 侧新增 bitable 载荷反序列化用例 1。
- 测试基线：`AI.Tools.Tests` 730 → **762**，`AI.Tests` 311 → **357**，`Abstractions.Tests` 794 → **795**，
  `Agent.Demo.Tests` 114 全绿；Release 全 TFM 构建 0 错误、AOT 严格模式 `IL2026/IL3050/AOT00x = 0`。

## [Unreleased] - R7 批次：AI 文本工具 + HITL 待确认快照 + 事件上下文装配 + 二进制出入向契约（2026-10-10）

> 方案与落地核验见 `.docs/MudFeishu-AI工具面功能完善方案-六域补齐与Agent可用性硬伤及遗留任务-R7.md` §10.7。
> 工具面 **161 → 163**（88 只读 + 75 写类）；合约面新增 4 个宿主契约；**AI 模块尚未发布，无兼容负担**。

### ✨ 新增

- **`ai.translate_text` / `ai.detect_language`（R7 / C3，+2 只读工具）**：飞书机器翻译与语种识别
  （`IFeishuTenantV1AITranslation`）。语言代码**大小写不敏感**归一化为平台口径；`text` 超 1000 字符
  （平台上限）与术语表超 128 项在**本地**拒绝并回填合法值清单；scope 为官方文档核实的 `translation:text`。
  新域 `ai` 随 L1 guidance（`Guidance/ai.md`）与新入口 `AddFeishuTranslationTools()` 交付。
  OCR / 文档识别 / STT **不策展**（入参为 base64 / 本地文件路径），改由宿主侧通道承担。
- **`IFeishuPendingApprovalStore` + `PendingApprovalSnapshot` + `InMemoryPendingApprovalStore`（R7 / C4a）**：
  HITL 待确认项的持久化 / 查询 / 取消契约。执行链在通知宿主通道**之前**落快照（best-effort，落库失败不阻断通知
  也不放行写工具）；`RunApprovalContinuationAsync(..., pendingApprovalStore: store)` 启用**幂等消费**
  （同一 `RequestId` 只生效一次；不存在 / 已过期 / 已被消费一律 fail-closed 丢弃迟到批准）。
  快照**不含任何凭据**；默认有效期 10 分钟；键为 `(AppKey, RequestId)`（租户隔离）。
- **`ApprovalContextAssembler` + `ContextBudgets`（R7 / C2 · T3-5）**：审批事件载荷 → 结构化 prompt 片段
  （untrusted 标注 + 逐行预算扣减 + 截断标记 + 缺字段降级为空片段）。`ConversationRequest` 追加
  `EventKey` / `EventFacts` 两个**末尾可选参数**（既有调用点源码兼容）；预算常量收敛为单一源
  （`KnowledgeContextAssembler` 的三级闸改为引用 `ContextBudgets`）。
- **`IFeishuBinaryArtifactSink`（出向）/ `IFeishuBinaryArtifactSource`（入向）（R7 / §3.B6）**：
  二进制出入成对宿主契约——出向把 SDK 的 `byte[]` 经宿主落盘后只回传**句柄**（零字节进工具结果），
  入向把 URL/字节转成 SDK 可用的上传载体（供宿主调用 OCR/STT）。两者**均无 SDK 默认实现**
  （软缺席：宿主不实现 ⇒ 相关能力不出现），方向差异已在 `Readme.md` §7.2 显式区分。

### 🐞 修复

- **AOT 破口（既有）**：`BoardTools.CreateWhiteboardNodeAsync` 直接调用
  `JsonSerializer.Deserialize<T>(string, options)`（带 `RequiresUnreferencedCode`/`RequiresDynamicCode`），
  在 net8+ 产生 **IL2026 + IL3050** ⇒ 违反"0 诊断"的 AOT 严格模式门禁。改用 AOT 安全入口
  `FeishuJsonAot.Deserialize<T>` + `FeishuJsonDefaults.DeserializerOptions`（DTO 全字段带
  `[JsonPropertyName]`，与命名策略无关）。
- **`Release` 全 TFM 编译破口（既有）**：`foreach (var (k, v) in <KeyValuePair 集合>)` 在 **netstandard2.0**
  目标不可用（`KeyValuePair.Deconstruct` 扩展缺失 ⇒ CS8129/CS8130），改为显式 `fact.Key` / `fact.Value`。
- **PublicAPI 基线漂移**：`Mud.Feishu.AI` 与 `Mud.Feishu.AI.Tools` 的 `PublicAPI.Unshipped.txt` 与真实公开面
  脱钩（RS0016 ×48 + RS0017 ×6，长期以警告形式刷屏、淹没真实漂移信号）。按工程过滤后补齐 185 条并清理 6 条陈旧项，
  两个工程现为 **RS0016/RS0017 = 0**。

### 🔒 契约/守卫

- 新增行为与守卫用例 47 条：`TranslationToolsTests`(17)、`PendingApprovalStoreTests`(15)、
  `ApprovalContextAssemblerTests`(8)、`BinaryArtifactContractTests`(5)、续跑幂等 2 条（含金丝雀式反向自证）。
- 规模基线同批更新：工具契约接口 161→163、`Source` 声明 155→157、Curation 文件 20→21、
  声明 scope 49→50（新增 `translation:text`，已对照官方文档 ✅）；golden 快照重新固化（仅 +2 行）。

## [Unreleased] - `Mud.Feishu.AI.FeishuTools` 更名为 `Mud.Feishu.AI.Tools`（2026-10-08）

### ⚠️ 破坏性变更登记

- **【工程/程序集/命名空间更名】** `Mud.Feishu.AI.FeishuTools` → **`Mud.Feishu.AI.Tools`**
  （测试工程同步为 `Tests/Mud.Feishu.AI.Tools.Tests`）。目录名、`.csproj`、`AssemblyName`、
  NuGet 包 id、命名空间（根 + `Tools`/`Internal`/`Events`/`Curation`/`Channels`/`Registration`/
  `SdkProfile`/`Knowledge` 全部子命名空间）、`InternalsVisibleTo`、剖面槽位
  （`OwnerAssembly` / `ContractNamespace` / `RegistrationNamespace`）与 `PublicAPI.Unshipped.txt`
  同步更名。AI 模块尚未发布，无兼容负担。

- **【执行器绑定特性迁至独立子命名空间】** `FeishuToolHandlerAttribute` 由根命名空间迁到
  **`Mud.Feishu.AI.Tools.Handlers`**。原因：更名后本工程根命名空间与 `Mud.Feishu.AI` 中
  `[FeishuTool]` 所在的 `Mud.Feishu.AI.Tools` 同名，若两者合并，剖面的
  `ToolAttributeNamespace` / `ToolHandlerAttributeNamespace` 两槽将塌陷为同值，引擎失去
  「工具声明面 vs 执行器绑定面」的可寻址性。迁移后两槽保持可区分，执行链 31 枚工具绑定
  全部解析成功（无 MUDFT022/023/024/025）。

- **【治理守卫判据改写】** 「退役本地生成器不得复活」原按**工程名**判据（目录
  `Mud.Feishu.AI.Tools` / `Tests/Mud.Feishu.AI.Tools.Tests` 不存在、`slnx`/csproj 不含该串），
  该判据与本次更名直接冲突（更名后必然假红）。改为按**能力**判据，与命名解耦：
  ① 全仓无任何 `[Generator]` 标注的增量生成器实现；② 无旧引擎实现类型名残留；
  ③ `ProjectReference` 图闭合（目标须登记于 `slnx`）；④ 全仓无直接引用
  `Microsoft.CodeAnalysis.CSharp` 的 Roslyn 组件宿主工程。
  `scripts/verify-pack.ps1` 的 ① 同步改为 ④ 的判据。

> **命名撞车提示**：R-1+2c 迁移时已删除的本地生成器工程**原名即为 `Mud.Feishu.AI.Tools`**
> （见下方 Unreleased 记录）。本轮更名后该名称被本工具面工程复用，故上方历史条目中的
> `Mud.Feishu.AI.Tools` 一律指**已删除的生成器工程**，与当前工程无关——阅读历史条目时需注意。

## [Unreleased] - AI 工具面生成引擎上游化 + `Source` 符号化（R-1+2c，2026-10-08）

> 方案、四维评审（产品/架构/程序员/QA）、实施记录与迁移台账见
> `.docs/AI/AI-SDK耦合解耦与工具上游化-Bug修复与功能完善方案.md`（§六/§七）。
> **AI 模块尚未发布，本轮为一次性破坏性重构，无兼容负担。**

### ⚠️ 破坏性变更登记

- **【工具面生成引擎上移组件侧】** 本地生成器工程 `Mud.Feishu.AI.Tools` 与 `Tests/Mud.Feishu.AI.Tools.Tests`
  **整体删除**；工具 Schema 改由 `Mud.HttpUtils.Generator` 3.0.3 的通用 `ToolSurface` 引擎按
  **剖面**（`SdkProfile/FeishuToolProfile.cs`，实现 `ISdkToolProfile` + 标注 `[SdkToolProfile]`）
  驱动生成。新增依赖：`Mud.HttpUtils.Generator`（analyzer）/ `Attributes` / `Abstractions` 3.0.3。
  两套引擎不得并存（同名 hintName 产物冲突），由 `FeishuToolProfileContractGuards` 机械守护。
- **【策划面命名约定收紧】** 83 个租户令牌工具接口由 `IFeishu<Domain><Verb>Tool` 统一改为
  **`IFeishuTenant<Domain><Verb>Tool`**（与既有 `IFeishuUser*Tool` 对称）：引擎的槽位 016 会比对
  「工具身份 ↔ 承载接口名推导出的令牌类型」，缺标记即 `MUDFT016`（Error）。公开接口因此改名
  （未发布窗口期，`PublicAPI.Unshipped.txt` 已同步）。
- **【能力目录产物改名】** 生成产物由 `FeishuCapabilityCatalog` 改为
  **`FeishuToolCapabilityCatalog`**（引擎按 `{ProductPrefix}CapabilityCatalog` 派生）。
- **【一个工具的输出契约收敛（有意）】** `im.get_message_content` 的 `output_schema` 不再暴露
  `FeishuApiListResult` 信封（此前 `data.items`，现直接 `items`），与 PageList 族的信封剥离口径一致；
  其余 **86/87** 工具的 Schema 逐字节不变。

### 🔧 变更

- **`Source` 符号化（R-1）**：84 处 `[FeishuTool(Source = …)]` 由字符串字面量改为
  `nameof(接口) + "." + nameof(接口.方法)` 常量拼接——编译期求值结果与旧字面量逐字节相同，
  SDK 接口/方法改名可随 IDE 重命名自动联动（不再依赖人工定位魔法串）。
- **诊断门禁扩展**：`scripts/diagnostics-gate.ps1` 新增 `SDKT001/SDKT002`（剖面接口↔特性成对性、
  剖面必填槽缺失）零容忍集，本地 `verify-build.ps1` 与 CI workflow 同源断言——
  `SDKT002` 会让引擎**静默不产任何工具面**，是比任何 MUDFT 都隐蔽的失败通道。
- **golden 一键再固化入口**：`dotnet test … -p:FeishuToolRefreeze=true`（配合
  `FeishuToolGoldenUpdate=true`）绕过"门禁即构建错误"的自锁，兑现 v1 方案 §2.1-C3 的承诺。
- **测试守卫重锚**：新增 `FeishuToolProfileContractGuards`（剖面 31 槽冻结 + 上游 24 槽镜像
  ↔ 门禁三集合 ↔ CI + 迁移台账 + `Source` 形态）；`GeneratorDiagnosticsContractGuards` 按
  "门禁链路"重锚；`GeneratorProductGateCoverageTests` 改为"剖面派生出口模板 + 产物存在性"双锚点；
  `BinaryDownloadToolExposureContractTests` 支持 nameof 形态并加双形态自证。

### 🐛 修复（既有门禁失败，随本轮一并清零）

- **AOT 严格模式失败 ×20**（10×`IL2026` + 10×`IL3050`）：`Internal/{BitableTools,MailTools,MinutesReadTools}.cs`
  的 5 处 `JsonArray.Add(new JsonObject{…})` 被重载解析绑到带
  `RequiresUnreferencedCode`/`RequiresDynamicCode` 的泛型 `Add<T>(T)`（非 `JsonNode` 的 T 走反射
  `JsonValue.Create`）⇒ 改用仓库**既有的** AOT 安全扩展 `ToolResultText.AddNode`（走
  `IList<JsonNode?>` 显式实现）。**真修根因，未加任何抑制**。
  ⇒ `verify-build.ps1` 七步全绿（编译 0 错误、`AotStrictMode` `AOT00x`/`IL2026`/`IL3050` 全 0、
  单元测试 9468 通过 / 0 失败）。
- **Demo 编译失败 ×4**（`Demos/FeishuFileServer`，`DeleteFileByFileTokenAsync` 第 3 参错位）：
  由并行提交以**具名参数** `cancellationToken:` 修复（保持「同步删除」语义），详见下方
  「Demo 编译错误修复」小节——合并时采纳该提交的代码形态，与本节 AOT 修复互补。

## [Unreleased] - Demo 编译错误修复（2026-10-08）

> 与本次 OpenTelemetry 迁移无关的**既有**缺陷，修复后全解决方案 Release 构建恢复 0 错误。

- **`Demos/FeishuFileServer/backend/Services/Feishu/FeishuDriveService.cs`**：`DeleteFileAsync` / `DeleteFolderAsync`
  调用 `IFeishuV1DriveFiles.DeleteFileByFileTokenAsync` 时把 `cancellationToken` 作为第 3 个位置参数传入，
  而该接口自 2026-10-01 起第 3 参为可选的 `[Query("async")] bool? async = null` ⇒ `CS1503`
  （`CancellationToken` 无法转换为 `bool?`），全解决方案构建恒 2 个错误。
  改为**具名参数** `cancellationToken: cancellationToken`（不传 `async`，保持「同步删除」语义与修复前一致）。

## [Unreleased] - OpenTelemetry 共享装配内核迁移（2026-10-08）

> 方案见 `.docs/MudFeishu-OpenTelemetry-SharedKernel-Migration-Plan-3.1.0.md`
> （上游内核契约见 `D:/Repos/MudHttpUtils/.docs/OpenTelemetry共享装配内核抽取方案_3.1.0.md`）。
> **依赖前置**：`Mud.HttpUtils 3.0.3`（含新增的 `Mud.HttpUtils.OpenTelemetry` 共享内核）已在上游落地，
> 本仓全量升级至同一版本；未发布前 CI 还原需可访问该版本的包源。

### ⚠️ 行为变更登记（宿主可感）

- **【P1】非法配置改为启动期抛错（此前静默通过）**：`AddFeishuOpenTelemetry` 现在在**注册期显式执行**
  `FeishuOpenTelemetryOptions.Validate`，失败即抛 `OptionsValidationException`。此前注册进 DI 的
  `IValidateOptions<FeishuOpenTelemetryOptions>` 从不被触发（扩展方法把预构建实例经 `OptionsWrapper`
  直接注册为 `IOptions<>`，绕过 .NET options 校验管道），导致 `ServiceName`/`ServiceVersion`/
  `DeploymentEnvironment` 为空、`OtlpEndpoint` 为相对 URI 等配置**静默生效**。
  `SamplingRatio` 越界仍抛 `ArgumentOutOfRangeException`（`paramName = "SamplingRatio"`，语义与优先级不变）。
- **【P2】`netstandard2.0` 下不再注册 ASP.NET Core Instrumentation**：该兜底上收至共享内核
  （`#if NETSTANDARD2_0` 强制 `EnableAspNetCoreInstrumentation = false`），`netstandard2.0` 消费方的行为与
  其他 TFM 一致化（此前 ns2.0 资产仍会调用 `AddAspNetCoreInstrumentation()`，该 TFM 下无 ASP.NET Core 语义）。
- **【P2】导出器注册去重**：源/Meter 集合在内核内去重后注册，避免 `AddSource`/`AddMeter` 重复注册导致
  同一 Activity/Metric 被重复采集（Span 翻倍）。
- **单一入口约束（fail-fast）**：同一宿主**不得**同时调用 `AddFeishuOpenTelemetry` 与
  `AddMudHttpOpenTelemetry`。二者都是「整条 OTel 管道」的装配入口，叠加会导致 Resource `service.name`
  被覆盖、同一 Span 被两个 OTLP 导出器重复上报。内核现按产品名拒绝异产品重复注册（抛
  `InvalidOperationException`）；同一产品重复注册幂等短路。仅使用飞书 SDK 的宿主调用本方法即可，
  Mud.HttpUtils 的源与指标已由 `IncludeMudHttpUtils`（默认 `true`）覆盖。

### ✨ 新增

- **OTLP 导出增强面（5 个配置项）**：`OtlpExportProtocol`（`Grpc` / `HttpProtobuf`，内网仅开 4318 时必需）、
  `OtlpHeaders`（托管型 collector 的 `Authorization` 等）、`UseShortExporterTimeout`（5s 短超时）、
  `ExportBatchSize`、`ExportIntervalMilliseconds`（后两者映射
  `BatchExportProcessorOptions<Activity>`，仅 `>0` 生效；负数在启动期抛 `OptionsValidationException`）。
- **`FeishuOpenTelemetryOptionsMapper`**（internal）：产品选项 → 上游共享内核选项的逐属性显式映射
  （选映射而非 options 继承，规避配置绑定源生成器对继承属性的未验证风险）。

### ♻️ 重构

- **`FeishuOpenTelemetryExtensions` 退化为薄壳**（约 200 行 → 约 160 行含文档）：装配（`AddOpenTelemetry` /
  Resource / Sampler / 源与 Meter 注册 / Instrumentation 开关 / OTLP 导出器 / 批量导出 / ns2.0 兜底 /
  重复入口守卫）全部委托上游共享内核，本包只保留「注册期校验 + 产品贡献描述 + 选项映射」。
  两个公开重载的**签名、默认值、`sectionPath` 默认值、异常语义与 DI 面**（`IValidateOptions<>` /
  `IOptions<>` 注册）逐项保持不变。
- **依赖面收窄**：`Mud.Feishu.OpenTelemetry` 的直接 `Mud.HttpUtils` 引用替换为
  `Mud.HttpUtils.OpenTelemetry 3.0.3`（`MudHttpActivitySource`/`MudHttpMeter` 由内核内部使用）。
- 全仓 `Mud.HttpUtils*`（`Mud.HttpUtils` ×3、`Mud.HttpUtils.Generator` ×3）由 `3.0.0` 统一升至 `3.0.3`
  （守卫 `TokenMultiAppContractGuards.MudHttpUtils_PackageReference_ShouldBeSingleVersion` 锁定单一版本）。

### 🧪 测试

- 新增 `FeishuOpenTelemetrySharedKernelTests`（11 例）：产品贡献契约（单根源 `Mud.Feishu` + **精确 Meter**
  非通配 + `IncludeMudHttpUtils` → `IncludeMudHttpSources` 镜像）、映射逐属性完整性、注册期真校验
  （空 `ServiceName` / 相对 `OtlpEndpoint` / 负数 `ExportIntervalMilliseconds`）、**单一入口禁令抛错**、
  同产品幂等、既有 DI 面保留。
- 既有 `Mud.Feishu.OpenTelemetry.Tests` 35 例（net8.0 + net10.0）**零修改**全绿，作为「迁移行为等价」回归证据。

---

## [Unreleased] - Mud.Feishu.AI 审查缺陷修复与能力完善 R5（2026-10-02）

> 方案、双视角复核与落地记录见 `.docs/AI/Mud.Feishu.AI-审查缺陷修复与能力完善方案-R5.md`
> （§0 复核订正与定级纠正、§1 缺陷修复、§2 能力完善、§3 批次 B0/B1/B2、§4 红转绿证据）。
> **AI 模块尚未发布，本轮为一次性重构，无兼容负担。**

### ⚠️ 行为变更登记（宿主可感）

- **【P0 / R5-12（原 P2-8「探针」定级纠正）】未应答的人工确认不再毒化会话**：发起确认的那一轮会把
  待应答的 `ToolApprovalRequestContent` 写进会话历史，而 MEAI `FunctionInvokingChatClient` 对**整段
  入站历史**做审批配对校验——残留未应答审批即抛
  `InvalidOperationException: ToolApprovalRequestContent found with FunctionCall.CallId(s) '…' that have no
  matching ToolApprovalResponseContent.`，且异常早于任何新消息落库 ⇒ 该会话**此后每一轮**都抛
  （幂等回滚 + 重投递 = 事件永久毒化循环）。**新语义**：事件层在每个新用户轮次开始时自愈——
  摘除历史中的孤儿审批请求内容 + 清空框架的待审批记录，并记 Warning。用户可见后果：该次写操作被
  **放弃**（需重新发起），会话保持可用；**迟到的批准**由框架绑定层丢弃（fail-closed，绝不重新放行）。
- **【P1 / R5-1】流式 `BeginAsync` 失败回退路径不再丢失审批**：回退非流式路径此前直接返回模型文本，
  未做审批判定 ⇒ 写工具审批被静默丢弃（用户收不到确认提示、宿主收不到回调、写工具永久挂起）。
  现与主非流式路径共用 `RunNonStreamingTurnAsync`（「审批判定先于空回复守卫」的唯一点）。
- **【P1 / R5-11】新增批准回灌续跑 SDK 闭环**：`FeishuAgent.RunApprovalContinuationAsync(...)`——
  加载会话 → 取**框架记录的**原始审批请求 → `ToolApprovalRequestContent.CreateResponse` →
  **显式重建租户上下文** → 经框架绑定层续跑 → 落库。此前宿主「手工四步」易漏第 ③ 步（重建上下文），
  表现为「批准了却不生效」；未命中框架记录即 fail-closed 抛错（绝不凭空放行）。
  `FrameworkToolApprovalRequest` 追加尾部可选字段 `ChatId`（源兼容；续跑轮重建工具上下文需要）。

### 🐛 修复

- **R5-1**：见上「行为变更登记」。
- **R5-4**：`TryTakePendingReply`（事件层）与 `ConversationSummarizer.SummarizeIfNeededAsync`（摘要器）
  的 catch 面由**枚举白名单**收口为 `when (ex is not OperationCanceledException)`，与 R4-7 同款口径
  （漏网的 `FormatException`/`KeyNotFoundException` 等会逃出坏值守护区并毒化会话）。
- **R5-5**：删除 `ConversationSummarizer` 中的死变量 `retainedCount`。
- **R5-6**：`IFeishuAttachmentStager.StageAsync` 的 `CancellationToken` 补缺省值（对齐异步规范）。
- **R5-7**：模型端点环回白名单支持 IPv6 字面量——`Uri.Host` 对 `http://[::1]:8000` 返回的是
  **带方括号、零压缩展开**的形态（`[0000:…:0001]`），原 `"::1"` 比较与 `IPAddress.TryParse` 均恒失败，
  「环回地址例外」在 IPv6 书写下静默失效（`http://[::1]:8000/v1` 启动即抛）。
- **R5-3**：`ResolveStreamTarget` 覆写方法的 XML 与实现矛盾订正（"返回 null 表示不适用流式" ⇒ 实为
  「回退默认解析值，作为 `BeginAsync` 的入参兜底目标」；「本通道不适用」由链内第二段重解析承接）。
- **R5-8**：知识注入块头部补 **untrusted 显式标注**（知识库内容属半可信数据，其中的指令性表述不得执行），
  与工具结果侧 `ToolResultContentSafety` 防线对称。
- **R5-2 / R5-9**：契约文档补全——`IFeishuToolApprovalChannel.RequestFrameworkApprovalAsync` 的
  **宿主续跑义务**（四步，含「批准 ≠ 放行」的上下文重建）与 `ConversationalFeishuEventHandler.BuildRequestAsync`
  的**租户作用域义务**（本方法在基类建立任何租户作用域之前执行）。

### 🧪 测试与守卫

- 新增 `ApprovalContinuationContractGuards`：① **枚举白名单 catch 结构守卫**（含检查器自证，取代
  「临时改坏生产代码跑一次」的一次性自证）；② MAF 内部待审批状态键 `_pendingApprovalRequests` 的
  **反射契约守卫**（框架改名即测试期报红，避免 HITL 续跑静默失效）。
- 新增 `FeishuAgentApprovalContinuationTests`（7 例）：走真实框架管线验证「批准 → 工具真执行且
  租户上下文正确」「缺上下文 fail-fast」「记录未命中 fail-closed」「拒绝路径」「闸门串行」
  「未应答审批自愈后会话仍可用」。
- 红转绿证据（详见方案 §4.3）：R5-1（回退路径审批通知缺失）、R5-12（框架抛
  `InvalidOperationException` 毒化会话）均已先在**修复前代码**上跑出红后再转绿。

## [Unreleased] - Mud.Feishu.AI 审查缺陷修复 R4（2026-10-01）

> 方案与双视角复核（高级程序员 / 系统架构师）见 `.docs/AI/Mud.Feishu.AI-审查缺陷修复与能力完善方案-R4.md`
> （§0.6 复核订正、§0.7 落地记录、§4 批次 B0/B1/B2、§5.3 红转绿登记）。
> 覆盖 1 个 P0（R4-1）、5 个 P1、8 个 P2；R4-14 与维度 B（能力补齐）维持 Phase/分期，不进本轮。
> **AI 模块尚未发布，本轮为一次性重构，无兼容负担。**

### ⚠️ 行为变更登记（宿主可感）

- **【P0 / R4-1】写工具 HITL 改为 fail-closed（不再静默放行）**：删除 `AuthorizeGateAsync` 中
  「`AuthorizationDecision.NeedsUserConfirmation && tool.IsWrite → Pass()`」特例。该特例的唯一依据
  （"MAF 未批准则本方法不会被调用"）在运行期**无任何校验**——`ApprovalRequiredAIFunction` 是 MEAI
  **纯标记类型**，拦截只发生在 `FunctionInvokingChatClient` 内；宿主直接 `InvokeAsync`、或同一进程内两种
  调用混用时，写工具会被**静默放行**。
  **新语义**：**授权器是批准状态的唯一所有者**（SDK 不签发/不校验任何凭据，WP3 不变）；
  读、写工具一律走 `ResolveNeedsConfirmationAsync` 挂起解析并**恒不放行**，宿主批准后须以同一键
  返回 `Allowed` 才会执行。**迁移**：原以"MAF 批准即放行"为前提的宿主，需在批准回调中更新
  `IToolExecutionAuthorizer` 状态；未注册批准通道时降级为纯提示（模型拿不到任何凭据）。
  **源破坏（项目未发布）**。
- **【R4-6】单例 `FeishuAgent` 不再接收根 `IServiceProvider`**：`ChatClientAgent` 的 `services`
  改为 `null`（原先在**单例工厂**内传入根容器 `sp`）。根容器被单例长期持有会把 Scoped 依赖钉成
  事实单例（Captive Dependency，TMA-13 同类问题）。需要 Scoped 服务的宿主应自行
  `new FeishuAgent(..., services: <由 scope 派生>)`。
- **【R4-13】结构化错误补 `code` 机器可读字段**：`[tool_error] <tool> (kind) code=<飞书业务码>: <reason>`
  ——此前 `ToolErrorClassifier` 已算出 code 却在文本中丢失，模型只能读中文后缀。**无 code 时不产出空槽**
  （HTTP 5xx/429 本就没有业务码）。人读文案形态保持不变（纯追加）。

### 🐛 修复

- **R4-3**：Aily 知识召回 `has_answer` 由**覆盖式赋值**改为**单调累积**。SSE 的 processing 中间事件与
  终结事件常**省略** `has_answer`，覆盖式会被最后一次无字段事件重置为 `false` ⇒ 有答案被判为无答案，
  消费点据此**静默丢弃整次召回**（无异常、无日志）。
- **R4-7**：会话损坏自愈的 catch 面由**枚举白名单**（`JsonException or ArgumentException or
InvalidOperationException or NotSupportedException`）收口为 `when (ex is not OperationCanceledException)`。
  白名单既与同处注释"覆盖全部异常类型"自相矛盾，也拦不住 MAF 内部字典/格式转换抛出的
  `FormatException`/`KeyNotFoundException`（残余类型会毒化会话）。
- **R4-8**：流式 Agent 路径补记 token 用量（在**迭代器循环体内**累积 `UsageDetails`，循环结束后
  `RecordUsage`）——此前仅非流式路径有遥测，流式调用在可观测性上不可见。
- **R4-9**：`FeishuToolCapabilityCatalog` 的 opt-in 表述与强依赖对齐——该产物对 `Mud.Feishu.AI.FeishuTools`
  是**必需**（`CapabilityLookupTools` 编译期无条件引用，置 `false` 即 `CS0103`）；
  `build_property.FeishuToolCatalog` 只对**其他引用本生成器的工程**才是可关闭的增量开关。
- **R4-10**：生成器 `AddSource` 唯一性护栏加固。`RegistrarTypeName` / Core 方法名 / hintName 三者
  均由执行器**简单名**派生并发射进**同一**命名空间 ⇒ 跨命名空间的同名执行器会产出同名类型（`CS0101`）
  与同名方法（`CS0111`），或让 `AddSource` 抛异常后被 `Guard<T>` 吞成 `MUDFT026`（"生成器内部异常"的
  **表面症状**）。修复：**仅对发生碰撞的执行器**追加命名空间段消歧后缀（无碰撞者名字零变更，手写
  `AddFeishu{X}Core` 调用点不受影响），并同步修正方法体内的类型引用（否则 `CS0246`）。
- **R4-11**：写工具文件收敛（**纯位移 + 重命名，不改任何方法体**）——新建 `Internal/BitableWriteTools.cs`
  与 `Tools/FeishuBitableWriteToolInterfaces.cs` 收纳 bitable 写域；`WriteTools.cs` →
  `MessageAndApprovalWriteTools.cs`、`WriteTools2.cs` → `DocxSheetsDriveWriteTools.cs`、
  `FeishuWriteToolInterfaces2.cs` → `FeishuDocxSheetsDriveToolInterfaces.cs`。仓库内不再有 `*2.cs`。
- **R4-12**：`Pass()` 的"注释写 Warning / 代码 `LogInformation`"矛盾随 R4-1 删除该分支一并消失。

### 🧪 测试与守卫

- **新增诊断 `MUDFT027`（Error，零容忍）**：工具名"字面不同但归一后同名"（如 `fake.a_b` 与
  `fake.a.b` 均归一为 `FakeAB`）会导致产物常量重复（`CS0101`）或 hintName 重复
  （→ `MUDFT026`）。**不复用 `MUDFT003`**——后者是"字面工具名重复"，二者真因不同。
  四处同批登记：`Diagnostics.ZeroToleranceIds` → `AnalyzerReleases.Unshipped.md` →
  `scripts/verify-build.ps1`（零容忍正则）→ `GeneratorNegativeCaseTests`（可触发负例）。
- **守卫反转/改写**（改源码不改守卫 = 门禁红）：
  `WriteTool_NeedsUserConfirmation_ShouldBeBypassed_AtAuthorizeGate` **反转为** `..._ShouldBeDenied_...`；
  `SessionRestore_ShouldEagerlyValidateHistoryState` 判据改为"catch 面非枚举白名单"；
  `FeishuToolBindingTests` 原放行用例拆为 `..._ShouldDeny_WhenAuthorizerNotApproved` +
  `..._ShouldPass_WhenAuthorizerApproved`（后者锁"宿主批准后不得死胡同"）。
- 落地口径：**能并入既有测试类即不新建类**（原方案 8 个新类实际只新建 2 个），避免重复 fixture 装配。
- 实测（net8.0 / net10.0 双 TFM，`--filter "Category!=Stress"`）：`Mud.Feishu.AI.Tests` **264/264**、
  `Mud.Feishu.AI.FeishuTools.Tests` **372/372**、`Mud.Feishu.AI.Tools.Tests` **32/32**。
- **R4-8 用例并行隔离修正**：`RunStreamingAsync_ShouldRecordTokenUsage` 原仅按 operation
  （`feishu.agent.run_streaming`）从**进程级** `ActivitySource` 取 `Single`——xUnit 并行下邻居用例的
  同名 Span 会一并入队，**net10.0 全量运行**抛 `Sequence contains more than one matching element`
  （net8.0 因时序侥幸通过）。改用本用例独有的 Agent 名过滤 `feishu.agent.name` 标签
  （对齐既有 `RunStreamingAsync_ShouldRecordLlmDuration_Once` 的既有做法）。
- **红转绿留档**：R4-3 临时还原覆盖式赋值 → `HasAnswer` 为 `False`（红），恢复累积后绿；
  R4-1/R4-7 守卫反转/改写均**先跑红再改源码**；R4-2 实测**全绿**（证否"增量陈旧"，
  故未转 `CompilationProvider` 方案，用例转为回归锁）。逐条登记见方案 §5.3。
- `FeishuToolSchemas.golden.txt` **零漂移**（R4-11 为纯位移，生成器按 `ToolName` 序数排序）。

### 📄 文档

- **R4-4**：工具包 `Readme.md` 的 HITL 段重写（原文仍称"签发 HMAC 令牌 / `confirm_token` /
  `[Obsolete]`"，与已删除令牌的源码相反），并**同批订正基座 `Mud.Feishu.AI/Readme.md`**
  （原文写 `NeedsUserConfirmation && IsWrite → Pass()` "**必须保留**"，与 R4-1 正面冲突）；
  新增文档守卫 `Readmes_ShouldNotDescribeConfirmToken` 扫描**两处** Readme。
- **R4-5**：工具包 `Readme.md` 幂等表 `docx.create_document` 行订正为
  "**平台端点与 SDK 均不支持** `client_token`"（底层 `CreateDocumentRequest` 无该字段，
  原文"底层支持 ✅"错误）。

---

## [Unreleased] - Mud.Feishu.AI 审查缺陷修复 R3（2026-10-01）

> 方案与四视角复核裁定见 `.docs/AI/Mud.Feishu.AI-审查缺陷修复与能力完善方案-R3.md`
> （§0.5 复核裁定、§4 落地批次 B0/B1/B2、§6 交付前置）。覆盖 3 个 P0（R3-1/R3-2/R3-5）、
> 7 个 P1、12 个 P2（其中 R3-14 经复核判为不可达死代码，不落地）；新增配置键 1 个（`BotName`）。

### ⚠️ 行为变更登记（宿主可感）

- **【P0 / R3-1 / R3-2】回复动作必须先建立 SDK 租户作用域**：IM 消息、审批任务、任务更新三个
  会话式事件器在**下发之前**调用 `ConversationalFeishuEventHandler.BeginAppScope(appKey)`。
  生成的客户端在无环境上下文时会退化为 `Current ?? GetDefaultApp()` ⇒ 多应用宿主下以**默认应用身份**
  把回复发给别的租户（TMA2-20 跨租户错发）。
  **fail-closed**：事件带 `appKey` 却未注入 `IFeishuAppContextScopeFactory` 时**显式抛**
  `InvalidOperationException`，**不**回退默认应用。**迁移**：多应用宿主须注册工厂
  （`AddFeishuTools()` 已包含）；`appKey` 为空的单应用宿主行为**零差异**。
- **【R3-1 步骤 0】`IFeishuAppContextScopeFactory` 契约上移**：接口由 `Mud.Feishu.AI.FeishuTools`
  上移到 `Mud.Feishu.AI`（命名空间 `Mud.Feishu.AI.Tools`），实现在 FeishuTools；`Mud.Feishu.AI`
  不再反向依赖 FeishuTools。**源破坏（项目未发布，无兼容负担）**。
- **【R3-5】作用域工厂解耦重写**：`FeishuAppContextScopeFactory` 去除 IM 客户端硬依赖
  （原先按域装配的宿主无法解析）；`BeginScope(string appKey)` 改为四步门禁——
  格式校验 → 授权器非空 → `CanSwitchTo` → `BeginScope(context)`，任一步拒绝都**零下游副作用**。
- **【R3-3】群聊「@Bot 本人」判定**：新增 `ImConversationOptions.BotName`（`string?`，默认 `null`）。
  此前仅判「`mentions` 非空」⇒ 任意 `@`（@别人、@全体）都触发回复；配置后必须存在
  `mentions[].name` 与之（忽略大小写）相等才进入会话。**`null` 保持旧行为**（升级零破坏）。
  详见 `documents/Configuration/CHANGELOG-Config.md`（R3-3）。
- **【R3-4】拒绝/阻断文案统一出站净化**：出口收敛为**单一闸门** `EgressResult`（3 个重载），
  授权拒绝、策略拒绝、参数非法、待确认等**非成功路径**的文案与成功路径同样经
  `Sanitize` + `Truncate`（截断长度复用 `MaxToolResultLength`）。此前拒绝文案未经净化（与成功路径双标）。
- **【R3-9】数值配置项补齐上界**：`FeishuAgentOptions` 五项由「仅下界」改为「下界 + 上界」
  （`MaxHistoryMessages` 1..10000、`MaxToolResultLength` 1..200000、`MaxStreamChunkLength` 1..100000、
  `SummaryThreshold` 0 或 4..10000、`MaxHistoryTokens` 0..1000000）。越界在**装配期**抛
  `InvalidOperationException`（此前运行期才表现为内存/序列化开销失控）。详见 `CHANGELOG-Config.md`（R3-9）。
- **【R3-8】知识上下文注入预算**：`KnowledgeContextAssembler` 注入上限 = **8 条**、总长 **3000 字**
  （单条预览 500 字，`TruncateChunk` 截断加 `…`），编号连续且带来源映射（无来源时退化）。
- **【R3-12】邮件头字段拒绝换行**：`mail.send_message` 的 `To`/`Cc`/`Bcc`/`Subject` 含 CR/LF
  一律**拒绝**（EML 头注入防护）；正文多行仍放行（`ToolArgumentSanitizer.ValidateHeaderValue`）。
- **【R3-15】流式通道未登记 `messageId` 时抛错**：`StreamingChannelChain` 的 `WriteStream`/`Flush`
  在 `messageId` 未注册时抛 `InvalidOperationException`（fail-fast），不再静默错投。

### 🐛 修复

- **R3-10**：`IFeishuToolContextAccessor` 以 `_owner` 引用做**所有权 + 幂等**判定
  （`ReferenceEquals(_owner._current.Value, applied)` 才回滚），外层先释放不再污染内层作用域。
- **R3-6**：Aily 召回按 `JsonValueKind` 分派，非字符串 chunk（数字/对象/null）不再抛异常
  （脏值降级为 JSON 原文，其余合法 chunk 全部保留）；原文 ②（`break` 提前终止）经复核判为误报，已删除。
- **R3-7**：流式 Agent 路径补记 LLM 时长指标（`FeishuAgentDiagnostics.RecordLlmDuration`，
  非流式与流式 `try/finally` 各一处）。
- **R3-14 / R3-16 / R3-19 / R3-11 / R3-13 / R3-17 / R3-18 / R3-21 / R3-22**：按方案 §4.3 实施期裁定处置
  （R3-14 判为不可达死代码不落地；R3-16 仅去 `!`；R3-19 保留 `throw`；其余为注释/常量/重命名/守卫收口）。
- **R3-20**：`internal` 类型重命名 `BitableWriteTools2` → `BitableWriteRecordOps`（不改方法体）。

### 🧪 测试与守卫

- 新增 T1–T12：`ReplyScopeContractGuards`（T11/T12）、`FeishuAppContextScopeFactoryTests`（T3b）、
  `KnowledgeContextAssemblerTests`（T6）、`ToolContextAccessorTests`（T8）、`MailToolsTests`（T9）、
  `ToolEgressPurificationContractGuards`（T10）等。
- 双 TFM 实测：`Mud.Feishu.AI.Tests` **261/261**、`Mud.Feishu.AI.FeishuTools.Tests` **368/368**
  （net8.0 / net10.0），`--filter "Category!=Stress"`。
- 守卫先行：`ReplyScopeContractGuards`、`ToolEgressPurificationContractGuards` 先于修复落地。

---

## [Unreleased] - Phase 4 P4-3：自研确认令牌退场（2026-09-29）

### ⚠️ 行为变更登记

- **【修复 P4-1 连带缺陷】写类工具不再被执行链二次拦截**：写工具经框架审批放行后才可能被调用，
  而执行链原先会对 `NeedsUserConfirmation` **再次**要求自研令牌——宿主无法把 `confirm_token`
  注入模型的工具参数，导致写工具卡死在「框架已批准、执行链仍拒绝」的**死胡同**。
  现在写类工具的待确认判定直接放行（记 Information 日志），`NeedsUserConfirmation` 对写工具
  退化为「由框架承载」。**非写类工具**行为不变（仍走令牌闭环）。
- **`IToolConfirmationTokenSecretProvider` 标记 `[Obsolete]`**：写类工具的人工确认已由 MAF
  审批管线承担；本契约仅剩「非写类工具的动态选择性确认」一种用途，**计划 next-major 移除**，
  新代码请改用 `IFeishuToolApprovalChannel`。现仍可用（不是 `error: true`），故未迁移宿主照常编译运行。
- **内部 `ToolConfirmationToken` 标记 `[Obsolete]`**（internal，不影响宿主）；同时修正其类注释中
  仍写着「模型转述令牌给用户」的过期描述（该行为已在 R2-1 作为 P0 修复）。

> **迁移注记（宿主需自查）**：写类工具的放行以「工具由 `FeishuToolsToolSource` 产出（即经
> `ApplyApprovalGate` 包装）」为前提。若宿主绕过工具源、直接构造 `FeishuToolAIFunction`，
> 则该写工具**不会**经过框架审批，此时执行链的放行等于绕过了人工确认——此类宿主须自行在授权器
> 中做拒绝判定，或改用受支持的注册路径。

### 🧪 测试与守卫

- `FeishuToolBindingTests` +2：写类工具放行且不回到令牌分支 / 非写类工具仍走令牌闭环（反向锁定）。
- `ToolConfirmationTokenFlowTests` 的工具定义由写类改为**非写类**——P4-3 后写类工具不再进入令牌路径，
  沿用写类定义会让这 8 个用例退化为"测试不可达代码"。
- 新增守卫 `DeprecatedTokenPath_ShouldStayObsolete_AndNotReclaimWriteTools`：
  锁定废弃标注（含"指向替代契约"）与写类工具放行分支，防止任一条被静默回退。

---

## [Unreleased] - Phase 4 P4-4：交付态补发（outbox，2026-09-29）

### 🌟 新增／行为变更

- **重投递不再重跑模型**：非流式回复在**下发之前**随会话落盘（两个 `string` 状态键
  `feishu.agent.pending_reply` / `feishu.agent.pending_reply_turn`）。下发改失败 ⇒ 幂等回滚 ⇒
  重投递时**补发同一份文本**，而不是再调一次模型。
  实测：整个「下发失败 + 重投递」过程模型只被调用 1 次。
- **陈旧条目丢弃**：轮次标识不匹配（用户已发新消息）时丢弃待补发条目并正常走模型——
  补发上一轮的回复属于答非所问。
- **根治 R2-8 的取舍代价**：原先「先落库再回复」在下发失败时会导致重投递重跑模型
  （多一次计费 + 历史内部多一轮）；现在只剩补发，次序维持不变。
- **代价（如实登记）**：非流式每轮 **+1 次会话落盘**（Redis 后端 = 每轮多一次往返）。
  用它换掉「重跑模型」——后者代价高一个量级。
- 新增 Span 属性 `feishu.agent.replayed`（补发命中），供宿主观测「省下的模型调用」。

### 🧪 测试与守卫

- `ConversationalFeishuEventHandlerOutboxTests` ×3：命中补发且不重跑模型 / 轮次不符时丢弃 / 送达后摘除。
- 状态键守卫扩展：outbox 两个键必须成对存在**且**在会话处理器中有真实消费点（防僵尸状态）。
- 同步修正 2 个既有用例的落库次数断言（P4-4 后非流式一轮落库 2 次）。

> **实施期实测缺陷（已修）**：初版先 `TryRemoveValue` 再读轮次键 ⇒ 轮次恒为 null ⇒
> 同一轮次被误判陈旧 ⇒ outbox 静默失效且无任何异常。已改为「先读后摘除」。
> 该类缺陷不产生失败信号，故用例必须断言到「模型调用次数」级别。

---

## [Unreleased] - Phase 4 P4-1：HITL 与 MAF 审批管线对齐（2026-09-29）

> 实施记录与探针证据见 `.docs/AI/Mud.Feishu.AI-审查缺陷修复与能力完善方案-R2.md` §4.1「P4-1 实施记录」。

### ⚠️ 行为变更登记（宿主可感 / 源破坏）

- **写类工具改由 MAF 审批管线把关**：写工具现在会用 MEAI `ApprovalRequiredAIFunction` 包装。
  模型发出写调用时，**工具不会立即执行**——MAF 的 `FunctionInvokingChatClient` 在调用**之前**
  把它转成 `ToolApprovalRequestContent`，须由宿主批准后回灌响应才真正执行。
  **安全收益**：批准资格由 `ApprovalResponseBindingChatClient` 绑定到「框架发出的请求」，
  模型<b>无法自批复</b>，从根本上弥补了 R2-1 只是封堵-but-仍依赖自律的面。
- **`IToolExecutionAuthorizer` 退化为策略判定**：`NeedsUserConfirmation` 不再参与动态判定
  （"是否需要人工确认"已由包装层在构造期决定），授权器只产出 `Allowed` / `Denied`。
- **未注册批准通道 = fail-closed**：写工具永久停在"等待确认"，不会自动放行，也不会静默——
  用户会收到一条"等待人工确认"答复。
- **新增 `ConversationalFeishuEventHandler` 构造可选参数 `approvalChannel`**（追加在末尾，
  已注入位置不受影响；DI 场景无需显式传参）。
- **源破坏**：`IFeishuToolApprovalChannel` 新增 `RequestFrameworkApprovalAsync`
  （netstandard2.0 无默认接口实现，自行实现该接口的宿主需补实现；项目未发布，无兼容负担）。

### 🌟 新增

- **`FrameworkToolApprovalRequest`** record（`Mud.Feishu.AI/Tools/`）：框架原生审批请求要素
  （`RequestId` / `ToolName` / `ToolCallId` / `AppKey` / `UserId` / `ConversationKey` / `RequiredScopes`）——
  **不含任何确认令牌**。
- **`ConversationalFeishuEventHandler.BuildApprovalPendingReply`**（`protected virtual`）：
  "等待人工确认"答复的可覆写构造点（宿主本地化）。文本刻意**不含** requestId / 入参 / 宿主关联号。

### 🧪 测试

- `FeishuToolApprovalWrappingTests` ×3（写工具被包装 / 只读不包装 / 包装后 Name·Description·JsonSchema 原样委派）。
- `ConversationalFeishuEventHandlerApprovalTests` ×3（提交宿主通道 / 答复不泄漏批准要素 / 通道抛异常时 fail-closed）。
- **先坐实后落地**：本轮用两个临时反射探针（API 形状探针 + 管线行为探针）验证了
  5 个原本只能猜的事实后才动工——探针用完即删。
  > 探针副产物：`ToolApprovalRequestContent.RequiresConfirmation` 带 `MEAI001`（实验性），
  > 本仓把该诊断当**错误**处理 ⇒ 生产代码禁止访问该属性。

### 🐛 修复（既有 flaky，非 R2/P4 引入）

- `Mud.Feishu.Abstractions.Tests` 的 `TokenStorePurgeGateTests.Gate_ShouldTrackLeasesPerAppKey`
  在全量并行负载下偶发失败。根因是**两个正交缺陷面**：
  ① 清库 `Task.Run` 的 fire-and-forget 撤门会跨越测试边界 → `TokenStorePurgeGate` 增世代隔离；
  ② 进程级静态门被多测试类并发读写，且变更者集合开放不可枚举 → 该测试程序集禁用测试类并行
  （代价实测 764 用例 ~2s → ~7s）。新增 2 条用例锁定世代语义。

---

## [Unreleased] - Mud.Feishu.AI 审查缺陷修复 R2（第二轮审查，2026-09-28）

> 方案与双视角复核裁定见 `.docs/AI/Mud.Feishu.AI-审查缺陷修复与能力完善方案-R2.md`（§0.5 复核裁定）。
> 覆盖 1 个 P0、5 个 P1、7 个 P2；新增配置键 **0**。

### ⚠️ 行为变更登记（宿主可感）

- **【P0 / R2-1】HITL 确认令牌不再进入模型上下文**：`NeedsUserConfirmation` 签发的 `confirm_token`
  改为**只**经新增契约 `IFeishuToolApprovalChannel` 投递给宿主；回填模型的文案改为中性语义（不含令牌、
  不再指示"以 `confirm_token` 重试"）。**迁移**：此前按旧文案「抄令牌重试」的宿主须改为实现该通道、
  由宿主侧回灌令牌；未注册通道时 HITL 降级为纯提示（fail-closed）。
  背景：旧路径下令牌、原参数、appKey、userId 都在模型上下文内，而令牌校验不校验"批准是否来自人"，
  模型可自行带令牌重试放行写操作。
- **【R2-3】新增 fail-fast**：「未装配 `IAppKeyAccessor` 的单应用宿主」+「已装配工具执行链」+ AppKey 缺失
  时首轮抛 `InvalidOperationException`（原为静默降级后工具/知识 100% 失败）。
  逃生门：覆写 `ConversationalFeishuEventHandler.AllowToolsWithoutAppKey => true`，
  或注册 `IAppKeyAccessor`（推荐）。
- **【R2-6】`FeishuAgent.Id` 取值变化**：由随机 GUID 改为委托内层 `ChatClientAgent.Id`
  （对齐 MAF `DelegatingAIAgent.IdCore`），使 `AgentResponse.AgentId` 与 `FeishuAgent.Id` 一致。
  若宿主持久化了旧 Id 做关联，需重建映射。
- **【R2-5】`MemoryConversationStore` 新增惰性分摊清扫**：只影响回收时机（未读过期键现在会被回收），
  内存语义与 TTL 不变；**不引入**后台线程/定时器。

### 🐛 修复

- **R2-2**：会话恢复期**急切**校验历史状态——MAF 状态袋惰性反序列化导致内层损坏只在首次类型化读取时抛，
  逃逸 `GetOrCreateSessionAsync` 的守护区 ⇒ 坏值永不删除、会话在 TTL 内永久毒化。
  同时在 `ConversationSummarizer` 增加纵深防御。
- **R2-4**：补齐 `Microsoft.ML.Tokenizers.Data.O200kBase` 词表包（原缺失使 `TiktokenTokenizer` 初始化失败
  被静默吞掉，`MaxHistoryTokens` 维度恒走字符估算且无任何信号）；新增
  `ChatTokenCounter.InitializationFailure` 并由 `FeishuAgent` 构造期告警一次（工具类不引入 `ILogger`）。
- **R2-7**：流式增量写入（`IMessageChannel.WriteStreamAsync`）增加调用方兜底——通道违反契约时只跳过分片，
  不再升级为"补偿 Flush + 幂等回滚 + 重投递（模型重复计费）"。
- **R2-9**：空模型回复不再下发（原会触发飞书 API 报错 → 回滚重投递 → 重复计费）。
- **R2-10**：`KeyedConversationGate` 取消路径由「断言运行时行为」改为「声明所依赖契约」，
  并由新增的取消竞态压测实证守护（**无代码行为变更**）。
- **R2-11**：摘要不可收敛边界——历史已缩到「摘要 + 1 条」且由 token 维度触发时跳过压缩，
  不再每轮一次模型调用并反复压缩"摘要的摘要"；并新增 Span 属性
  `feishu.agent.summarize_unconverged` 供宿主观测。
- **R2-12**：`MaxHistoryMessages` 文档校准（实测为**不可逆落库删除** + 工具消息剔除 + 仅保留首条 system，
  旧注释「折叠」与实测不符）。**无行为变更**。

### 🌟 新增

- **`IFeishuToolApprovalChannel` / `ToolApprovalRequest`**（`Mud.Feishu.AI/Tools/`）：宿主人工批准通道契约
  （SDK 只定契约、不内实现，与 `IToolExecutionAuthorizer` 同款定位）。
- **`FeishuAgentDiagnostics`**：`TagSummarizeUnconverged`；OTel GenAI 语义约定附加面
  `gen_ai.agent.name` / `gen_ai.operation.name`（**只增不改**，`feishu.*` 面保持不变）。

### 🧪 测试与守卫

- 新增/改写用例 30 个（`Mud.Feishu.AI.Tests` 192 → 222，`Mud.Feishu.AI.FeishuTools.Tests` 302 → 306）。
- 新增契约守卫 **8 条**（含 `MaxHistoryMessages` 消费点守卫——既有两条 Phase2/Phase12 守卫均未覆盖）。
- 新增 `KeyedConversationGateCancellationStressTests`：闸门取消竞态压测
  （判定「.NET `SemaphoreSlim` 取消/获取竞态不丢许可」的唯一实证手段）。
- **修正两处被精确计数暴露的用例**：`TokenWindowTests.Count_ShouldEstimateByCharsPerToken`
  与 `ConversationSummarizerConvergenceTests` 的条数断言原先按「4 字符/token」**估算**校准，
  R2-4 使精确计数真正生效后失准；已改为不绑定计数实现。

---

## [Unreleased] - AI 工具面 R3（对照官方 CLI 审查第三轮，2026-09-28）

> 四维评审结论（PM / 架构师 / 高级程序员 / QA）与逐条落地核验见
> `.docs/MudFeishu-AI-Tooling-vs-LarkCli-Review-Remediation-Plan-R3.md`（§0.3 评审、§13 落地核验）。
> 本轮**不改变已决策①~⑩**，且**不做** Tier R 自动执行器、通用裸 `api` 工具、Skills 导出。

### ⚠️ 行为变更登记（模型可见 / 宿主可感）

- **入站参数净化（无开关）**：含 C0/C1 控制字符、危险不可见 Unicode（零宽 / Bidi / BOM / U+2028-2029）
  或**独立 CR** 的工具参数将被**拒绝**（返回 `(invalid_args)`，零调用下游），不再静默下发。
  换行 `\n`、`\r\n`、Tab **照常放行**；Emoji ZWJ 序列中的 `U+200D` **不误伤**
  （官方 CLI 无差别拒绝 `U+200D` 的做法不可照抄——它是 Emoji 的合法组成）。
- **授权拒绝文案分类**：拒绝路径统一带语义前缀——`authorization_denied:`（权限被拒，放弃或改只读）、
  `policy_denied: {reason_code}`（宿主策略禁止）、`invalid_args`（参数问题，可自愈）。
  **待用户确认**（HITL）改用独立语义 `(needs_confirmation)`，不再复用 `forbidden`/`invalid_args`
  （原实现会让模型去做错误的自愈动作）。
- **新增策略轴（默认不收紧现行为）**：`FeishuAgent:MaxToolRisk`（默认 `high-risk-write`）与
  `FeishuAgent:AllowedIdentities`（默认 `["tenant"]`）在**授权门禁之前**判定，拒绝时零调用下游、不切租户。
- **内置出站内容安全检测**：4 条注入规则（`instruction_override` / `role_injection` /
  `system_prompt_leak` / `delimiter_smuggle`）按行扫描工具结果，`FeishuAgent:ContentSafetyMode`
  = `off|warn|block`，**默认 `warn`**（命中即加 `[untrusted_content: 规则]` 标注，不阻断）。
- **源破坏（项目未发布，无兼容负担）**：`FeishuToolDefinition` 新增必填参数 `Risk`（`FeishuToolRisk`）
  与 `Identity`，值**只能**来自编译期 Schema 的 `x-feishu.risk` / `x-feishu.identity`；
  宿主自行构造该 record 的代码需同步。

### 🌟 新增

- **`contact.search_user`**（工具面 22 → 24）：按姓名/关键字搜人（`GET /open-apis/search/v1/user`），
  返回 `open_id`/`user_id`/姓名/部门——打通「发给张三」整条写链路的**首环**，
  并有两跳端到端用例锁定 `search_user` → `im.send_message(receive_id_type=open_id)`。
- **`feishu.capability_lookup`**：能力出处元工具（只读、**默认不启用**）。回答"这个能力 SDK 里有没有 /
  是否已策展 / 对应哪些工具"，用于对冲"模型遇到未覆盖能力时凭空臆造调用"。
  **只返回分组级元数据**（分组名 / 方法数 / 是否已策展 / 工具名清单），不返回方法名、不返回请求构造。
  独立入口 `AddFeishuCapabilityTools()`（`AddFeishuTools()` 已包含）。
- **写操作预演 `dry_run`**（三个写工具均支持，默认 `false`）：返回将要下发的 `method`/`path` 与
  请求体字段摘要（**只回字段名与长度，不回原文**——否则预演会成为绕过净化的回显通道），不调用下游。
- **`ToolArgumentSanitizer` / `ToolResultContentSafety`**：入站净化与出站内容安全两个阶段
  （与既有 `ToolResultSanitizer` 构成"内容安全 → 净化 → 整形"的固定顺序，由行为断言锁定）。
- **新诊断上报点**：`MUDFT005`（缺 XML summary）/`MUDFT006`（参数缺说明）接线；
  `MUDFT009`（输出 Schema 截断）**聚合为单条**上报（当前命中 7 个工具，此前完全静默）；
  删除 4 个**引用不存在机制的僵尸诊断**（`MUDFT007/011/012/013`——`[FeishuScopes]`/`[FeishuToolRisk]`
  等特性在仓库中根本不存在）。
- **新增契约守卫**：`Diagnostics_ShouldNotDeclareUnreportedDiagnostics`（定义集 == 上报点集）、
  `ToolRiskAndIdentity_ShouldMatchSchemaValues_WhenRegistered`（防风险/身份双真相源）、
  `PermissionDoc_ShouldCoverExactlyAllContractTools`（《工具权限对照表》与工具名契约表精确相等）、
  `ZeroToleranceDiagnostics_ShouldEachHaveATriggerableCase`（负例登记表 + 债务预算）。

### 🐞 修复

- **`MUDFT015` 假门禁**：原实现的 `required` 与 `properties` 取自**同一** `entry.Parameters`
  （条件恒为假，从未真正触发）。改为**两个独立来源**比对（参数意图模型 ↔ 渲染产物文本，手写最小扫描器），
  并新增真实检出能力：**参数名归一后重名**导致 JSON 重复键。
- **`MUDFT005/006/009` 死定义**：有定义无上报点，使"MUDFT 零容忍 == 0"这一断言形同注释。
- **`GetHashCode` 字段子集**（`CapabilityEntry`/`CapabilityParameter`/`ScannedTool`/`ToolSchemaModel`）：
  仅哈希标量字段，集合与长字符串被整体跳过——增量管线的值比较会退化为逐字段 `Equals`。
  现按 `Equals` 认可字段**全量组合**（刻意拒绝"计数 + 首元素"这种补了等于没补的省算写法）。
- **能力目录覆盖数字的假绿**：`SdkMethodCount`/`DomainCount` 原被 `BeGreaterThan(200)`/`BeGreaterThan(100)`
  宽松断言，SDK 面缩水一半仍绿。现精确锁定（`1169 / 182 / 24`）。
- **文档/注释漂移**：工具数（16/19/22 → **24**）、《工具权限对照表》缺 contact 三工具
  （而守卫却声称"精确相等"）、`Extractors` 双路扫描注释声称"SDK 接口自动派生工具"（与实现不符）、
  `PageSizes` 悬挂常量（本包无 Calendar/Task 注册器）。
- **`contact` 自述与实际不符**：接口注释声称覆盖"姓名/关键词"却只实现邮箱/手机号——
  本轮补上 `search_user` 后改为事实描述。
- **`approval.create_instance.form` 零校验**：非 JSON / 非数组会被原样下发并换来一个语焉不详的飞书错误码，
  现与 `bitable.add_record.fields` 同级校验并给出可读修复指引。

### ⏭️ 有意未交付（附理由）

- **工具层幂等键**（`idempotency_key`）：飞书侧幂等参数 `client_token` 需给 `Mud.Feishu` 的写接口加可选查询参数
  （触碰"不改 SDK 业务签名"的决策⑤）；工具层自建缓存属新机制。**待决策**（方案 §12 D-8）。
- **日历 / 任务 / 云文档写侧工具**（原 P0）：SDK 签名与 DTO 未核，且应先吃注册器样板降本；
  见方案 §6.3 的排期重新论证（C-8）。
- **scope 集中映射表**、**域级 guidance 资产**、**IM 扩容 / 邮件 / 多模态 / 长尾域**：见方案 §13.1。

## [Unreleased] - AI 工具面契约整改（对照官方 CLI 审查，2026-09-28）

> 评审结论（四视角）与落地状态见
> `.docs/MudFeishu-AI-Tooling-vs-LarkCli-Review-Remediation-Plan.md`（§0.5 评审、§12 落地、§11 完整 CHANGELOG 文本）。
> 下文为该轮**摘要**；完整条目以方案 §11 为准。

### ⚠️ 行为变更登记

- **工具结果出站强制净化**：`FeishuToolBinding` 在执行链内**无条件**净化工具结果
  （ANSI 转义与控制字符剥离 + 凭据类 JSON 键值脱敏 + 中国大陆手机号脱敏）。
  **净化点无开关、不可绕过**；邮箱与 `*_token`/`*_id` 标识类字段**有意保留**
  （邮箱是平台寻址货币——`im.send_message` 的 `receive_id_type=email` 依赖它；
  标识类字段是多步工具调用链的必需凭据）。
- **工具描述符形状统一**：`x-feishu` = `{risk, is_write, identity, required_scopes, source, output_schema}`；
  `is_write` 由 `risk` 单处派生；新增 `source`（工具锚到 SDK 的接口.方法 + HTTP 路由）。
- **Schema 质量修正（模型可见契约变更）**：数组 `items` 由元素类型真实推导、
  C# `enum` 产出 `enum` 约束、`DateTimeOffset`/`Guid`/`TimeSpan` 产出 `format`、
  复合 DTO 展开为对象、`byte[]`/`Stream` 标注 `format:binary`。
- **`AIFunction.JsonSchema` 口径修正**：由「描述符信封」改为**纯参数 JSON Schema**（MEAI 契约）。
  信封顶层没有 JSON Schema 的 `type` 关键字，消费方按 Schema 解释时等价于「任意 JSON 可接受」，
  参数约束全部静默丢失。
- **源破坏**：`FeishuToolNames` 常量表改由源生成器从 `[FeishuTool]` 派生
  （手写 `FeishuToolNames.cs` 删除，`PageSizes` 迁至 `Tools/PageSizes.cs`）。
  常量名与数组名逐一保持不变，消费方无需改动。
- **新增构建期门禁**：工具描述符 golden 快照 `Mud.Feishu.AI.FeishuTools/FeishuToolSchemas.golden.txt`，
  漂移即 `MUDFT014` **中断构建**（重新固化路径见 `FeishuToolGoldenTests`）。

### 🌟 新增

- **通讯录三工具（工具面 19 → 22）**：`contact.resolve_user`（邮箱/手机号 → `user_id`/`open_id`）、
  `contact.get_user`、`contact.batch_get`——打通「用户说『发给张三』 → 模型凑出 `open_id`」的链路首环。
  新增 `AddFeishuContactTools()` 与 `AddFeishuTools()` 全域入口接入。
- **`[FeishuTool(Source = "接口.方法")]` 源挂钩**：编译期用 `Mud.Feishu` 符号交叉校验并派生
  HTTP 路由、风险分级、返回形状；声明与 SDK 不符即 `MUDFT019` 构建失败（工具面不得与 SDK 脱钩）。
- **Tier R 能力目录**（`build_property.FeishuToolCatalog=true` 开启）：聚合覆盖报告——
  实测 `SDK 能力 1160 项 / 策展 22 项 / 能力分组 182 个`，使「能力面差距」从主观判断变为构建期事实。
- **新增诊断**：`MUDFT015`（Schema 内部不一致）/`MUDFT016`（身份与接口令牌类型不符）/
  `MUDFT017`（读写分类与 SDK 事实脱钩）/`MUDFT018`（能力覆盖报告，Info）/`MUDFT019`（SDK 源无法解析）。

### 🐞 修复

- 源生成器 L1/L2/L4 三层**未接线**（1491 行死代码）——现全量接线为「扫描 → 渲染 → 校验」单管线。
- 零容忍诊断 `MUDFT002/004/008/010/014` **无上报点**，致 `verify-build.ps1` 的 MUDFT 断言
  恒为 0（**假绿门禁**）；零容忍集扩至 11 项并补齐上报点，配元守卫机械锁定「定义 ↔ 上报点同源」。
- `Extractors.UnwrapTaskType` 按字面 `"System.Threading.Tasks.Task<T>"` 比对恒为 false
  （BCL 泛型参数名为 `TResult`）——死代码期从未暴露，接线后表现为 100% 工具报 `MUDFT004`。
- 风险推导把 `{app_token}` 等**路由占位符**当危险词、且按 `POST` 判写面，
  导致几乎全部 Bitable 工具被误判 `high-risk-write`；改为只匹配方法名且 `POST` 不视为写面。
- `DescriptorValidator` 诊断 ID 与语义错配（`MUDFT012`/`MUDFT002`/`MUDFT003` 被用于无关语义）。
- 净化顺序缺陷：先剥控制字符会使 ANSI 序列残留为 `[31m` 类残渣（由单元用例当场捕获）。
- 删除重复/不可用实现：`SchemaWriter.WriteDescriptor`/`WriteMeta`/`MapJsonType`、
  生成器内第二份 `Quote`、`Extractors.DeriveModuleName`/`TryDeriveToolNameFromSdkInterface`/
  `DeriveActionFromMethodName`。

---

## [Unreleased] - AI-Native Phase 1/2 功能深化（AI-FD-D12 批次 A/B）

> 本轮聚焦 **会话并发正确性、单聊流式解锁、工具面扩容与治理、零自定义接入**。
> 详细设计见 `.docs/AI/AI-Native-Agent-Deepening-Phase12-Design.md`；实施进度见
> `documents/AIAgent/AI-Native-实施进度-Phase1-2深化.md`。

### ⚠️ 行为变更登记

- **core 缺陷修复（P1D-1a）**：`IFeishuTenantV1Message.GetContentListByMessageIdAsync` 路由由
  `[Get("/open-apis/im/v1/messages")]`（缺 `{message_id}` 段，`[Path]` 参数无法展开、调用必然命中
  错误端点）修正为官方语义 `[Get("/open-apis/im/v1/messages/{message_id}")]`。现路由必然调错端点、
  无人可能正确依赖，属缺陷修正而非破坏性变更（R5 规则 5 登记精神）。
- **工具执行器解析语义收敛（P1D-1c）**：`AddFeishuTools` 系注册从「缺任一域客户端启动崩溃」
  （`GetRequiredService` 硬失败）收敛为「客户端缺席 → 该域工具不进注册表、白名单映射期 fail-fast
  报『未注册』」。全域入口保留且产物等价（等价性用例锁定）。
- **工具结果截断升级（P1D-2a）**：JSON 结果超限时按 `items` 数组逐条删除并追加
  `truncated`/`hint` 标记（截断后仍为合法 JSON）；纯文本维持字符级截断。
- **工具错误回填分类（P1D-2b）**：错误回填从「裸原因」升级为「分类 + 原因 + 建议」三段式
  （`retryable`/`invalid_args`/`forbidden`/`api_error`），授权拒绝与参数错误可区分。
- **工具 scope 定稿（P1D-3a）**：19 个工具的 `required_scopes` 核对回填为开放平台真实权限点
  （`im.send_message` 由占位 `im:message` 修正为 `im:message:send_as_bot` 等），契约守卫升格为
  精确值断言。对照表见 `documents/AIAgent/工具权限对照表.md`。

### 🌟 新增

- **会话串行化三层（P2D-1，最高优先级架构修复）**：`IConversationGate` 契约下沉 Abstractions；
  `KeyedConversationGate` 进程内默认实现（同键串行、跨键并行、空闲回收）；`RedisConversationGate`
  分布式实现（SET NX 租约 + 比较删除释放 + 有限次重试后快速失败，忙时抛
  `ConversationBusyException` 由事件层重投递承接）。
- **流式正解通道（P2D-2a）**：`CardStreamMessageChannel`（应用消息卡片流，Create→Update→终态）+
  `AddFeishuStreamingChannel` 降级链（卡片流失败自动降级编辑通道，事件处理器零感知）。
- **会话目标解耦（P2D-2b）**：`ConversationRequest` 新增可空 `ChatId`（回复/流式目标）与
  `ParentId`（引用消息）——单聊流式解锁。
- **速率自适应分片（P2D-2c）**：`BufferedMessageChannel` 基类抽取，分片长度 + 最小更新间隔
  （800ms）双阈值；`EditMessageChannel`/`CardStreamMessageChannel` 缓冲语义同构。
- **RAG-A 可用性（P2D-4a/b）**：`knowledge.search` 工具化（模型按需检索）+
  `KnowledgeContextAssembler` 注入桥（`AddFeishuKnowledgeContext`，注入模式）。
- **零自定义接入（P2D-5a/b）**：`ImMessageConversationalEventHandler` + `AddFeishuImConversationHandler`
  一行接入（Bot 自激过滤、群聊 @ 过滤安全内建）；内置装配器集（SenderInfo/QuoteMessage/Knowledge 位标记装配）。
- **工具面扩容 13→19（P1D-1a/b）**：新增 `im.get_message_content`、`docx.get_document_blocks`、
  `bitable.get_records_by_ids`、`drive.list_folder_files`、`drive.get_file_metas`、`knowledge.search`
  6 个只读工具；`bitable.query_records` 增 `sort` 简化文法（字段:asc|desc，≤3 个）。
- **子域注册粒度（P1D-1c）**：`IFeishuToolDomainRegistrar` 逐域注册器；
  `AddFeishuBitableTools`/`AddFeishuImTools`/`AddFeishuDocxTools`/`AddFeishuWikiTools`/
  `AddFeishuSearchTools`/`AddFeishuSheetsTools`/`AddFeishuDriveTools`/`AddFeishuKnowledgeTools`/
  `AddFeishuWriteTools` 按需装配。
- **结果整形钩子（P1D-2a）**：`IToolResultShaper`（宿主注册后对投影结果最终整形，失败回退默认）。
- **结构化审计出口（P1D-3b）**：`IToolExecutionAuditSink` + `ToolExecutionAuditRecord`
  （允许/拒绝/错误均投递；`ArgsDigest` SDK 侧脱敏，宿主 sink 不接触原始参数）。
- **工具目录与 Schema 导出（P1D-4）**：`IToolCatalog`（注册表之上的稳定目录契约）+
  `IToolSchemaExporter`（OpenAI-compatible tools JSON 导出）。
- **工具可观测（P1D-5）**：`feishu.tool.executions` / `feishu.tool.duration` /
  `feishu.agent.llm.duration` 三指标（高基数纪律：conversation/chat/user 键不入 tags）。
- **记忆深化（P2D-3a/b）**：`FeishuAgentOptions.MaxHistoryTokens`（token 窗口，默认 8000，
  条数与 token 双窗口先触发者生效）；摘要输入每条 500 字压缩、摘要调用 30s 超时钳制。
- **RAG 引用回链（P2D-4c）**：`RetrievedChunk.Source` 按 `DataAssetIds` 填充（弱引用）；
  `KnowledgeAnswer.Sources` 编号尾注投影。

### ⚙️ 配置面新增（R4/R5 对齐）

| 配置属性                | 配置节                       | 默认值             | 消费点                                |
| ----------------------- | ---------------------------- | ------------------ | ------------------------------------- |
| `MaxHistoryTokens`      | `FeishuAgent`                | `8000`（0=不启用） | `ConversationSummarizer`              |
| `RequireMentionInGroup` | `FeishuAgent:ImConversation` | `true`             | `ImMessageConversationalEventHandler` |
| `AllowP2pConversation`  | `FeishuAgent:ImConversation` | `true`             | 同上                                  |

---

## [3.0.1] - 2026-10-07

> 主题：**适配 `Mud.HttpUtils` 3.0.0**（上游移除 `UseApp` / `UseDefaultApp` / `BeginScope(string)`，改以作用域式切换）与 **事件 v1.0 / v2.0 双格式解析修复**。
> 本仓对外契约**无移除**（三个旧成员由 `IFeishuAppContextSwitcher` 接续声明并标 `[Obsolete]`）；
> 含少量行为修正（`EventData.CreateTime` 单位、`EventData.Event` 写侧类型），升级前请阅读「变更（Changed）」与「迁移提示」。

### 依赖升级

- `Mud.HttpUtils` / `Mud.HttpUtils.Generator`：`2.0.9` → **`3.0.0`**（两者必须同版本）。
- 上游 3.0.0 起，非 HttpClient 模式的生成类**默认不再发射** `UseApp(string)` / `UseDefaultApp()` / `BeginScope(string)`，
  且 `IAppContextSwitcher` 不再声明它们；生成类**自动附加** `IAppScopeSwitcher`（`UseAppScope` / `UseDefaultAppScope`）。

### 新增（Added）

- **`IFeishuAppContextSwitcher` 继承 `IAppScopeSwitcher`**：下游可直接以推荐面（作用域式、释放时自动归还上下文）编程。
- **`IFeishuAppContextSwitcher` 接续声明三个旧成员并标注 `[Obsolete]`**：保持对外契约不变，
  188+ 个生成实现类**继续发射**这三个成员，行为与 2.0.9 完全一致。
  三个成员将在本 SDK 的下一个大版本随上游一并移除，请按提示迁移到 `UseAppScope` / `UseDefaultAppScope`。
- **共享事件解析器 `FeishuEventDataParser`**（Abstractions，单一真源）：v2.0 / v1.0 官方形态 / `data` 包裹兼容形态统一解析，
  WebSocket 与 Webhook 双通道删除各自平行实现。
- **事件失败落盘**：WebSocket 通道处理失败可经 `AddFailedEventStore<T>()` / `AddFailedEventStore(instance)` 注册 `IFailedEventStore`（TryAdd 语义，不默认注册，未注册零行为变化；业务失败分支落盘，取消/拦截终态不落盘）。
  配套新增 `FeishuWebSocketOptions.FailedEventInitialRetryDelaySeconds`（默认 10，非正数回退默认）。
- **读取扩展 `GetEventRawJson()`**：统一读取 `EventData.Event` 携带的 JSON 原文。

### 变更（Changed）

- **`FeishuAppManager.GetWebApi` / `GetDefaultWebApi` 内部实现迁移**：由生成类的
  `UseApp(appKey)` / `UseDefaultApp()` 改为 `IAppContextHolder.SwitchToApp(appKey, this, serviceProvider)` /
  `SwitchToDefaultApp(this)`（**语义等价**：完整守卫 + 立即切换 + 不归还 + 返回上下文）。
  对外行为不变（仍返回 DI 解析到的服务实例，且其 `Current` 已绑定目标应用）。
- **`EventData.Event` 写侧统一为 JSON 原文**字符串**（原 WebSocket 通道写 `JsonElement`）。
  属性类型保持 `object?` 不变；读取请改用扩展 `GetEventRawJson()`，下一 major 收敛为 `string?`。
- **行为变更**：`EventData.CreateTime` 语义由「秒」修正为**毫秒**（与 XML 注释对齐，v1.0 `ts` 一并纳入统一启发式）。
  按秒消费的宿主请 ×1000 或改用 `DateTimeOffset.FromUnixTimeMilliseconds`。
- **`IgnoreUnknownEventTypes` 默认值两通道保持差异**（Webhook=true / WS=false，不改默认）：
  两侧 Options XML 已注明差异原因，WS=false 时启动期输出一次性对齐告警。

### 修复（Fixed）

- **Webhook 通道 v1.0 事件字段映射错误**导致事件被空字段 fail-closed 400 拒绝；
  **WebSocket 通道 v1.0 官方帧**（根级 `uuid`/`token`/`ts`、事件字段在 `event` 内）被整帧静默丢弃。
  现两通道均正常路由 v1.0 事件（事件类型取 `event.type` 原值，handler 按此注册）。
- **v1.0 事件根级 `token` 现解析进入合成 Header**（`Schema == null`，字段取自根级 `uuid`/`token`/`ts` 与 `event.*`）：
  判定 v1.0 请用 `Schema == null`，不要再用 `Header == null`；来源校验与依赖 Header 的幂等逻辑在 v1.0 下恢复可用。

### 迁移提示

- 以 `IFeishuAppContextSwitcher` 类型调用 `UseApp` / `UseDefaultApp` / `BeginScope(string)` 会产生 **`CS0618`** 警告（有意引导）。
  若宿主启用 `TreatWarningsAsErrors`，请改用 `UseAppScope` / `UseDefaultAppScope`（守卫相同，额外自动归还上下文），或临时 `NoWarn CS0618`。
- 语义提醒：`UseAppScope` 在 `using` 结束时**自动回切**；`UseApp` 会一直保持到下次切换（长生命周期宿主须显式切回）。

## [3.0.0] - 2026-09-28

> 3.0 是一次面向**生产可靠性与性能**的全面升级：原生 AOT 一等支持、令牌与多应用管理重构加固、Webhook 安全基线、Redis / WebSocket 稳定性专项，并统一配置结构。包含较多破坏性变更，升级前务必阅读「升级要点」；逐项明细见下方 rc2 / rc3 记录。

### 🌟 核心升级

- **原生 AOT 全面支持**：net8.0+ 全链路源生成 JSON 序列化与配置绑定，AOT 严格模式门禁保证 0 反射告警，附带端到端验证工程（`Demos/Mud.Feishu.AotVerification`）。配置 DTO 不再使用 `required`，校验统一由 `Validate()` 承担。
- **令牌与多应用管理重构**：多应用配置热更新（`BaseUrl`/`TimeoutSeconds` 运行期生效、按 AppKey 增量应用）、per-app 认证客户端与端点隔离、凭据变更即清库、401 令牌恢复真正生效（级联清 store）、Memory/Redis 令牌键布局统一（`TokenKeyBuilder` 单一真相源）、OAuth 失败可重试性分类、令牌存储加密（可选）。
- **Webhook 安全基线**：生产环境强制分布式去重、多应用必须声明 `ExpectedAppId`、拦截语义与默认处理器路由收敛、可恢复故障一律 503 触发飞书重推——杜绝跨应用串扰与事件永久丢失。
- **WebSocket 可靠性专项**：连接生命周期与调用方取消令牌解耦、事件处理失败回 ACK `code=500` 触发服务端重投、并发闸门与重连熔断、僵尸连接消除、消息按 UTF-8 字节统一计量、背压前移到接收路径。
- **Redis 加固**：`rediss://` 真正启用 TLS、四类键统一构造与转义（`RedisKeyBuilder`）、去重竞态 Lua 原子化、Cluster 全节点覆盖、异常可分类（`FeishuRedisFailureKind`）、SeqID 去重改容量窗口、新增运维诊断门面与指标。
- **质量门禁**：`verify-build.ps1` 全新门禁（缓存自检、全 TFM 构建 + 诊断白名单、AOT 严格模式冒烟、TRX 测试计数断言），CI 同步接入。

### ⚠️ 升级要点（破坏性变更）

**配置结构**

- 配置统一为嵌套分组：`FeishuWebSocketOptions` → `Reconnect.*` / `Certificate.*`；`FeishuAppConfig` / `RedisOptions` → `HttpRetry.*` / `CircuitBreaker.*` / `Connection.*`。JSON 旧扁平键仍可自动回填，**C# 代码必须改用嵌套 API**；日志开关统一为 `Logging:LogLevel:*`。
- `nuget.config` 收紧为包来源锁定；依赖钉住 `Mud.HttpUtils` 2.0.7。

**令牌与多应用**

- Redis 令牌键布局变更（`feishu:token:*` → `feishu:{appKey}:token*`），旧键不再读取；`SingletonFeishuTokenStoreFactory` 废弃，改用 `IFeishuTokenStoreFactory.Create(appKey)`；`AddFeishuRedisTokenStore` 不再注册 `ITokenStore`/`IUserTokenStore` 单例。
- 令牌失效级联清除持久层；租户 401 恢复不再回退用户级；`SetDefaultApp` 运行期切换真正生效。
- 含特殊字符（`* ? [ ] : \`）的 AppKey 旧令牌键不再可达，受影响部署建议升级后轮换 AppSecret。
- 注入的 `IFeishuAppContext` 变为无状态转发代理（每次取当前默认应用）；需实例快照语义请设 `FeishuAppOptions.ForwardDefaultAppContext=false`。
- 配置热更新默认开启（`EnableConfigReload=true`）；如需「变更需重启」的旧语义请显式关闭。
- OAuth 刷新失败按可重试性分类：`invalid_grant` 等将清除 refresh token 并要求重新授权。

**Webhook**

- 生产环境未注册分布式去重将启动失败（单实例可设 `FeishuWebhook:AllowInMemoryNonceDedupInProduction=true`）；多应用必须配置 `ExpectedAppId`；`Build()` 要求至少注册一个全局默认处理器。
- `BeforeHandleAsync` 返回 `false` 由 500（可重试）改为 200（已消费）；需「拦截后重推」设 `InterceptionAckMode=Retryable`。拦截器组合默认 `Merge`（旧行为设 `InterceptorFallbackMode=AppOnly`）。
- 解密超时由 400 改为 503（可恢复故障让飞书重投）。

**WebSocket**

- 终止连接请调用 `DisconnectAsync()` / `DisposeAsync()`（`ConnectAsync` 的令牌只约束建连 + 认证）；`StartReceivingAsync` 已弃用；`MessageReceived` 改为并发派发（可能乱序），需顺序保护请用 `IMessageHandler`。
- 同步 `Dispose()` 不再停止服务；配置上界收紧并在启动期校验（重连预算、超时、消息大小等）；构造签名变更（移除 `seqIdDeduplicator` / `ProcessingTask` 等死参数）。
- 指标 API 迁移：`FeishuMetrics.WebSocketConnectionObserver` 等静态可写属性移除，改用 `RegisterWebSocketMetricsSource(...)`。
- 连接默认白名单 `*.feishu.cn;*.larksuite.com`，自建端点请配置 `AllowedHostSuffixes`。

**DTO 重命名（修复源生成同名冲突 SYSLIB1031）**

- `DepartmentsV1.DepartmentLeader` → `DepartmentLeaderV1`、`DepartmentDetail` → `DepartmentDetailV1`、`ApprovalExternal.ApprovalCreateViewers` → `ExternalCreateViewers` 等 7 组，完整列表见 3.0.0-rc2。

**Redis 去重**

- SeqID 去重键增加 scope 隔离维度（默认 `AppKey|MachineName`），窗口由 TTL 改为容量（`FeishuRedis:SeqIdWindowCapacity`，默认 100000）；`GetCacheCount()` 等语义收窄为窗口内真实值。
- `RedisOptions` 非法值改为启动期校验失败；`NonceFailureMode` 仅对 Redis 连接类故障生效。

### 🐛 重点修复

- **事件永久丢失**：事件处理失败 ACK 恒 200、Nonce 基础设施故障伪装 403、处理器类型不匹配静默丢失、解密超时吞成 400 等已全部修复，可恢复故障统一 503 触发飞书重推。
- **令牌正确性**：401 恢复复用被拒令牌、租户重试被注入用户令牌、Redis 过期令牌 TTL=0 永不过期、凭据变更清库在内存后端无效等。
- **稳定性**：去重工厂自解析 `StackOverflowException`、空前缀清库误删全库、WebSocket 僵尸连接 / 死锁 / `ObjectDisposedException` 竞态、Redis 连接串日志泄漏口令等。
- 同轮完成 200+ 项次级修复与加固，明细见下方 rc2 / rc3 记录。

## [3.0.0-rc3] - 2026-09-23

> 本版聚焦 **Webhook 多地部署安全加固、令牌/多应用热更新稳定性、Redis 去重与令牌存储正确性、WebSocket 连接可靠性**。包含若干破坏性变更，升级前请务必阅读「升级须知」。

### 🌟 本版亮点

- **Webhook 防重放与多应用隔离**：生产环境强制分布式去重、多应用必须声明 `ExpectedAppId`、拦截语义与默认处理器路由收敛，杜绝跨应用串扰与事件永久丢失。
- **令牌与多应用热更新稳定性**：默认应用桥接改为「解析桥接」（DI 注入立即跟随运行时切换）、per-app 认证客户端真正生效、Memory/Redis 令牌键布局逐字节对齐、`OnConfigurationChanged` 不再阻塞配置回调线程。
- **Redis 去重与令牌存储正确性**：SeqID 改为容量窗口、`ClearCacheAsync` 真正删除、`rediss://` 真正启用 TLS、令牌键前缀可配置、运维诊断门面与指标补齐。
- **WebSocket 连接可靠性**：连接生命周期与调用方 `CancellationToken` 解耦、僵尸连接消除、分片消息边界保护、并发派发与存活/丢弃指标。

### ⚠️ 升级须知（破坏性变更 / 必须处理）

**Webhook**

- **生产环境默认禁止内存 Nonce 去重**：未注册分布式去重（`AddFeishuRedisDeduplicators()`）时，生产环境（`ASPNETCORE_ENVIRONMENT=Production`）启动直接失败。单实例部署请显式设置 `FeishuWebhook:AllowInMemoryNonceDedupInProduction=true`（会输出风险告警）。
- **多应用必须配置 `ExpectedAppId`**：`FeishuWebhook:Apps` 条目 > 1 时缺失该键将启动失败——它是防止 `EncryptKey` 误配导致跨应用串扰的唯一兜底。单应用部署仍可选。
- **拦截语义变更**：`BeforeHandleAsync` 返回 `false` 由原本的 `500`（可重试）改为 `200`（已消费并落去重标记，飞书不再重推）。如需「拦截后重推」，设置 `FeishuWebhook:InterceptionAckMode=Retryable`（响应 503，不落去重标记）。
- **必须注册全局默认处理器**：`Build()` 现在要求至少一个不带 appKey 的 `AddHandler<T>()`，否则启动失败（此前会静默用应用专属处理器作默认，导致其它应用事件被错误路由）。
- **应用专属拦截器不再静默屏蔽全局拦截器**：组合策略默认改为 `Merge`（全局先行 → 应用专属）。旧行为（某应用一旦注册专属拦截器就**完全丢弃**全局拦截器，使安全/审计横切在该应用上静默失效）可通过 `FeishuWebhook:InterceptorFallbackMode=AppOnly` 保留（此时启动期会 Warning 列出被屏蔽的全局拦截器）；另提供 `AppThenGlobal`（应用专属先行）。
- **解密超时由 400 改为 503**：解密超时此前被吞成 `null` → 400（终态，飞书不重推）；超时属可恢复的服务端问题，现按 503 让飞书重投，解密 CTS 同时链接 `RequestAborted`。
- **`IEnvironmentService` 缺失即启动失败**：它是「生产环境内存 Nonce 去重阻断」的唯一判据来源，缺失会导致生产锁**静默失效**，故要求其必然可解析。
- **空 `appKey` 的去重键不再退化为裸键**：改用固定哨兵前缀，消除跨应用同 ID 事件的碰撞面。

**令牌与多应用**

- **默认应用上下文桥接语义变更（默认生效）**：注入的 `IFeishuAppContext` 变为无状态转发代理，每次访问都取当前默认应用。若代码强转为具体 `FeishuAppContext`，请设 `FeishuAppOptions.ForwardDefaultAppContext=false` 恢复实例快照语义（需重启生效）。
- **含特殊字符的 AppKey 键布局变化**：`TokenKeyBuilder` 现转义 glob 元字符（`* ? [ ] : \`）。升级前请确认 AppKey 不含这些字符；否则旧令牌键在热更新后不再匹配。
- **自定义 `UserTokenStoreBase` 子类**：若覆写了 `KeyPrefix`，需改为委派 `TokenKeyBuilder.BuildKeyPrefix(appKey)`，与 Memory/Redis 端保持一致。
- **`OnConfigurationChanged` 不再同步等待清库完成**（此前最多约 10s）；需等待的宿主请以清库门撤除为完成信号自行轮询。清库完成前不恢复旧令牌的语义不受影响。
- **运行时添加的应用**：一旦由配置声明，其「运行时添加」标记会被撤销，之后从配置删除该应用不再被永久忽略。

**Redis**

- **新增 `FeishuRedis:SeqIdWindowCapacity`**（默认 100000，非正值启动失败）：SeqID 去重窗口由「TTL 时间窗口」改为「容量窗口」，`GetCacheCount()` / `GetMaxProcessedSeqId()` 语义收窄为窗口内真实值，不可用于推断剩余去重空间。
- **新增 `FeishuRedis:TokenKeyPrefix`**（默认 `feishu`）：令牌键前缀由硬编码改为可配置，多环境共用 Redis 时可隔离键空间。
- 含 `:` / `\` 的 AppKey 旧令牌键不再可达，建议受影响部署轮换 AppSecret（升级后首次取令牌会重新落库）。

**WebSocket**

- **连接生命周期不再跟随调用方 `CancellationToken`**：`ConnectAsync` 的令牌只约束「建连 + 认证」；终止连接请调用 `DisconnectAsync()` / `DisposeAsync()`，恢复接收请调用 `ReconnectAsync()`。
- **`IFeishuWebSocketClient.StartReceivingAsync` 已弃用**：接收循环由 `ConnectAsync` 统一管理；已有循环时为幂等 no-op，未连接时抛 `InvalidOperationException`。
- **`MessageReceived` 线程契约变更**：由「接收循环同步串行派发」改为「并发租约内派发，可能并发、可能乱序」。需要顺序/超时保护请改用 `IMessageHandler`。
- **`PingPongMessageHandler` / `HeartbeatMessageHandler` 构造函数移除了 `FeishuWebSocketOptions` 参数。**
- 配置上界收紧（启动期 fail-fast）：`Reconnect.TotalBudget ≤ 7 天`、`Reconnect.BaseDelayMs/MaxDelayMs ≤ 1 小时`、`MessageSizeLimits.MaxTextMessageSize ≤ 10MB`、`ConnectionTimeoutMs/AuthTimeoutMs/AuthGateTimeoutMs ≤ 5 分钟`。
- 入站报文不再全文入日志（改为长度 + 200 字符脱敏预览），连接 URL 日志整体剥离 query。

### ✨ 新增

- **Webhook**：`FeishuWebhook:AllowInMemoryNonceDedupInProduction`、`FeishuWebhook:InterceptionAckMode`、`FeishuWebhook:InterceptorFallbackMode`（默认 `Merge`）、`FeishuWebhook:NonceTtlSeconds`（显式配置时强制 `> TimestampToleranceSeconds` 的重放窗口不变量）；启动期选项校验（宿主启动失败而非首个请求 500）；`intercepted` / `intercepted_retryable` 指标标签；未匹配事件类型与软超时可观测（Warning + `unhandled` / `timeout_overshoot` 指标）；健康检查 `nonceDedup` 形态数据项（生产 + 内存 = `Degraded`）；启动 Summary 日志（去重形态、时间戳容差、Nonce TTL、重放窗口不变量、各应用处理器/拦截器注册自检）。
- **令牌/多应用**：清库链路可观测性 `PurgeTokenStoreFailureEvent`（EventId 5601，宿主可据此建告警）；热更新竞态门闸测试基建 `HotReloadRaceHarness`。
- **Redis**：运维诊断门面 `IRedisDeduplicationDiagnostics.GetSnapshotAsync` → `RedisDeduplicationDiagnosticsSnapshot`；Redis 指标 `feishu.redis.operation` / `feishu.redis.operation.duration` / `feishu.redis.scan.keys`（挂在既有 `Mud.Feishu` Meter）；健康检查注册可选（`registerHealthCheck=false`）。
- **WebSocket**：连接存活探针 `ConnectionLiveness`（`ReceiveLoopAlive` / `LastReceiveUtc` / `IdleMs` / `IsZombie`）；存活/丢弃指标 `feishu.websocket.receive.idle_ms` / `.loop_alive` / `.zombie` / `feishu.websocket.frames.discarded`（四个受控丢弃点全部接入计数）；健康检查 `data` 新增 `receive_loop_alive` / `last_receive_utc` / `idle_ms` / `is_zombie`；架构不变量守卫与 7 条契约守卫测试；`FeishuWebSocketServiceBuilder` 不再静默丢弃直接注册的 `IFeishuEventInterceptor`。

### 🐛 修复

- **进程崩溃**：`FeishuDeduplication:Mode=Distributed` 且未注册 Redis 时，去重工厂自解析导致 `StackOverflowException` 已修复；改为正常构建并告警「事件去重仍为内存实现」。
- **事件永久丢失**：Nonce 去重基础设施故障（Redis 连接/超时）不再伪装成 403 验签失败，改由 `FeishuDeduplicationFatalException`（`FailureKind=Server`）转 **503** 触发飞书重推；客户端断开（`OperationCanceledException`）不再被吞成「验签失败 403」写向已中止连接；处理器 `SupportedEventType` 不匹配不再静默丢失事件；解密超时不再被吞成 400。
- **Webhook 技术债**：内存 Nonce 去重补上容量上限（原为无界增长）；多处理器同时失败时补记**全部**异常（此前仅重抛首个）；删除不可达的补偿分支；消除处理器注册表的并发注册竞态；失败事件重试服务每轮结束显式清除 AppKey 上下文。
- **令牌/多应用**：默认应用 DI 桥接改为解析桥接（修复继续用旧凭据 / 指向已释放上下文）；per-app 认证客户端编译期直引，装配失败显式失败（修复「凭据发往错区域」）；Redis 令牌键前缀去预转义 + SCAN glob 字面量转义（修复清库/枚举/全用户清库永不命中）；OAuth 刷新失败分类收紧（瞬时故障不再误清 refresh token）；运行时添加的应用可被配置正确接管；凭据变更清库不再阻塞配置回调线程；未实例化应用的凭据变更也能被检出。
- **Redis**：`rediss://` 现在真正启用 TLS（此前明文连 TLS 端口）；`RedisFeishuEventDistributedDeduplicator` 补声明 `IDisposable`；事件去重 Lua 补 `tonumber(timestamp)` 护栏；`GetStatusAsync` 改用服务端 `TIME` 求差；令牌 SCAN 类 API 改为异步分批删除（500/批）；健康检查 PING 成功即 `Healthy`；`FeishuRedisFailureKind.InvalidArgument` 首次真实产生；清零 4 处编译警告。
- **WebSocket**：取消退出导致「连接正常但收不到事件」的僵尸连接；`StartReceivingAsync` 幂等守卫可创建第二条接收循环；分片超限丢弃不排空致边界失步；重连窗口未钳制致自动重连失效；入站完整报文未脱敏入日志；修复释放与事件派发期间的 `ObjectDisposedException` 及死锁竞态；socket 类型收敛到抽象 `WebSocket`；`ResolveMaxTextMessageBytes` 整型溢出；`IsConnected` 双真源。

### 📝 文档与测试

- `Mud.Feishu.Redis/README.md`：键布局改为实测样例（含双冒号与 `\:` 转义）、配置表补齐 `SeqIdWindowCapacity` / `TokenKeyPrefix`、新增「运维诊断」「可观测性」两节。
- `Tests/Mud.Feishu.Redis.Tests/README.md`：删除不存在的 `RedisFeishuEventDistributedDeduplicatorWithFallback` 章节、按实际结构/技术栈刷新。
- 新增契约守卫（7 条）并以「故意违规金丝雀」验证可拦截回归；WebSocket 新增存活/分片排空/装配重连/配置上界/压力等用例；`verify-build.ps1` 步骤 4 追加 `--filter "Category!=Stress"`。
- `AGENTS.md` / README 中多应用与令牌清库相关表述及 AppKey 命名约束同步。

## [3.0.0-rc2] - 2026-09-18

### 🌟 亮点

- ⚡ **原生 AOT 全面适配**：net8.0+ 一等公民支持 Native AOT 发布——全链路源生成 JSON 序列化与
  配置绑定、AOT 严格模式质量门禁保证 0 反射告警，附带端到端验证工程与完整文档。
- 🔐 **令牌与多应用管理三轮专项加固**：用户令牌可续期、401 恢复真正生效、
  凭据变更即清库、Memory/Redis 令牌键统一、配置热更新事务化、OAuth 失败语义分类等 60+ 项修复。
- 🪝 **WebSocket 可靠性**：事件处理失败不再静默吞异常（飞书将重发）、重连熔断、并发闸门、健康检查并发指标。
- 🗄️ **Redis 去重加固**：四类键统一构造与转义、Cluster 全节点覆盖、竞态 Lua 原子化、失败可分类、新增 Testcontainers 集成测试。
- 🛡️ **质量门禁**：`verify-build.ps1` 全新门禁（缓存自检、全 TFM 构建 + 诊断白名单、AOT 严格模式冒烟、TRX 测试计数断言），CI 同步接入。
- 📦 **依赖升级**：`Mud.HttpUtils` / `Mud.HttpUtils.Generator` 统一钉住 **2.0.7** （组件首个正式版系列，生成器修复源生成同名类型冲突 SYSLIB1031）。

### ⚠️ 升级须知（破坏性变更 / 行为变更）

**令牌与多应用**

- 令牌失效现在级联清除持久层存储——401 恢复才真正生效（此前重试仍用被拒旧令牌）。
- Redis 令牌键布局变更：`feishu:token:*` → `feishu:{appKey}:token*`，升级后旧键不再读取；
  `SingletonFeishuTokenStoreFactory` 已废弃，请改用 `PerAppRedisTokenStoreFactory`。
- `AddFeishuRedisTokenStore` 不再注册 `ITokenStore`/`IUserTokenStore` 单例，
  改用 `IFeishuTokenStoreFactory.Create(appKey)`。
- 租户请求的 401 恢复不再回退为用户级恢复（重试不会被注入用户令牌）。
- `GetAllApps()` 不再隐式实例化全部应用；后台令牌刷新默认仅覆盖默认应用，其余应用首次访问时增量注册。
  需要启动期预热请设 `WarmUpAllAppsOnStartup = true`。
- `SetDefaultApp` / `TrySetDefaultApp` / `DefaultAppKey` 现在真正生效，请复核运行期切换默认应用的调用点。
- `TryGet*` 令牌管理器解析器无默认应用时返回 null（不再抛异常）。
- 认证/取令牌请求改用本应用的命名客户端（多区域部署不再取错平台端点）；
  必要时以 `EnablePerAppAuthenticationClient = false` 降级。
- 配置热更新默认开启（`EnableConfigReload = true`），`BaseUrl`/`TimeoutSeconds` 变更无需重启；
  如需「配置变更需重启」的旧语义，显式设为 `false`。
- OAuth 刷新失败按可重试/不可重试分类：`invalid_grant` 等将清除 refresh token 并要求重新授权。
- 凭据变更即清库：热更新检测到 (AppId, AppSecret) 变更，立即清除该应用全部持久化令牌。
- 已删除死配置项：`EnableContextRetirement` / `PurgeStoreOnTokenInvalidation`（默认安全行为保留）。

**配置与 HTTP**

- **配置结构统一为嵌套（C# 代码破坏性）**：扁平属性收敛为分组对象——`FeishuWebSocketOptions` 的重连/证书
  改为 `Reconnect.*` / `Certificate.*`（`AutoReconnect`→`Reconnect.Auto`、`ReconnectDelayMs`→`Reconnect.BaseDelayMs`、
  `ValidateServerCertificate`→`Certificate.ValidateServerCertificate` 等），新增 `Certificate.Mode`（`Strict`/`Dev`/`Custom`）；
  `FeishuAppConfig` 的 `TimeOut`/`RetryCount`/`CircuitBreaker*` 与 `RedisOptions` 连接键同样收敛为
  `HttpRetry.*` / `CircuitBreaker.*` / `Connection.*`。**JSON 旧扁平键仍可绑定**（启动期自动回填到嵌套属性），
  C# 代码必须改用嵌套 API；各模块日志开关统一为 `Logging:LogLevel:*`（`EnableLogging` 已移除）。
- `FeishuAppConfig` 移除 `required`：AOT 源生成绑定要求，校验统一由 `Validate()` 承担。
- 增强 HttpClient 装配基线化：此前手工 `new` 静默取默认值的 10 个字段现与注册路径同源。
- `nuget.config` 收紧为包来源锁定（`<clear />` + `packageSourceMapping`）。

**WebSocket**

- 事件处理失败不再静默吞异常：ACK 返回 `code=500`，飞书服务端将重发——**请确保业务处理器幂等**。
- 同步 `Dispose()` 不再尝试停止服务；确定性停止请 `await StopAsync()` 或 `await DisposeAsync()`。
- 服务端 Pong 下发的 ClientConfig 不再覆盖本地重连策略（`PingInterval` 仍生效，钳制 5–30 秒）。
- 构造签名变更：`FeishuEventMessageHandler` 移除 `seqIdDeduplicator` 参数；
  `WebSocketBinaryMessageEventArgs.ProcessingTask` 移除。
- 重连协调器不再在持锁期间触发事件：订阅者在回调内**同步阻塞等待**重入 `TryReconnectAsync` 不再死锁，
  并发重连请求改为立即返回 `false`。**注意：回调内不得同步阻塞**（会占住重连闸门导致后续重连被跳过），
  耗时操作请自行 `Task.Run` 或改由 `ReconnectFailed`/`ReconnectLimitReached` 触发异步补偿。
- 旧连接接收循环的迟到异常不再误报为"新连接断开"（断线声明绑定 socket 身份）；
  "旧连接已关闭 + 新连接握手失败"场景下 `Disconnected` 事件不再丢失。
- `Dispose()` 之后调用连接/发送 API 现在确定性抛出 `ObjectDisposedException`（此前为随机抛出或偶发成功）；
  内部 `SemaphoreSlim` 不再随 `Dispose` 释放（消除在途 `Release()`/租约归还的 `ObjectDisposedException` 竞态，
  未访问 `AvailableWaitHandle`，无 OS 句柄泄漏）。
- `Reconnect.MaxDelayMs` 不再自动抬升到 `Reconnect.BaseDelayMs`：非法组合改由 `Validate()` 在启动期报错
  （此前赋值结果依赖配置绑定顺序）。
- 文本消息发送与接收统一按 **UTF-8 字节**计量（新增 `MessageSizeLimits.MaxTextMessageBytes`，
  0 = 3 × `MaxTextMessageSize` 自动推导）。默认值下属**放宽**：旧"字符语义"的合法消息全部继续通过；
  二进制发送补齐此前完全缺失的 `MaxBinaryMessageSize` 校验。
- 背压前移到接收路径：并发槽位耗尽（`MaxConcurrentHandlers`，默认 32）时接收循环被阻塞以施加 TCP 反压
  （排队任务数与消息副本数一并受上界约束）。需要旧行为可设 `MaxConcurrentHandlers = 0`。
- 分片文本消息的接收上限由"1MB 字节"改为与发送侧同源（默认 3MB 字节），不再误拒"1MB 字符级"合法消息。
- 关闭握手回显服务端下发的关闭码/描述（RFC 6455 §5.5.1），不再固定 `NormalClosure`；同步 `Dispose()` 的
  关闭握手超时后会强制中止并观察残留任务异常（不再遗留无人观察的任务）。
- **指标 API 变更**：`FeishuMetrics.WebSocketConnectionObserver` / `WebSocketBacklogObserver` 两个静态可写属性**已移除**，
  改为 `FeishuMetrics.RegisterWebSocketMetricsSource(appKeyProvider, activeConnectionsProvider, pendingMessagesProvider)`
  （返回注销令牌，`Dispose` 后停止采集）。原因：静态单值属性在多应用场景互相覆盖，且长期持有已释放的服务实例；
  新形态按注册实例聚合，AppKey 由提供器每次采集时读取（支持热更新）。自定义集成请迁移到新 API。
- **主机白名单（新默认，可能影响自定义网关）**：`ConnectAsync` 新增 `AllowedHostSuffixes` 主机校验，
  默认 `*.feishu.cn;*.larksuite.com`；连接白名单之外的主机会抛 `ArgumentException`。
  连接自建代理/本地测试端点时，请把主机加入该列表（支持 `*.` 通配后缀与精确主机名，分号分隔），
  或将该项置空表示不限制。
- **二进制帧副本入池**：`FeishuWebSocketClient` 的帧私有副本改由 `ArrayPool<byte>` 提供
  （消除每帧一次的 Gen0/LOH 分配，副本以 `(buffer, 0, count)` 三元组传递并在处理完成后归还池）；
  对外 API 不变，`WebSocketBinaryMessageEventArgs.Data` 仍为按帧精确长度的独立副本。

**DTO 重命名（修复 SYSLIB1031，AOT 源生成要求）**

- `DepartmentsV1.DepartmentLeader` → `DepartmentLeaderV1`、`DepartmentDetail` → `DepartmentDetailV1`、
  `DepartmentPathInfo` → `DepartmentPathInfoV1`
- `ApprovalExternal.ApprovalCreateViewers` → `ExternalCreateViewers`；
  `Drive.Folder.FileShortcutInfo` → `FileShortcutTargetInfo`；`TasksSections.TaskSummary` → `TaskSectionSummary`
- 移除嵌套类型 `AppTableViewProperty.AppTableViewPropertyHierarchyConfig`；
  `IFeishuV2TaskSections.GetTaskSectionsPageListByIdAsync` 返回类型变更为
  `FeishuApiPageListResult<TaskSectionSummary>`

**Redis 去重**

- `RedisOptions` 非法值（0/负 TTL/空前缀）改为启动期校验失败；事件去重亚秒 `ttl` 抛异常；
  `MarkAsCompletedAsync` 不再为不存在的键创建永久记录。
- SeqID 去重键增加 `scopeKey` 隔离维度（默认 `AppKey|MachineName`），多实例不再互相判重；
  `GetCacheCount`/`GetMaxProcessedSeqId` 语义收窄为 TTL 窗口内。
- `NonceFailureMode` 仅对 Redis 连接类故障生效；服务端/配置类错误直接抛出。
- 四类 Redis 键（事件/Nonce/SeqID/令牌）统一走 `RedisKeyBuilder` 构造（分段转义 `:`，杜绝跨段碰撞）。

### ✨ 新增

- **令牌存储加密**：`EncryptedTokenStore` 系列 + `FeishuAppOptions.EnableTokenEncryption`
  （默认关闭；未注册加密提供程序时降级明文并告警，解密失败按缓存未命中处理）。
- **多应用配置热更新**：`appsettings.json` 变更按 AppKey 增量应用；`BaseUrl`/`TimeoutSeconds` 运行期热更新，
  多区域切换无需重启。
- **AOT 安全 JSON 入口**：`FeishuJsonAot`（`JsonTypeInfo` 重载）+
  `FeishuJsonDefaults.ConfigureUserResolver` 自定义 Context 注册。
- **多应用 API**：`IFeishuAuthenticationFactory` / `PerAppFeishuAuthenticationFactory`、
  `IFeishuAppManager.ConfiguredAppKeys`、`AppInstantiated` 事件、`FeishuAppContextRetirement` 退休队列、
  `TokenKeyBuilder`、`FeishuOAuthErrorClassifier`。
- **WebSocket**：并发闸门 `FeishuWebSocketConcurrencyService`（`MaxConcurrentHandlers` 默认 32）、
  真实 `backlog` 指标、按 app_key 分组连接指标、`AllowCertificateNameMismatch`、
  `ProtocolKeepAliveInterval`（默认 20s）、统一去重中间件接入、健康检查并发指标与重连熔断态、
  `AckResponse`/`SubscriptionRequest` 强类型 DTO。
- **WebSocket（本轮加固）**：`MessageSizeLimits.MaxTextMessageBytes`（字节维度上限，0=自动推导）；
  已解析帧在处理异常时补 ACK `code=500`（服务端即时重投，不再等超时）；`WebSocketBinaryMessageEventArgs.ReceiveStartTime`
  现在真实赋值（`ReceiveDurationMs` 可用）；`FeishuWebSocketClient` 的 `AppKey`/认证闸门读取支持
  `IOptionsMonitor` 热更新（其余配置项需重启，已在 XML 注释中口径化）。
- **Redis**：`RedisKeyBuilder`、`FeishuRedisException` + `FeishuRedisFailureKind` 可分类失败契约、
  `RedisOptions.ValidateOnStart()`、Cluster 全节点聚合 `GetServers()`、Testcontainers 集成测试工程。
- **文档**：`documents/ErrorHandling.md`（下载方法错误契约与「HTTP 200 + JSON 错误体」残余风险）、
  `documents/ResponseCaching.md`（多应用缓存键隔离约束）；README 多租户部署指引。

### 🐛 修复（按模块摘要）

- **多应用/令牌**：401 恢复拿到同一被拒令牌；租户重试被注入用户令牌；默认应用状态分裂；
  Lazy 异常缓存自愈失效致应用永久毒化；热更新快照并发修改 / 节流漏比对字段 / 缺 IsDefault 校验；
  旧上下文令牌维护 Timer 泄漏；HostedService 启动期强制实例化阻断宿主启动；稳态 store 恢复永不命中；
  无过期存储值编造有效期；重复 `AddFeishuApp` 双重加解密；凭据变更清库在内存后端无效；
  热更新锁内同步阻塞 IO；恢复路径 IssuedAt 缺失；指标缺 appKey 维度；
  Redis 过期令牌 TTL=0 永不过期；Redis 连接串日志含口令等。
- **JSON / AOT**：net8+ 未覆盖类型抛 `NotSupportedException`（补链尾反射兜底 + 幂等 + 加锁）；
  开放泛型误标 `[HttpJsonSerializable]`（AOT006 / SYSLIB1030）；DataModels 7 组同名 DTO 触发
  SYSLIB1031（重命名修复）；低 TFM 7486 条 AOT006 噪音豁免。
- **HTTP / 配置**：`IFeishuAuthentication` 未注册；`BaseUrl`/`TimeoutSeconds` 不参与热更新；
  per-app 弹性策略固化注册期配置；解密失败完全静默（补节流告警）；
  `FailedEventRetryService` 反序列化选项失配；204 条 NU1603 版本漂移。
- **WebSocket**：健康检查并发指标缺失、重连无熔断、配置热更新不一致、协议保活硬编码、
  未接入统一去重中间件、ACK 恒 200 致事件永久丢失。
- **Redis**：空前缀 `ClearCacheAsync` 误删全库；`redis://`/`rediss://` 地址无法连接；
  Rollback/Mark 并发竞态（Lua 原子化）；超时判定依赖客户端时钟；Sorted Set 无界增长；
  `CancellationToken` 全链路失效；Cluster 单节点覆盖；键 `:` 跨段碰撞。

<details>
<summary>内部加固与质量基建明细</summary>

- **AOT 适配**：net8.0+ 全部源工程启用 AOT / 裁剪分析器与源生成 JSON、配置绑定，配置 DTO 改由
  `Validate()` 校验；WebSocket 二进制链路改为编译期协议模型；`verify-build.ps1` 以 AOT 严格模式
  冒烟并断言 0 反射告警；`FeishuJsonAot` / `FeishuJsonDefaults.ConfigureUserResolver` 提供 AOT 安全
  JSON 入口；`Demos/Mud.Feishu.AotVerification` 双 RID 端到端验证（JSON 序列化 / HTTP 客户端 /
  事件处理 / WebSocket 协议消息）。
- **同名 DTO 冲突（SYSLIB1031）**：源生成器按类型简单名生成元数据，DataModels 7 组同名 DTO 曾编译
  报错，且其余同名类型运行时静默退化为反射兜底（AOT 下失效）；已按命名空间语义重命名（见升级须知）
  并移除重复嵌套定义。
- **依赖组件（Mud.HttpUtils）**：打包防呆（Release 打包 + 包内 DLL 逐字节校验）与清单漂移防护；
  修复多处 DI 构造歧义与用户令牌复制丢失签发时间（导致过期提前量失效）等问题；组件侧新增机器护栏
  用例，AGENTS.md / README 依赖表同步。
- **质量门禁**：测试步骤改为逐（工程，TFM）运行并逐组合断言 TRX 计数，不再依赖中文输出；CI 构建
  日志断言诊断白名单全零；移除测试过滤器使架构守卫真实生效。
- **令牌多应用加固**：用户令牌续期独立读取 refresh token；Memory / Redis 令牌键布局统一为单一真相源；
  凭据变更即清库（进程级共享记账 + Redis SCAN 全量清理，Memory 与 Redis 语义统一）；热更新两阶段
  事务化并将凭据检测与清库 IO 移出锁内；OAuth 失败按可重试性分类；应用装配失败确定性失败并释放
  scope；新增键布局单一真相源、包版本唯一、配置属性集契约等守卫测试；令牌存储加密
  （`EncryptedTokenStore` 系列）与解密失败节流告警。
- **WebSocket 加固**：事件处理失败向上传播并回 `code=500` 让飞书重发；并发闸门、重连熔断器与健康
  检查并发指标；服务端 Pong 不覆盖本地重连策略；协议保活间隔可配置；接入统一去重中间件
  （EventId + SeqID 双重去重）；连接指标按 app_key 分组；关闭握手回显服务端关闭码；帧副本改由
  `ArrayPool<byte>` 提供。
- **Redis 加固**：四类键统一经 `RedisKeyBuilder` 构造（分段转义、长度上限、空前缀防护）；Cluster
  全节点覆盖；Rollback / Mark 并发竞态 Lua 原子化；SeqID Sorted Set 写入时刷新 TTL 并裁剪；超时
  判定改用服务端 `TIME`；`CancellationToken` 全链路生效；异常可分类（`FeishuRedisException` +
  `FeishuRedisFailureKind`）；新增 Testcontainers 真实 Redis 集成测试。
- **文档**：`documents/ErrorHandling.md`（统一响应模型与下载方法错误契约）、
  `documents/ResponseCaching.md`（多应用缓存键隔离约束）；README 多租户部署指引、「能力 ↔ 实现 ↔
  测试」映射表与令牌明文存储安全披露；移除不存在的 `RedisFeishuEventDistributedDeduplicatorWithFallback`
  及降级承诺。

</details>

## [2.1.5] - 2026-06-25

### ✨ Added

- **AI 文档处理**: 新增飞书 AI 文档处理模块（智能文档处理能力，支持 17 种证件识别）
  - 添加简历信息解析接口及数据模型
  - 添加名片识别接口及数据模型
  - 添加合同字段识别接口及数据模型
  - 添加营业执照识别接口及数据模型
  - 添加增值税发票识别接口及数据模型
  - 添加驾驶证识别接口及数据模型
  - 添加食品经营许可证识别接口及数据模型
  - 添加食品生产许可证识别接口及数据模型
  - 添加身份证识别接口及数据模型
  - 添加出租车发票识别接口及数据模型
  - 添加火车票识别接口及数据模型
  - 添加车辆行驶证识别接口及数据模型
  - 添加银行卡识别接口及数据模型
  - 添加中国护照识别接口及数据模型
  - 添加台湾居民来往大陆通行证识别接口及数据模型
  - 添加港澳居民来往内地通行证识别接口及数据模型
  - 添加机动车发票识别接口及数据模型
  - 添加健康证识别接口及数据模型
  - 支持租户级别和用户级别调用
- **AI 文字识别 (OCR)**: 新增飞书基础图片 OCR 识别功能
  - 添加基础图片 OCR 识别接口及数据模型
- **AI 语音转文字**: 新增飞书语音转文字（Speech-to-Text）功能
  - 添加飞书语音文件识别接口及数据模型
  - 添加流式语音识别接口及数据模型
- **AI 翻译**: 新增飞书翻译相关接口（语言检测、文本翻译等）
- **搜索模块**: 新增飞书搜索（Search）模块
  - 添加飞书 V2 文档 Wiki 搜索接口及数据模型
  - 添加飞书 V2 套件搜索接口及数据模型
  - 添加飞书 V2 搜索数据源（DataSource）全套接口及数据模型
  - 添加搜索数据源索引管理接口（创建、批量创建等）及数据模型
  - 添加数据模式（Schema）相关 API 接口及数据模型

### 🔧 Changed

- **AI 接口命名空间**: 调整 AI 接口命名空间，统一归入 `Interfaces/AI` 目录结构
- **邮件组**: 移除邮件组接口定义（功能下架，相关 API 不再对外暴露）
- **机动车发票识别**: 重构机动车发票识别数据模型结构，新增健康证识别支持

### 🐛 Fixed

- 修复飞书消息序列化与测试用例相关问题
- **SpeechToText**: 将 `SpeechStreamConfig` 移动到独立文件，便于维护

### 🧪 Tests

- 调整测试项目目标框架与依赖配置，统一测试项目环境

### 📝 Docs

- 修正 README 示例代码中错误的配置变量名

## [2.1.4] - 2026-06-03

### ✨ Added

- **邮箱别名管理**: 新增飞书邮箱别名管理功能
  - 添加邮箱别名创建接口及数据模型
  - 添加邮箱别名查询接口及数据模型
  - 添加邮箱别名删除接口
  - 添加邮箱地址状态查询接口
- **公共邮箱管理**: 新增飞书公共邮箱管理功能
  - 添加公共邮箱创建接口及数据模型
  - 添加公共邮箱查询、更新、删除接口
  - 添加公共邮箱成员管理全套API接口与数据模型
  - 添加公共邮箱别名管理API接口与数据模型
- **邮件组管理**: 新增飞书邮件组管理功能
  - 添加邮件组创建、删除、更新、查询、列表接口及数据模型
  - 添加邮件组成员管理API（创建、删除、获取、分页列表、批量创建、批量删除）
  - 添加邮件组权限成员管理API（创建、删除、获取、批量查询）
  - 添加邮件组别名管理API（创建、删除、获取列表）
  - 添加邮件组管理员批量操作API（批量创建、批量删除、分页列表）
- **邮箱联系人管理**: 新增飞书邮箱联系人管理功能
  - 添加邮箱联系人增删改查分页管理功能
  - 支持租户级别和用户级别的调用
- **邮箱收信规则管理**: 新增飞书邮箱收信规则管理功能
  - 添加收信规则创建、删除、更新、列表、排序接口及数据模型
- **用户邮箱事件订阅**: 新增用户邮箱事件订阅功能
  - 添加订阅、获取订阅状态、取消订阅用户邮箱事件接口
- **用户邮箱邮件管理**: 新增用户邮箱邮件管理功能
  - 添加发送用户邮箱邮件接口及数据模型
  - 添加获取用户邮箱邮件详情接口及数据模型
  - 添加分页列表用户邮箱邮件接口（支持按文件夹、未读状态、标签等查询）
  - 添加按卡片获取用户邮箱邮件接口
- **邮箱文件夹管理**: 新增用户邮箱文件夹管理功能
  - 添加创建、删除、更新、获取、列表邮箱文件夹接口及数据模型
  - 添加批量删除邮箱文件夹接口
- **邮箱标签管理**: 新增用户邮箱标签管理功能
  - 添加创建、删除、更新、获取、列表邮箱标签接口及数据模型
  - 添加批量删除邮箱标签接口
- **邮件会话管理**: 新增用户邮箱会话管理功能
  - 添加获取、更新、列表、移动邮箱会话接口及数据模型
  - 添加批量删除邮件会话接口
- **邮件模板管理**: 新增飞书邮件模板管理功能
  - 添加邮件模板创建、删除、更新、获取、列表接口及数据模型
  - 添加列出可发信邮箱接口及数据模型
  - 添加邮件附件下载相关数据模型
- **日历模块**: 新增飞书日历管理功能
  - 新增租户级/用户级日历管理API
  - 新增日历ACL管理API
  - 新增日程管理API
- **视频会议**: 新增视频会议租户告警记录查询功能
  - 添加告警联系人和告警记录数据模型
  - 添加分页查询告警记录接口

### 🔧 Changed

- **模块注册**: 新增邮箱模块支持，注册邮件模块服务
- **接口命名规范**: 修正邮件组接口方法命名，统一为驼峰命名规范
  - 部分更新接口重命名为UpdateMailGroupPartialAsync
  - 全量更新接口重命名为UpdateMailGroupAsync
- **数据模型结构**: 调整邮件模板相关数据模型结构，移动到对应子目录

### 📝 Docs

- 新增飞书邮箱全套API文档
- 更新飞书视频会议API文档
- 更新日历相关API文档
- 统一API文档标题中的令牌术语
- 修正Exchange绑定接口文档与注释

## [2.1.3] - 2026-05-22

### ✨ Added

- **视频会议导出**: 新增飞书视频会议 V1 导出功能
  - 添加飞书视频会议 V1 导出接口定义
  - 添加会议列表导出 API 及数据模型
  - 添加参会人明细导出接口及数据模型
  - 添加参会人会议质量数据导出接口
  - 添加会议预约数据导出和查询 API
  - 添加下载导出文件 API 接口
- **会议室层级**: 新增飞书会议室层级管理功能
  - 添加会议室层级创建接口及数据模型
  - 添加删除会议室层级接口
  - 添加更新会议室层级接口及请求模型
  - 添加获取会议室层级详情接口
  - 添加批量查询会议室层级详情 API
  - 添加分页查询会议室层级列表 API
  - 添加搜索会议室层级 API 及数据模型
- **会议室**: 新增飞书会议室管理功能
  - 添加飞书租户视频会议室 API 接口定义
  - 添加会议室管理 API 及数据模型
  - 添加删除会议室接口
  - 添加更新会议室接口
  - 添加获取会议室详情接口及数据模型
  - 添加批量获取会议室 API 支持
  - 添加分页查询会议室列表 API
  - 添加搜索会议室 API
- **会议室配置**: 新增飞书会议室配置相关功能
  - 添加飞书会议室配置相关接口和数据模型
  - 添加创建范围配置 API 及请求模型
  - 添加获取范围配置 API 及数据模型
  - 添加获取预约配置表单 API 及数据模型
  - 添加更新预约配置表单 API 支持
  - 添加获取预约配置管理员 API 及数据模型
  - 添加更新预约配置管理员 API 支持
  - 添加获取停用状态变更通知配置 API
  - 添加更新停用通知配置 API 及数据模型
- **视频会议数据查询**: 新增飞书视频会议数据查询功能
  - 添加飞书会议数据查询 API 及数据模型
  - 添加会议质量和预约数据模型及 API

### 🔧 Changed

- **模块注册重构**: 重构模块注册逻辑，新增视频会议和认证授权模块
- **会议室预定配置**: 整理会议室预定配置的数据模型结构

### 📝 Docs

- 修复视频会议文档注释并更新接口参数

## [2.1.2] - 2026-05-11

### ✨ Added

- **视频会议录制**: 新增飞书会议录制 API 接口和相关数据模型
  - 添加会议录制 API 接口及请求模型
  - 添加会议录制 API 和相关数据模型
  - 添加设置会议录制权限 API 和相关模型
  - 添加租户级别和用户级别的会议录制接口
- **视频会议报告**: 新增视频会议报告功能
  - 添加视频会议报告相关模型和接口
  - 添加获取每日报告 API 及响应模型
  - 添加获取 Top 用户报告 API 及响应模型

### 🔧 Changed

- **HTTP 客户端增强**: 增强 FeishuHttpClient 的 HTTP 客户端选项支持

### 🧹 Chore

- 更新 Mud.HttpUtils 相关包到 1.7.1 版本

## [2.1.1] - 2026-05-11

### ✨ Added

- **视频会议**: 新增飞书视频会议 V1 模块支持
  - 新增飞书视频会议接口及实现
  - 添加预约会议相关数据模型和接口
  - 添加删除预约接口并完善预约文档
  - 添加更新预约接口及相关数据模型
  - 添加获取预约详情功能
  - 添加获取活跃会议相关数据模型和接口
  - 添加会议相关数据模型和接口
  - 添加会议分页列表接口及相关模型
  - 添加会议搜索功能及相关模型
  - 添加邀请参会人接口及相关数据模型
  - 添加移除会议用户接口及相关模型
  - 添加设置主持人接口并重构参会人管理

### 🔧 Changed

- **数据模型重构**: 将 Meeting 类重构为继承 MeetingBaseInfo
- **多应用配置重构**: 重构飞书多应用配置注册逻辑
- **视频会议重构**: 重命名预约会议相关类和方法

### 🧹 Chore

- 移除冗余的会议信息代码

## [2.1.0] - 2026-05-01

### ✨ Added

- **日历管理**: 新增飞书日历管理模块支持
  - 添加飞书日历 V4 接口定义
  - 添加日历创建相关数据模型和接口
  - 添加主日历查询接口及相关数据模型
  - 添加批量获取主日历信息功能
  - 添加获取日历信息的接口方法
  - 添加批量查询日历信息接口
  - 添加更新日历接口及数据模型
  - 增加查询日历列表接口及默认分页大小调整
  - 添加删除共享日历接口
  - 添加搜索日历接口
  - 添加日历订阅和取消订阅接口
- **日历 ACL**: 新增日历访问控制列表功能
  - 添加飞书日历 ACL 接口定义
  - 添加日历访问控制列表功能并重构相关类
  - 添加日历 ACL 创建和删除事件支持
- **日历日程**: 新增日历日程管理功能
  - 添加日历日程相关数据模型和接口
  - 添加获取日程接口及相关数据模型
  - 添加获取日程分页列表功能
  - 添加日历日程搜索功能及相关模型
  - 添加更新日程功能并重构响应模型
  - 添加租户和用户日历事件接口
  - 添加回复日程接口及请求模型
  - 添加取消订阅日程变更事件接口
  - 添加获取重复日程实例接口及响应模型
  - 添加获取日程实例视图相关数据模型和接口
- **日程参与人**: 新增日程参与人管理功能
  - 添加日程参与人相关功能接口及数据模型
  - 添加获取日程参与人列表接口
  - 添加获取日程参与群成员列表接口
- **会议群**: 新增会议群管理功能
  - 添加创建会议群接口及响应模型
  - 添加解绑会议群接口方法
  - 添加创建会议纪要和会议群响应模型及接口
- **请假日程**: 新增请假日程管理功能
  - 添加创建请假日程接口及模型
  - 添加删除请假日程接口
- **会议室**: 新增会议室相关功能
  - 添加会议室忙闲查询功能及相关数据模型
  - 添加会议室日程查询相关数据模型和接口
  - 添加会议室日程实例回复接口
  - 新增查询日历忙闲信息接口
  - 添加批量查询日历忙闲信息接口
- **Exchange 集成**: 新增 Exchange 账户绑定功能
  - 添加 CalDAV 配置生成功能
  - 添加 Exchange 账户绑定到飞书账户功能
  - 添加删除 Exchange 绑定接口
- **日历事件回调**: 新增日历相关事件回调支持
  - 添加日历变更事件类型和处理类
  - 添加日程变更事件支持
  - 添加第三方会议室日程变动事件支持
  - 添加会议室状态变更事件支持
- **多维表格**: 添加飞书多维表格工作流接口定义

### 🔧 Changed

- **日历模块重构**: 重构日历模块文件结构，将数据模型按功能分类整理
- **Exchange 重构**: 重命名 Exchange 绑定响应模型并添加查询接口
- **云文档重构**: 重命名评论接口以匹配驱动模块命名规范

### 📦 Build & Config

- 更新所有 demo 项目中的 Mud.Feishu 相关包至 2.1.3 版本
- 更新项目版本号至 2.1.3

### 📝 Docs

- 新增多维表格、电子表格和画板模块文档
- 添加飞书云文档相关接口文档
- 更新多个 README 文档，完善功能说明和配置示例

## [2.0.9] - 2026-04-24

### ✨ Added

- **云文档权限管理**: 新增飞书云文档权限管理功能
  - 添加云文档协作者权限相关模型和接口
  - 添加批量增加协作者权限接口
  - 添加删除云文档协作者权限接口
  - 添加云文档所有者转移功能
  - 添加判断用户云文档权限接口
  - 添加获取云文档权限设置接口
- **云文档密码**: 新增云文档密码功能支持
  - 添加云文档密码设置功能
  - 添加云文档密码刷新和停用接口
- **云文档订阅**: 新增云文档文件订阅功能
  - 添加文件订阅功能相关接口和模型
  - 添加更新文件订阅状态的接口
  - 更新用户订阅接口
- **云文档评论**: 新增飞书云文档评论功能
  - 添加飞书文档评论相关数据模型和接口
  - 支持批量获取评论
  - 添加解决/恢复评论接口
  - 添加文件评论表情回复支持
  - 添加全文评论接口及模型
  - 添加创建文件评论回复的接口
  - 添加更新和删除文件评论回复的接口
  - 添加评论表情回应接口
- **画板 (Board)**: 新增飞书画板功能模块
  - 添加画板主题接口及相关数据模型
  - 添加画板缩略图下载和语法解析接口
  - 添加创建画板节点接口及响应模型
  - 添加画板 V1 租户和用户接口
- **事件回调**: 更新事件处理器以使用新的事件回调模块

### 🔧 Changed

- **画板重构**: 重构画板相关数据模型和接口，优化模型文件结构
- **评论重构**: 统一云文档评论响应模型类名以 Result 结尾，重构评论接口支持批量获取
- **去重器重构**: 重构缓存清理逻辑并添加异步清空方法

### 🐛 Fixed

- 修复 RedisFeishuSeqIDDeduplicator 同步方法阻塞问题

### 📦 Build & Config

- 更新多个演示项目的依赖包版本至 2.0.8
- 更新项目版本号至 2.0.9

### 📝 Docs

- 更新云文档评论接口文档，添加分页获取回复方法和租户/用户实现说明

## [2.0.8] - 2026-04-12

### ✨ Added

- **EventCallback 项目**: 新增独立的事件回调处理项目
  - 重构数据模型结构，将原有抽象层的数据模型迁移至 EventCallback 项目
  - 添加项目文档说明
- **飞书 V2 事件头支持**: 添加对飞书 v2.0 事件 Header 的强类型支持
  - 支持飞书 V2 事件头解析
  - 统一事件处理器默认 Header 类型
- **云文档事件订阅**: 新增飞书云文档事件订阅功能
  - 添加云文档事件订阅接口
  - 添加云文档事件订阅状态查询功能
  - 添加取消用户云文档事件订阅接口
  - 添加查询用户云文档事件订阅状态功能
- **云文档事件回调**: 新增云文档相关事件支持
  - 添加文件已读和文件编辑事件支持
  - 添加文件协作者权限申请和添加事件支持
  - 添加文档协作者移除事件支持
  - 添加文件删除和回收站事件支持
  - 添加文件评论新增事件支持
- **多维表格自动化流程**: 新增自动化流程相关功能
  - 添加自动化流程相关接口及数据模型
  - 添加更新自动化流程状态接口支持
  - 添加获取工作流列表接口
  - 添加多维表格字段和记录变更事件支持
- **审批订阅**: 添加飞书 V4 审批订阅接口
- **多应用隔离**: 实现多应用隔离的事件处理机制
- **配置验证**: 添加配置选项验证器并集成到 DI 容器
- **WebSocket 增强**:
  - 添加配置验证器并优化消息处理性能
  - 添加心跳超时事件并优化连接管理
  - 重构 WebSocket 客户端并添加心跳管理和消息队列功能
- **中间件**: 添加请求耗时日志记录并优化签名验证配置

### 🔧 Changed

- **命名空间重构**: 重构命名空间结构并更新发布脚本
  - 重命名事件处理命名空间并迁移事件类型常量
  - 更新 HandlerNamespace 路径
- **事件处理器重构**:
  - 更新事件处理类并迁移 EventData 文件
  - 更新事件结果类的 HeaderType 配置
  - 更新事件处理类并修正文档链接
  - 移除冗余构造函数
- **Webhook 重构**:
  - 重构签名验证逻辑并扩展应用配置选项
  - 重构多应用键上下文传播机制
  - 移除 ISetAppKeyAware 接口及相关实现
- **配置验证重构**: 重构配置验证逻辑并移除数据注解
- **Drive 重构**: 重构云文档订阅相关模型文件路径
- **代码清理**: 清理冗余代码，移除未使用的命名空间引用

### 🐛 Fixed

- 修复并发测试中的变量捕获问题并简化 .gitignore

### 🧪 Tests

- 添加 FeishuWebhook 配置验证的集成测试和单元测试
- 调整重试延迟测试范围以适应 CI 环境

### 📦 Build & Config

- 更新依赖包版本至 1.7.0 并调整项目引用结构
- 更新 Mud.HttpUtils 依赖至 1.6.6 版本
- 删除 appsettings.example.json 配置文件

### 📝 Docs

- 添加类和方法注释以提升代码可读性
- 更新接口文档链接

## [2.0.7] - 2026-04-07

### ✨ Added

- **WebSocket 增强**: 全面增强 WebSocket 连接管理和错误处理
  - 新增消息处理状态跟踪功能
  - 添加重连状态重置功能
  - 实现重连协调器，优化重连逻辑和心跳间隔
  - 增强连接管理，支持更完善的错误处理机制
- **Webhook 增强**: 新增异步验证方法和 IP 白名单支持
  - 添加异步验证方法以提升性能
  - 支持 IP 白名单配置，增强安全性
- **分布式去重服务重构**: 重构分布式去重服务，支持状态机和事件处理生命周期
  - 实现状态机模式，支持 Processing → Completed 状态转换
  - 添加事件处理超时和回滚机制
  - 支持处理状态跟踪和异常恢复
- **指标收集优化**: 使用并发集合优化指标收集性能
  - 采用 `ConcurrentDictionary` 替代传统锁机制
  - 提升高并发场景下的性能表现
- **工具类扩展**: 添加 HttpClient 扩展方法用于 JSON 序列化配置
  - 新增 `ConfigureJsonSerializerOptions` 扩展方法
  - 简化 JSON 序列化配置流程

### 🔧 Changed

- **Token 管理重构**: 将令牌格式化逻辑提取到 TokenUtils 工具类
  - 统一 `FormatBearerToken` 和 `RemoveBearerPrefix` 方法
  - 消除代码重复，提升代码可维护性
- **签名验证优化**: 移除废弃方法并优化签名验证逻辑
  - 清理过时的验证方法
  - 简化验证流程，提升性能
- **WebSocket 重构**: 重构 WebSocket 相关类的命名空间
  - 统一命名空间结构
  - 优化代码组织
- **测试重构**: 重构订阅验证测试以使用加密密钥提供者
  - 更新测试用例以适配新的验证器架构
  - 提升测试覆盖率和代码质量

### 🐛 Fixed

- 修复大量空引用警告，提升代码健壮性
- 修复 `FeishuWebhookService` 异步方法未等待的问题
- 修复重连逻辑中的心跳间隔问题

### 🧪 Tests

- 新增 WebSocket 核心功能测试
  - 添加 Bug 修复验证测试
  - 新增指数退避重连策略测试
  - 添加重连协调器测试
- 新增去重服务测试
  - 添加统一去重中间件测试
  - 新增配置选项测试
  - 添加降级告警服务测试
- 新增指标收集测试
  - 优化并发性能测试用例

### 📦 Build & Config

- **CI/CD 增强**: 增强发布流程并支持多版本 .NET 构建
  - 支持 .NET 6.0、8.0、10.0 多版本并行构建
  - 优化构建和测试配置

## [2.0.6] - 2026-04-05

### ✨ Added

- **多维表格高级权限**: 新增飞书多维表格高级权限管理功能
  - 新增高级权限接口及相关数据模型
  - 添加角色成员管理相关接口及模型
  - 新增批量添加协作者接口及请求模型
  - 添加自定义角色查询、更新和删除接口
- **多维表格仪表盘**: 新增飞书多维表格仪表盘功能
  - 新增仪表盘接口定义
  - 添加仪表盘复制功能及相关数据模型和接口
- **多维表格表单**: 新增表单管理功能
  - 添加表单字段更新相关模型和接口
  - 新增更新表单元数据接口及相关模型
  - 添加表单升级接口及相关数据模型
- **多维表格字段**: 新增字段管理功能
  - 添加创建字段编组功能接口及模型
  - 新增删除字段接口及响应模型
  - 添加更新字段接口
- **多维表格记录**: 新增记录批量操作功能
  - 添加批量新增记录接口及相关数据模型
  - 新增批量获取记录接口及相关模型
  - 添加批量删除记录接口及相关模型
  - 新增删除记录接口及响应模型
- **用户认证**: 添加飞书用户认证中间件及相关服务配置

### 🔧 Changed

- **授权机制重构**: 统一使用 Token 属性声明授权方式
  - 将 Token 属性从 TokenType 枚举改为字符串类型
  - 移除接口中的 Header 属性，统一使用 Token 属性设置授权
- **多维表格重构**: 优化模型和接口结构
  - 重构角色相关请求模型文件结构
  - 重构表单字段相关接口及模型
  - 重构字段操作请求模型并优化目录结构
  - 重构记录操作请求模型并更新接口
- **文档接口**: 将文档块接口移动到主命名空间
- **演示项目**: 将演示项目从 NuGet 包引用改为本地项目引用

### 🐛 Fixed

- 修正 Bitable 注册方法名称
- 更新依赖版本并添加请求体加密方法

### 📚 Documentation

- 更新 NuGet 包下载量徽章样式

### 🧪 Tests

- 添加多维表格接口的单元测试

### 📦 Build & Config

- 更新项目版本至 2.0.6 并调整 token 管理器参数
- 更新演示项目依赖项，添加飞书认证包

## [2.0.5] - 2026-03-27

### ✨ Added

- **电子表格**: 新增飞书电子表格管理模块支持
  - 工作表管理：创建、查询、更新、删除工作表
  - 行列操作：插入、更新、移动、删除行列
  - 单元格功能：合并、查找、样式设置、数据读写
  - 筛选功能：筛选视图创建与管理、筛选条件设置
  - 数据保护：保护范围设置与管理
  - 数据验证：验证规则设置与管理
  - 条件格式：格式规则创建与管理
  - 浮动图片：图片插入、查询、删除
- **多维表格**: 新增飞书多维表格管理模块支持
  - 多维表格元数据获取与更新
- **用户认证**: 新增飞书用户认证模块
  - 用户认证中间件和上下文服务
  - 用户认证测试项目
- **演示项目**: 新增飞书任务管理演示项目

### 🔧 Changed

- **电子表格重构**: 重构飞书电子表格相关模型和接口
  - 重构飞书表格相关模型和接口
  - 调整飞书电子表格接口命名空间并新增租户和用户接口
  - 重构单元格操作相关模型和接口
  - 重命名单元格相关类以更准确描述功能
  - 重构筛选条件相关模型和接口
  - 统一筛选视图响应模型并添加获取接口
  - 重构飞书电子表格数据验证接口
- **认证重构**: 重构用户令牌管理器和依赖项
  - 将 token 格式化方法移至 TokenUtils 工具类
- **Webhook 重构**: 重构验证器基类与工具类
  - 使用环境服务替代直接环境变量访问
- **测试优化**: 使用环境服务模拟替换环境变量设置
- **项目优化**: 移除冗余的 EnsureUserContext 调用并调整项目引用

### �📚 Documentation

- 更新 README 文档说明
- 更新依赖项文档和版本说明
- 更新接口文档注释并添加数据保护相关模型
- 完善 xml 注释

### 📦 Build & Config

- 更新前端依赖版本
- 更新依赖包版本至 10.0.5 并添加认证测试

## [2.0.4] - 2026-03-19

### ✨ Added

- **文档管理**: 新增飞书文档管理模块支持
  - 添加飞书文档接口及数据模型
  - 新增文档块模型及接口支持
  - 添加获取和批量更新文档块接口及模型
  - 新增获取文档块子块分页列表接口
  - 添加批量删除块功能及相关模型
  - 新增内容转换功能支持
- **知识库**: 新增知识库管理功能
  - 添加知识空间和节点管理接口
  - 新增节点标题更新和复制功能
  - 添加节点管理接口和测试
  - 新增移动云文档至知识空间功能
- **云盘管理**: 新增飞书云盘管理 API 服务支持
  - 添加云空间文件夹相关接口及数据模型
  - 新增云空间文件接口及元数据模型
  - 添加文件版本管理功能
  - 新增文件上传功能支持
  - 添加文件导入导出任务相关数据模型和接口
  - 新增创建文件快捷方式相关模型和接口
  - 添加搜索云文档功能及相关数据模型
  - 新增获取云文档点赞者列表接口
- **任务模块**: 添加任务和卡片模块并重构服务注册逻辑
- **认证增强**: 为 FeishuAuthenticationException 添加可恢复标志构造函数
- **枚举配置**: 将枚举和配置类提取到独立文件

### 🔧 Changed

- **项目重构**: 重构请求和响应模型的文件结构
  - 重构文档块模型文件结构，将块模型移动到 Common 目录并删除原 Blocks 目录
  - 重构文件操作接口及模型命名
  - 重构代码以优化文件上传请求模型和 HTTP 客户端
- **测试优化**: 统一测试中数据模型命名空间并优化测试代码
- **数据库优化**: 为收藏节点添加索引优化查询性能

### 🐛 Fixed

- **消息发送**: 修复发送 Multipart/form-data 请求时 request.Content 的值问题
- **Webhook**: 修复飞书回调配置通不过的问题，在 FeishuEventDecryptor 中指定 EventType

### 📦 Build & Config

- **依赖更新**: 更新多个项目的依赖版本

### 🧪 Tests

- **新增测试**: 添加飞书 Webhook 模块的单元测试

## [2.0.3] - 2026-02-26

### ✨ Added

- **考勤管理**: 新增完整的考勤管理功能
  - 休假发放记录相关数据模型和接口
  - 打卡流水记录相关模型和接口
  - 考勤归档报表相关接口及数据模型
  - 补卡相关数据模型和接口
  - 用户人脸照片上传接口
  - 用户设置查询接口
  - 考勤审批相关数据模型及接口
  - 审批状态更新接口及数据模型
  - 审批结果写入功能及数据模型
  - 考勤统计相关数据模型和接口
  - 查询统计数据相关模型和接口
  - 查询统计表头接口及请求模型
  - 用户人脸识别信息管理相关接口和模型
  - 考勤组相关接口及数据模型
- **审批功能**: 重构审批实例预览功能
- **卡片功能**: 更新卡片元素请求字段名
- **任务功能**: 飞书任务自定义字段更新接口返回类型

### 🔧 Changed

- **项目结构**: 重构项目结构，调整命名空间和文件位置
- **接口规范**: 统一接口方法命名规范
- **考勤接口**: 重构考勤组接口继承结构，重命名考勤用户管理接口并添加下载功能
- **令牌管理**: 重构令牌管理模块并更新依赖版本
- **国际化**: 重构国际化资源模型并更新相关引用
- **测试结构**: 重构测试文件目录结构，将相关测试文件移动到对应功能模块目录下
- **Demo 项目**: 更新 demo 项目以使用包引用而非项目引用
- **响应模型**: 重构响应模型

### 🐛 Fixed

- **用户组成员**: 修正类名拼写错误并更新相关引用
- **测试用例**: 修复测试用例并完善测试数据
- **活动订阅**: 修正活动订阅接口返回类型并完善测试用例

### 📚 Documentation

- **代码注释**: 为测试类和缓存方法添加 XML 注释
- **README**: 更新 README 文档结构和内容

### 📦 Build & Config

- **依赖更新**: 更新 Mud.HttpUtils 依赖至 1.5.3 版本
- **构建配置**: 忽略测试项目的构建输出目录

### 🧪 Tests

- **完善测试用例**: 为多个模块添加和完善测试用例
  - 聊天群组和标签页测试
  - 飞书任务单元测试
  - 飞书用户和用户组接口测试
  - 飞书职位和职级接口测试
  - 员工类型相关接口测试
  - 飞书部门接口测试
  - 任务板块相关测试
  - 任务列表接口测试
  - 飞书任务附件和评论测试
  - 群公告相关接口测试
  - 卡片接口测试
  - 飞书审批任务相关测试
  - 审批查询接口测试
  - 审批评论相关测试
  - 飞书审批相关接口测试
  - 审批消息接口测试

## [2.0.2] - 2026-01-30

### ✨ Added

- **性能指标监控**: 添加完整的性能指标收集功能
  - 新增 MeterExtensions、FeishuMetrics 和 FeishuMetricsHelper 类
  - 在 TokenManager、HttpClientUtils 等关键组件中添加指标记录
- **WebSocket 和 Webhook 指标**: 为 WebSocket 和 Webhook 添加专项指标监控
  - WebSocket 连接数统计和认证/事件处理指标
  - Webhook 签名验证、事件解密和处理指标
  - 事件去重命中指标
  - 支持 WebSocketConnectionCountProvider 获取实时连接数
- **调试日志增强**: 增加响应内容调试日志和错误处理
  - 添加调试日志记录原始响应内容
  - 在 JSON 反序列化失败时提供更详细的错误信息
- **测试控制器**: 新增日志测试控制器和网络测试控制器

### 🔧 Changed

- **依赖更新**: 更新 Mud.HttpUtils 依赖至 1.5.2 版本
- **HTTP 客户端优化**:
  - 增强异常处理和日志记录
  - 改进安全配置和连接池设置
- **认证优化**: 优化令牌获取逻辑并移除冗余指标记录
  - 改为直接从缓存获取令牌时记录指标
- **配置简化**: 移除断路器功能及相关代码
  - 移除断路器配置项和文档说明
  - 简化代码结构和 Webhook 服务
- **时间戳验证**: 优化时间戳验证逻辑，优先使用应用特定配置
- **Demo 项目**: 调整 Demo 项目的配置和依赖

### 🐛 Fixed

- **JobLevel 接口**: 将 JobLevel 接口的 name 参数改为可空类型
- **JobFamilies 接口**: 允许 GetJobFamilesListAsync 的 name 参数为 null

### 📚 Documentation

- 更新 README 文档说明指标使用方式
- 添加日志测试相关文档

### 📦 Build & Config

- 更新项目版本至 2.0.2

## [2.0.1] - 2026-01-28

### 🚨 BREAKING CHANGE

- **移除 FeishuOptions 类** - 完全移除旧的配置类，所有场景统一使用 `FeishuAppConfig`
- **多应用架构支持** - API 签名和配置方式发生变化，需要迁移
- **配置系统重构** - 重试机制和 Token 管理配置方式改变

### ✨ Added

- **多应用支持**: 支持配置和管理多个飞书应用
- **应用上下文切换**: 新增 `IFeishuAppContextSwitcher` 接口支持运行时切换应用
- **自动推断默认应用**: 三种自动推断规则简化配置
- **应用级 Token 缓存隔离**: 每个应用拥有独立的 Token 缓存
- **新配置参数**: 新增 `RetryDelayMs` 和 `TokenRefreshThreshold` 配置
- **文档**: 新增配置迁移指南和多应用配置文档

### 🔧 Changed

- **重构 HttpClient 配置**: Polly 重试策略使用配置参数
- **重构 Token 重试逻辑**: 使用配置的 `RetryDelayMs` 替代硬编码值
- **WebSocket 重试**: 使用配置的 `RetryDelayMs` 替代硬编码值
- **依赖更新**: 替换代码生成器为 `Mud.HttpUtils`

### 🐛 Fixed

- **修复 `RetryCount` 配置不一致问题**: 统一应用到 HttpClient 和 TokenManager
- **WebSocket 重试硬编码问题**: 使用配置参数替代硬编码值

### 📚 Documentation

- 新增配置迁移指南
- 更新示例配置文件
- 新增多应用配置文档

---

## [1.2.2] - 2026-01-19

### ✨ Added

- **考勤管理 API**: 新增完整的考勤班次管理能力
- **审批功能**: 新增审批消息 API 和审批任务查询接口
- **演示项目**: 新增飞书 OAuth 登录演示项目

### 🐛 Fixed

- **解密失败处理**: 修复解密失败时空引用问题
- **令牌管理**: 修复用户令牌管理及状态清理问题
- **项目文件**: 修复 PackageTags 标签重复闭合问题
- **Webhook 中间件**: 修复验证请求属性和缩进问题

### 🔧 Changed

- **模型重构**: 班次模型提取公共基类
- **认证服务**: 重构认证服务和接口命名
- **项目结构**: 移动项目文件到 Sources 文件夹
- **工具类**: 重组异常和 HTTP 客户端工具
- **Redis 性能**: 使用 SCAN 替代 KEYS 命令
- **缓存管理**: 优化令牌缓存和格式化逻辑
- **代码清理**: 移除未使用变量和多余引用

### 📚 Documentation

- API 文档更新和完善
- 项目文档链接和版本号更新
- 演示项目文档优化和重组

### 📦 Build & Config

- 版本更新至 1.2.2
- 依赖管理优化
- Git 配置安全加固

---

## [1.2.1] - 2026-01-16

### ✨ Added

- **配置验证**: 新增 AppId、AppSecret、EncryptKey 格式和长度验证
- **敏感信息保护**: 配置类添加敏感信息掩码功能
- **示例配置**: 新增完整的 appsettings.example.json 文件

### 🔒 Security

- **数据注解验证**: 添加 Data Annotations 属性验证必填字段
- **Redis 配置验证**: 新增 ServerAddress 格式和连接参数验证

### 🧪 Tests

- **配置单元测试**: 全面覆盖 AppId/AppSecret 验证、敏感信息掩码等

### 📚 Documentation

- **XML 文档**: 完善配置类参数说明和示例值

### ⚠️ BREAKING CHANGE

- 修改了 `FeishuAppConfig.Validate()` 方法验证规则
- 添加了 `FeishuWebhookOptions.EncryptKey` 长度验证（32 字符）
- 添加了 `RedisOptions.Validate()` 方法验证连接参数

---

## [1.1.2] - 2026-01-10

### ✨ Added

- **Webhook/WebSocket**: 异步验证、重试功能和请求体签名验证
- **审批功能**: 第三方审批实例验证、同步和状态分页接口
- **配置验证**: WebSocket 和 Feishu 配置选项验证

### 🐛 Fixed

- **WebSocket 配置**: 修复配置相关问题

### 🔧 Changed

- **事件处理器**: 调整为异步处理器设计
- **Redis 服务**: 简化服务注册方法
- **服务注册**: 统一方法命名规范

### 📚 Documentation

- **英文文档**: 重构文档结构和内容组织

### 📦 Dependencies

- 更新项目依赖包至 1.1.2 版本

---

## [1.1.1] - 2026-01-06

### ✨ Added

- **审批功能**: 新增多种审批相关接口和常量
- **任务管理**: 自定义字段管理和选项管理功能
- **API 响应**: 统一为 FeishuApiResult 格式

### 🔧 Changed

- **接口设计**: 统一消息发送接口
- **服务注册**: 统一服务注册 API

### 📚 Documentation

- **README**: 优化项目描述和功能说明
- **架构文档**: 移除过时的架构设计文档

---

## [1.1.0] - 2025-11-12

### ✨ Added

- **用户管理**: 完整的用户 CRUD 操作接口
- **用户组管理**: 用户组创建、更新、删除接口
- **部门管理**: 完整的部门管理功能
- **员工类型管理**: 员工类型相关接口

### 🔧 Changed

- **API 结果模型**: 重构为 FeishuApiResult 命名
- **服务注册**: 优化服务注册代码结构

### 📚 Documentation

- **README**: 更新内容和结构
- **功能说明**: 新增项目功能说明和使用示例

---

## [1.0.9] - 2025-11-14

### ✨ Added

- **跨平台支持**: 支持.NET Standard 2.0
- **HTTP 客户端**: 新增飞书 HTTP 客户端扩展方法

### 🐛 Fixed

- **HttpClient 配置**: 修复配置和 API 端点 URL 格式问题

### 🔧 Changed

- **消息和事件 API**: 重构实现以提高可维护性
- **文件下载**: 优化 HTTP 请求方法提高性能

---

## [1.0.7] - 2025-11-12

### ✨ Added

- **任务管理**: 任务评论、附件、活动订阅和成员管理接口
- **JsTicket API**: 新增 JsTicket API 接口支持前端开发

### 🔧 Changed

- **代码生成器**: 升级 Mud.ServiceCodeGenerator 版本
- **依赖管理**: 优化项目依赖配置

### 📚 Documentation

- **任务文档**: 更新任务成员信息注释

---

## [1.0.3-dev] - 2025-11-12

### ✨ Added

- **基础框架**: 飞书 API 基础框架搭建
- **认证服务**: 认证服务和令牌管理
- **通讯录**: 企业通讯录相关接口
- **消息功能**: 消息发送和接收功能
- **事件支持**: Webhook 和 WebSocket 支持
- **演示项目**: Webhook 和 WebSocket 演示项目

### 📚 Documentation

- **项目文档**: 初始 README 和项目结构说明
