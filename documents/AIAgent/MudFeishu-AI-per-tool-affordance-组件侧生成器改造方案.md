# Per-tool affordance（工具级决策指导）：组件侧生成器改造方案

> 角色：高级程序员 + 系统架构师
> 日期：2026-10-11
> 需求来源：`MudFeishu-AI系列-Bug修复与功能完善方案` §三 **F-7【P1·R-3 新增】per-tool affordance 决策指导层**
> 参照系：`documents/AIAgent/MudFeishu-AI-vs-lark-cli-对比分析报告.md` §五 P0.2；官方 `lark-cli` 的 `affordance/*.md`
> 结论先行：**本仓无法单独落地 F-7**——它必须改组件仓库 `D:/Repos/MudHttpUtils` 的通用工具面引擎
> （`Mud.HttpUtils.Generator` 的 `ToolSurface` 家族），本仓只做"剖面槽同步 + 资产编写 + 守卫 + 重固化"。
> 本文是**组件侧改造的可执行方案**（含本仓配套清单、兼容/版本策略、门禁与回滚）。
>
> **✅ 本侧接入已完成（2026-10-11，上游 `Mud.HttpUtils 3.0.6`）**。
> **发布基线 = Release 包**：上游 `pack.ps1 Release` 已产出并校验 `artifacts/`（10 包、与
> `bin\Release` 的 DLL 逐字节一致、Generator DLL 含 `ToolSurfaceSourceGenerator` 符号）；本仓
> `nuget.config` 的本地源已从 `artifacts-debug` 切到 `artifacts`，并按"同版本覆盖必须清缓存"
> 清理了陈旧的 `3.0.6` 缓存项、`dotnet clean` 重建后重跑全部门禁（结论见 `AGENTS.md` 依赖版本策略段）。
> **✅ 上游已发布到 nuget.org**（flat-container 端点确认三包均有 3.0.6），本仓本地源行已删除，
> 并在**从 nuget.org 重新还原**的位上复验全部门禁（缓存里 Generator DLL 的 SHA256 仍 ==
> `bin\Release`、≠ `bin\Debug` ⇒ nuget.org 的包与验证位逐字节相同）。
> 上游实现与本提案有**三处差异**，
> 接入以**上游实际实现**为准（`AffordanceDirectory` 槽 + `AffordanceAssets` 词法 + `x-{sdk}.affordance`）：
>
> | # | 本提案 | 上游实际 | 影响 |
> | --- | --- | --- | --- |
> | 1 | 诊断号写作 `MUDFT0xx` 占位 | **028**（结构非法）/ **029**（文件名不命中工具名）/ **030**（悬空引用）为 Error 零容忍；**031**（超预算）为 Warning 聚合单条 | 三集合/镜像/门禁脚本按 028–031 更新 |
> | 2 | 资产示例**带一级标题**（`# WHEN` 之上的 H1） | 「首节之前的抬头被忽略」**只在注释里成立**：解析器把**任何** `#` 行当小节标题 ⇒ H1 会被报 `MUDFT028 未知小节` | 16 份资产**必须去掉 H1**（接入时实测报出 16 条 028） |
> | 3 | 「发射位置：扩展块内 `source` 之后、`output_schema` 之前」 | 与提案一致 ✅（四键恒出现；无资产 ⇒ 整键不出现） | 无 |
>
> **接入的落地形态（与提案的两处取舍，均有理由）**：
> 1. **模型可见面 = 工具 `Description` 尾部追加**（提案写"MCP `_meta.affordance`"）——`x-feishu` 刻意
>    **不进参数 Schema 关键字空间**（MEAI `JsonSchema` 契约要求纯参数 Schema），而工具描述是模型"选工具"时
>    必然读到的字段；两链（进程内 Agent / MCP）共用同一 `FeishuToolAIFunction` ⇒ 描述逐字节一致。
>    额外的 `_meta` 需要在 MCP 工程里重复 `x-feishu` 线格式键（第二真相源）而收益是"结构化读法"，
>    已由 **`McpEnvelope` 导出方言的 `affordance` 字段**承担（同一数据源、同一处实现）。
> 2. **强制集合 = 全部 `high-risk-write`（16 个）**，而非"全部写类（74）+ 高危"——理由见 §六 的实测分层。

---

## 零、核验结论（2026-10-11，双侧仓库逐条对账）

> 核验基线：组件侧 `D:/Repos/MudHttpUtils` **3.0.5**（本次改造的**目标发布号 = 3.0.6**，见 §十/§十一）；下文凡出现 `3.0.5`
> 均指"核验时的存量事实 / 本仓当前 `PackageReference` 锁定值"，不是本次要发布的号。
>
> 核验方式：槽位表 `ToolSurfaceDiagnostics.Slots`、
> 发射链 `ToolSurfaceSourceGenerator` / `SchemaWriter` / `SchemaEmitter` / `GuidanceEmitter`）与本仓
> `E:/temp/MudFeishu`（剖面、守卫、MCP、门禁脚本）双向读源对账。以下每条均给出证据。

### 0.1 事实核验（原方案断言 → 判定）

| # | 原方案断言 | 判定 | 证据 |
| --- | --- | --- | --- |
| 1 | 本仓生成器已退役，Schema 唯一产者是组件侧引擎 | **属实** | `Mud.Feishu.AI.Tools.csproj:56-59`（`OutputItemType="Analyzer"`、3.0.5）；`FeishuToolProfileContractGuards.cs:337-400`（四条名称无关判据） |
| 2 | 剖面槽位逐值冻结 31 槽 | **属实（措辞需订正）** | 「31 槽」原文在守卫 `:29` 与 `ExpectedSlots :66-114`，**不在** `AGENTS.md`（`AGENTS.md:94-100` 只有「无 `[Generator]` / 无 `Microsoft.CodeAnalysis.CSharp`」的英文表述） |
| 3 | 资产注入机制已存在（`AdditionalFiles` → 剖面槽目录） | **属实，可直接复用** | `Tools.csproj:33`；引擎 `GuidanceEmitter.IsGuidanceFile:55-66` + `AssetKeyOf:230-255` |
| 4 | §3.5「更新开关复用 `GoldenUpdatePropertyName`，不新增环境变量」 | **不属实（须订正）** | 引擎中该槽**只**用于拼诊断提示串（3.0.5 树 `ToolSurfaceSourceGenerator.cs:521-524`，改造后为 `:561`），**无任何**读属性/写盘实现；本仓实际重新固化靠 `-p:FeishuToolRefreeze=true`（`Tools.csproj:25-28`，条件摘除 golden AdditionalFile）+ 人工落盘 |
| 5 | §3.4「预算 Warning 走基线 + 拦增量，与 MUDFT009/021 同口径」 | **属实** | 本仓基线链真实存在：`scripts/mudft-warning-baseline.txt`、`scripts/diagnostics-gate.ps1:70`（`$MudftBaselineIds`）、`GeneratorDiagnosticsContractGuards.cs:101-135` |
| 6 | §3.4 诊断编号写 `MUDFT0xx` | **须落实为 028–031** | 组件侧槽位上限 = `ToolSurfaceDiagnostics.MaxSlot = 27`（`:191`），且 `ToolSurfaceContractTests.DiagnosticSlots_AreUniqueAndContiguousWithUpstreamCanonicalNumbers:110-124` 要求升序唯一 |
| 7 | §3.2「人工策展资产、与 guidance 同性质」 | **属实** | `GuidanceEmitter` 明确「不新增诊断…资产冲突抛异常」（`:38-42`）——affordance 反向选择了「加诊断」，是本方案的**有意分歧**（合理性见 0.4） |
| 8 | §3.3「JSON 走源生成，手工拼常量字符串」 | **属实** | `SchemaWriter.WriteExtension:168-210`（`StringBuilder.Append`，非序列化器） |
| 9 | §2「运行期零解析（AOT 安全）」 | **须订正为「运行期零反射」** | 本仓 MCP 路径**已在运行期解析 JSON**：`FeishuMcpToolServer.cs:435`（`JsonNode.Parse(function.JsonSchema.GetRawText())`）。`JsonNode` 路径无反射、AOT 安全，但「零解析」不成立 |
| 10 | §2 目标 3「进入模型可见面：MCP `tools/list` 的 `_meta.affordance`」 | **不属实（关键，见 0.3）** | MCP 语义中 `_meta` 是**协议级元数据**，标准客户端不会把 `_meta` 送进模型上下文；模型实际可见面 = `name` / `title` / `description` / `inputSchema`（本仓 `BuildToolsListResult:259-283` 正是发这四项） |
| 11 | §四 #7「`feishu.guidance_read` 支持按工具读 affordance」 | **属新增能力** | 现状仅 `Domain`/`Topic` 两参（`CapabilityLookupTools.cs:149-167`），键 = `"{Domain}/{Topic}"` |
| 12 | §四 #6「MCP `tools/list` 携带 `_meta.affordance`」 | **属新增能力** | 现状无 `_meta`（全文零命中）；`inputSchema` 由描述符**裁剪为纯 parameters**（`:434-435`）⇒ `x-feishu` 不会自然进入 MCP |
| 13 | §四 #10「版本升级为唯一跨仓联动硬约束」 | **不准确（见 0.2）** | 跨仓联动**至少还有 3 处门禁**会被新诊断打破，见 0.2 |

