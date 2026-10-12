# Mud.Feishu.AI.Tools `Tools/` 目录重组方案

> 角色：系统架构师评审稿　|　范围：`Mud.Feishu.AI.Tools/Tools/`（44 个 `.cs` 文件）
> **本次不动代码**：本方案只定义"做什么、为什么、怎么落地、如何验证"，不实施任何改动。
> 核心承诺：重组是**纯物理搬迁 + 命名空间迁移**，类型名、成员名、行为、生成器契约、golden 快照全部不变。

---

## 一、现状盘点（事实基线，全部经代码级核实）

### 1.1 目录实况

`Mud.Feishu.AI.Tools` 项目当前目录组织：

| 目录 | 文件数 | 命名空间 | 性质 |
|---|---|---|---|
| `Tools/` | **44 cs** | `Mud.Feishu.AI.Tools`（根，扁平） | **运行时基础设施（本次重组对象）** |
| `Curation/` | 27 cs | `Mud.Feishu.AI.Tools.Curation` | 人工策展的工具契约声明（`*ToolInterfaces.cs`，162 个契约接口） |
| `Internal/` | 43 cs | `Mud.Feishu.AI.Tools.Internal` | 分域执行器（BitableTools/DocxTools/…） |
| `Handlers/` | 1 cs | `Mud.Feishu.AI.Tools.Handlers` | `[FeishuToolHandler]` 特性 |
| `Registration/` | 1 cs | `Mud.Feishu.AI.Tools.Registration` | 域注册器契约 + 登记助手 |
| `SdkProfile/` | 1 cs | `Mud.Feishu.AI.Tools.SdkProfile` | 生成剖面（`FeishuToolProfile`） |
| `Extensions/` | 1 cs | `Mud.Feishu.AI.Tools`（根） | DI 入口 `FeishuToolsServiceCollectionExtensions` |
| `Guidance/`、`Affordance/` | 39 md / 16 md | — | 生成器 AdditionalFiles 资产（路径即契约，不动） |
| `Channels/`、`Events/`、`Knowledge/` | 0 | — | 空目录（R-9 后内容已并入 Mud.Feishu.AI） |

### 1.2 问题诊断（为什么"凌乱"）

1. **单目录 44 个文件、9 类关注点混居**：入参读取、出站裁剪、错误分类、执行链、MAF 桥、目录导出、宿主钩子、安全基元、领域解析、常量表全部平铺。按文件名首字母排序后，`Tool*` 前缀 27 个文件连成一片，认知负荷极高。
2. **与项目自身约定冲突**：全项目除 `Tools/` 与 `Extensions/` 外，所有目录都遵守"**目录 = 子命名空间**"（`Curation/` 甚至有守卫 `CurationFiles_ShouldDeclareTheCurationNamespace` 机械锁定）。`Tools/` 是唯一把 44 个文件压在**根命名空间**的目录——它是 R5/F-1「载体 C」迁移（契约声明迁去 `Curation/`）后**遗留的半成品**：声明面搬走了，基础设施面原样留在原地。
3. **公有面与内部实现不可区分**：22 个 public 类型（`FeishuToolRegistry`、`ToolError`、`IToolCatalog`、4 个宿主钩子及其记录类型……）与 20 个 internal 实现类混在同一目录，读者无法快速回答"哪些是产品面"。
4. **导航反模式**：`FeishuToolBinding.cs`（执行链）与 `FeishuSkillDocumentBuilder.cs`（Skills 导出）相距 0 行却毫无关系；`BitableFilterParser.cs`（被 `Internal/BitableTools` 消费）孤悬在根。

### 1.3 44 个文件职责分类（重组输入）

