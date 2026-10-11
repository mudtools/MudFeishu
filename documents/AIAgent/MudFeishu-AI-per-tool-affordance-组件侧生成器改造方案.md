# Per-tool affordance（工具级决策指导）：组件侧生成器改造方案

> 角色：高级程序员 + 系统架构师
> 日期：2026-10-11
> 需求来源：`.docs/MudFeishu-AI系列-Bug修复与功能完善方案.md` §三 **F-7【P1·R-3 新增】per-tool affordance 决策指导层**
> 参照系：`documents/AIAgent/MudFeishu-AI-vs-lark-cli-对比分析报告.md` §五 P0.2；官方 `lark-cli` 的 `affordance/*.md`
> 结论先行：**本仓无法单独落地 F-7**——它必须改组件仓库 `D:/Repos/MudHttpUtils` 的通用工具面引擎
> （`Mud.HttpUtils.Generator` 的 `ToolSurface` 家族），本仓只做"剖面槽同步 + 资产编写 + 守卫 + 重固化"。
> 本文是**组件侧改造的可执行方案**（含本仓配套清单、兼容/版本策略、门禁与回滚）。

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
2. 与工具 Schema **同源同 pass** 发射（编译期常量 + golden 逐字节），运行期**零解析**（AOT 安全）；
3. 进入模型可见面：进程内工具 Schema 的 `x-feishu.affordance`；MCP `tools/list` 的 `_meta.affordance`；
   `feishu.guidance_read` 可按工具键读取。

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
| `AffordanceDirectory` | `string?` | `null` | affordance 资产根目录（相对工程根，形如 `/Affordance/`）。`null` = 该剖面不启用本特性 ⇒ **对既有消费方零影响** |

> **为什么是"可空 + 默认 null"而不是"必填"**：组件是多消费方产品（本仓只是其一），
> 必填槽会让所有既有剖面**立即编译失败**；可空槽把这次升级变成"**opt-in 的增量能力**"，
> 本仓显式给出目录即启用——这正是"未发布窗口期零成本"之外仍需保持的工程纪律：
> **组件升级不得强迫不相关的消费方改代码**。

配套槽位复用（不新增）：`SchemaExtensionKey`（本仓为 `x-feishu`）、`GeneratedNamespace`、`GoldenFileName`、
`GoldenUpdatePropertyName`、`DiagnosticPrefix`。

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
- 已知 record_id 且只要一条 → 用 `bitable.get_records_by_ids`（更省 token）；
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

- **JSON 走源生成**（引擎手工拼常量字符串，本仓 `ToolErrorPayloadSerializer` 同款做法）；
- 四个子键**值全部是数组**（`when` 也允许数组：允许多条"何时用"），便于模型与守卫统一处理；
- 键名用 snake_case，与 `x-feishu` 既有风格一致（`is_write` / `required_scopes`）。

### 3.4 诊断（纳入既有 `MUDFT` 族）

| ID | 级别 | 判据 |
| --- | --- | --- |
| `MUDFT0xx` (affordance 结构) | **Error** | 资产缺固定小节 / 未知小节 / 空小节 |
| `MUDFT0xx+1` (affordance 归属) | **Error** | 资产文件名不命中任何已策展工具名 |
| `MUDFT0xx+2` (affordance 引用) | **Error** | 资产内 `域.工具` 引用不存在 |
| `MUDFT0xx+3` (affordance 预算) | **Warning（基线）** | 单资产超字节/字符上界（进 `mudft-warning-baseline.txt` 基线 + 拦增量） |

> **级别取舍**：结构与归属是"策展错误"（人写错了），必须 Error；预算是"内容偏胖"，
> 天然 >0（有资产就有字节数），故走"基线 + 拦增量"——与既有 `MUDFT009/021` 同口径。

### 3.5 生成 pass 与 golden

- **同一 pass**：在既有 `ToolSurface` 生成流程中，`AdditionalFiles` 的 affordance 资产按
  `AffordanceDirectory` 归集 → 解析 → 与工具 Schema 一起发射（**不新增一次遍历**，避免"两条 pass 顺序耦合"）；
- **golden**：affordance 内容随 `FeishuToolSchemas.golden.txt` 逐字节固化 ⇒ 改一个字即 golden 漂移（可 review、可回滚）；
- **更新开关**：复用 `GoldenUpdatePropertyName`（本仓为 `FeishuToolGoldenUpdate`），不新增环境变量。

### 3.6 wire-stable 的前置校验（反射面冻结）

> ⚠️ **本项属"未发布窗口期"的一次性形态决策**：`x-feishu.affordance` 的**键名与数组形态**
> 一旦随包发出即成为对外契约（模型与 MCP 客户端会按它解析）。故本方案要求组件侧在**同一版本**
> 内定形，不得先发"草案形态"再改。