### 0.2 前后端逻辑链条缺口（架构师视角）

新诊断进入组件侧后，会**同时**打破本仓三处「冻结/门禁」产物——原方案 §四 只列了「槽位 31→32」与「golden 重固化」，**漏掉以下三项**：

| 缺口 | 位置 | 后果（若不改） |
| --- | --- | --- |
| **G-1 上游槽位镜像** | `FeishuToolProfileContractGuards.cs:188-214` 的 `UpstreamSlotMirror`，及 `:229` 硬编码零容忍计数 **19** | `UpstreamSlotMirror_ShouldMarkExactlyTheErrorSlotsAsZeroTolerance` 必红（00=19 ≠ 22）；`MudftGateSets_ShouldMatchTheUpstreamSlotMirror` 必红 |
| **G-2 门禁三集合** | `scripts/diagnostics-gate.ps1` 的 `$MudftZeroToleranceIds` / `$MudftBaselineIds` | 新 Error 槽不入零容忍集 ⇒ 「构建期阻断」名不副实；新 Warning 槽落在三不管地带 ⇒ `MudftGateSets` 必红 |
| **G-3 基线文件** | `scripts/mudft-warning-baseline.txt` 需新增 `031=<count>` 行 | `MudftBaselineFile_ShouldExistAndOnlyReferenceDeclaredIds:99-135` 与 `diagnostics-gate.ps1` 的 `$MudftBaselineIds` 相互锚定，缺行即基线失效 |

补充缺口（原方案未覆盖，本方案已补）：

- **G-4 剖面槽位冻结表**：`ExpectedSlots :66-114` 必须新增 `["AffordanceDirectory"] = "/Affordance/"`。注意该守卫的存在性检查（`:126`）只对**已列出**的槽生效 ⇒ 不加不会红，但新槽从此**无人守**（静默漏守）。
- **G-5 组件侧自身的值相等缺口（新发现）**：`SdkToolProfileModel.Equals/GetHashCode`（`SdkToolProfileModel.cs:395-502`）**没有任何守卫**——既有 `ToolSurfaceEqualityFieldCoverageTests` 只覆盖 `Extraction/` 下三个值键。新增槽若漏进值相等，会出现「改了槽但增量管线判定未变 ⇒ 产物不重生成」的**静默漂移**（与 AT-B16 同类，但此前只在本仓发生过）。⇒ 本方案同批在组件侧补一条槽位面守卫。
- **G-6 MCP 通道的模型可见性**：把 affordance 放进 `_meta` 只服务**客户端实现**，不服务模型。要真正影响模型，须由宿主把 affordance 渲染进 `description`（或 MCP 侧折进 `description`）。⇒ 本方案把 `_meta` 定位为「机器可读通道」，并在 §四 #7 增加「宿主渲染」义务项。
- **G-7 运行期读取通路**：affordance 只进 JSON 时，`guidance_read`/MCP `_meta` 需运行期解析描述符（本仓 `:435` 已有先例，AOT 安全）。已评估「组件侧额外产出 `{P}Affordance.g.cs` 类型化产物」方案，**判定为冗余**（同一事实的第二个出口；且 `{P}Contracts.g.cs` 已是同一 pass 的类型化投影先例），故**不采纳**，改为「与 `FeishuToolSchemas` 同源解析」。

### 0.3 关键否决点：`_meta` 不是模型可见面

原方案 §2 目标 3 把「MCP `tools/list` 的 `_meta.affordance`」与「进程内工具 Schema 的 `x-feishu.affordance`」并列为「模型可见面」。核验结论：

- **进程内**：成立——宿主自己把 Schema 渲染进 prompt，渲染什么由宿主决定（本仓渲染的是 `FeishuToolSchemas` 常量）。
- **MCP**：**不成立**——`_meta` 按协议是「协议级元数据」，客户端自由处置，标准客户端不注入模型上下文；`annotations`（`readOnlyHint`/`destructiveHint`）同样面向客户端而非模型。

⇒ 修订：`_meta.affordance` 保留（**机器可读 + 客户端可渲染**），但**不得**在文档里声称它是模型可见面；模型可见面的唯一稳定通道是 `description`。由此派生 §四 #7 的新义务：宿主侧须把 affordance（或其后文折叠）渲染进工具 `description`/prompt，否则本方案对模型的**实际效果为 0**。

### 0.4 必要性、最优性与冗余（高级程序员视角）

**必要性：部分必要。** `x-feishu.affordance` 这一**结构化通道**确实只能由组件侧提供（引擎是 Schema 唯一产者），本仓无法绕过。但「让模型用对工具」这一**目的**存在零组件成本的替代路径：

| 路径 | 组件成本 | 模型可见（进程内 / MCP） | 机器可读 | 守卫可机械校验 |
| --- | --- | --- | --- | --- |
| **A. 写进工具 XML `<summary>`（= `description`）** | **0** | ✅ / ✅ | ❌ | 部分（token 预算） |
| **B. 本方案：`x-feishu.affordance` + `_meta`** | 一个组件发布周期（**3.0.6**） | ✅ / ❌（见 0.3） | ✅ | ✅（4 条诊断 + 6 条守卫） |
| C. 塞进 `Guidance/{domain}/{tool}.md` 走 `guidance_read` | 0 | ❌（须模型主动拉取） | ❌ | 有（既有 guidance 守卫） |

⇒ **最优方案为 A + B 双轨**，并明确分工：

