# ADR-0005：事件外桥处置（DP-1 = 维持 Demo 参考实现 + 迁移指南）

- 状态：**已决策（默认 (b)；(a)/(c) 由用户确认后可升级）**
- 日期：2026-10-16（来源：`MudFeishu-AI-审查整改-Bug修复与功能完善方案-2026-10-16.md` §4 D-4 / §9 DP-1 / BUG-4）
- 影响面：`Demos/Mud.Feishu.Agent.Demo/Events/`、`documents/AIAgent/事件外桥迁移指南.md`、
  `Tests/Mud.Feishu.Agent.Demo.Tests/Events/`

## 背景

NDJSON 事件外桥（`FeishuEventNdjsonBridge` + `RegexEventFileRouter` + `FeishuEventEnvelope` +
`IFeishuEventSink` + `IFeishuEventFileRouter` + `FeishuEventCatalog`，共 37 条公开面）自 R-10 起
**退出库发布面**，落点 `Demos/Mud.Feishu.Agent.Demo/Events/`。移除的理由是结构性的：

- 外桥是「把事件交给**仓外进程**」的**产品主张**，而库本身不做 IO；
- 留在发布面等于对外承诺一条**无人验证**的路径（正确性只能靠单测维护）。

代价随即出现（审查报告 BUG-4）：**仓外 Agent 消费飞书事件失去官方支持通道**——进程内
WebSocket/Webhook 仍在，但它面向「进程内宿主」，不解决「事件如何交给另一个进程」。

## 决策

**默认执行 (b)：维持 Demo 参考实现 + 文档明示「复制 Demo 实现」为唯一受支持路径，并补迁移指南。**

三个候选的取舍：

| 选项 | 内容 | 处置 |
|---|---|---|
| (a) | 回归库发布面（提升为受支持组件，`Mud.Feishu.AI` 或新包） | **不选**。与 R-10 的核心理由直接冲突（库不做 IO），且要重新把 37 条公开面挂回 semver 承诺 |
| (b) | 维持 Demo + 文档 + 迁移指南 | **默认**。零回归风险；把「怎么复制」写清楚，等价于把库不承诺的部分变成**可自助**的部分 |
| (c) | MCP 事件资源化（`resources`/`subscriptions` 形态） | **登记为中期终态**，待 E-3（MCP HTTP 通道，GAP-10）落地后随其升级；当前 stdio 单进程形态与之不匹配 |

**升级条件**：DP-1 由用户确认后，(a) 或 (c) 任一项均可升级；升级即须重开本 ADR（不得静默实现）。

## 理由

- **生态位与承诺一致性**：R-10 的裁定是「库不做 IO」，外桥的本质是 IO 主张。(a) 会把一条
  与架构价值观相反的路径重新放回发布面，收益（省一次复制）远小于风险（一条无人验证的公开承诺）。
- **(b) 的真实成本是文档，而非代码**：外桥实现已存在且被 Demo 真实调用（`DomainEventsDemo`
  的三条事件落盘 + 落点断言 + 互斥闸门），复制成本是「两个文件」；(b) 把它明示出来即闭环。
- **(c) 才是终态但不该现在做**：事件流作为 MCP `subscriptions` 需要**服务器主动推送**与
  会话状态外置，而当前 MCP 是 stdio 顺序处理（防 AsyncLocal 租户串号的结构性决策，不推翻）。
  先做 (c) 会迫使 MCP 传输层提前重构，与 E-3 的阶段划分冲突。

## 后果

- **唯一权威实现**：`Demos/Mud.Feishu.Agent.Demo/Events/FeishuEventNdjsonBridge.cs`
  （含 `RegexEventFileRouter`）。Ways of consuming events out-of-process today = 复制这两份文件。
- **迁移指南**：`documents/AIAgent/事件外桥迁移指南.md`（复制步骤 + 三种落地路径 + NDJSON 格式 +
  互斥闸门 + 消费建议）。
- **回流禁止（已锁定）**：`Tests/Mud.Feishu.Agent.Demo.Tests/Events/` 持有两条守卫
  （外桥被 Demo 真实调用且带落点断言 / 外桥类型**只在** Demo 程序集、库程序集不得再暴露）。
- **中期升级路径**：(c) 落地时本 ADR 转为「被 E-3 取代」，并把迁移指南的第 3 节从「未交付」
  改为「推荐路径」。
- **库发布面零改动**：本决策不新增/删除任何 `Mud.Feishu.*` 公开类型。
