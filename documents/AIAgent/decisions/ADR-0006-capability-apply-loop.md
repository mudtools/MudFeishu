# ADR-0006：长尾能力覆盖走「自省—申请闭环」（DP-2 = (a)）

- 状态：**已执行（默认 (a)；(b) 需重开本 ADR）**
- 日期：2026-10-19（来源：`MudFeishu-AI-审查整改-Bug修复与功能完善方案-2026-10-16.md` §5 S-3 / §9 DP-2 / GAP-5）
- 影响面：`feishu.capability_lookup`、`feishu.schema_read` 的输出面与描述、
  `Mud.Feishu.AI.Tools/Catalog/CapabilityApplyLoop.cs`、`Guidance/feishu.md`

## 背景

SDK 方法目录 1268 个方法 / 1266 个唯一限定名（2 处同接口同名声明在键上合并），其中 342 条已策展
（对应 344 枚工具）⇒ **73% 的方法没有可调用工具**。在 R-12
（已删除 `feishu.api_call` 万能兜底通道）之后，模型查到一个存在但未策展的方法，只能得到
「该能力暂不可用」——用户视角是「这个 SDK 明明有，你们的产品说没有」，长尾覆盖失去出口。

三个候选（方案 §9 DP-2）：

| 选项 | 内容 | 处置 |
|---|---|---|
| (a) | 自省—申请闭环：元工具回「存在性 + 缺口计数 + 候选策展名 + 申请通道」 | **选定**（方案默认，本轮执行） |
| (b) | 强约束逃生舱：受白名单/审批约束的「按签名直调」通道 | **不选**。与 R-12 的价值观裁定直接冲突（一条"任意方法 + 任意参数"的面会绕开每工具一策展的授权、幂等与风险分级），且需要新的审批子系统——本轮禁止新增进程/包 |
| (c) | 维持现状（只回「不存在/未策展」） | **不选**。等于把 GAP-5 长尾永久固定为「用户自己猜」 |

**升级条件**：若 (b) 要被重新考虑，必须先撤销 R-12 的裁定并另立 ADR；不得在本 ADR 之下
顺手加一条调用通道。

## 决策

未策展面从「死胡同」改为**可申请面**，两个元工具各加一层投影，零新架构（数据源已是编译期常量）：

- `feishu.capability_lookup`：每个命中分组追加 `curated_methods` / `uncurated_methods`（缺口口径与
  `MethodsByDomain` 同源），未策展且候选名可用时追加 `suggest_tools`（单组上限
  `MaxSuggestionsPerGroup = 10`）；信封追加 `suggest_group_count` 与 `apply_channel`。
- `feishu.schema_read`：`curated=false` 的方法追加 `suggested_tool`；信封追加同一 `apply_channel`。
- `apply_channel` 是**单一来源常量**（`CapabilityApplyLoop.ApplyChannel`），两处共用——同一事实
  写两份必然漂移（守卫判据⑦锁死）。文案明确：策展 = 声明 `[AgentTool]` + `[ToolHandler]` 执行器
  + 权限对照表/scope 登记 + golden/skills/affordance 同批刷新，且**建议名不可调用**。

## 关键实测：候选名只能是「候选」

对既有策展面逐条比对「机械投影名」与「实际工具名」（样本 = `FeishuToolMethodCatalog` 里
`curated && curated_tool != null` 的 342 条）：

| 口径 | 实测 | 结论 |
|---|---|---|
| **模块前缀**一致率 | **95.6%**（15 条不一致） | 值得锁——跌破即说明别名表或投影规则漂移 |
| **全名**一致率 | **23.1%** | 不可当契约——策展命名是人类的语义行为 |

15 条不一致全部是同一类语义事实：**能力写在 A 域接口上，工具落在资源的归属域**。例如
`Role.CreateRoleAsync → bitable.role_create`、`WorkCity.GetWorkCityByIdAsync → org.get_work_city`、
`AttendanceUserSettings.DownloadFileAsync → drive.download_file`、
`DriveFiles.GetFileContentByFileTokenAsync → docx.get_markdown_content`、
`ChatGroupMember.GetMemberPageListByIdAsync → im.list_chat_members`。别名表按**分组首段**工作，
把这些补进别名会把整组（含真正属于原域的方法）一起改道，因此**接受 <100% 并锁下限**，
而不是为了凑数放宽比对轴。

另两条实测前提（决定了无需新架构、以及为什么必须做冲突抑制）：

- 能力目录与方法目录共用同一分组轴：**204/204 组、逐组方法数 0 失配** ⇒ 缺口计数可以直接从
  方法目录回填，不必另建索引；
- 投影名可能与**既有工具名**相同（策展人恰好取了同一个名字），也可能在未策展面内部重复
  ⇒ 执行器对候选名做两道抑制：**命中 `SchemaByToolName` 的不输出**（否则模型把「建议宿主策展」
  读成「已有可用工具」），同组内去重并受 `MaxSuggestionsPerGroup` 限流。

## 边界（与既有裁定的张力点）

- **不构成第二条调用路径（R-12）**：`CapabilityApplyLoop` 只做命名投影，不产出 HTTP/路由/参数形状，
  不提供任何执行入口；`apply_channel` 文案显式否定可调用性。
- **不破 D4（capability_lookup 不回方法级事实）**：方案 (a) 的字面表述含「对应接口方法名」，本轮
  **有意不照做**——分组级只回计数与建议名，SDK 方法名/请求构造继续只由 `schema_read` 承载
  （既有 D4 用例 `NotContain "route"/"http"/"Async"` 原样通过）。方法的「名字」只在用户已经指名
  要查它时给出，避免 capability_lookup 变成半个 schema 浏览器。
- **未策展期间的正确处置不变**：如实告知用户「能力存在但当前工具集没有」，建议名不改变这一点。

## 后果

- 新增：`Mud.Feishu.AI.Tools/Catalog/CapabilityApplyLoop.cs`（投影 + 单源文案）、
  `Tests/Mud.Feishu.AI.Tools.Tests/ContractGuards/CapabilityApplyLoopContractGuards.cs`（7 条判据：
  解析器自证 `[Theory]` 14 例 / `Unparsed` 无候选 / 全目录只出工具名形态 / 复用别名表 /
  模块前缀一致率 ≥0.90 / 确定性 / 申请文案单源）。
- 修改：两个执行器（`CapabilityLookupTools` / `SchemaReadTools`）、两个接口声明的 Description 与
  `<returns>`、`Guidance/feishu.md`（+78 字符，1927/2048 预算内）、golden 重固化（仅两行描述差异）。
- **不新增工具、不改 scope、不改 public 面**（`suggested_tool` 只是 JSON 载荷里的字段）。
- 后续若把某个建议名真正策展出来，走的就是 `apply_channel` 写的六件套流水线，无需本 ADR 之外的新决策。