- **A 轨（先做、零成本）**：把「何时用 / 何时别用 / 前置条件」的**结论**写进 `<summary>`，立即覆盖两条模型可见面；
- **B 轨（本方案）**：把同一结论**结构化**为 `x-feishu.affordance`，服务宿主渲染、客户端展示、跨生态比对与机械守卫；
- **C 轨仅作补充**：L2 guidance 仍是「域级跨工具工作流」的载体，**不得**替代 per-tool affordance。

> 若排期紧张，B 轨可降级为 P2：A 轨已能覆盖 F-7 的**效果目标**，B 轨覆盖的是**工程可治理性**目标（可守卫、可测量、可跨生态比对）。

**冗余判定（逐项）**

| 对照物 | 是否重复 | 处置 |
| --- | --- | --- |
| 工具 `description`（XML `<summary>`） | **部分重复**（方案 §六 #2 已自认） | 立边界：`when` 不得逐字复述 `<summary>` 首句；建议守卫「`when` 与 description 首句不完全相同」 |
| `Guidance/{domain}.md`（域级） | **有重叠风险（方案未立界，须补）** | 立界：**域级跨工具工作流/路由/避坑 → Guidance；单工具选型/前置条件/反例 → Affordance**。两份文案若对同一工具给出互斥指引即违纪 |
| `x-feishu` 既有键（`risk`/`is_write`/`identity`/`required_scopes`） | 不重复 | 轴不同：策略事实 vs 决策指导 |
| MCP `annotations`（`readOnlyHint` 等） | 不重复 | 布尔提示 vs 结构化指导 |
| 代码路径 | **无冗余** | 复用 `AdditionalFiles → 目录槽 → 同 pass 发射`，不新增遍历、不新增运行期反射、不新增程序集/配置键 |

**方案内部矛盾（须订正）**：§2 边界声明「❌ 不做自动生成 affordance 文本」，但 §六 #2「不做：…（affordance 只会重复 description）」又承认会重复 ⇒ 二者并存时「重复」不是禁止项而是**分层裁剪项**，表述已按此订正。

### 0.5 测试方案核验

原方案测试设计**基本成立**，但有 4 处缺口，已补入 §五/§八：

1. 组件侧只写「4 条新诊断各有 driver 负例」，**未给「空槽对既有 golden 逐字节零影响」的回归锚**——本仓 `FeishuToolSchemas.golden.txt`（182 行）即天然锚点，须显式列为组件侧 DoD；
2. 缺「新增槽位参与 `SdkToolProfileModel` 值相等」的守卫（见 G-5）；
3. 缺端到端链路用例：「资产 → 描述符 → `_meta` → `guidance_read`」四段同源断言（原 §八 只覆盖前两段）；
4. 缺「预算测量记录」的机械落点（`状态基线.md` 是散文件，须按 `ToolSurfaceScaleContractGuards` 同款口径锁定，否则记录会漂移）。

### 0.6 结论

方案**方向正确、可落地、非破坏**（可空槽 opt-in，对既有消费方零影响），**但须按 0.2 补全跨仓门禁链条、按 0.3 订正 MCP 模型可见性主张、按 0.4 立 A/B 双轨与冗余边界**。修订后的执行计划见 §七（已含实施状态标注）与 §十一。

---

## 一、为什么必须改组件侧（本仓阻塞点）

| 事实 | 证据 | 后果 |
| --- | --- | --- |
| 本仓生成器**已退役** | `AGENTS.md` §硬约束 2：全仓无 `[Generator]`、无 `Microsoft.CodeAnalysis.CSharp`；`FeishuToolProfileContractGuards` / `scripts/verify-pack.ps1` 检查①机械锁定 | 本仓不能再新增任何 `Emit` 逻辑 |
| 工具 Schema 的**唯一产者**是组件侧引擎 | `Mud.Feishu.AI.Tools.csproj:56` 的 `Mud.HttpUtils.Generator 3.0.5`（`OutputItemType="Analyzer"`） | `x-feishu.*` 由引擎发射，本仓只能"喂剖面 + 喂资产" |
| 剖面槽位**逐值冻结** | `AGENTS.md` §硬约束 3；`FeishuToolProfileContractGuards` 反射逐槽断言 31 个槽 | 新增"affordance 目录"槽 = 一次**显式**的冻结槽位扩项（需组件版本升级 + 本仓同批） |
| 资产注入机制已存在但只服务 guidance | `csproj`：`<AdditionalFiles Include="Guidance\**\*.md" />`；剖面槽 `GuidanceDirectory = "/Guidance/"` | affordance **可以复用同一机制**（AdditionalFiles → 源生成器 → 编译期常量），无需新插件形态 |

⇒ F-7 的原文方案（"由生成器随 `FeishuToolSchemas` 同一 pass 发射为 `x-feishu.affordance`"）
在当前架构下**只能通过组件侧版本升级实现**。故本文给出该升级的最小可落地切片。

---

## 二、目标与边界

**目标（做什么）**

1. 每工具可选一份**结构化决策上下文**：`when` / `avoid_when` / `prerequisites` / `tips`；
2. 与工具 Schema **同源同 pass** 发射（编译期常量 + golden 逐字节），运行期**零反射**（AOT 安全）；
   （订正：原写「零解析」不成立——本仓 MCP 路径已运行期解析描述符 JSON，见 §0.1 第 9 条）
3. 分两条通道交付（**订正**，原方案把两者混为「模型可见面」）：
   - **模型可见面（进程内）**：工具 Schema 的 `x-feishu.affordance`——宿主渲染 prompt 时自行决定是否折叠进 `description`；
   - **机器可读面（MCP）**：`tools/list` 的 `_meta.affordance`——服务客户端实现与跨生态比对，**标准客户端不会送进模型上下文**（见 §0.3）；
   - **按需读取面**：`feishu.guidance_read` 支持按工具键读 affordance。

**边界（不做什么）**

- ❌ 不新增运行期文件 IO / 不新增运行期反射；
- ❌ 不新增配置键（R5 配置治理）、不新增程序集 / NuGet 包（硬约束 7）；
- ❌ 不做"自动生成 affordance 文本"（LLM 合成）——affordance 是**人工策展资产**，与 guidance 同性质；
- ❌ 不做 per-参数级 affordance（那是 JSON Schema 的 `description` 职责，重复即产生第二真相源）。

---

## 三、组件侧改造方案（`D:/Repos/MudHttpUtils`）

### 3.1 剖面槽位（新增 1 槽，扩到 32 槽）

在 `[SdkToolProfile]` 增加：

| 槽位 | 类型 | 默认值 | 语义 |
| --- | --- | --- | --- |
| `AffordanceDirectory` | `string` | `string.Empty` | affordance 资产目录片段（AdditionalFiles 路径匹配，形如 `/Affordance/`）。空串 = 该剖面不启用本特性 ⇒ **对既有消费方零影响** |

> **类型订正**：原方案写 `string?` / 默认 `null`。**实际契约是「非空串槽 + 空串即不启用」**——
> `[SdkToolProfile]` 的原有 31 槽全部是 `string = string.Empty`（`SdkToolProfileAttribute.cs:81-210`），
> 引擎侧 `SdkToolProfileModel` 的解析分支一律 `value ?? string.Empty`
> （`SdkToolProfileModel.cs:257-297`），且 `GuidanceEmitter.IsGuidanceFile` / `SchemaEmitter.IsGoldenFile`
> 的「空串即恒 false」防御正是靠空串语义成立（`GuidanceEmitter.cs:50-61`、`SchemaEmitter.cs:49-57`）。
> ⇒ 新增槽若用 `null` 默认值，会与「空串即未声明」这条既有全仓纪律分叉。