| 关注面 | 文件 | 关键类型 | 可见性 |
|---|---|---|---|
| 入参面 | `ToolArgs.cs` | 取值门面 | internal |
| | `ToolArgumentNormalizer.cs` | JsonElement 形态认知**唯一中心**（守卫白名单按文件名锁定） | internal |
| | `ToolArgumentSanitizer.cs` | 入站净化 | internal |
| | `ToolArgsDigester.cs` | 审计摘要（敏感键掩码） | internal |
| | `ToolArgumentException.cs` | 参数异常 | internal |
| 出站面 | `FeishuToolResult.cs` | 公开结果 DTO | **public** |
| | `IToolResultShaper.cs` | 宿主整形钩子 | **public** |
| | `ToolResultPipeline.cs` | 出口编排 + `ToolErrorFactory` | internal |
| | `ToolResultJson.cs` | 统一序列化器（中文不转义） | internal |
| | `ToolResultJsons.cs` | 单页信封构造（唯一） | internal |
| | `ToolResultText.cs` | 截断 | internal |
| | `ToolResultSanitizer.cs` | 脱敏/控制字符剥离 | internal |
| | `ToolResultContentSafety.cs` | 注入载荷检测 | internal |
| | `ToolDryRun.cs` | 写操作预演 | internal |
| | `ToolPagination.cs` | fetch_all 聚合翻页 | internal |
| 错误面 | `ToolError.cs` | 公开错误记录 | **public** |
| | `ToolErrorClassifier.cs` | 8 类闭集 + subtype | internal |
| | `ToolErrorPayloadSerializer.cs` | ≤512B JSON 首行载荷 | internal |
| | `IToolErrorTextFormatter.cs` | 接口 + 内置实现同文件 | **public**（接口） |
| | `ToolRetryPolicy.cs` | 只读重试/写零重试退避 | internal |
| 执行链 | `FeishuToolBinding.cs` | **护城河执行链**（授权→租户切换→调用→归一） | internal |
| | `FeishuToolAIFunction.cs` | MEAI `AIFunction` 桥 + `FeishuToolsToolSource` | internal |
| | `ToolBudgetAIFunction.cs` | 预算装饰器 | internal |
| | `IFeishuToolFunctionFactory.cs` | 工厂接口 + 实现同文件 | **public**（接口） |
| | `UserTokenPrecheck.cs` | 用户令牌就绪探测（含 `UserTokenPrecheckModes`） | internal |
| | `UserTokenPrecheckResult.cs` | 三态结果 | **public** |
| 目录/导出 | `FeishuToolRegistry.cs` | 注册表 + `FeishuToolDefinition` + `FeishuToolHandler` 委托 | **public** |
| | `FeishuToolCatalog.cs` | `IToolCatalog` 实现 + Schema 导出器 | internal |
| | `IToolCatalog.cs` | `ToolCatalogEntry`/`ToolSchemaDialect`/`IToolSchemaExporter`（一文件四类型） | **public** |
| | `ToolSchemaJson.cs` | 信封→纯参数 Schema 唯一转换点 | internal |
| | `ToolAffordanceText.cs` | per-tool affordance 渲染 | internal |
| | `FeishuSkillDocumentBuilder.cs` | Agent Skills 产物构造（纯函数） | internal |
| | `ISkillDocumentReader.cs` | 接口 + 内置实现同文件 | **public**（接口） |
| 宿主钩子 | `IToolExecutionAuthorizer.cs` | 授权门禁 + `AuthorizationDecision`/`AuthorizationResult` | **public** |
| | `IToolExecutionAuditSink.cs` | 审计 sink + `ToolExecutionAuditRecord` | **public** |
| | `IFeishuAttachmentStager.cs` | 附件落盘 + `AttachmentSource`/`StagedAttachment` | **public** |
| | `IFeishuUserAuthorizationLinkProvider.cs` | 用户授权链接 | **public** |
| 安全基元 | `SecurityTextPrimitives.cs` | 敏感键/控制字符/正则超时**单源**（入站+出站共用） | internal |
| | `DownloadedContentGuard.cs` | "错误体伪装成文件"防线 | internal |
| 领域解析 | `BitableFilterParser.cs` | filter 文法解析（被 `Internal/BitableTools` 消费） | internal |
| | `BitableSortParser.cs` | sort 文法解析 | internal |
| 常量表 | `PageSizes.cs`、`ReceiveIdTypes.cs`、`TranslationLanguages.cs` | 分页上限/receive_id 类型/语言码 | internal |

---

## 二、重组原则（架构约束，先于目录设计）

1. **目录 = 命名空间**：与 `Curation/Internal/Handlers/Registration/SdkProfile` 对齐，每个新目录声明 `Mud.Feishu.AI.Tools.{Dir}`，由新守卫机械锁定（复用 `CurationFiles_ShouldDeclareTheCurationNamespace` 模式）。
2. **公有面可一眼定位**：所有"宿主实现的公开钩子"收敛到单一 `Hosting/`；公开 DTO 随其关注面走，但在文档与 `PublicAPI.Unshipped.txt` 保持全量登记。
3. **共享基元中立位**：`SecurityTextPrimitives` 被入站（`ToolArgumentSanitizer`）与出站（`ToolResultSanitizer`）**双向**消费（其 XML 注释明确"两侧键集差异是有意的"），必须放在中立目录，不得下沉到 `Args/` 或 `Results/` 任一侧。
4. **生成器零感知**：源生成器（上游 `Mud.HttpUtils.Generator` 3.0.6 的 ToolSurface 引擎）只认特性与剖面槽（`ResultTypeName="FeishuToolResult"`、`BindingTypeName="FeishuToolBinding"` 等**简单名**），与源文件目录无关；生成产物继续落在 `Mud.Feishu.AI.Tools` / `.Generated` / `.Registration` 命名空间，**不受重组影响**。简单名解析经项目级 `global using` 保持（见 §5.1）。
5. **golden 与资产不动**：`FeishuToolSchemas.golden.txt`、`Guidance/**`、`Affordance/**`、`skills/` 全部与目录重组正交，不触碰。
6. **守卫优先于习惯**：凡文件名/路径被守卫或测试硬编码的（§5.3 共 7 处），必须在同一变更集内同步更新，否则 CI 红；凡守卫**扫描面**覆盖 `Tools/` 的，必须同步扩展扫描面，防止"假绿"。

