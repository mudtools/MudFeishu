# Skills 外部消费实证报告（E-4 · GAP-12 / GAP-7）

> 日期：2026-10-21
> 方案：`.docs/AI/MudFeishu-AI-审查整改-Bug修复与功能完善方案-2026-10-16.md` §E-4
> 判据产物：`Tests/Mud.Feishu.AI.Tools.Tests/ContractGuards/SkillsExportContractTests.cs`
> 导出器：`Mud.Feishu.AI.Tools/Catalog/FeishuSkillDocumentBuilder.cs`

## 0. 结论摘要

| 项 | 结论 |
|---|---|
| frontmatter 兼容性 | ✅ 被真实 harness 接受（含非标准键 `package_version`，无告警、无丢弃） |
| 三层渐进披露 | ✅ 可路由：L0 列表只带 `description`、L1 显式加载、L2 `references/*.md` 需再取一次 |
| 格式不兼容 | ⚠️ **发现两处真实缺陷并已在导出器修复**（27 条硬切吞掉权限串；14 个域包的 `feishu.guidance_read` 指针在文件形态下是死链） |
| 内容不一致（非导出器问题） | ⚠️ 登记 4 枚工具的散文与 `required_scopes` 不符、2 枚 user 身份工具散文未提身份 ⇒ 转**策展**待办 |
| suite 聚合（GAP-7） | ✅ 新增 `skills/feishu-suite/SKILL.md`（34 域条目 + 规模数字 + 路由判据），产物 34→**35** 个 `SKILL.md` |
| harness 覆盖度 | ⚠️ **只有一个真实 harness**（Qoder）。Claude Code / Cursor 本机不存在，未测。见 §6 |

## 1. 消费路径与环境约束

- 方案原建议「Claude Code + Cursor」两个 harness。本机实测：`claude` / `cursor` / `codex` 均不在 `PATH`，
  `AppData/Local/Programs` 无对应安装 ⇒ 安装第三方 CLI 需要联网与外部账号，不属于本轮可自主完成的动作。
- 改用的真实 harness = **Qoder 技能面**（与 Claude Code 同源的 `SKILL.md` 约定：目录即包、frontmatter
  `name`/`description` 进发现列表、正文按需注入）。消费方式是把导出产物**逐字**复制进
  `.qoder/skills/`（`.qoder` 已 gitignored ⇒ 复制不污染版本面），然后用 harness 的原生命令加载：

```bash
cp -r skills/feishu-im skills/feishu-suite skills/feishu-bitable skills/feishu-drive skills/feishu-ai .qoder/skills/
```

- 这一步是**必要的**：harness 只发现 `<项目>/.qoder/skills/`，不知道 `skills/` 的存在。
  也就是说，"包内产物随 NuGet 分发"与"宿主真正能读到它"之间确实还有一次人工落盘 —— 这条路径本身可行、无报错。

## 2. 三层渐进披露的实测行为

| 层 | harness 行为 | 实测证据 |
|---|---|---|
| **L0 发现** | 技能列表里每一项显示导出器写的 `description` 原文，**不读正文** | 5 个复制进去的包全部出现在列表里，描述与 `skills/*/SKILL.md` frontmatter 逐字一致（含 `feishu-suite` 的「34 个域技能包、365 个工具（只读 188 / 写 177）」） |
| **L1 加载** | 显式调用后注入整份正文，并打印 `Base directory for this skill: …` | `feishu-im` 11,143 B、`feishu-suite` 7,815 B 一次载入完成（无截断、无编码问题、LF 保留） |
| **L2 细则** | **不自动加载** `references/*.md`；只给出 base directory ⇒ Agent 需再发一次文件读取 | `feishu-im/references/{cards,topics-and-replies}.md` 等 17 个 L2 文件在包内就位，需显式读取 |

- L0 的成本只有 `description`（全树最长 **121** 字符），L1 平均 **6,281** B/包，最大 `feishu-im` 11,143 B
  ⇒ "先读路由表、再按域加载一包"这个设计意图在真实 harness 里成立；单入口 `feishu-suite`（7,815 B）
  确实足以完成"选域"这一步而不必先读 34 个包。
- 非标准 frontmatter 键（`package_version`）与多行 `description` 未被拒绝 ⇒ **F-8 的版本自证字段可以留在产物里**。

## 3. 结构审计（机械对账，可复现）