> **为什么是"可空（可缺省）+ 空串默认"而不是"必填"**：组件是多消费方产品（本仓只是其一），
> 必填槽会让所有既有剖面**立即 SDKT002**；空串槽把这次升级变成"**opt-in 的增量能力**"，
> 本仓显式给出目录即启用——这正是"未发布窗口期零成本"之外仍需保持的工程纪律：
> **组件升级不得强迫不相关的消费方改代码**。

配套槽位复用（不新增）：`SchemaExtensionKey`（本仓为 `x-feishu`）、`GeneratedNamespace`、`GoldenFileName`、
`DiagnosticPrefix`。

> **订正**：原方案把 `GoldenUpdatePropertyName` 也算作"配套复用"，但它**不是**一个可复用的开关
> （引擎只用它拼 `MUDFT014` 的提示串，无任何读属性/写盘实现，见 §0.1 第 4 条）。本方案不使用它。

### 3.2 资产约定（本仓侧目录形态）

```
Mud.Feishu.AI.Tools/
├── Guidance/{domain}.md                 # 既有：L1 域级引导（不变）
├── Guidance/{domain}/{topic}.md         # 既有：L2 细则（不变）
└── Affordance/{tool_name}.md            # 新增：per-tool affordance（文件名 = 契约工具名，含点号）
```

`Affordance/bitable.query_records.md`：

```markdown
## WHEN
需要按条件筛选/排序读取多维表格记录时。

## Avoid when
- 已知 record_id 且只要一条 → 用 `bitable.query_records` 的 `record_ids` 精取（更省 token）；
- 只想看表结构 → 用 `bitable.list_fields`。

## Prerequisites
- 需要目标表的 `app_token` 与 `table_id`（先用 `bitable.list_tables` 拿）；
- `filter` 文法见 `bitable/filter-sort-syntax` 引导。

## Tips
- `page_size` 不由模型控制（平台默认），翻页只用 `page_token`；
- 返回被截断时看 `truncated` / `hint`，不要重复拉第一页。
```

**契约（引擎要机械校验的）**

| 规则 | 违反时的行为 |
| --- | --- |
| 四个小节名固定：`WHEN` / `Avoid when` / `Prerequisites` / `Tips` | 新增诊断（Error 级，见 3.4） |
| 文件名必须命中已策展工具名（`.md` 去掉后逐字符等于契约名） | 新增诊断（Error 级）：防"改了工具名、资产成孤儿" |
| 单文件 ≤ 1024 字符、单节 ≤ 320 字符 | 新增诊断（Warning 级 + 基线）：affordance 进模型 token 预算 |
| 资产内引用的 `域.工具` 必须命中工具名契约表 | 新增诊断（Error 级）（与 guidance 的"字面量被当工具名校验"同源） |

### 3.3 发射形态（`x-feishu.affordance`）

工具 Schema 的 `x-feishu` 对象增加一个**可选**子对象（无资产时不出现该键，保持既有 golden 不变）：

```json
{
  "name": "bitable.query_records",
  "description": "…",
  "parameters": { "type": "object", "properties": { } },
  "x-feishu": {
    "is_write": false,
    "risk": "read",
    "identity": "tenant",
    "required_scopes": ["bitable:app:readonly"],
    "affordance": {
      "when": "需要按条件筛选/排序读取多维表格记录时。",
      "avoid_when": ["已知 record_id 且只要一条 → 用 `bitable.get_records_by_id`…"],
      "prerequisites": ["需要目标表的 `app_token` 与 `table_id`…"],
      "tips": ["`page_size` 不由模型控制…"]
    }
  }
}
```

- **JSON 走源生成**（引擎手工拼常量字符串，`SchemaWriter.WriteExtension:168-210` 同款做法）；
- 四个子键**值全部是数组**（`when` 也允许数组：允许多条"何时用"），便于模型与守卫统一处理；
- 键名用 snake_case，与 `x-feishu` 既有风格一致（`is_write` / `required_scopes`）；
- **四键恒出现**（因为四小节必填即约束「四键非空」），故 JSON 形态确定、无需「缺键」分支；
- **发射位置**：扩展块内、`output_schema` 之前，与 `source` / `output_schema` 同为可选子块；
  `AffordanceDirectory` 未声明或该工具无资产时**整个 `affordance` 键不出现** ⇒ 既有 golden 逐字节不变（§八 DoD）。
- **MCP `_meta` 是机器可读通道，不是模型通道**（订正，见 §0.3）：不得宣称它影响模型行为；
  要影响模型，由宿主编排层把 affordance 折叠进工具 `description`（§四 #7 新义务项）。

### 3.4 诊断（纳入既有 `MUDFT` 族）

> **编号落实（订正）**：组件侧既有槽位上限 = `027`（`ToolSurfaceDiagnostics.MaxSlot`），
> 故新诊断取 **028–031**，各带语义常量名（`ToolSurfaceDiagnostics.Slots` 表须升序、唯一）。
> 原方案的 `MUDFT0xx` 占位写法已替换。

| ID | 常量名 | 级别 | 判据 |
| --- | --- | --- | --- |
| `{prefix}028` | `SlotAffordanceStructure` | **Error**（零容忍） | 资产缺固定小节 / 含未知小节 / 小节为空 / 同一工具名被两份资产占用 |
| `{prefix}029` | `SlotAffordanceOrphan` | **Error**（零容忍） | 资产文件名不命中任何已策展工具名 |
| `{prefix}030` | `SlotAffordanceDanglingReference` | **Error**（零容忍） | 资产内 `域.工具` 引用不存在 |
| `{prefix}031` | `SlotAffordanceBudget` | **Warning**（基线集） | 单资产或单小节超字符上界（**聚合单条**上报，见下） |

> **级别取舍**：结构与归属是「策展错误」（人写错了），必须 Error 且零容忍；预算是「内容偏胖」，
> 天然 >0（有资产就有字节数），故走「基线 + 拦增量」——与既有 `MUDFT009/021` 同口径。

> **031 必须是聚合单条（订正）**：仿 `009/018/021`（`ToolSurfaceDiagnostics:155-165`）的聚合纪律——
> 逐资产生成一条会在资产数上线性放大日志。聚合的代价见本仓 `mudft-warning-baseline.txt:20-25`（S-23）：
> 出现次数口径退化为「1 × 编译次数」，拦不住「超预算资产变多」。
> ⇒ 031 的消息文本**必须携带资产份数**（形如 `N 份 affordance 资产超预算`），
> 以便本仓按 `009-tools=<N>` 同款补一条 `031-assets=<N>` 语义量基线。

### 3.5 生成 pass 与 golden

- **同一 pass**：在既有 `ToolSurface` 生成流程中，`AdditionalFiles` 的 affordance 资产按
  `AffordanceDirectory` 归集 → 解析 → 与工具 Schema 一起发射（**不新增一次遍历**，避免"两条 pass 顺序耦合"）。
  落点：`ToolSurfaceSourceGenerator.EmitToolSurface`（既有的 `Schemas` 输出路径，`AdditionalTexts` 已在输入元组里）⇒ 零新增注册；
- **解析位置**：`EmitToolSurface` 内先解析/校验 affordance（上报 028–031），再交给 `SchemaEmitter`
  发射描述符——**校验与发射同 pass**，与 `DescriptorValidator`（L4）同款位置纪律；