---

## 三、目标结构

`Tools/` 解散，在项目根下与 `Curation/Internal/…` 平级新增 **9 个关注面目录**：

```
Mud.Feishu.AI.Tools/
├── Args/          Mud.Feishu.AI.Tools.Args         入参面（5）
├── Results/       Mud.Feishu.AI.Tools.Results      出站面（10）
├── Errors/        Mud.Feishu.AI.Tools.Errors       错误面（5）
├── Execution/     Mud.Feishu.AI.Tools.Execution    执行链与 MAF 桥（6）
├── Catalog/       Mud.Feishu.AI.Tools.Catalog      注册表/目录/导出（7）
├── Hosting/       Mud.Feishu.AI.Tools.Hosting      宿主扩展点·公开钩子（4）
├── Security/      Mud.Feishu.AI.Tools.Security     共享安全基元（2）
├── Parsers/       Mud.Feishu.AI.Tools.Parsers      领域 DSL 解析（2）
└── Constants/     Mud.Feishu.AI.Tools.Constants    常量表（3）
```

### 3.1 完整迁移映射表（44/44）

| # | 文件 | 现命名空间 | 目标目录 | 目标命名空间 |
|---|---|---|---|---|
| 1 | `ToolArgs.cs` | 根 | `Args/` | `.Args` |
| 2 | `ToolArgumentNormalizer.cs` | 根 | `Args/` | `.Args` |
| 3 | `ToolArgumentSanitizer.cs` | 根 | `Args/` | `.Args` |
| 4 | `ToolArgsDigester.cs` | 根 | `Args/` | `.Args` |
| 5 | `ToolArgumentException.cs` | 根 | `Args/` | `.Args` |
| 6 | `FeishuToolResult.cs` | 根 | `Results/` | `.Results` |
| 7 | `IToolResultShaper.cs` | 根 | `Results/` | `.Results` |
| 8 | `ToolResultPipeline.cs` | 根 | `Results/` | `.Results` |
| 9 | `ToolResultJson.cs` | 根 | `Results/` | `.Results` |
| 10 | `ToolResultJsons.cs` | 根 | `Results/` | `.Results` |
| 11 | `ToolResultText.cs` | 根 | `Results/` | `.Results` |
| 12 | `ToolResultSanitizer.cs` | 根 | `Results/` | `.Results` |
| 13 | `ToolResultContentSafety.cs` | 根 | `Results/` | `.Results` |
| 14 | `ToolDryRun.cs` | 根 | `Results/` | `.Results` |
| 15 | `ToolPagination.cs` | 根 | `Results/` | `.Results` |
| 16 | `ToolError.cs` | 根 | `Errors/` | `.Errors` |
| 17 | `ToolErrorClassifier.cs` | 根 | `Errors/` | `.Errors` |
| 18 | `ToolErrorPayloadSerializer.cs` | 根 | `Errors/` | `.Errors` |
| 19 | `IToolErrorTextFormatter.cs` | 根 | `Errors/` | `.Errors` |
| 20 | `ToolRetryPolicy.cs` | 根 | `Errors/` | `.Errors` |
| 21 | `FeishuToolBinding.cs` | 根 | **`Tools/`（保留）** | **根（不动）** —— 生成器钉死，见 §十 |
| 22 | `FeishuToolAIFunction.cs` | 根 | `Execution/` | `.Execution` |
| 23 | `ToolBudgetAIFunction.cs` | 根 | `Execution/` | `.Execution` |
| 24 | `IFeishuToolFunctionFactory.cs` | 根 | `Execution/` | `.Execution` |
| 25 | `UserTokenPrecheck.cs` | 根 | `Execution/` | `.Execution` |
| 26 | `UserTokenPrecheckResult.cs` | 根 | `Execution/` | `.Execution` |
| 27 | `FeishuToolRegistry.cs` | 根 | `Catalog/` | `.Catalog` |
| 28 | `FeishuToolCatalog.cs` | 根 | `Catalog/` | `.Catalog` |
| 29 | `IToolCatalog.cs` | 根 | `Catalog/` | `.Catalog` |
| 30 | `ToolSchemaJson.cs` | 根 | `Catalog/` | `.Catalog` |
| 31 | `ToolAffordanceText.cs` | 根 | `Catalog/` | `.Catalog` |
| 32 | `FeishuSkillDocumentBuilder.cs` | 根 | `Catalog/` | `.Catalog` |
| 33 | `ISkillDocumentReader.cs` | 根 | `Catalog/` | `.Catalog` |
| 34 | `IToolExecutionAuthorizer.cs` | 根 | `Hosting/` | `.Hosting` |
| 35 | `IToolExecutionAuditSink.cs` | 根 | `Hosting/` | `.Hosting` |
| 36 | `IFeishuAttachmentStager.cs` | 根 | `Hosting/` | `.Hosting` |
| 37 | `IFeishuUserAuthorizationLinkProvider.cs` | 根 | `Hosting/` | `.Hosting` |
| 38 | `SecurityTextPrimitives.cs` | 根 | `Security/` | `.Security` |
| 39 | `DownloadedContentGuard.cs` | 根 | `Security/` | `.Security` |
| 40 | `BitableFilterParser.cs` | 根 | `Parsers/` | `.Parsers` |
| 41 | `BitableSortParser.cs` | 根 | `Parsers/` | `.Parsers` |
| 42 | `PageSizes.cs` | 根 | `Constants/` | `.Constants` |
| 43 | `ReceiveIdTypes.cs` | 根 | `Constants/` | `.Constants` |
| 44 | `TranslationLanguages.cs` | 根 | `Constants/` | `.Constants` |