---

## 四、本仓侧配套清单（组件升级后执行）

| # | 动作 | 位置 | 判据 |
| --- | --- | --- | --- |
| 1 | 剖面新增槽值 `AffordanceDirectory = "/Affordance/"` | `SdkProfile/FeishuToolProfile.cs` | 槽位数 31 → 32，`FeishuToolProfileContractGuards` 同批更新 |
| 2 | `csproj` 增加 `<AdditionalFiles Include="Affordance\**\*.md" />` | `Mud.Feishu.AI.Tools.csproj` | 与 `Guidance\**\*.md` 并列（同一注入机制） |
| 3 | 编写 affordance 资产（**分层**，见 §六） | `Mud.Feishu.AI.Tools/Affordance/*.md` | 每份含四节 |
| 4 | golden 重固化 | `FeishuToolSchemas.golden.txt` | `$env:FeishuToolGoldenUpdate='true'` + `-p:FeishuToolRefreeze=true` |
| 5 | skills 重固化（若 affordance 进 SKILL.md） | `skills/**` | `$env:FeishuSkillsUpdate='true'` |
| 6 | MCP `tools/list` 携带 `_meta.affordance` | `Mud.Feishu.AI.Mcp/FeishuMcpToolServer.cs::BuildToolsListResult` | 新用例：字段与 `FeishuToolSchemas` 同源 |
| 7 | `feishu.guidance_read` 支持"按工具读 affordance" | `Internal/CapabilityLookupTools.cs` | 未命中列出候选（既有纪律：不臆造键） |
| 8 | 新增守卫 `AffordanceContractGuards` | `Tests/Mud.Feishu.AI.Tools.Tests/ContractGuards/` | 见 §五 |
| 9 | 文档计数 | `Mud.Feishu.AI.Tools/Readme.md`、`.docs/AI/状态基线.md` | `ToolSurfaceScaleContractGuards`（裸数字扫描面） |
| 10 | 版本升级 | 全仓 `Mud.HttpUtils*` 的 `PackageReference` | `TokenMultiAppContractGuards.MudHttpUtils_PackageReference_ShouldBeSingleVersion`（单一版本） |

> ⚠️ 第 10 项是**唯一**跨仓联动的硬约束：组件新版本必须 `pack.ps1 Release` 产出并**发布到 nuget.org**
> （本地源已移除）。若在本地联调，需按 `AGENTS.md` 的"三步刷新缓存"（`nuget locals` → `clean` → `build`），
> 并**必须 bump 组件版本号**——同版本重打包不会让下游缓存失效（历史事故 3 次）。

---

## 五、本仓侧守卫设计：`AffordanceContractGuards`

| 用例 | 判据 | 负例自证 |
| --- | --- | --- |
| `EveryAffordanceAsset_ShouldTargetAnExistingTool` | `Affordance/*.md` 文件名 ⊆ `FeishuToolNames.All` | 合成一份"孤儿"资产必须被点名 |
| `WriteTools_ShouldDeclareAvoidWhenAndPrerequisites` | 写类工具的 affordance **必含** `avoid_when` 与 `prerequisites`（写面是"误用代价最高"的面） | 合成缺节资产必须报红 |
| `AffordanceSections_ShouldBeExactlyTheContract` | 四节名逐字匹配、无未知节、无空节 | 合成未知节必须报红 |
| `AffordanceToolReferences_ShouldExist` | 资产内 `域.工具` 引用 ⊆ 工具名契约表 | 合成 `im.not_a_tool` 必须报红 |
| `AffordanceBudget_ShouldBeMeasuredAndBounded` | 全部资产的字符数合计 + 单资产上界，与 `FeishuGuidanceComposer.MaxGuidanceLength` 的关系**留测量记录** | 合成超界资产必须报红 |
| `Schema_ShouldCarryAffordance_OnlyForDeclaredTools` | golden 里 `x-feishu.affordance` 出现次数 == 资产数（防"资产写了但没进 Schema"） | 合成缺失必须报红 |

> **为什么这组守卫必须存在**：affordance 的失败模式与 guidance 同款且**更隐蔽**——
> 资产写了但没生效（文件名/节名写错）不会触发任何诊断，表现为"模型还是不看这些提示"，
> 从日志上完全看不出来。

---

## 六、预算与分层策略（必须同批测量）

affordance 进**模型可见 token 预算**，这是它与 guidance 的公共成本约束：

1. **先测量再分层**：实现第一版后，实测"全部资产合计字符数"与"单工具 Schema 增量字节数"，
   把结果写进 `状态基线.md`；