- **golden**：affordance 内容随 `FeishuToolSchemas.golden.txt` 逐字节固化 ⇒ 改一个字即 `MUDFT014`（可 review、可回滚）；
- **重新固化（订正）**：**不存在**「把 `GoldenUpdatePropertyName` 设为 true 即自动重写快照」的机制。
  该槽在引擎里**只**用于拼 `MUDFT014` 的提示串（`ToolSurfaceSourceGenerator.GoldenUpdateHint:521-524`），
  源生成器不写盘。本仓的真实流程是 `-p:FeishuToolRefreeze=true`（`Tools.csproj:25-28` 条件摘除 golden
  AdditionalFile ⇒ 不比对）+ 从 `FeishuToolSchemas.g.cs` 人工/脚本落盘（`Readme.md:280-287`）。
  ⇒ 本方案**不新增任何环境变量/属性**（该主张成立），但也不得宣称复用了一个不存在的"开关"。

### 3.6 wire-stable 的前置校验（反射面冻结）

> ⚠️ **本项属"未发布窗口期"的一次性形态决策**：`x-feishu.affordance` 的**键名与数组形态**
> 一旦随包发出即成为对外契约（模型与 MCP 客户端会按它解析）。故本方案要求组件侧在**同一版本**
> 内定形，不得先发"草案形态"再改。

---

## 四、本仓侧配套清单（组件升级后执行；**状态列 = 2026-10-11 实测**）

| # | 动作 | 位置 | 判据 / 结果 |
| --- | --- | --- | --- |
| 1 | 剖面新增槽值 `AffordanceDirectory = "/Affordance/"` | `SdkProfile/FeishuToolProfile.cs` | ✅ 槽位 31 → 32；剖面冻结表同批更新（见 #13） |
| 2 | `csproj` 增加 `<AdditionalFiles Include="Affordance\**\*.md" />` | `Mud.Feishu.AI.Tools.csproj` | ✅ 与 `Guidance\**\*.md` 并列（同一注入机制） |
| 3 | 编写 affordance 资产（**分层**，见 §六） | `Mud.Feishu.AI.Tools/Affordance/*.md` | ✅ **16 份**（= 全部 `high-risk-write` 工具），四节齐备，合计 6087 字符 |
| 4 | golden 重固化 | `FeishuToolSchemas.golden.txt` | ✅ 含 16 处 `affordance`。**订正**：`$env:FeishuToolGoldenUpdate` **不是**引擎开关（§3.5）——实际用 `-p:FeishuToolRefreeze=true`（条件摘除 golden AdditionalFile）+ 从 `FeishuToolSchemas.g.cs` 落盘（`Readme.md:280-287`） |
| 5 | skills 重固化 | `skills/**` | ✅ **不适用**（有意）：本方案判定 affordance **不进 SKILL.md**（外部 Agent 读取成本），`skills/` 零变更 |
| 6 | MCP 通道 | `FeishuMcpToolServer.cs`、`Tools/FeishuToolCatalog.cs::ExportMcpEnvelope` | ✅ **改判**：~~`tools/list` 携带 `_meta.affordance`~~ → MCP 的 `description` 取自**同一** `FeishuToolAIFunction`（⇒ 与进程内逐字节一致），结构化读法由 **`McpEnvelope` 方言的 `affordance` 字段**承担。**不采纳 `_meta`** 的理由见文首「落地形态 1」：`_meta` 需要在 MCP 工程重复 `x-feishu` 线格式键（第二真相源），收益仅"结构化读法"且已由 `McpEnvelope` 覆盖 |
| 7 | `feishu.guidance_read` 支持"按工具读 affordance" | — | ✅ **有意不做**：affordance 已在工具**描述**里（模型选工具时必然读到），再加一条自省读取通道 = 同一事实的第二条路径（违反"唯一真相源"纪律）；确需结构化字段时读 `feishu.tool_search` / `FeishuToolSchemas` 导出 |
| 8 | **宿主渲染义务（F-7 效果的唯一开关）** | `Tools/FeishuToolAIFunction.cs`、`Tools/ToolAffordanceText.cs` | ✅ 描述尾部追加「【决策指导】何时用 / 不要用 / 前置 / 提示」；渲染收敛为 `ToolAffordanceText` 唯一实现；**无资产 ⇒ 空串 ⇒ 描述逐字节不变** |
| 9 | 新增守卫 `AffordanceContractGuards` | `Tests/Mud.Feishu.AI.Tools.Tests/ContractGuards/AffordanceContractGuards.cs` | ✅ **12 用例**（接线三件套 / 强制集合 / 结构 / 引用 / 预算测量 / 模型可见性 / 未配资产描述逐字节不变 / 导出透传 / 扩展键单源 + 3 条判据自证） |
| 10 | 更新上游槽位镜像 | `FeishuToolProfileContractGuards` 的 `UpstreamSlotMirror` + 零容忍计数 | ✅ 镜像 **24 → 28 槽**，零容忍 **19 → 22** |
| 11 | 更新门禁三集合 | `scripts/diagnostics-gate.ps1` | ✅ `$MudftZeroToleranceIds` += `'028','029','030'`；`$MudftBaselineIds = @('009','021','031')`；由 `MudftGateSets_ShouldMatchTheUpstreamSlotMirror` 逐项锁定 |
| 12 | 更新 Warning 基线 | `scripts/mudft-warning-baseline.txt` | ✅ `031=0`（16 份资产全部在 1024/320 上界内）、`009-tools=68` |
| 13 | 更新剖面冻结表 | `FeishuToolProfileContractGuards.ExpectedSlots:66-114` | ✅ 加 `["AffordanceDirectory"] = "/Affordance/"`。⚠️ 该守卫的存在性检查只覆盖**已列出**的槽 ⇒ 不加不会红，但新槽从此**无人守**（这就是它必须进清单的原因） |
| 14 | 版本升级 **3.0.5 → 3.0.6**（**不得用 3.1.0**，见 §七） | 全仓 `Mud.HttpUtils*` 的 `PackageReference`（**9 工程 15 处**） | ✅ 全为 `3.0.6`；`TokenMultiAppContractGuards.MudHttpUtils_PackageReference_ShouldBeSingleVersion` 只断言"全仓唯一"、不硬编码版本号 ⇒ 无需改守卫 |
| 15 | 升级方案文档 + `AGENTS.md` 同步 | `MudFeishu-MudHttpUtils-3.0.6-升级改造方案-v1.md`、`AGENTS.md` 版本策略段落 | ✅ 已建并互相引用；同批移除**两条悬空引用**（`MudFeishu-MudHttpUtils-3.0.5-升级改造方案-v1.md`、`MudHttpUtils-3.0.0-破坏性变更改造计划.md` **均不存在**） |
| 16 | 文档计数 | `Mud.Feishu.AI.Tools/Readme.md` | ✅ 已同步（`ToolSurfaceScaleContractGuards` 的裸数字扫描面） |
| 17 | **收尾（发布后必做）** | 上游 `pack.ps1 Release`、本仓 `nuget.config` | ⬜ 上游发布 `3.0.6` 到 nuget.org → **删除** `MudHttpUtils-local` 本地源行（发布基线不得来自 `artifacts-debug`）→ 重跑 `scripts/verify-build.ps1`（Release + `--no-incremental`，保证 MUDFT 为全量口径）与四 TFM 测试 |