**留在根命名空间的内容**（不动）：`Extensions/FeishuToolsServiceCollectionExtensions.cs`（DI 入口，公开面 `FeishuToolsServiceCollectionExtensions` 是宿主第一触点，刻意留根）+ 生成产物（`FeishuToolNames`/`FeishuToolSchemas`/`FeishuToolContracts`/`FeishuToolGuidance`，由剖面 `GeneratedNamespace`/`ContractNamespace` 决定）。

### 3.2 目录级依赖方向（保持无环）

```
Constants ─┐
Security ─┼─→ Args ─┐
Parsers ──┼────────┼─→ Execution ─→ Catalog ─→ Extensions/Registration/Mcp 宿主
          │        │       ↑            ↑
          └─→ Results ←───┘            │
                 ↑                      │
              Errors ──────────────────┘（ToolErrorFactory 在 Results，错误面被 Results/Execution 消费）
Hosting ──→ Execution（可选注入：授权器/审计/落盘器/链接提供者/precheck）
```

无环，且与现有编译期依赖一致（`FeishuToolBinding` 注入 4 个 `Hosting/` 钩子 + `Execution/` 内部协作者）。

### 3.3 备选方案对比（架构决策记录）

| 方案 | 做法 | 优点 | 缺点 | 结论 |
|---|---|---|---|---|
| **A. 只挪目录、不改命名空间** | 44 文件移入 9 个子目录，namespace 仍写 `Mud.Feishu.AI.Tools` | `PublicAPI.Unshipped.txt`/`public-surface-rationale.json`/Demo/Mcp 消费方**零改动**；风险最低 | 违反项目"目录=命名空间"约定（`Curation` 有守卫先例），目录导航与命名空间导航二义性长期存在 | 备选（若评估后认为公开面改写风险不可接受） |
| **B. 目录 + 命名空间同步迁移（推荐）** | 目录即命名空间 | 与全项目约定一致；公有面按目录可定位；为后续拆文件/加守卫打好结构基础 | 需机械改写 `PublicAPI.Unshipped.txt`（1215 行中约 300 行含受影响全名）、`public-surface-rationale.json` 基线/理由键、3 个消费方 using | **推荐**：项目**未发布**（`PublicAPI.Shipped.txt` 仅一行 `#nullable enable`，csproj R3-11 自述"未发布窗口期零成本建立基线"），公共 API 改名空间零兼容负担；且改写全部是机械替换，可脚本化 + 编译器 RS0016 逐条验证 |
| C. 维持现状 + 加 README 导读 | 不动结构 | 零风险 | 未解决任何问题 | 否决 |

**推荐 B，A 作为风险回退预案**（B 的 P1 完成后若公开面改写出现意外，可只保留目录移动、namespace 回退根——即降级为 A）。

---

## 四、影响面清单（机械变更项，逐条可核对）

### 4.1 本项目内

| # | 文件 | 变更 |
|---|---|---|
| 1 | `Mud.Feishu.AI.Tools/GlobalUsings.cs` | 增补 9 条 `global using Mud.Feishu.AI.Tools.{Args,Results,Errors,Execution,Catalog,Hosting,Security,Parsers,Constants};`（保留现有根 using 给 `Extensions/` 与生成类型）。**这是生成器简单名解析（`FeishuToolResult`/`FeishuToolBinding`）与全包词汇表不掉线的保证** |
| 2 | `Mud.Feishu.AI.Tools/PublicAPI.Unshipped.txt` | 22 个 public 类型的类型行与全部成员行全名改写（如 `Mud.Feishu.AI.Tools.ToolError` → `Mud.Feishu.AI.Tools.Errors.ToolError`）；RS0016 清零为验收项 |
| 3 | `documents/AIAgent/public-surface-rationale.json` | `baseline` 数组与 `reasons` 键的同名改写（`PublicSurfaceRationaleContractGuards.DeclaredReasons_ShouldNotBeStale` 强制） |