| 指标 | 实测 | 对账对象 |
|---|---|---|
| `skills/**/*.md` | **52** 个文件，共 243,302 B | — |
| `SKILL.md` | **35** 个（34 域 + 1 suite） | 域数 34 与 `FeishuToolGuidance.ByDomain` 闭合 |
| L2 `references/*.md` | **17** 个 | 与 L2 guidance 资产 17 篇一致（`ReferenceFiles_ShouldMatchGuidanceAssets_Verbatim`） |
| 工具清单行 | **365** 条 | ≡ `FeishuToolSchemas.golden.txt` 365 行（`SkillToolList_ShouldMatchToolNames_Bidirectionally`） |
| 超预算描述 | **0** 条（上限 240） | `ToolDescription_ShouldNotLosePermissionOrIdentityJudgement` |
| 触发截断并回填尾注 | **27** 条分布在 **14** 个域包 | `ClampDescription_*` |

## 4. 发现的问题与处置

### 4.1 硬切吞掉契约判据（已修，导出器）

修复前实测：27 条工具行被 240 字符**按字符**硬切，切口落在权限串或身份要求中间，判据直接消失。样例：

```
修复前  ……撤回用 im.delete_reaction。reaction_id 在本工具            ← 句子被切断，权限串 im:message 整段丢失
修复前  - `minutes.upload`（写）：……用户身份工具：宿主须在…           ← user 身份要求被切断
```

处置（`ClampDescription`）：**先按标点边界切**（`。！？；，、 `（`）」』`），**再在行尾补一份从
`x-feishu` 元数据推导的契约尾注**，且只要发生截断就回填**完整**契约（非 tenant 身份 + 全部 `required_scopes`）：

```
修复后  ……reaction_id 在本工具回执里。写操作：默认空名单不启用… 〔需 im:message〕
修复后  ……用户身份工具：宿主须在… 〔user 身份；需 minutes:minutes.upload:write〕
```

- 为什么"尾注取自元数据而不是散文"：散文会漏（见 4.3），而 `required_scopes`/`identity` 是执行期真正生效的那份。
- 为什么"截断时给全量而不只补缺席的那条"：第一版只回填散文里缺席的 scope，结果 `minutes.upload` 的 user 身份
  在预算收缩后仍被切掉 —— 守卫 `ToolDescription_…` 报红。这条判据由此从"补缺"改为"截断即全量重申"。
- **短描述逐字节不变**（`ClampDescription_ShouldNotTouchShortDescriptions`）⇒ 本轮只有 27 条清单行 +
  14 个域的尾注段发生变化，其余产物零 diff。
- 复核时另修一处同源缺陷：尾注构造函数在「既无 `required_scopes` 又只有默认 tenant 身份」时返回空壳
  `〔〕`，使 `ClampDescription` 的"无契约即纯边界截断"分支永不可达（消费方会把空括号读成"有要求但没写出来"）。
  按当前实面它是**休眠分支**（365 枚里只有 `feishu.capability_lookup` 无 scope，其描述 153 字符不触发截断），
  但一旦有长描述的工具不带 scope 就会写成噪声 ⇒ 已改为返回空串，并给 `[Theory]` 补上第四极
  `("", "")` 断言"截断行不得出现 `〔`"。修后 skills 产物逐字节零差异（由 `SkillsTree_ShouldMatchExporterOutput` 证明）。

### 4.2 `feishu.guidance_read(...)` 指针在文件形态下是死链（已修，导出器）

L1 正文用 `feishu.guidance_read(im, cards)` 指向细则。这是**进程内工具**调用形态；只拿到 `skills/` 文件的
Agent（本 harness 即如此）没有这个工具，于是 14 个域包里的 14 条指针全部无法执行。

处置：导出器**不改写正文**（改了就是第二份真相源），改为在每个有细则的包末尾追加一条**解码规则**：

```markdown
- 正文里形如 `feishu.guidance_read(im, 主题)` 的指针，在文件形态下读本包的 `references/主题.md`（本包细则就是那份资产，无需进程内工具）
```

守卫 `GuidanceReadPointers_ShouldResolveToReferenceFiles` 逐条扫全部产物：指针的「域, 主题」必须能在
`skills/feishu-{域}/references/{主题}.md` 落位；字面量 `domain`/`域`/`主题` 判为教学模板跳过；
`scanned > 0` 防假绿。实测 30 处命中 = **14 条真指针全部解析成功** + 14 条域专属解码行 + 2 条通用模板。

### 4.3 散文与元数据不符（登记为策展待办，未改导出器）