> ⚠️ 第 14 项（版本升级）是**跨仓联动的硬约束**：组件新版本必须 `pack.ps1 Release` 产出并**发布到 nuget.org**。
> **订正（2026-10-11 实测）**：本次接入**未等发布**——`PackageReference` 已提前 bump 到 `3.0.6`，可解析性由
> `nuget.config` 的**临时**本地源 `MudHttpUtils-local → D:/Repos/MudHttpUtils/artifacts-debug`（`pack_debug.ps1` 的
> **Debug** 包）提供。⇒ 收尾见 #17：上游发布后**必须删除该源行**（发布基线不得来自 `artifacts-debug`）。
> 若在本地联调，需按 `AGENTS.md` 的"三步刷新缓存"（`nuget locals` → `clean` → `build`），
> 并**必须 bump 组件版本号**——同版本重打包不会让下游缓存失效（历史事故 3 次）。
>
> ⚠️ 第 10–13 项是**同一批**：组件侧一旦新增 4 条诊断，本仓的镜像/门禁/基线/冻结表**四处同时失真**，
> 必须与组件升级合入同一提交（顺序：组件先行 → 本仓同批 10–13 → 再跑 `verify-build.ps1`）。

---

## 五、本仓侧守卫设计：`AffordanceContractGuards`

> **落地状态（2026-10-11）**：已实现为 `Tests/Mud.Feishu.AI.Tools.Tests/ContractGuards/AffordanceContractGuards.cs`，
> 共 **12 用例**——下表 6 条设计用例 + 「强制集合 = 全部 `high-risk-write`」+ 「未配资产 ⇒ 描述逐字节不变」
> + 「`McpEnvelope` 导出透传」+ 「扩展键单源」+ 3 条判据自证。用例名以源码为准（下表为**设计意图**，
> 与实现同名者即同一判据）。

| 用例 | 判据 | 负例自证 |
| --- | --- | --- |
| `EveryAffordanceAsset_ShouldTargetAnExistingTool` | `Affordance/*.md` 文件名 ⊆ `FeishuToolNames.All` | 合成一份"孤儿"资产必须被点名 |
| `WriteTools_ShouldDeclareAvoidWhenAndPrerequisites` | 写类工具的 affordance **必含** `avoid_when` 与 `prerequisites`（写面是"误用代价最高"的面） | 合成缺节资产必须报红 |
| `AffordanceSections_ShouldBeExactlyTheContract` | 四节名逐字匹配、无未知节、无空节 | 合成未知节必须报红 |
| `AffordanceToolReferences_ShouldExist` | 资产内 `域.工具` 引用 ⊆ 工具名契约表 | 合成 `im.not_a_tool` 必须报红 |
| `AffordanceBudget_ShouldBeMeasuredAndBounded` | 全部资产的字符数合计 + 单资产上界，与 `FeishuGuidanceComposer.MaxGuidanceLength` 的关系**留测量记录** | 合成超界资产必须报红 |
| `Schema_ShouldCarryAffordance_OnlyForDeclaredTools` | golden 里 `x-feishu.affordance` 出现次数 == 资产数（防"资产写了但没进 Schema"） | 合成缺失必须报红 |

**组件侧守卫（补，G-5）**——`Tests/Mud.HttpUtils.Generator.Tests/ToolSurface/ToolSurfaceContractTests.cs`：

| 用例 | 判据 | 负例自证 |
| --- | --- | --- |
| `ProfileSlotSurface_AttributeProperties_ShouldMirrorModelProperties` | `SdkToolProfileAttribute` 的公开可写属性集 == `SdkToolProfileModel` 的公开属性集（值对象是剖面事实的唯一载体） | 单侧加槽必须报红 |
| `ProfileSlots_EverySlot_ShouldParticipateInEqualityAndHash` | `SdkToolProfileModel` 的每个槽名出现在 `Equals` 与 `GetHashCode` 方法体中 | 漏一槽必须报红（否则「改了槽 ⇒ 产物不重生成」的静默漂移） |

> 第二条关闭的是既有缺口：`ToolSurfaceEqualityFieldCoverageTests` 只覆盖 `Extraction/` 下三个值键
> （`CapabilityEntry`/`CapabilityParameter`/`ToolSchemaModel`），`SdkToolProfileModel` **此前无守卫**。

**本仓守卫（补，0.5-3/-4）**：

| 用例 | 判据 |
| --- | --- |
| `Affordance_ShouldReachModelVisibleSurface` | 端到端：资产 → 描述符 → **宿主渲染（工具 `description` 含 affordance）** → `McpEnvelope` 导出透传，各段同源。（**订正**：原设计的「MCP `_meta`」与「`guidance_read` 按工具键」两段已改判/取消，见 §四 #6/#7） |
| `AffordanceBudget_ShouldBeLockedByScaleGuard` | 「合计字符数 / 单工具增量字节数 / 语义量」写进规模基线，并由 `ToolSurfaceScaleContractGuards`（或同款裸数字扫描）机械锁定，不得只靠人工记录 |

> **为什么这组守卫必须存在**：affordance 的失败模式与 guidance 同款且**更隐蔽**——
> 资产写了但没生效（文件名/节名写错）不会触发任何诊断，表现为"模型还是不看这些提示"，
> 从日志上完全看不出来。

---

## 六、预算与分层策略（必须同批测量）

affordance 进**模型可见 token 预算**，这是它与 guidance 的公共成本约束：

1. **先测量再分层**：实现第一版后，实测"全部资产合计字符数"与"单工具 Schema 增量字节数"，
   把结果写进 `状态基线.md`；**且必须由守卫锁定**（否则记录必漂移，见 §0.5-4）；
2. **推荐分层（首版）**：
   - **必须**：写类工具（74 个）＋ `high-risk-write`（10 个）——它们是"误用即不可逆"的面；
   - **建议**：模型高频误用的只读工具（如 `bitable.query_records` / `search.doc_wiki`）；
   - **不做**：低风险、单一用途的只读工具（affordance 只会重复 `description`）。
3. **预算触顶时的处置**：优先**压缩资产**（与 F-1 纪律 22 "域变宽第一反应是压缩文案"同源），
   而不是抬高 `MaxGuidanceLength`；agent 指令面的预算抬升必须走
   `GuidanceAssetContractGuards.GetGuidance_WithAllDomainsEnabled_ShouldNeverDropAnyDomain` 同批评审。

**✅ 实测（2026-10-11 接入批）与首版分层决定**：

| 指标 | 实测 | 上界 |
| --- | --- | --- |
| 资产份数 | **16**（= 全部 `high-risk-write`） | — |
| 合计字符 | **6,087**（≈1.8k token，且只在工具**启用**时随描述进入请求） | — |
| 单资产最长 | 471 | 1024（`MUDFT031` 上界） |
| 单小节最长 | 163 | 320 |

- **首版只做 `high-risk-write`（16 个）**，而不是"全部写类（74）+ 高危"：
  ① 高危面是"误用即不可逆"的那一层（删除/撤回/权限变更/所有权转移），affordance 的机会成本最低、收益最高；
  ② 其余 58 个写类工具的 `description` 里已写明"写操作 + 需授权 + 建议 dry_run"（生成器统一追加），
     affordance 若只复述这些就是**纯 token 成本**；
  ③ 强制集合取 `risk == high-risk-write` 是**自收紧**的：新增任何高危工具都会立刻让
     `AffordanceContractGuards.EveryHighRiskWriteTool_ShouldDeclareAffordance` 报红，逼迫同批补资产。