### 4.2 跨项目消费方（均经 global using 收敛，改动极小）

| # | 文件 | 变更 |
|---|---|---|
| 4 | `Mud.Feishu.AI.Mcp/GlobalUsings.cs`（L15 现有 `using Mud.Feishu.AI.Tools;`） | 增补 `Catalog`（消费 `FeishuToolRegistry`/`FeishuToolNames`） |
| 5 | `Demos/Mud.Feishu.Agent.Demo/` 11 个文件（`ToolsDemo/SkillsDemo/ImConversationDemo/DomainEventsDemo/DocAgent*`/`ConsoleToolAuthorizer/InMemoryAuditSink/DemoAttachmentStager/DocAgentSettings/AgentConsoleLoop`） | 增补 `Catalog` + `Hosting` 两条 using（Demo 消费的正是 `FeishuToolRegistry`、`ISkillDocumentReader`、`IToolExecutionAuthorizer`、`IToolExecutionAuditSink`、`IFeishuAttachmentStager` 及其记录类型） |
| 6 | `Tests/Mud.Feishu.Agent.Demo.Tests/`（`TestDoubles/SkillsDemoTests/ConsoleToolAuthorizerTests/DocAgentDemoContractGuards`） | 同上 |
| 7 | `Tests/Mud.Feishu.AI.Tools.Tests/GlobalUsings.cs` | 增补对应子命名空间 using（测试用例按名字引用类型，全局 using 后测试代码本身零改） |
| 8 | `Tests/Mud.Feishu.AI.Tests/GlobalUsings.cs` | 视其用例引用增补（当前仅经 `AgentTools` 接缝，预计零改或 1 条） |

### 4.3 路径/文件名硬编码的守卫与测试（**必须同批更新，否则 CI 红**）

| # | 文件 | 硬编码 | 更新为 |
|---|---|---|---|
| 9 | `Tests/Mud.Feishu.AI.Tools.Tests/ContractGuards/ToolArgumentShapeContractGuards.cs` L44 | 扫描 `Mud.Feishu.AI.Tools/Tools`（JsonValueKind.String 白名单，`SearchOption.AllDirectories`） | 改为新 9 目录集合（或"除 Curation/Internal 外全扫"）；**不扩展即假绿**——`Results/` 里新出现一份形态认知将无人拦截 |
| 10 | 同上 L100-122 | `Curation/` vs `Tools/` 分离守卫（Tools 内不得有 `*ToolInterfaces.cs`） | `Tools/` 已不存在：`Directory.EnumerateFiles` 会因目录缺失失败 ⇒ 改为对 9 个新目录扫描 + 目录不存在时容错 |
| 11 | `Tests/Mud.Feishu.AI.Tools.Tests/ContractGuards/ToolBudgetContractGuards.cs` L56-60 | `Tools/FeishuToolAIFunction.cs` | `Execution/FeishuToolAIFunction.cs` |
| 12 | `Tests/Mud.Feishu.AI.Tools.Tests/ContractGuards/ToolErrorContractGuards.cs` L215 | `Tools/ToolResultPipeline.cs` | `Results/ToolResultPipeline.cs` |
| 13 | `Tests/Mud.Feishu.AI.Tools.Tests/ContractGuards/ToolEgressPurificationContractGuards.cs` L36 | `Tools/FeishuToolBinding.cs` | `Execution/FeishuToolBinding.cs` |
| 14 | `Tests/Mud.Feishu.AI.Tools.Tests/ContractGuards/UserTokenPrecheckContractGuards.cs` L71 | `Tools/UserTokenPrecheck.cs` | `Execution/UserTokenPrecheck.cs` |
| 15 | `Tests/Mud.Feishu.AI.Tools.Tests/Tools/ErrorRecoverabilityTests.cs` L171 | `Tools/ToolResultPipeline.cs` | `Results/ToolResultPipeline.cs` |
| 16 | `Tests/Mud.Feishu.AI.Tests/ContractGuards/AgentContractGuards.cs` L480/529/609 | `Tools/FeishuToolBinding.cs`（**跨测试项目**引用） | `Execution/FeishuToolBinding.cs` |
| 17 | `PaginationContractGuards.cs` L165、`BinaryDownloadToolExposureContractTests.cs` L19、`PackageOwnershipContractGuards.cs` L86-87、`MudFeishu-AI-vs-lark-cli-对比分析报告.md` | 文案提及"`ToolResultJsons` 在 `Tools/`"、"`Tools/Feishu*ToolInterfaces.cs`"、"工具契约与实现同包（根 ns）" | 文案订正（防误导后续维护者） |

### 4.4 测试目录镜像