对 golden 逐条比对「`required_scopes` 是否出现在 description 原文」，4 枚不符：

| 工具 | 元数据要求 | 散文情况 |
|---|---|---|
| `feishu.schema_read` | `feishu:base` | 未提 |
| `im.send_message` | `im:message:send_as_bot` | 只写了 `im:message` |
| `im.send_card` | `im:message:send_as_bot` | 只写了 `im:message` |
| `knowledge.search` | `aily:knowledge:readonly` | 未提 |

另有 2 枚 user 身份工具（`task.list_my_tasks` / `task.search_tasks`，27 枚非 tenant 身份工具中仅此 2 枚）
散文未声明身份要求。**这是策展层的内容缺陷，不是导出器缺陷** —— 导出器的契约尾注已保证消费方在截断形态下
仍能看到正确要求；根治要在下一批改 `[AgentTool]` 描述文案（会触发 golden 重固化，不属于 E-4 范围）。

## 5. suite 聚合入口（GAP-7）

`skills/feishu-suite/SKILL.md`（7,815 B）由 `BuildSuiteMarkdown` 发射，内容全部是**推导**结果：

- 域路由表 34 行，每行 `- \`feishu-{域}\`（n 工具：只读 r / 写 w）：{GuidanceRoutingSummary.RouteBody(...)}`
  —— 行体与 MCP 路由摘要**共用同一次推导**（`RouteBody` 是从 `Create` 里抽出的共用段），不存在第二份真相源；
- 规模数字（34 域 / 365 工具 / 只读 188 / 写 177）与 `feishu-suite` 自身描述同源，由
  `SuiteSkill_ShouldRouteEveryDomainPackage` 逐行重算并与 `FeishuToolNames` 对账（域集合双向闭合、
  `read + write == total`）；
- **不复述任何域内安全规则**（避免与 34 份域包形成漂移对）。

GAP-7 登记的「31 域条目」是方案撰写时点的域数，实测已长到 34 ⇒ 守卫按当前域集合断言，不写死数字。

## 6. 诚实边界（单 harness）

- 本轮实证覆盖 **1 个** harness（Qoder）。Claude Code、Cursor 与自建 Agent（`Mud.Feishu.AI` 的
  `FeishuAgent`）路径**未做真机加载记录**。可迁移的只是 `SKILL.md` 约定层（目录 + frontmatter + 正文），
  各 harness 的发现/注入策略仍可能有差异。
- 未覆盖项：① harness 的 `description` 截断策略（Qoder 全文显示；未知是否所有 harness 都如此）；
  ② 多包共存时的**选域准确率**（这属于 E-1 评测层，不在消费实证层）；③ NuGet 包安装后的实际落盘位置。
- 若要补第二个 harness：需要联网安装第三方 CLI（外部动作），建议由宿主明确决定后再做，不在本轮自主推进。

## 7. 复现步骤

```bash
# 1) 重固化 skills 产物（改了工具描述或导出器时）
FeishuSkillsUpdate=true dotnet test Tests/Mud.Feishu.AI.Tools.Tests --filter "FullyQualifiedName~SkillsExportContractTests"

# 2) 装进 harness 可见目录并加载（本项目 .qoder 已 gitignored）
mkdir -p .qoder/skills && cp -r skills/feishu-im skills/feishu-suite .qoder/skills/

# 3) 消费面守卫（结构审计 + 契约判据 + 死链判据）
dotnet test Tests/Mud.Feishu.AI.Tools.Tests --filter "FullyQualifiedName~SkillsExportContractTests"          # 17 条
dotnet test Tests/Mud.Feishu.AI.Tools.Tests --filter "FullyQualifiedName~SkillsVersionConsistencyContractGuards"  # 读取闭环 + 版本自证
```

实测（2026-10-21）：`Tests/Mud.Feishu.AI.Tools.Tests` **1,729 通过 / 0 失败**（E-4 +8：
`SuiteSkill_…` 1 + `ToolDescription_…` 1 + `ClampDescription_ShouldKeepContractTokens_WithinBudget` 4 个形态 +
`ClampDescription_ShouldNotTouchShortDescriptions` 1 + `GuidanceReadPointers_…` 1）。
标准档门禁全绿：**5,603 通过 + 25 门控跳过 = 5,628 总计、0 失败**（golden 逐字节零漂移、AOT 严格模式净零、
skills 产物在最后一轮修复后**逐字节零差异**）。