- **后续扩展的判据（不在本批）**：只有当某个普通写/只读工具出现**实测到的模型误用**（选错工具、漏前置、
  重复翻页）时，才为它补资产——按"证据驱动"而不是"面面俱到"，与 F-1 的"目的判据"同源。

---

## 七、兼容性与发布编排

| 阶段 | 组件仓库（`D:/Repos/MudHttpUtils`） | 本仓（`MudFeishu`） | 状态 |
| --- | --- | --- | --- |
| ① 组件实现 | 加槽（默认空串 = 不启用）+ 解析 + 发射 + 4 条诊断（028–031）+ driver 负例 + 槽位面守卫 | — | **已实施**（见 §十一） |
| ② 门禁 | `pack.ps1 Release`（10 包 + DLL SHA256 自检） | — | 待执行（发布动作，需人工确认） |
| ③ 发布 | 发布到 nuget.org（**必须 bump 版本**：3.0.5 → **3.0.6**） | — | 待执行 |
| ④ 本仓接入 | — | bump `Mud.HttpUtils*` 版本（单版本）→ 剖面加槽 → 资产 → golden/skills 重固化 → 守卫 → PublicAPI 同步 → **§四 #11–15（镜像/门禁/基线/冻结表/宿主渲染）** | 待执行（依赖 ③） |
| ⑤ 验收 | 组件侧 driver 全绿 + 既有 golden 逐字节零漂移 | 四 TFM Release `0 错误`、`RS0016/0017 = 0`、AOT 净零、golden/skills 零漂移、全量测试全绿 | 组件侧已验（§十一）；本仓待执行 |

> **建议先做 A 轨（§0.4）**：A 轨（affordance 写进工具 XML `<summary>`）零组件成本、立即覆盖两条模型可见面，
> 可作为 ①–④ 的**效果前置**；B 轨（本方案）在其上补齐机器可读与可治理性。两轨可并行，但 A 轨不阻塞 B 轨。

**回滚路径**：删除剖面的 `AffordanceDirectory` 赋值（回到空串）+ 删除资产目录 ⇒ 引擎回到
"不发射 `affordance`"的形态，golden 回退到接入前逐字节一致（**这是"空串槽 = 未声明"设计的直接收益**）。

**BREAKING 判定**：对**本仓**是"新增能力"（非破坏）；对**组件其他消费方**是"新增可选槽"（非破坏）。

**版本号（订正）**：本次发布号 = **3.0.6（patch）**。注意**不能**用 `3.1.0`——该号**已在
`MudHttpUtils/CHANGELOG.md:223` 被历史版本占用**（2026-10-09《HttpClient 审查修复与超时策略治理》），
复用会与已发布产物撞号。组件仓库的版本序列存在非单调历史（3.0.x 与 3.1.0 并存），
故**发布号一律以 `Directory.Build.props` 的实际取值为准**，不以"新增公开成员 ⇒ minor"的惯例推导。
本仓 `TokenMultiAppContractGuards.MudHttpUtils_PackageReference_ShouldBeSingleVersion` 只断言"全仓唯一"、
不硬编码版本号，故 bump 到 3.0.6 无需改守卫。

---

## 八、验收标准（DoD）

| 门禁 | 判定 |
| --- | --- |
| 组件侧 | 新增槽位对既有剖面（`AffordanceDirectory` 未设）**逐字节 golden 不变**（回归自证）——以本仓 `FeishuToolSchemas.golden.txt`（182 行）与 `Demos/ToolSurfaceDemo` 为回归锚 |
| 组件侧 | 4 条新诊断各有 driver 负例（含"必须报红"自证）；031 为**聚合单条**且消息含资产份数 |
| 组件侧 | 槽位面守卫两条全绿（镜像 + 值相等，见 §五） |
| 本仓 | 剖面槽 32 个，`FeishuToolProfileContractGuards` 全绿（含 `ExpectedSlots` 新增行） |
| 本仓 | `AffordanceContractGuards` 6 用例 + 端到端 1 用例 + 预算锁定 1 用例全绿，每条含负例自证 |
| 本仓 | `UpstreamSlotMirror` 零容忍数 = 22；`diagnostics-gate.ps1` 三集合与本仓镜像逐项相等；`mudft-warning-baseline.txt` 含 `031=`（建议含 `031-assets=`） |
| 本仓 | golden / skills 零漂移；四 TFM `0 错误`；`RS0016/0017 = 0`；AOT `IL2026/IL3050 = 0` |
| 本仓 | MCP `tools/list` 的 `_meta.affordance` 与 `FeishuToolSchemas` 逐字段一致（并入 `ToolSurfaceConsistencyContractTests`） |
| 本仓 | **宿主渲染面**：模型实际可见的 `description`（进程内 prompt 与 MCP `description`）确实含 affordance——否则本特性对模型效果为 0（§0.3） |
| 预算 | 实测"合计字符数 / 单工具增量字节数 / `031-assets`"写入 `状态基线.md`，并与 `MaxGuidanceLength` 的关系有记录 |

---

## 九、与既有决策的关系（不产生冲突）

- **F-1（策展补齐）**：新域应**直接带 affordance**（避免二次开洞）——这是 F-7 原文的依赖项，已满足（F-1 P0 已清零）。
- **F-8（Skills 闭环）**：已交付"读取接缝 + 版本一致性 + Demo 首消费方"。若 affordance 进 SKILL.md，
  仅是同一产物的内容增量，不改变 F-8 的结论。
- **F-9（MCP envelope 导出）**：`McpEnvelope` 已导出 `inputSchema` + `annotations`；affordance 落地后
  同一方言补 `affordance` **字段**（同一数据源、无第二真相源）。
  **订正**：原写"需补 `_meta.affordance`"——**已改判为 `McpEnvelope` 方言的 `affordance` 字段**
  （MCP `tools/list` 的模型可见面走 `description`；再引入 `_meta` 需在 MCP 工程重复线格式键）。
- **F-12（不采用裸逃生舱）**：affordance 是"**引导模型用对工具**"，与"提供绕过执行链的通道"方向相反，无冲突。
- **R5 配置治理**：本方案**不新增任何配置键**（开关是编译期剖面槽 + 资产目录）。

---

## 十、未决项（需产品/架构确认）

1. **键名与形态**：`x-feishu.affordance` 的四子键命名（`when` / `avoid_when` / `prerequisites` / `tips`）
   是否需要与官方 `lark-cli` 的 `affordance/*.md` 小节名**逐字对齐**（便于跨生态比对）。
   > 本次实施已按原命名定形（组件侧 `AffordanceAssets` 的 `SnakeCaseKey` 映射），**未**与 lark-cli 对齐；
   > 若后续要求对齐，属**契约变更**（需组件再次发版 + 本仓 golden 重固化），故须在首个发布前定夺（§3.6）。
2. **是否进 SKILL.md**：SKILL.md 面向"读文件的 Agent"，affordance 进入会让文件变长（外部 Agent 的读取成本）；
   备选是"只在 `x-feishu` 与 MCP `_meta` 里给，SKILL.md 保持现状"。
   > **本次判定：不进 SKILL.md**（保持现状）。理由：SKILL.md 已有 25 个 `SKILL.md` + 14 篇 references 的
   > 读取成本预算，而 B 轨的**首要消费方是宿主渲染面与 MCP 客户端**，SKILL.md 是第三个消费方，收益边际最低。