`Tests/Mud.Feishu.AI.Tools.Tests/Tools/`（64 个测试文件）按"测试对象所在目录"镜像重组：测 `Tools/` 旧文件的约 25 个用例（`FeishuToolRegistryTests`、`FeishuToolBindingTests`、`ToolError*Tests`、`ToolResult*Tests`、`UserTokenPrecheckTests`、`PaginationAggregationTests`、`Bitable*ParserTests` 等）迁入对应子目录；测 `Internal/` 执行器的用例留在 `Tests/Tools/`（或另立 `Tests/Internal/`，列为可选项）。测试代码本身因 global using 零改。

---

## 五、实施计划（分阶段，每阶段可独立验证）

### Phase 0 — 基线与工具（0.5h）
- 记录当前全绿基线：`dotnet build Mud.Feishu.slnx`（4 TFM）+ 全 `dotnet test` + `pwsh ./scripts/verify-build.ps1`。
- 准备脚本：`git mv` 映射表（§3.1）；namespace 正则替换（`^namespace Mud.Feishu.AI.Tools;` → 目标）；PublicAPI 全名替换表（22 个类型名 → 新全名，含成员行内嵌引用）。
- 分支：`refactor/ai-tools-tools-regroup`，**单一提交**策略（便于整体 revert）。

### Phase 1 — 物理搬迁 + 命名空间（主体，0.5d）
1. `git mv` 44 文件 → 9 目录；
2. 各文件 `namespace` 行改写；
3. `GlobalUsings.cs` 增补 9 条（§4.1-1）；
4. 消费方 using 增补（§4.2）；
5. **验证**：`dotnet build` 四 TFM 0 错 0 警（含 net8.0 AOT strict 冒烟：确认生成器简单名解析未断）。

### Phase 2 — 公开面机械同步（1h）
- `PublicAPI.Unshipped.txt` 全名改写 → 构建至 **RS0016 = 0**（PublicApiAnalyzers 逐条兜底，改写遗漏会直接报红，不会静默）；
- `public-surface-rationale.json` 的 `baseline`/`reasons` 键同步改写 → `PublicSurfaceRationaleContractGuards` 两条用例转绿。

### Phase 3 — 守卫与文案更新（2-3h）
- 按 §4.3 逐条更新 7 处硬编码路径 + 2 处扫描面；
- **新增守卫**（沿用 `CurationFiles_ShouldDeclareTheCurationNamespace` 模式）：9 个新目录的文件必须声明对应子命名空间；`Tools/` 目录不得复活（文件落入即红）——把本次重组固化为不变量；
- 文案订正（§4.3-17）。

### Phase 4 — 测试目录镜像（1h）
- 按 §4.4 镜像迁移；`dotnet test Tests/Mud.Feishu.AI.Tools.Tests` 全绿。

### Phase 5 — 全量验收（1h）
- `dotnet test Mud.Feishu.slnx` 全量（含 `Mud.Feishu.AI.Tests`、`Mud.Feishu.Agent.Demo.Tests`）；
- `pwsh ./scripts/verify-build.ps1` 全门禁（0 错 / 0 CS1750 / 0 NU1603 / 0 HTTPCLIENT·MUD·FORM·AOT 诊断 / AOT strict net-zero / format）；
- `FeishuToolGoldenTests`（golden 漂移零变化——重组不动特性与剖面，这是"纯搬迁"的最强证据）；
- 更新 `Readme.md` / 相关方案文档中涉目录结构的表述（如有）。

---

## 六、验收标准（Definition of Done）

- [ ] 44/44 文件落位，`Tools/` 目录不存在（或仅余本方案文档引用）；
- [ ] 9 个新目录 = 9 个子命名空间，新守卫锁定；
- [ ] `dotnet build Mud.Feishu.slnx`：0 error / 0 warning，四 TFM；
- [ ] RS0016/RS0017 = 0；`PublicSurfaceRationaleContractGuards` 3/3 绿；
- [ ] §4.3 全部硬编码路径更新完毕，无守卫因路径失败；
- [ ] 全解决方案测试全绿（基线对等，无新增失败）；
- [ ] `scripts/verify-build.ps1` 全门禁通过；`FeishuToolSchemas.golden.txt` 零漂移；
- [ ] Demo 三件套（`ToolsDemo`/`SkillsDemo`/`DocAgent`）编译通过（公有面消费方完好的活体证明）。

## 七、风险与缓解

| 风险 | 等级 | 缓解 |
|---|---|---|
| `PublicAPI.Unshipped.txt` 手工改写遗漏 | 中 | 脚本化替换 + RS0016 编译器兜底（遗漏即红，不会静默）；P2 独立提交便于二分 |
| 生成器按**全限定名**引用 `FeishuToolResult`/`FeishuToolBinding`（而非简单名） | 低 | P1 构建即暴露；缓解 = global using（§4.1-1）。剖面槽均为简单名（`FeishuToolProfile.cs` L97-98），风险本身很低 |
| `PackageOwnershipContractGuards` 断言工具包含 `Mud.Feishu.AI.Tools` 命名空间 | 低 | 生成类型（`FeishuToolNames` 等）与 `Extensions/` 仍在根 ns，断言继续成立；注释文案同批订正 |
| 守卫扫描面未扩展导致"假绿" | 中 | §4.3-9/10 明确列为必改项；新增"目录=命名空间"守卫一并固化 |
| 大规模 diff 淹没 review | 低 | 单提交 + 纯搬迁声明；`git diff --stat` 应显示 44 renames + 少量内容行 |
| 回滚 | — | 单提交 `git revert`；无行为变更，回滚无数据/兼容性后果 |

