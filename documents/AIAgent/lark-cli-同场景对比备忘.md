# 与官方 lark-cli 同场景对比备忘（T1-11）

> **日期**：2026-09-27
> **任务**：Phase 1 T1-11（DoD：[AI-Native-Agent-Phase1-Design.md](../../.docs/AI/AI-Native-Agent-Phase1-Design.md) §5「与官方 `lark-cli` 的同场景对比备忘」）
> **场景**：「查多维表格里的记录」——同一业务目标，两条实现路径
> **对比口径**：①延迟 ②权限模型 ③类型安全（Phase 1 §5 DoD 三维度）
> **结论前置**：分层互补、非替代（已决策⑥）。**本框架不要求运行期依赖 lark-cli**（对比仅作文档备忘）；进程内多租户授权执行链是官方 CLI 不承担的职责，也是本规划的护城河。

---

## 1. 同场景两条路径

| | **Mud.Feishu 进程内工具链**（Phase 1） | **官方飞书 CLI**（`lark-cli`，Go 二进制） |
| --- | --- | --- |
| 调用形态 | 模型 tool_call → `AIFunction`（Schema 编译期产出）→ `FeishuToolBinding` 执行链 → **进程内直连**强类型接口 | 外部编码 Agent（Claude Code 等）→ 子进程 `lark-base records search` → stdout JSON |
| 运行依赖 | NuGet 引用即可，零外部进程 | 安装/升级 Go 二进制，Agent 每次调用经 JSON 管道 |
| 本仓交付物 | `Mud.Feishu.AI.Tools`（**114 个策展工具：62 只读 + 52 写类，跨 16 个业务域 + 4 个元工具**；口径见下表） | **实测 15 个服务 / 251 个方法 + 20 个 shortcuts 域**（原"18 域 200+ 命令"为 README 口径，R3 逐文件计数修正）+ 通用 OpenAPI 调用层（**广度官方胜**，已决策⑥不比广度） |

> **工具面数字口径（R7 更新，2026-10-09）**：本仓工具数为 **114**（62 只读 + 52 写类，跨 16 个业务域 + 4 个元工具）。
> **权威数字以编译期产物为准**：`FeishuToolNames.All` / `FeishuToolSchemas.golden.txt`
> / 《工具权限对照表》三者由契约守卫断言精确相等。
> 三层结构（**目录全量 / 暴露策展 / 能力出路**）见
> `Mud.Feishu.AI.Tools/Readme.md` §4。

## 2. 三维度对比

### ① 延迟

- **进程内**：tool_call 参数在内存中直接交给强类型客户端，无进程启动、无 JSON 管道序列化往返；同一进程内还可以与事件层（WebSocket/Webhook）、会话存储共享连接池与令牌缓存（`FeishuAppManager` 令牌命中即免鉴权请求）。
- **CLI**：每次命令一次子进程冷启动（Go 二进制启动本身快，但叠加 JSON 管道编解码与凭据文件读取）；命令间无共享会话状态，令牌管理由 CLI 自行缓存，跨进程缓存命中依赖其本地存储。
- **结论**：单次调用差距对批量 tool_call 场景会被放大——Agent 一轮对话常连续发多次工具调用，进程内共享 HTTP 连接池与令牌缓存的收益是**结构性**的；CLI 的广度优势不改变这一点。

### ② 权限模型

- **进程内（宿主强制门禁）**：白名单（`FeishuToolRegistry`：注册不启用，需 `MapTool`/`FeishuAgent:Tools`）→ `IToolExecutionAuthorizer.AuthorizeAsync` 强制门禁（`EnforceToolAuthorization=true` 时写工具未注册授权器即拒绝）→ `BeginScope(appKey)` 租户切换 → 执行。拒绝路径**零调用**下游接口、不切入租户上下文（单测锁定）。`required_scopes` 随 Schema 产出并进 OTel 审计属性（`feishu.tool.scopes`/`feishu.tool.decision`）。
- **CLI（调用方自愿）**：`--dry-run` 是**调用方自愿**的执行前预览；scope 补权引导面向终端用户授权流；CLI 自身不提供「服务端强制拒绝」语义——官方 embed 指引明确把集中凭据/审计/命令面限制**留给宿主**。
- **结论**：这是两类件的本质分工。CLI 管「外部 Agent 怎么方便地调飞书」；本框架管「你的 .NET 服务里，Agent 调哪些工具、以哪个租户身份、被谁批准」。护城河硬验收（双 appKey 切换 + 授权拒绝 + scope 审计）已在本批次落地为可演示/可断言用例（`FeishuToolBindingTests`）。

### ③ 类型安全

- **进程内**：编译期强类型——工具 Schema 由 `[FeishuTool]` 源生成器产出（字符串常量，零运行时反射，AOT 净零）；执行走源生成 HTTP 客户端；`FeishuApiResult<T>` 统一解包（`code != 0` 转可读错误回填模型）；结果白名单投影 + `MaxToolResultLength` 截断防超窗。
- **CLI**：命令参数经 shell/JSON 管道传递，出参为 JSON 字符串——由调用方（Agent 或宿主代码）自行解析；错误语义靠约定码值，无编译期保障。
- **结论**：进程内的错误语义显式化（解包不变式 + 结构化错误文本 `[tool_error] <tool>: <reason>`）让模型能理解失败原因并自我修正；类型安全同时换来 AOT/原生部署能力（CLI 为子进程形态，AOT 宿主引入它意味着发布面外溢，已决策⑥明确不做）。

## 3. 一页结论

1. **广度让给官方**：CLI/MCP/Skills 的命令广度与工具分发生态不重复建设（已决策⑥/⑦/⑧）。
2. **深度守住四要素**：进程内直连、强类型 + AOT、多租户授权执行链、事件-会话-模型-工具一体化闭环——均为官方件（子进程/跨进程形态）不承担的职责。
3. **可验证差异**：本批次以单测把「为何不用官方 CLI」固化为证据——`FeishuToolBindingTests`（授权拒绝零调用、双 appKey 隔离、scope/decision 审计）+ 工具名契约守卫；后续 Phase 2 写工具授权器强化后持续跟踪 CLI 演进（§5.6 风险行）。

> **维护约定**：本备忘为 Phase 1 静态对位结论（证据时点 2026-09-24 CLI 状态复核 + 2026-09-27 实现落地）；官方 CLI 周更节奏下，若未来出现服务端授权链/会话编排能力，须重新评估 §3.2 差异（对齐路线图 §5.6 风险应对）。