2. **推荐分层（首版）**：
   - **必须**：写类工具（74 个）＋ `high-risk-write`（10 个）——它们是"误用即不可逆"的面；
   - **建议**：模型高频误用的只读工具（如 `bitable.query_records` / `search.doc_wiki`）；
   - **不做**：低风险、单一用途的只读工具（affordance 只会重复 `description`）。
3. **预算触顶时的处置**：优先**压缩资产**（与 F-1 纪律 22 "域变宽第一反应是压缩文案"同源），
   而不是抬高 `MaxGuidanceLength`；agent 指令面的预算抬升必须走
   `GuidanceAssetContractGuards.GetGuidance_WithAllDomainsEnabled_ShouldNeverDropAnyDomain` 同批评审。

---

## 七、兼容性与发布编排

| 阶段 | 组件仓库（`D:/Repos/MudHttpUtils`） | 本仓（`MudFeishu`） |
| --- | --- | --- |
| ① 组件实现 | 加槽（默认 `null`）+ 解析 + 发射 + 4 条诊断 + driver 负例 | — |
| ② 门禁 | `pack.ps1 Release`（10 包 + DLL SHA256 自检） | — |
| ③ 发布 | 发布到 nuget.org（**必须 bump 版本**） | — |
| ④ 本仓接入 | — | bump `Mud.HttpUtils*` 版本（单版本）→ 剖面加槽 → 资产 → golden/skills 重固化 → 守卫 → PublicAPI 同步 |
| ⑤ 验收 | 组件侧 driver 全绿 | 四 TFM Release `0 错误`、`RS0016/0017 = 0`、AOT 净零、golden/skills 零漂移、全量测试全绿 |

**回滚路径**：`AffordanceDirectory = null` + 删除资产目录 ⇒ 引擎回到"不发射 `affordance`"的形态，
golden 回退到接入前逐字节一致（**这是"可空槽"设计的直接收益**）。

**BREAKING 判定**：对**本仓**是"新增能力"（非破坏）；对**组件其他消费方**是"新增可选槽"（非破坏）。
按组件仓库的版本纪律，这仍是一次**minor** 版本递增（新增公开特性属性成员）。

---

## 八、验收标准（DoD）

| 门禁 | 判定 |
| --- | --- |
| 组件侧 | 新增槽位对既有剖面（`AffordanceDirectory` 未设）**逐字节 golden 不变**（回归自证） |
| 组件侧 | 4 条新诊断各有 driver 负例（含"必须报红"自证） |
| 本仓 | 剖面槽 32 个，`FeishuToolProfileContractGuards` 全绿 |
| 本仓 | `AffordanceContractGuards` 6 用例全绿，每条含负例自证 |
| 本仓 | golden / skills 零漂移；四 TFM `0 错误`；`RS0016/0017 = 0`；AOT `IL2026/IL3050 = 0` |
| 本仓 | MCP `tools/list` 的 `_meta.affordance` 与 `FeishuToolSchemas` 逐字段一致（并入 `ToolSurfaceConsistencyContractTests`） |
| 预算 | 实测"合计字符数 / 单工具增量字节数"写入 `状态基线.md`，并与 `MaxGuidanceLength` 的关系有记录 |

---

## 九、与既有决策的关系（不产生冲突）

- **F-1（策展补齐）**：新域应**直接带 affordance**（避免二次开洞）——这是 F-7 原文的依赖项，已满足（F-1 P0 已清零）。
- **F-8（Skills 闭环）**：已交付"读取接缝 + 版本一致性 + Demo 首消费方"。若 affordance 进 SKILL.md，
  仅是同一产物的内容增量，不改变 F-8 的结论。
- **F-9（MCP envelope 导出）**：`McpEnvelope` 已导出 `inputSchema` + `annotations`；affordance 落地后
  同一方言需补 `_meta.affordance`（同一数据源，无第二真相源）。
- **F-12（不采用裸逃生舱）**：affordance 是"**引导模型用对工具**"，与"提供绕过执行链的通道"方向相反，无冲突。
- **R5 配置治理**：本方案**不新增任何配置键**（开关是编译期剖面槽 + 资产目录）。

---

## 十、未决项（需产品/架构确认）

1. **键名与形态**：`x-feishu.affordance` 的四子键命名（`when` / `avoid_when` / `prerequisites` / `tips`）
   是否需要与官方 `lark-cli` 的 `affordance/*.md` 小节名**逐字对齐**（便于跨生态比对）；
2. **是否进 SKILL.md**：SKILL.md 面向"读文件的 Agent"，affordance 进入会让文件变长（外部 Agent 的读取成本）；
   备选是"只在 `x-feishu` 与 MCP `_meta` 里给，SKILL.md 保持现状"；
3. **组件仓库排期**：本次改造需要组件仓库一个 minor 发布周期，需与 `Mud.HttpUtils` 的其他待办合并评估。