## 八、明确不做（边界）

- 不拆分多类型文件（如 `IToolCatalog.cs` 一文件四 public 类型）——列为后续可选项；
- 不改任何类型名/成员名/行为/默认值；
- 不动 `Internal/`、`Curation/`、`SdkProfile/`、`Guidance/`、`Affordance/`、`skills/`、golden；
- 不动 `Extensions/FeishuToolsServiceCollectionExtensions.cs` 的位置（宿主 DI 第一触点，刻意留根命名空间）；
- 不借机引入新抽象——本次是**结构归位**，不是重构。

---

## 九、附录：关键证据索引

- 目录=命名空间约定先例：`ToolArgumentShapeContractGuards.CurationFiles_ShouldDeclareTheCurationNamespace`（Tests/Mud.Feishu.AI.Tools.Tests/ContractGuards/ToolArgumentShapeContractGuards.cs:129-149）
- Curation/Tools 分离不变量：同文件 `CurationDirectory_ShouldContainOnlyToolContractDeclarations`（:95-123）
- 公开面基线门禁：`PublicSurfaceRationaleContractGuards`（:40 `BaselineTypeCount = 297`）
- 未发布窗口期自述：`Mud.Feishu.AI.Tools.csproj:95`（R3-11 公共 API 兼容门禁注释）
- 生成器简单名契约：`SdkProfile/FeishuToolProfile.cs:97-98`（`ResultTypeName`/`BindingTypeName`）
- 跨项目公有面消费：`Demos/Mud.Feishu.Agent.Demo`（11 文件）、`Mud.Feishu.AI.Mcp/GlobalUsings.cs:15`

---

## 十、实施期重大发现（对方案的修订）

### 10.1 `FeishuToolBinding` 被生成器钉死在根命名空间（方案假设被实证推翻）

方案原文假设"生成器按**简单名**引用 `FeishuToolResult`/`FeishuToolBinding`，靠 global using 保持解析"。**实证（Phase 1 首轮构建）推翻了一半**：

- 上游生成器 `Mud.HttpUtils.Generator` 3.0.6 的 `ToolRegistrarEmitter`（`D:/Repos/MudHttpUtils` 源码 L386）把剖面槽
  `ContractNamespace`（`Mud.Feishu.AI.Tools`）+ `BindingTypeName`（`FeishuToolBinding`）拼成**全限定名**
  发射进 `FeishuToolsServiceCollectionCoreExtensions.g.cs`：`sp.GetRequiredService<Mud.Feishu.AI.Tools.FeishuToolBinding>()`。
  **全限定名不经 using 解析** ⇒ `FeishuToolBinding` 一旦迁往 `.Execution`，生成代码立即 CS0234（实测 94 处）。
- 该槽值被 `FeishuToolProfileContractGuards` 反射逐字节锁定（"每个槽的值都必须逐字节忠实"），**不可为迁目录而改槽**。
- 与之对照，`ResultTypeName` 仅在 `ToolHandlerScanner` 按**简单名**校验执行器方法签名（`task.TypeArguments[0].Name != ...`），
  与命名空间无关 ⇒ `FeishuToolResult` 可自由迁移（实证通过）。
- 生成器对**真实符号**（执行器类型、构造参数类型）按符号发全限定名，自动跟随新命名空间
  （实测正确发出 `Mud.Feishu.AI.Tools.Hosting.IFeishuAttachmentStager`、`Mud.Feishu.AI.Tools.Catalog.FeishuToolRegistry`）。

**处置（架构决策）**：`Tools/` 目录**不解散**，改制为「**生成器契约钉死的根命名空间类型**」白名单目录——
当前恰一户：`FeishuToolBinding.cs`（根命名空间）。该约束由新守卫 `CurationDirectory_ShouldContainOnlyToolContractDeclarations`
的第三断言机械锁定（Tools/ 只允许 `FeishuToolBinding.cs`，新增/迁入即红），并在类 XML 文档登记原因。
这比"文件留在 Execution/ 但声明根命名空间"更诚实：folder↔namespace 一致性 100% 保持，约束从隐性魔咒变为显式不变量。

### 10.2 公有类型计数修正

迁出根命名空间的 public 类型为 **23 个**（方案正文曾写 22，漏计 `IFeishuToolFunctionFactory`）：
Catalog 8 / Hosting 9 / Results 2 / Errors 2 / Execution 2。