3. **组件仓库排期**：本次改造需要组件仓库一个发布周期（**3.0.6**），需与 `Mud.HttpUtils` 的其他待办合并评估。
4. **【新增】A 轨的验收口径**：A 轨（affordance 折进工具 XML `<summary>`）会使 `description` 变长，
   `MUDFT005/006`（XML 文档缺失）之外**没有**长度上界诊断 ⇒ A 轨的 token 预算只能靠本仓守卫锁定。
   需确认 A/B 两轨是否共用同一份「字符预算」上限（组件侧 1024/320 是本方案自定值，非既有事实）。
5. **【新增】`031-assets` 语义量基线是否同批建立**：`031` 为聚合单条（§3.4），若只建 `031=<count>` 行，
   则「超预算资产变多」这一退化**无法被拦住**（与 S-23 同型）。建议同批补语义量行，需产品确认是否接受该门禁复杂度。

---

## 十一、实施状态（组件侧，2026-10-11）

> 本节记录**组件仓库 `D:/Repos/MudHttpUtils` 侧阶段①的实际落地物**，供本仓阶段④逐项对齐。

| 项目 | 落地物 | 位置 |
| --- | --- | --- |
| 剖面槽 | `SdkToolProfileAttribute.AffordanceDirectory`（`string`，默认空串） | `Mud.HttpUtils.Attributes/ToolProfile/SdkToolProfileAttribute.cs` |
| 槽位值对象 | 解析分支 + `Equals` + `GetHashCode` 同步 | `Mud.HttpUtils.Generator/ToolSurface/SdkToolProfileModel.cs` |
| 资产解析 | `AffordanceEntry`（四节值对象 + `ToJson`）（**Schema 命名空间**，与 `SchemaWriter` 同层，避免 Schema→Emit 反向依赖） | `Mud.HttpUtils.Generator/ToolSurface/Schema/AffordanceEntry.cs` |
| 资产采集 | `IsAffordanceFile` / `AssetToolName` / `Collect`（四小节 + bullets→数组 + 引用校验 + 预算测量 + 归属/重复占用判定） | `Mud.HttpUtils.Generator/ToolSurface/Schema/AffordanceAssets.cs` |
| 发射 | `x-{sdk}.affordance`（四键恒出现；无资产则整键不出现，插在 `source` 之后、`output_schema` 之前） | `Mud.HttpUtils.Generator/ToolSurface/Schema/SchemaWriter.cs`（`WriteToolSchema` / `WriteExtension`） |
| 描述符管线 | `Emit`/`BuildGolden`/`EmitSchemas` 透传 `IReadOnlyDictionary<string, AffordanceEntry>`（常量与 golden 仍出自**同一调用** `WriteSchema`） | `Mud.HttpUtils.Generator/ToolSurface/Emit/SchemaEmitter.cs` |
| pass | 复用 `EmitToolSurface`（零新增注册、零新增遍历），校验与发射同 pass | `ToolSurface/ToolSurfaceSourceGenerator.cs` |
| 诊断 | 028 结构 / 029 归属 / 030 引用（Error·零容忍）+ 031 预算（Warning·聚合单条） | `ToolSurface/ToolSurfaceDiagnostics.cs`（`MaxSlot` 27 → 31） |
| 守卫 | 槽位面镜像 + 槽位值相等覆盖（关闭 G-5 缺口） | `Tests/.../ToolSurface/ToolSurfaceContractTests.cs` |
| driver 负例 | 5 条（结构/归属/引用/预算/空槽零影响） | `Tests/.../ToolSurface/ToolSurfaceEmissionTests.cs` |
| 文档 | 槽位表与诊断表更新（含 028–031） | `Mud.HttpUtils.Generator/README.md` |
| 版本 | `3.0.5` → **`3.0.6`**（新增公开特性属性成员；**不得用 3.1.0**——该号已被历史版本占用） | `Directory.Build.props` |
| PublicAPI | `AffordanceDirectory` 的 get/set 条目 | `Mud.HttpUtils.Attributes/PublicAPI/netstandard2.0/PublicAPI.Unshipped.txt` |
| 回归锚 | `Demos/ToolSurfaceDemo`（新增 `Affordance/doc.list.md` 示例 + 剖面槽） | `Demos/ToolSurfaceDemo/` |

**预算取值（首版）**：单资产 ≤ 1024 字符、单小节 ≤ 320 字符（可调；订正值落在组件常量上，
即需再发一版，故发布前须确认，见 §十 #4）。

### 11.1 验证证据（本地实测，2026-10-11）

| 验证项 | 命令 / 判据 | 结果 |
| --- | --- | --- |
| 生成器工程编译 | `dotnet build Mud.HttpUtils.Generator.csproj` | **0 错误**（3 条既有 CS1574 警告不变） |
| ToolSurface 测试 | `dotnet test ... -f net10.0 --filter "FullyQualifiedName~ToolSurface"` | **62 通过 / 0 失败**（原 53 + 新增 9） |
| Generator 全量测试 | `dotnet test ... -f net10.0` | **1107 通过 / 0 失败** |
| 解决方案编译 | `dotnet build Mud.HttpUtils.slnx` | **0 错误**；`RS0016/0017 = 0`（PublicAPI 已加 `AffordanceDirectory.get/set`）；仅剩既有的 `RS0024`（Resilience）/`RS0027`（Client） |
| 既有 golden 零漂移 | `WithoutAffordanceDirectory_DescriptorIsByteIdenticalToEnabledButAssetless`（新增用例） | 通过——**空串槽与"有槽无资产"描述符逐字节相同**，且 golden 不含 `affordance` |
| Demo 端到端 | `dotnet build ToolSurfaceDemo.csproj -t:Rebuild -p:EmitCompilerGeneratedFiles=true` 后检视 `DemoToolSchemas.g.cs` | 通过——`doc.delete` / `doc.list` 四节均为数组、项目符号各成一条、反引号引用（`doc.list`）通过校验、转义正确（`\"删除\"`）；落盘目录已清理 |

> 组件侧**未**在本阶段运行 `pack.ps1 Release` 与 nuget.org 发布（§七 阶段②③），
> 二者是发布动作，须在**版本与预算取值确认后**执行（§十 #4/#5）。

### 11.2 与核验结论的对应（自证清单）

| 核验发现 | 本阶段处置 |
| --- | --- |
| §0.1 第 4 条（`GoldenUpdatePropertyName` 非开关） | 本方案**不引用**该槽，也不新增任何属性/环境变量 |
| §0.1 第 6 条（编号须落实） | 已落为 **028–031**，并加 `MaxSlot 27 → 31`、槽位表升序唯一（契约用例） |
| §0.2 G-5（值相等无守卫） | 已补 `ProfileSlotSurface_*` + `ProfileSlots_EverySlot_ShouldParticipateInEqualityAndHash`（含金丝雀自证） |
| §0.2 G-1/G-2/G-3/G-4（本仓门禁链） | 属阶段④，已写入 §四 #11–14 |
| §0.3（`_meta` 非模型可见面） | 文档已订正；组件侧 README 明示"是否进模型上下文由宿主决定" |
| §0.4（冗余边界） | README 新增「与 `GuidanceDirectory` 的分工」；结构校验强制四节非空以抑制"半个 description" |
| §0.5-1（缺回归锚） | 已补字节级回归用例（见上表第 5 行） |
| §0.5-3（缺端到端） | 组件侧已补 Demo 端到端检视；**本仓侧四段同源用例**（描述符→宿主渲染→`_meta`→`guidance_read`）仍属阶段④ |