---

## 十一、实施偏离与补充（相对方案正文）

1. **消费方 using 按实际引用增补（非一刀切）**：`Mud.Feishu.AI.Mcp` 实证消费 Catalog/Errors/Execution/Results 四族
   （`FeishuToolRegistry`/`IToolErrorTextFormatter`/`IFeishuToolFunctionFactory`/`FeishuToolResult`），首批扫描漏掉后两类，
   由编译器 CS0246 兜底暴露后补齐——"脚本初判 + 编译兜底"是本类机械迁移的正确工作方式。
2. **测试项目 `GuardProviderFactory` 提取**：该共享容器工厂原住在 `FeishuToolCatalogTests.cs`，被 6 个
   `ContractGuards/` 文件经 `using ...Tests.Tools;` 消费。测试镜像后若随 Catalog 迁入子命名空间，6 个守卫立刻 CS0103。
   处置：提取为 `Tests/Mud.Feishu.AI.Tools.Tests/GuardProviderFactory.cs`，置于**项目根命名空间**
   `Mud.Feishu.AI.Tools.Tests`——`Tests.Tools.*` 与 `Tests.ContractGuards` 两个子命名空间均可经**命名空间向上查找**
   解析，两侧零 using。这比"给 6 个守卫各加一条 Catalog using"更符合其共享基础设施的本体地位。
3. **`FeishuToolRegistryTests.cs` 命名空间归一**：该文件原声明孤例命名空间 `Mud.Feishu.AI.Tests.Tools`
   （疑似历史复制粘贴残留，全仓唯一），镜像时顺带归一为 `Mud.Feishu.AI.Tools.Tests.Tools.Catalog`。
4. **未实施项**：`IToolCatalog.cs` 一文件四 public 类型未拆分（方案 §八 明确不做）；`Tests/Tools/` 中测
   `Internal/` 执行器的 42 个用例未另立 `Tests/Internal/`（维持既有约定，控制 churn）。

---

## 十二、落地核验记录（2026-10-12）

| 阶段 | 内容 | 结果 |
|---|---|---|
| P0 | 全解决方案构建基线 | 0 错误；真基线（6 个受影响项目 `--no-incremental`）716 条警告 |
| P1 | 44 文件搬迁（git mv 保留历史）+ 命名空间重写 + 3 个项目 GlobalUsings 增补 + 17 个消费方文件 using 增补 + `FeishuToolBinding` 回驻 Tools/ | 解决方案 0 错误；同口径警告逐类别与基线**完全一致**（CS1573/1574/8604/1570/8602/1587/1591/0105/MUDFT009/1580/9113/8601/8605/0414 全部等计数），**零新增警告** |
| P2 | `PublicAPI.Unshipped.txt` 23 类型 FQN 机械改写 + `public-surface-rationale.json` baseline/reasons 键同步 | **RS0016 = 0 / RS0017 = 0**（PublicApiAnalyzers 逐条兜底）；`PublicSurfaceRationaleContractGuards` 3/3 绿 |
| P3 | 守卫更新：JsonValueKind 扫描面扩至 11 目录、契约声明扫描面扩至 10 运行时目录、Tools/ 白名单断言、**新增** `ConcernDirectories_ShouldDeclareMatchingNamespace`、5 处硬编码路径更新、4 处文案订正 | 随 P4 测试全量通过 |
| P4 | 22 个测试按被测对象镜像入 9 个子目录（+ `GuardProviderFactory` 提取） | 947/947 双 TFM 绿 |
| P5 | 全解决方案测试 | **9,337 项全部通过，0 失败**（net6.0 按环境惯例跳过：本机无 .NET 6 运行时）；golden 快照零漂移（`FeishuToolGoldenTests` 含在 947 内） |
| P5 | 门禁复刻（`pwsh` 本机未安装，逐项复刻 `scripts/verify-build.ps1` 断言） | Release 全 TFM `--no-incremental`：0 错误 / 0 CS1750 / 0 NU1603 / 0 HTTPCLIENT·MUD·FORM·AOT 诊断；AotStrictMode 冒烟 12 个源项目 net8.0：0 错误 / 0 IL2026 / 0 IL3050 / 0 AOT00x |

**已知既有欠账（非本次引入，git 实证未触碰）**：
`Curation/FeishuToolInterfaces.cs` 与 `Tests/.../WriteIdempotencyKeyTests.cs` 存在 `dotnet format` 空格差异；
`ChannelStateLifecycleContractGuards.cs` 第 10/12 行重复 using（CS0105）。三者均在基线即存在，建议另立小额修复提交。

**变更集规模**：66 个 git mv（44 源 + 22 测试，历史保留）+ 23 个修改文件 + 2 个新文件
（`GuardProviderFactory.cs`、本方案文档）+ 23 类型公开面命名空间迁移。未提交（按仓库纪律待用户审阅后单次提交，便于整体 revert）。

