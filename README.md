# MudFeishu

<div align="center">

![MudFeishu Logo](icon.png)

飞书生态的 **AI 原生** .NET SDK —— 331 个策展工具 + Agent 运行时 + MCP 服务器 + 人工确认管线，
让 AI Agent 经自然语言直接操作飞书组织、文档、审批、日历、任务、多维表格等 31 个业务域。

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Mud.Feishu](https://img.shields.io/nuget/v/Mud.Feishu?label=Mud.Feishu "Mud.Feishu")](https://www.nuget.org/packages/Mud.Feishu/)
[![Mud.Feishu.AI](https://img.shields.io/nuget/v/Mud.Feishu.AI?label=Mud.Feishu.AI "Mud.Feishu.AI")](https://www.nuget.org/packages/Mud.Feishu.AI/)
[![Mud.Feishu.AI.Tools](https://img.shields.io/nuget/v/Mud.Feishu.AI.Tools?label=Mud.Feishu.AI.Tools "Mud.Feishu.AI.Tools")](https://www.nuget.org/packages/Mud.Feishu.AI.Tools/)
[![Mud.Feishu.WebSocket](https://img.shields.io/nuget/v/Mud.Feishu.WebSocket?label=Mud.Feishu.WebSocket "Mud.Feishu.WebSocket")](https://www.nuget.org/packages/Mud.Feishu.WebSocket/)
[![Mud.Feishu.Webhook](https://img.shields.io/nuget/v/Mud.Feishu.Webhook?label=Mud.Feishu.Webhook "Mud.Feishu.Webhook")](https://www.nuget.org/packages/Mud.Feishu.Webhook/)

[架构](#ai-底座五层架构) • [工具面](#-工具面331-个策展工具不是-1268-个裸方法) • [端到端示例](#端到端示例读多维表格经人工确认写回记录) • [快速开始](#快速开始) • [CLI 对照](#与飞书官方-cli-的对照) • [文档](#详细文档)

</div>

---

## 为什么做这个

.NET 生态里飞书 SDK 长期是空白。仅有的一个社区项目停留在「能调通几个接口」的阶段——
覆盖面窄、无多应用支持、无实时事件、无 AOT、更无 AI 能力。当 AI Agent 要**自主操作飞书**
（创建审批、更新多维表格、发送邮件、管理日历）时，这套底子根本接不上：

- Agent 需要强类型参数 Schema，而不是 `Dictionary<string, object>` + 反射。
- 写操作需要权限分级、幂等保证、人工确认，而不是无差别放行。
- 多租户场景需要作用域隔离，而不是进程级全局单例。
- MCP 工具服务器需要 Native AOT 单文件冷启动，而不是 `Newtonsoft.Json` 反射序列化。

MudFeishu 填的就是这个空白——不是「又一个能调接口的 SDK」，而是 **.NET 上第一个为 AI Agent 时代设计的飞书底座**：

- **331 个策展工具**把飞书 API 编译期暴露为模型可调用的 Function Call，每个工具都有强类型 Schema、
  风险等级、权限范围、幂等语义——不是把 1268 个 SDK 方法无差别扔给模型。
- **七步执行链**在每次工具调用时依次过：上下文校验 → 入站净化 → 策略轴 → 授权门禁 → 租户切换 →
  内容安全 → 出站净化，**fail-closed 默认**，写工具未经授权一律拒绝。
- **人工确认管线**让高风险操作（发消息、创建审批、删除记录）先经宿主批准再执行，批准状态单一所有权，
  模型无法自批复。
- **MCP 服务器**把同一套工具面经 stdio JSON-RPC 暴露给进程外 Agent（Claude Code / Cursor / 自建 harness），
  执行链零旁路。
- **Skills 产物**随 NuGet 包分发，仓外 Agent 直接 `npx skills add` 即可获得飞书域知识。

AI 能力不是 Sample 层的补丁，是 SDK 的**第一公民**。

---

## AI 底座：五层架构

```
┌─────────────────────────────────────────────────────────────┐
│  ⑤ 生态位    MCP Server (stdio)  ·  Skills 导出  ·  NDJSON  │
├─────────────────────────────────────────────────────────────┤
│  ④ Agent     会话管理  ·  流式消息通道  ·  事件驱动对话     │
│              知识检索 (Aily + RAG)  ·  HITL 审批续跑        │
├─────────────────────────────────────────────────────────────┤
│  ③ 工具面    331 策展工具 (31 域)  ·  编译期 Schema         │
│              风险分级  ·  幂等键  ·  affordance 资产         │
├─────────────────────────────────────────────────────────────┤
│  ② 执行链    入站净化 → 策略轴 → 授权 → 租户切换            │
│              → 内容安全 → 出站净化 → 审计                   │
├─────────────────────────────────────────────────────────────┤
│  ① SDK 基座  1268 强类型接口  ·  源生成 JSON  ·  AOT 净零   │
│              多应用作用域  ·  令牌全生命周期托管            │
└─────────────────────────────────────────────────────────────┘
```

![技术架构](documents/AIAgent/images/05-arch.png)

### ① SDK 基座：1268 个强类型接口，Agent 看不到，工具面看得到

飞书全部 33 个 API 模块（组织架构、消息、群聊、审批、任务、日历、视频会议、文档、知识库、云盘、
画板、电子表格、多维表格、考勤、卡片、AI 能力、邮箱、搜索、服务台、OKR、妙记、妙搭……）
均为声明式接口 + 源生成实现类。Agent 不直接调这些接口——**工具面从中策展 331 个高频操作**暴露给模型。

### ② 执行链：七步不可变，fail-closed 默认

```
① appKey 上下文校验
①' 入站净化        —— 控制字符/危险 Unicode/独立 CR → 拒绝（invalid_args），零调用下游
② 策略轴           —— MaxToolRisk / AllowedIdentities → 拒绝（policy_denied: reason_code）
③ 授权门禁         —— IToolExecutionAuthorizer → 拒绝（authorization_denied）
                     NeedsUserConfirmation → 通知宿主批准通道，中性拒绝（不含任何凭据）
④ 租户上下文切换    —— UseAppScope(appKey)
④' 用户上下文       —— 仅 identity=user 工具：写入 AsyncLocal，finally 清理
⑤ 内容安全         —— 4 条注入规则扫描原始结果（off | warn | block）
⑥ 出站净化（强制）  —— ANSI/控制字符剥离 + 凭据与手机号脱敏；无开关、不可绕过
⑦ 整形钩子 + 审计   —— IToolResultShaper（可空）+ IToolExecutionAuditSink（允许/拒绝/错误三类投递）
```

**关键安全属性**：入站是「拒绝」而非「剥离」（剥离会静默改写用户内容）；出站净化无开关不可绕过；
授权器未返回 `Allowed` 的待确认调用**读、写工具同路径**走挂起解析；`dry_run=true` 的写工具不消耗幂等键。

### ③ 工具面：331 个策展工具，不是 1268 个裸方法

| 域 | 只读 | 写类 | 小计 | | 域 | 只读 | 写类 | 小计 |
| --- | ---: | ---: | ---: | --- | --- | ---: | ---: | ---: |
| 多维表格 | 13 | 15 | 28 | | OKR | 9 | 6 | 15 |
| 云文档 | 8 | 9 | 17 | | 视频会议 | 7 | 7 | 14 |
| Wiki | 5 | 6 | 11 | | 知识库 | 1 | — | 1 |
| 搜索 | 1 | — | 1 | | 画板 | 2 | 4 | 6 |
| IM | 8 | 12 | 20 | | 考勤 | 7 | 1 | 8 |
| 云空间 | 5 | 12 | 17 | | 妙搭 | 5 | 5 | 10 |
| 电子表格 | 8 | 17 | 25 | | AI 文本 | 2 | — | 2 |
| 通讯录 | 8 | — | 8 | | 邮件 | 10 | 14 | 24 |
| 审批 | 2 | 6 | 8 | | 妙记 | 5 | — | 5 |
| 日历 | 4 | 5 | 9 | | 任务 | 5 | 15 | 20 |
| 组织 | 8 | — | 8 | | 招聘 | 7 | — | 7 |
| 安全 | 2 | — | 2 | | 终端管理 | 1 | — | 1 |
| 元工具 | 4 | — | 4 | | 应用信息 | 7 | 2 | 9 |
| 群聊管理 | 4 | 12 | 16 | | 服务台 | 6 | 4 | 10 |
| 飞书词典 | 6 | 5 | 11 | | 关联组织 | 8 | 3 | 11 |
| Aily 数据知识 | 3 | — | 3 | | | | | |
| **合计** | **171** | **160** | **331** | | | | | |

**策展而非全暴露**：把 1268 个 SDK 方法无差别暴露给模型 = 模型上下文爆炸 + 无法做权限分级。
MudFeishu 按高频工作流策展为 331 个工具，每个工具身上都挂着六样东西：

- **编译期 Schema**（`FeishuToolSchemas`）：参数、必填、枚举，模型照着它填，字段拼错在本地就被拒
- **风险等级与 scope**：`read` / `write` / `high-risk-write` + 所需权限范围
- **`source` 三元组**：背后是哪个 SDK 方法、哪个 HTTP 方法、哪条路由——与真实 SDK 交叉校验，
  对不上直接构建失败（`MUDFT019`），防止改了 SDK 不动工具面、两边悄悄错位
- **输出 Schema**：把下游返回里该给模型看的部分挑出来
- **幂等语义**：写操作可带 `idempotency_key`（以各工具 Schema 为准），`dry_run` 可预演
- **per-tool affordance**（`x-feishu.affordance`）：「什么时候该用 / 什么时候不该用」的说明书——
  比如「转交审批」会直接写：这条审批不该由我处理、或我判断不了时才转交；本来自己能处理的就别转，
  那只会让流程多走一圈。这类东西没法指望模型去读文档链接，得跟工具一起进上下文

配套还有**类型化契约**（`FeishuToolContracts`，零运行期解析，注册器直接构造定义）与
**域 guidance**（31 个业务域 + 子主题 references）：宿主指令之后追加，2 KB 预算截断。

**策展粒度举例**：多维表格最终落了 28 项——记录的查 / 按 ID 取 / 增 / 改 / 删 / 批量增改，
再到表、字段、视图的元信息，外加表单、仪表盘、角色权限、自动化流程，正好够模型「把一张多维表格当业务库使」
的完整来回；它背后的 SDK 方法远不止这个数，重复的、只在后台配置里才用得上的都收起来了。
招聘域更明显：OpenAPI 层面能拆出二十来个资源，最后按功能子域收成 7 项就够了。

**4 个元工具**（`feishu.tool_search` / `feishu.schema_read` / `feishu.capability_lookup` / `feishu.guidance_read`）
不发任何飞书请求，只查编译期的方法目录（1268 个方法）与域指引。模型遇到未策展的能力时，
正确动作是问 `feishu.capability_lookup`（判断不存在 vs 未策展），再**如实告知用户**——而不是臆造一次调用。
**不存在万能兜底通道**（已整条删除，双轨调用路径风险最高）。

### ④ Agent 运行时：会话、流式、知识、事件驱动

```csharp
builder.Services.AddFeishuApp(builder.Configuration);
builder.Services.AddFeishuTools();                    // 工具面
builder.Services.AddFeishuAgent(builder.Configuration) // Agent 运行时
    .AddFeishuEditMessageChannel()                    // 流式消息通道（编辑已发消息）
    .AddFeishuImConversationHandler();                // IM 事件 → 对话
```

- **流式消息通道**：`EditMessageChannel`（流式编辑已发消息）、`CardStreamMessageChannel`（卡片流）、
  `StreamingChannelChain`（链式组合）——让 Agent 的流式回复直接呈现为飞书消息的实时更新。
- **事件驱动对话**：`ConversationalFeishuEventHandler` 把飞书事件（IM 收消息 / 审批任务 / 任务变更 /
  多维表格记录变更）变成模型可处理的上下文 + 回复，事件载荷经 `IContextAssembler` 装配为结构化片段
  （带 untrusted 标注、超预算截断、缺字段降级）。
- **知识检索**：
  - **RAG-A（Aily 托管）**：`AilyKnowledgeProvider` 接飞书 Aily 知识问答，零基础设施。
  - **RAG-B（自建检索）**：`DocumentChunker`（结构优先切片）→ `VectorRetriever` → `CorpusIndexer`，
    向量库与嵌入由宿主提供（SDK 不引入依赖），`CorpusChunk.ScopeKey` 必须按租户隔离。
  - 两个检索面用 `TryAddSingleton`，先注册者生效，**并存不冲突**。
- **HITL 审批续跑**：`FeishuAgent.RunApprovalContinuationAsync` 一次调用完成
  加载会话 → 取原始审批请求 → 重建租户上下文 → 续跑 → 落库。批准状态唯一所有者是宿主授权器，
  SDK 不签发任何凭据，模型无法自批复。

**运行时基座**：Agent 循环不自造，站在 `Microsoft.Agents.AI`（MAF）+ `Microsoft.Agents.AI.OpenAI`（v1.25.0）上——
MEAI 的 `FunctionInvokingChatClient` 负责自动 Function Call 并用调用预算框住（默认单轮最多 10 次工具调用），
MAF 的 `ApprovalResponseBindingChatClient` 把「写操作的人工确认」和原始请求配对（HITL 能成立的关键），
最底下经 `IChatClient` 对接任意 OpenAI-compatible 端点，换个 `modelId` 和端点即可。
可复用的中立运行时（11 阶段回合状态机、会话自愈存储、token 计量、域指引合成）在独立发包、独立版本线的
`Mud.AI.Agent` / `Mud.AI.Abstractions` 里，刻意不含任何飞书字面量；依赖边界是干净的——
MAF 只允许 AI 包引用，核心 SDK 一个都不碰，**不用 AI 就不会被拖进任何东西**。

### ⑤ 生态位：MCP 服务器 + Skills 导出

**MCP 服务器**（`Mud.Feishu.AI.Mcp`，独立可选包）：

```csharp
builder.Services.AddFeishuTools();
builder.Services.AddFeishuMcpServer(o => o.AppKey = "cli_xxx");
await app.Services.RunFeishuMcpStdioAsync();   // 阻塞到客户端关闭 stdin
```

- 白名单同源（MCP 暴露的工具 == `FeishuAgent:Tools` + `WriteAllowList`，不新增开关）
- 执行链零旁路（每个 tool call 走同一个 `FeishuToolBinding`）
- 租户边界 = 进程（appKey 只来自配置，协议层传 appKey 等于把切租户交给客户端）
- 协议层自研（不引官方 `ModelContextProtocol` SDK，仅覆盖 `tools` 能力 + stdio）

**Skills 产物**（随 NuGet 包分发）：

```csharp
IToolSchemaExporter.Export(ToolSchemaDialect.Skills);
// => {"dialect":"skills","files":[{"path":"skills/feishu-im/SKILL.md","content":"…"}, …]}
```

31 个域的 `skills/feishu-{domain}/SKILL.md` + `references/{topic}.md`，仓外 Agent（Claude Code / Cursor）
直接加载即获得飞书域知识。数据源全是编译期常量 ⇒ 零反射、零 IO、确定性。

**NDJSON 事件外桥**（Demo 参考实现，R-10 起不在库发布面）：把飞书事件以一行一事件的 NDJSON
交给仓外任意进程，`payload` 原样嵌入（不再序列化），避免字段丢失与 AOT 反射破口。
实现见 `Demos/Mud.Feishu.Agent.Demo/Events/FeishuEventNdjsonBridge.cs`（约 370 行、零 SDK 依赖），
并在 `Modes/DomainEventsDemo.cs` 有真实调用点。库侧不再承诺该生态位——需要它的宿主复制 Demo 实现即可。

---

## 完整飞书 API 覆盖

**33 个 API 模块、1268 个强类型方法**（含租户 / 用户令牌变体），按业务域拆分为独立模块，
可在 `CreateFeishuServicesBuilder()` 里按需注册（`AddXxxApi()`），也可 `AddAllApis()` 一次全注册。

| 模块 | API 版本 | 主要功能 |
| --- | --- | --- |
| 认证授权 | V3 | 应用/租户/用户令牌、OAuth 2.0、多应用管理 |
| 组织架构 | V1/V3 | 用户、部门、员工、用户组、职级、职务、角色 |
| 消息服务 | V1 | 文本/图片/卡片消息、批量发送、群聊管理 |
| 审批流程 | V4 | 审批定义、实例、任务、消息、订阅 |
| 任务管理 | V2 | 任务创建、更新、分组、附件、评论、自定义字段 |
| 日程会议 | V4 | 日历管理、日程事件、ACL 访问控制 |
| 视频会议 | V1 | 会议、会议室、配置、导出、录制、纪要、报告 |
| 文档管理 | V1 | 飞书文档、文档块、内容转换 |
| 知识库 | V2 | 知识空间、节点管理、复制移动 |
| 云盘管理 | V1 | 云空间、文件夹、上传、版本、权限、评论、订阅 |
| 画板管理 | V1 | 画板主题、缩略图、节点、语法解析 |
| 电子表格 | V2/V3 | 电子表格、区域、筛选、数据验证、条件格式 |
| 多维表格 | V1/V2 | 数据表、记录、字段、视图、表单、仪表盘、角色、自动化 |
| 考勤管理 | V1 | 考勤组、打卡、班次、请假、统计、档案 |
| 卡片管理 | V1/V2 | 卡片、卡片元素、消息流卡片 |
| AI 能力 | V1 | 文档解析、OCR（18 种证件票据）、语音转文字、文本翻译 |
| 邮箱管理 | V1 | 邮件消息、别名、联系人、草稿、文件夹、邮件组、标签、模板、规则、会话 |
| 搜索 | V2 | 数据源搜索、文档/知识库搜索、套件搜索 |
| 服务台 | V1 | 客服、技能、排班、工单、工单消息 |
| OKR | V1 | 目标、关键结果、周期、进展 |
| 妙记 | V1 | 妙记创建、管理、附件 |
| 妙搭 | V1 | 妙搭应用、页面、组件 |
| 群聊管理 | V1 | 群组、群成员、群公告、会话标签、群菜单 |
| Aily 智能体 | V1 | 智能体、会话与消息、数据知识与知识问答、技能 |
| 招聘 | V1 | 职位与招聘需求、候选人、面试、Offer、内推、招聘附件、生态服务、外部投递 |
| 关联组织 | V1 | 协作租户、协作规则、共享实体、协作部门与协作成员 |
| 智能门禁 | V1 | 门禁设备、门禁用户与人脸、访客、通行记录、外部规则 |
| 词典 | V1 | 词条、草稿、分类、词库、文件上传下载、高亮与实体匹配 |
| 绩效 | V1/V2 | 考核活动与周期、阶段任务、指标库与指标明细、评估数据 |
| 安全与合规 | V1/V2 | 审计日志、OpenAPI 调用日志、用户迁移、设备登记与设备申请记录 |
| 主数据管理 | V1/V3 | 国家/地区主数据、用户授权数据关系绑定与解绑 |
| 薪酬发放 | V1 | 发薪活动与明细、成本分摊计划/明细/报表、数据源与记录、发薪组 |
| 个人设置 | V1 | 系统状态创建/更新/删除/列表、批量开启与关闭 |

---

## 实时事件：WebSocket + Webhook 双通道

| 通道 | 包 | 特性 |
| --- | --- | --- |
| **WebSocket** | `Mud.Feishu.WebSocket` | 长连接实时订阅、指数退避重连（可插拔策略）、消息序号验证（重放/丢失/回退检测）、事件去重（内存/Redis）、SSL/TLS 证书校验、24h 会话管理 |
| **Webhook** | `Mud.Feishu.Webhook` | HTTP 回调、签名验证 + 时间戳 + Nonce 三道 fail-closed 闸、AES-256-CBC 解密、频率限制、多应用独立路由、失败事件重试、健康检查 |

两通道共享同一套事件处理器接口（`IFeishuEventHandler<T>`）与事件去重抽象，处理器代码零差异复用。

---

## 安全默认不可协商

| 闸 | 约束 |
| --- | --- |
| 工具授权 | 写工具默认不启用；启用后未注册 `IToolExecutionAuthorizer` → 默认拒绝；`NeedsUserConfirmation` → 通知宿主 + 中性拒绝（不含凭据） |
| 入站净化 | 控制字符 / 危险 Unicode / 独立 CR → 拒绝（`invalid_args`），零调用下游；换行 / Tab / Emoji（含 ZWJ 序列）不误伤 |
| 出站净化 | ANSI/控制字符剥离 + 凭据与手机号脱敏；**无开关、不可绕过**；在整形钩子之前 |
| 内容安全 | 4 条注入规则扫描原始结果（`off` / `warn`（默认）/ `block`）；命中标注 `[untrusted_content: 规则]` |
| 幂等键 | 160 个写类工具中 23 个暴露 `idempotency_key`（部分对应平台侧 `client_token` 去重）；`dry_run=true` 不消耗幂等键，可先预演「将要下发的 method / path / 请求体字段摘要」再决定下发 |
| 多租户 | appKey 上下文校验 → 租户切换 `UseAppScope(appKey)`；用户上下文仅 `identity=user` 工具写入 AsyncLocal，finally 清理；跨租户召回即数据泄露 |
| Webhook | 签名 + 时间戳 ±30s + Nonce 三道 fail-closed；生产环境未注册分布式 Nonce 去重 → **启动即失败**（不静默退化） |
| SSRF | `BaseUrl` 强制 HTTPS + 主机白名单；工具面多模态附件落盘由宿主 `IFeishuAttachmentStager` 实现（域名白名单 / 大小上限 / 扩展名校验全是宿主策略） |
| 凭据不外泄 | `AppSecret` / 令牌不进审计载荷；待确认快照不含任何凭据（由用例反射断言） |
| 二进制边界 | 字节两侧都不进工具面：入向模型只给 URL → 宿主落盘 → SDK 上传；出向受控下载出口 → 结果只回路径+大小+类型 |

---

## Native AOT：MCP Server 单文件冷启动

全线 DTO 标 `[HttpJsonSerializable]`，`JsonSerializerContext` 由源生成器编译期发射，序列化零反射。
`net8.0` / `net10.0` 下逐工程 `AotStrictMode`（10 类反射诊断升为错误）保持净零。

```bash
dotnet publish -r win-x64 -c Release /p:PublishAot=true   # 单文件原生二进制，冷启动 < 100ms
```

**这意味着**：MCP 工具服务器可以 Native AOT 发布，AI Agent 频繁拉起工具进程的性能前提成立。
反射序列化（如 `Newtonsoft.Json`）在 AOT 下不可用，这条路根本走不通——源生成是唯一可行路径。

---

## AI 原生 vs 外挂式：两条路线的根本差异

「在回调处理器里接一个大模型」和「让 Agent 自主操作飞书」是两条完全不同的技术路线。
下表列的是**架构路线的差异**，不是产品对比——.NET 生态里外挂式路线尚无成体系实现，
MudFeishu 是 AI 原生路线的**首个完整落地**。

| 维度 | 外挂式集成（行业常见做法） | AI 原生（MudFeishu） |
| --- | --- | --- |
| AI 定位 | Sample 层补丁：回调处理器里调大模型回复消息，单轮问答 | SDK 第一公民：五层架构（基座 → 执行链 → 工具 → Agent → 生态） |
| 工具暴露 | 无，或反射扫描全部方法（`Type.GetType` + `method.Invoke`，AOT 不可用 + RCE 风险） | 331 个策展工具，编译期 Schema + 类型化契约，零运行期解析 |
| 权限控制 | 无 | 七步执行链 + 风险分级 + 授权门禁 + HITL 审批管线，fail-closed 默认 |
| 多租户 | 全局静态容器，进程级单例 | appKey 作用域切换 + 用户上下文 AsyncLocal + 跨租户召回即数据泄露 |
| 序列化 | `Newtonsoft.Json` 反射（AOT 不可用） | 源生成 `JsonSerializerContext`，AOT strict 净零 |
| MCP 工具 | 无，或反射调用器 | 自研协议层 stdio server，执行链零旁路，租户边界 = 进程 |
| 生态对接 | 无 | Skills 产物随包分发（Claude Code / Cursor 直接加载）+ NDJSON 事件外桥（Demo 参考实现） |
| 知识检索 | 无 | Aily 托管 + 自建 RAG（结构优先切片 + 向量检索），并存不冲突 |
| 流式回复 | 无 | EditMessageChannel / CardStreamMessageChannel / StreamingChannelChain |
| 事件驱动 | 回调处理器单轮回复 | 事件 → 上下文装配 → Agent 对话 → 工具调用 → 流式回复 |
| 契约校验 | 运行期发现路由 / 字段漂移 | 编译期 golden 门禁 + Public API 分析器 + 契约守卫 |
| 目标框架 | `net462` + `netstandard2.0` + `LangVersion 10` | `netstandard2.0` / `net6.0` / `net8.0` / `net10.0` + `LangVersion 13` |

---

## 与飞书官方 CLI 的对照

一个常被问到的问题：飞书官方已经有 CLI 了，为什么还要这个？**结论先放前面：二者互补，不是替代。**

官方 lark-cli 是「人和脚本操作飞书」的命令行入口，跑在终端里，广度优先，甚至留了一个 `api` 逃生口
能直打任意开放平台端点，面向「我知道我要干嘛，我想用命令干」；MudFeishu 面向的是
「模型在业务进程里，得替我安全地把活干了」——跑在进程内，深度优先，**没有通用裸 `api` 工具**。

![定位差异 · vs 飞书官方 CLI](documents/AIAgent/images/06-vs.png)

|  | 飞书官方 CLI | MudFeishu |
| --- | --- | --- |
| 交互对象 | 人 · 运维脚本 | 大模型 · Agent 主循环 |
| 能力面 | 可直发任意开放平台端点 | 331 个编译期策展工具，**无通用裸 `api`** |
| 凭证 · 租户 | 本地凭证 · 人工运维 | 多租户进程内隔离 · 令牌全生命周期托管 |
| 写操作确认 | 人工执行即视为确认 | HITL 审批绑定 · 执行链 fail-closed |
| 契约约束 | 运行时解释，无编译期契约 | `source` 交叉校验（MUDFT019）· 禁与 SDK 脱钩 |
| 部署形态 | 终端命令行工具 | 进程内 SDK / MCP stdio server |

「无通用裸 `api` 工具」是**故意**的选择——给了模型一把万能钥匙，前面那条安全链基本就白搭了。
判断标准很简单：**是要「人执行命令」，还是「让模型在业务里自主行动」**。前者用 CLI，后者用这个；
两个都要也不冲突——CLI 给人用，MudFeishu 给 Agent 用。

---

## 边界与取舍：我们没做的事

- **没有通用裸 `api` 工具**。如上所说，是故意的；想要「什么都能调」的灵活性，这个包不提供。
- **写工具默认关着，授权要宿主自己实现**。`IToolExecutionAuthorizer` 只给契约，SDK 不带现成的审批 UI，
  也不带租户级鉴权实现——这些是宿主业务的事（`UseAppScope` 只切上下文，不做租户隔离授权）。
- **Skills 产物目前的仓外使用者还很少**。仓库内有 Demo 把它当真实消费方（读出来注入域 guidance），
  我们只承诺「域知识导出成标准形态」，不承诺生态成熟度。
- **工具数一律以 `FeishuToolSchemas.golden.txt` 为准**（当前 331：171 只读 + 160 写类）。
  它是生成产物，别手改；文档数字对不上时以它为准，且计数由 `ToolSurfaceScaleContractGuards` 机械比对。
- **它不解决「模型靠不靠谱」**。工具面再整齐、执行链再严，也只是把「模型出错时的损失」控制在可接受范围里——
  它管的是安全边界，不是模型质量。

---

## 安装

```bash
# 核心 HTTP API
dotnet add package Mud.Feishu
dotnet add package Mud.Feishu.Abstractions      # 事件处理抽象层（自动依赖）

# AI Agent 全栈
dotnet add package Mud.Feishu.AI                 # Agent 运行时（会话/流式/知识/HITL）
dotnet add package Mud.Feishu.AI.Tools           # 331 策展工具 + 执行链 + 编译期 Schema
dotnet add package Mud.Feishu.AI.Mcp             # MCP stdio server（进程外 Agent）

# 实时事件
dotnet add package Mud.Feishu.WebSocket          # 长连接实时订阅
dotnet add package Mud.Feishu.Webhook            # HTTP 回调事件处理

# 横向扩展
dotnet add package Mud.Feishu.Authentication     # 用户认证中间件
dotnet add package Mud.Feishu.EventCallback      # 事件回调强类型模型
dotnet add package Mud.Feishu.Redis              # 分布式去重（事件/Nonce/SeqID）
dotnet add package Mud.Feishu.OpenTelemetry      # 可观测性
```

---

## 快速开始

### 1. AI Agent 操作飞书

```csharp
// appsettings.json
{
  "FeishuApps": [{
    "AppKey": "default", "AppId": "cli_xxx", "AppSecret": "dsk_xxx", "IsDefault": true
  }],
  "FeishuAgent": {
    "Tools": ["bitable.list_tables", "contact.search_user", "docx.get_document"],
    "WriteAllowList": ["im.send_message"],
    "MaxToolRisk": "high-risk-write"
  }
}
```

```csharp
builder.Services.AddFeishuApp(builder.Configuration);
builder.Services.CreateFeishuServicesBuilder().AddAllApis().Build();
builder.Services.AddFeishuTools();
builder.Services.AddFeishuAgent(builder.Configuration)
    .AddFeishuEditMessageChannel()
    .AddFeishuImConversationHandler();

// 注入 FeishuAgent，在事件处理器或 API 端点中调用
public class ChatController(IFeishuAgent agent) : ControllerBase
{
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] string userMessage, CancellationToken ct)
    {
        var response = await agent.RunAsync(userMessage, ct);
        return Ok(response.Text);
    }
}
```

> 💡 **第一版怎么开**：只开 `tenant` 身份、写工具一个不放，让它在读场景里跑顺，再按需逐个放写工具——
> 每个放开的写工具都值得单独想一想「万一它真调了，后果是什么」。配置项集中在 `FeishuAgentOptions`，
> 默认值见「配置参考」。

### 2. MCP Server（进程外 Agent）

```csharp
builder.Services.AddFeishuTools();
builder.Services.AddFeishuMcpServer(o => o.AppKey = "default");
await app.Services.RunFeishuMcpStdioAsync();
```

Claude Code / Cursor / 自建 harness 经 stdio 连接即可调用 331 个飞书工具，
授权 / 租户切换 / 净化 / 审计与进程内 Agent **完全一致**。

### 3. HTTP API 调用

```csharp
builder.Services.AddFeishuApp(builder.Configuration);
builder.Services.CreateFeishuServicesBuilder().AddAllApis().Build();

// 注入即调用，令牌全生命周期托管
app.MapGet("/users/{userId}",
    (IFeishuTenantV3User userApi, string userId, CancellationToken ct) =>
        userApi.GetUserInfoByIdAsync(userId, ct));

// 多应用作用域切换：using 结束后自动恢复默认应用
var userApi = _serviceProvider.GetRequiredService<IFeishuTenantV3User>();
using (userApi.UseAppScope("hr-app"))
{
    return await userApi.GetUserInfoByIdAsync(userId, ct);
}
```

### 4. Webhook 事件处理

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<DepartmentCreatedEventHandler>()
    .Build();
app.UseFeishuWebhook();
```

### 5. WebSocket 实时订阅

```csharp
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration, "default")
    .AddHandler<MessageEventHandler>()
    .Build();
```

### 6. 可观测性

```csharp
builder.Services.AddFeishuOpenTelemetry(o => o.OtlpEndpoint = new Uri("http://localhost:4317"));
```

### 7. 多租户部署指引

> **多租户 = 多应用**。每个租户对应一个 `FeishuAppConfig`（独立 AppId/AppSecret），通过 `UseAppScope`（作用域式，结束自动恢复）或 `IAppContextHolder.SwitchToApp`（切换并保持）切换应用上下文。

**最小装配片段**：

```csharp
// 1. 为每个租户注册应用配置
builder.Services.AddFeishuApp(configure =>
{
    configure.AddDefaultApp("tenant-a", "cli_aaa", "dsk_aaa");
    configure.AddApp("tenant-b", "cli_bbb", "dsk_bbb");
});

// 2. 注册 HTTP API 服务
builder.Services.CreateFeishuServicesBuilder()
    .AddAllApis()
    .Build();

// 3. 在请求中按租户切换应用上下文
public class TenantController : ControllerBase
{
    private readonly IFeishuTenantV3User _userApi;

    public TenantController(IFeishuTenantV3User userApi)
    {
        _userApi = userApi;
    }

    [HttpGet("tenant/{tenantKey}/users/{userId}")]
    public async Task<IActionResult> GetUser(string tenantKey, string userId)
    {
        // 以作用域方式切换到租户应用上下文，using 结束后自动恢复默认应用
        using var scope = _userApi.UseAppScope(tenantKey);
        var result = await _userApi.GetUserInfoByIdAsync(userId);
        return Ok(result);
    }
}
```

---

## 端到端示例：读多维表格，经人工确认写回记录

场景：把多维表格里状态为「待办」的记录捞出来，处理完再改回「已完成」——一个来回串起「读」「写」「人工确认」
三件事。多维表格域共 28 项，这条链路用到 9 项：`list_tables` / `list_fields` / `list_views` / `get_view` /
`query_records` / `get_records_by_ids` 只读，`add_record` / `update_record` 普通写，`delete_record` 归 high-risk-write。

**第一步，把写工具点名启用**（写工具默认全关）：

```csharp
builder.Services.AddFeishuAgent(builder.Configuration, o =>
{
    o.ModelServiceKey   = "feishu-chat";
    o.Instructions      = "你是多维表格助手：先查再改，改前说明改哪条。";
    o.AllowedIdentities = ["tenant"];
    o.WriteAllowList    = ["bitable.update_record"];   // 写工具逐个点名
});
```

只放 `update_record`、不放 `delete_record`——从名单上就把删记录这条路断了。

**第二步，宿主实现批准通道**（SDK 只定契约，宿主自己实现）：

```csharp
public sealed class HostApprovalChannel : IFeishuToolApprovalChannel
{
    private readonly ConcurrentDictionary<string, FrameworkToolApprovalRequest> _pending = new();

    // 框架原生审批请求：写工具在「执行前」被拦下，在这里登记；只登记、不阻塞等人
    public Task<string?> RequestFrameworkApprovalAsync(
        FrameworkToolApprovalRequest request, CancellationToken ct = default)
    {
        _pending[request.RequestId] = request;              // 内含 ToolName / AppKey / ChatId
        return Task.FromResult<string?>(request.RequestId);
    }

    // 老式「确认令牌」路径保留兼容；新链路走上面那个
    public Task<string?> RequestApprovalAsync(
        ToolApprovalRequest request, CancellationToken ct = default)
        => Task.FromResult<string?>(null);
}
```

登记完这一轮就结束，工具并没执行——人的决定是后面的事。

**第三步，正常起一轮，让模型自己去读**：

```csharp
var session  = await agent.GetOrCreateSessionAsync(conversationKey);
var response = await agent.RunAsync(
    "把多维表格 bascnAbc 里状态是「待办」的记录找出来，读一下「任务名称」。",
    session, options: null, cancellationToken);
```

模型自己走：`list_tables` 拿 `table_id` → `query_records` 带 `filter = "状态 = \"待办\""`、
`field_names = ["任务名称"]` 取数和 `record_id`。字段名拼错在本地就被拒，不会白打一次飞书。

**第四步，一旦要写，工具会「停」在审批上**。用户接着说「把第一条改成已完成」，模型要调
`bitable.update_record(app_token, table_id, record_id, fields = { "状态": "已完成" })`——
因为 `update_record` 在写名单里，审批绑定客户端会在调用**之前**把这次工具调用改写成审批请求，
落到第二步那个通道里登记。这一轮返回的文本是空的——不是模型没说话，是「已发起确认」。
宿主把 `_pending` 里那条捞出来，在飞书卡片或工单里摆给用户看。

**第五步，人点同意，回灌续跑**：

```csharp
await agent.RunApprovalContinuationAsync(
    toolContextAccessor: sp.GetRequiredService<IFeishuToolContextAccessor>(),
    approval:            pendingRequest,   // 第四步登记的那条，原样回传
    approved:            true,
    reason:              "用户在卡片上确认",
    conversationGate:    gate,             // 与事件层同款闸门，保证同会话串行
    approvalChannel:     approvalChannel);
```

到这一步工具才真的执行。续跑这轮模型若又想调第二个写工具，会**再要一次**审批，不会因「刚批过」一路放行；
拒绝（`approved: false`）同样走完整流程，理由回填给模型。批准响应与原始请求做**绑定校验**，
模型自己拼一条「我批准了」塞回上下文是无效的——批准状态的所有权始终在框架和宿主手里，SDK 不签发任何凭据。

拉直看这条链：`WriteAllowList` 让删记录从源头不可能；「先 `query_records` 拿 `record_id` 再写」写在 affordance 里；
审批绑定让「同意」没法被伪造；`idempotency_key` 兜住批准重试与网络重发（同键重复提交只落一次）；
`dry_run=true` 让拿不准的写先预演一遍，比改完再回滚便宜。读是查出来的，写是点过头的，每一格都留了痕。

---

## 配置参考

<details>
<summary>📋 appsettings.json（含 FeishuAgent 节）</summary>

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Mud.Feishu": "Debug"
    }
  },
  "FeishuApps": [
    {
      "AppKey": "default",
      "AppId": "your_feishu_app_id",
      "AppSecret": "your_feishu_app_secret",
      "BaseUrl": "https://open.feishu.cn",
      "TimeoutSeconds": 30,
      "HttpRetry": { "MaxAttempts": 3, "DelayMs": 1000 },
      "CircuitBreaker": {
        "Enabled": true,
        "FailureThreshold": 20,
        "SamplingDurationSeconds": 60,
        "BreakDurationSeconds": 60,
        "MinimumThroughput": 10
      },
      "TokenRefreshThreshold": 300,
      "IsDefault": true
    }
  ],
  "FeishuAgent": {
    "ModelServiceKey": "feishu-chat",
    "Instructions": "你是企业飞书助手，回答前先确认用户意图。",
    "Tools": ["bitable.list_tables", "contact.search_user", "docx.get_document"],
    "WriteAllowList": [],
    "AllowedIdentities": ["tenant"],
    "MaxToolRisk": "high-risk-write",
    "ContentSafetyMode": "Warn"
  },
  "FeishuWebSocket": {
    "Reconnect": {
      "Auto": true, "MaxAttempts": 5, "BaseDelayMs": 5000,
      "MaxDelayMs": 30000, "TotalBudget": "00:30:00", "Cooldown": "00:00:05"
    },
    "Certificate": {
      "Mode": "Strict",
      "AllowInsecureWebSocket": false,
      "ValidateServerCertificate": true,
      "AllowSelfSignedCertificates": false,
      "AllowCertificateNameMismatch": false
    },
    "HeartbeatIntervalMs": 25000,
    "AllowedHostSuffixes": "*.feishu.cn;*.larksuite.com",
    "EventDeduplication": { "Mode": "InMemory", "CacheExpiration": "2.00:00:00" }
  },
  "FeishuWebhook": {
    "GlobalRoutePrefix": "feishu",
    "EnforceHeaderSignatureValidation": true,
    "TimestampToleranceSeconds": 30,
    "Apps": {
      "default": {
        "AppKey": "cli_xxx",
        "VerificationToken": "your_verification_token",
        "EncryptKey": "your_encrypt_key_32_bytes_long"
      }
    }
  },
  "FeishuRedis": {
    "Connection": { "ServerAddress": "redis://localhost:6379" },
    "EventKeyPrefix": "feishu:event:",
    "NonceKeyPrefix": "feishu:nonce:",
    "SeqIdKeyPrefix": "feishu:seqid:"
  }
}
```

</details>

<details>
<summary>📋 FeishuAgent 配置项（默认值即安全起点）</summary>

| 配置项 | 默认值 | 说明 |
| --- | --- | --- |
| `ModelServiceKey` | — | 键控模型客户端（`AddFeishuOpenAIChatClient` 的 `serviceKey`） |
| `Instructions` | 空 | 宿主系统指令；域 guidance 在其后追加 |
| `Tools` | — | 允许暴露的工具名清单（域或全名） |
| `WriteAllowList` | **空** | 写工具逐个点名才启用；默认「能读不能写」 |
| `AllowedIdentities` | `["tenant"]` | 允许的令牌身份（`tenant` / `user`） |
| `MaxToolRisk` | `high-risk-write` | 风险上限（`read` / `write` / `high-risk-write`） |
| `ContentSafetyMode` | `Warn` | 内容安全（`off` / `warn` / `block`） |
| `EnforceToolAuthorization` | `true` | 授权门禁开关（关掉 = 无授权校验，不建议） |
| `MaxToolCallsPerTurn` | `10` | 单轮工具调用预算 |
| `MaxConcurrentTurns` | `32` | 同会话并发回合上限 |
| `MaxToolResultLength` | `4000` | 单工具结果进上下文的长度预算 |
| `MaxHistoryTokens` / `SummaryThreshold` | `8000` / `30` | 历史 token 预算 / 触发摘要的消息数 |
| `GuidanceMode` | `Routing` | 域指引注入模式 |
| `TurnQueueTimeoutSeconds` | `30` | 回合排队超时 |

启动期 `ValidateOnStart` 校验；想收紧直接改这些。

</details>

<details>
<summary>📋 弹性策略（FeishuApps，全局共享）</summary>

| 配置项 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `TimeoutSeconds` | int | 30 | HTTP 请求超时（秒），范围 1-300 |
| `HttpRetry.MaxAttempts` | int | 3 | 重试次数，范围 0-10 |
| `HttpRetry.DelayMs` | int | 1000 | 重试延迟（毫秒），范围 100-60000 |
| `CircuitBreaker.Enabled` | bool | true | 是否启用熔断器 |
| `CircuitBreaker.FailureThreshold` | int | 20 | 熔断失败率阈值（百分比） |
| `CircuitBreaker.SamplingDurationSeconds` | int | 60 | 熔断采样窗口（秒） |
| `CircuitBreaker.BreakDurationSeconds` | int | 60 | 熔断持续时间（秒） |
| `CircuitBreaker.MinimumThroughput` | int | 10 | 熔断最小吞吐量 |

> ⚠️ 多应用模式下弹性策略为全局共享，仅默认应用（`IsDefault=true`）的配置生效。

</details>

<details>
<summary>📋 Webhook 安全配置与多实例部署约束</summary>

| 配置项 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `AllowedSourceIPs` | HashSet&lt;string&gt; | 空 | 源 IP 白名单（支持 CIDR），非空时自动启用 IP 验证 |
| `EnforceHeaderSignatureValidation` | bool | true | 强制验证 `X-Lark-Signature` 签名（生产必须开） |
| `TimestampToleranceSeconds` | int | 30 | 时间戳容差（秒），超过视为无效请求 |
| `MaxConcurrentEvents` | int | 10 | 最大并发事件处理数 |
| `MaxRequestBodySize` | long | 10MB | 最大请求体（字节） |
| `EventHandlingTimeoutMs` | int | 30000 | 事件处理**软超时**（只取消令牌，不强制中断处理器） |
| `InterceptionAckMode` | `Ack` / `Retryable` | Ack | 拦截器中断事件时的 ACK 语义（`Ack`→200 已消费；`Retryable`→503 要求重推） |

**多实例部署约束（必读）**：内存实现只对**单实例**有效。生产环境若未注册分布式 Nonce 去重
（`AddFeishuRedisDeduplicators()`），**宿主启动即失败**——静默的安全退化必须由代码阻断而非文档建议。
确为单实例部署时显式设置 `AllowInMemoryNonceDedupInProduction=true` 承担风险（仍输出 Warning）。

| 能力 | 单实例 | 多实例（负载均衡） |
| --- | --- | --- |
| Nonce 防重放 | 内存可用（生产需显式豁免） | **必须 Redis** |
| 事件去重（EventId） | 内存可用 | 建议 Redis |
| 请求限流 | 内存可用 | 每实例独立（等效配额 ×N） |
| 失败事件重投 | 进程内（重启即丢） | 需自定义 `IFailedEventStore` |

</details>

<details>
<summary>📋 Redis 分布式去重（FeishuRedis）</summary>

| 配置项 | 默认值 | 说明 |
| --- | --- | --- |
| `Connection.ServerAddress` | `localhost:6379` | 连接地址（`host:port` / `redis://host:port`） |
| `Connection.Password` / `Connection.Ssl` | 空 / false | 密码 / TLS |
| `Advanced.AllowAdmin` | false | 是否允许管理员操作（如 FLUSHDB），生产保持 false |
| `EventKeyPrefix` / `NonceKeyPrefix` / `SeqIdKeyPrefix` | `feishu:event:` 等 | 去重键前缀（应用/环境/租户隔离用） |
| `EventCacheExpiration` / `SeqIdCacheExpiration` | `2.00:00:00` | 去重缓存过期 |
| `NonceTtl` | `00:05:00` | Nonce 有效期（须 ≥ `TimestampToleranceSeconds`） |

> 🔒 多租户场景为每个租户设置不同的三个键前缀；TimeSpan 四段格式（`"2.00:00:00"`）首段是**天**。

</details>

<details>
<summary>📋 模块日志级别（EnableLogging 已移除）</summary>

日志级别统一走标准 `Logging:LogLevel`，各模块独立、互不覆盖：

| 模块 | 日志控制位置 |
| --- | --- |
| HTTP API 客户端层 | `Logging:LogLevel:Mud.Feishu`（令牌相关：`Mud.Feishu.Abstractions.TokenManager`） |
| WebSocket 模块 | `Logging:LogLevel:Mud.Feishu.WebSocket` |
| Webhook 模块 | `Logging:LogLevel:Mud.Feishu.Webhook` |

> 旧配置键 `EnableLogging` 仍可绑定但不产生任何效果；生产建议 `Information` 或 `Warning`。

</details>

---

## 更多示例

### 多应用切换

```csharp
// 作用域式切换：using 结束自动恢复默认应用
using (userApi.UseAppScope("hr-app"))
{
    var userResult = await userApi.GetUserInfoByIdAsync("user_123");
}

// 切换并保持（直到再次切换）：经 IAppContextHolder 受控切换
IAppContextHolder.SwitchToApp("hr-app", appManager, serviceProvider);
var jobTitleResult = await jobTitleApi.GetJobTitlesListAsync(10, null);
```

### 事件处理器（WebSocket / Webhook 同款接口）

```csharp
public class MessageHandler : IFeishuEventHandler
{
    public string SupportedEventType => "im.message.receive_v1";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        var messageEvent = JsonSerializer.Deserialize<MessageReceiveResult>(
            eventData.Event?.ToString() ?? "{}");
        Console.WriteLine($"Message received: {messageEvent!.Message.Content}");
    }
}

builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<MessageHandler>()
    .Build();
```

多应用事件处理：`CreateFeishuWebSocketServiceBuilder(config, "hr-app")` 与
`CreateFeishuWebhookServiceBuilder(config).AddHandler<App1Handler>("app1")` 各自独立注册即可，
路由、处理器、拦截器按应用隔离。

### SSRF 防护与 URL 白名单

```csharp
// BaseUrl 白名单验证（SSRF 防护，来自 Mud.HttpUtils）
UrlValidator.ConfigureAllowedDomains(["open.feishu.cn", "open.larksuite.com"]);
UrlValidator.ValidateBaseUrl("https://open.feishu.cn/api", allowCustomBaseUrl: false);
```

### 事件拦截器

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddInterceptor<LoggingEventInterceptor>()   // 内置日志拦截器
    .AddHandler<DepartmentCreatedHandler>()
    .Build();
```

也可实现 `IFeishuEventInterceptor` 做自定义前后置处理（`BeforeHandleAsync` 返回 `false` 即中断，
ACK 语义由 `InterceptionAckMode` 决定）。

---

## 包家族

| 包 | 职责 |
| --- | --- |
| `Mud.Feishu.Abstractions` | 事件处理抽象层、策略模式 + 工厂模式、FeishuMetrics 指标收集、事件去重、SSRF 防护 |
| `Mud.Feishu` | 核心 HTTP API 客户端（33 个模块），自动令牌管理、智能重试、熔断器、多应用上下文 |
| `Mud.Feishu.DataModels` | 全线强类型请求/响应模型 + 源生成 `JsonSerializerContext` |
| `Mud.Feishu.Authentication` | 基于 AsyncLocal 的线程安全用户上下文 + JWT Claims 提取 + 分布式追踪 |
| `Mud.Feishu.EventCallback` | 事件回调强类型模型 + 源生成器自动生成处理器基类 |
| `Mud.Feishu.WebSocket` | 长连接实时订阅、指数退避重连、序号验证、事件去重、SSL/TLS 校验 |
| `Mud.Feishu.Webhook` | HTTP 回调、签名/时间戳/Nonce 三道闸、AES-256-CBC 解密、频率限制、多应用、重试 |
| `Mud.Feishu.Redis` | 分布式去重（事件/Nonce/SeqID）+ 降级策略 + 多应用隔离 + 健康检查 |
| `Mud.Feishu.OpenTelemetry` | 一键开启分布式追踪与指标采集（OTLP gRPC，Jaeger/Tempo/Prometheus） |
| **`Mud.Feishu.AI`** | **Agent 运行时**：会话管理、流式消息通道、事件驱动对话、知识检索（Aily + RAG）、HITL 审批续跑 |
| **`Mud.Feishu.AI.Tools`** | **331 策展工具**：编译期 Schema + 类型化契约 + 七步执行链 + affordance 资产 + Skills 导出 |
| **`Mud.Feishu.AI.Mcp`** | **MCP stdio server**：进程外 Agent 工具面，执行链零旁路，租户边界 = 进程 |

目标框架：`netstandard2.0` / `net6.0` / `net8.0` / `net10.0`，`LangVersion 13.0`。
AI 底座依赖 `Microsoft.Agents.AI` v1.25.0 + `Microsoft.Agents.AI.OpenAI` v1.25.0（仅 AI 包引用，核心包不依赖）。

---

## 质量门禁

```bash
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/verify-build.ps1    # 构建 + AOT strict + 测试
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/audit-config-keys.ps1 # 配置消费点审计
```

- **11 个测试工程**覆盖全部包 + AI 工具面 + Agent Demo。
- **编译期契约**：`FeishuToolSchemas` / `FeishuToolNames` / `FeishuToolContracts` / `FeishuToolGuidance`
  由源生成器同一 pass 发射，golden 逐字节比对（`MUDFT014` 中断构建）。
- **不变量守卫**（A1~A10）：Schema ↔ Names ↔ Contracts ↔ Registry ↔ 权限对照表五向一致；
  每个零容忍诊断有上报点且有可触发反例；出站净化在整形钩子之前；入站净化先于授权门禁。
- **防假绿铁律**：「工具存在」不算通过（必须有断言 method/path/body 的链路用例）；
  「必经」类性质不得用源码扫描验证（改用运行时行为断言）。
- **消费方门禁**：`PublicSurfaceRationaleContractGuards` 用反射比对三份产品程序集的 public 面，
  新增 public 类型必须在 `documents/AIAgent/public-surface-rationale.json` 登记消费方理由（无 `PublicAPI.txt` 台账）。

### 原生 AOT 注意事项

> ⚠️ **注意事项**：
> - Native AOT 仅支持 net8.0+ 目标框架；netstandard2.0 / net6.0 目标不启用 AOT 分析
> - 自定义类型的序列化请通过 `FeishuJsonDefaults.ConfigureUserResolver` 等入口注册自定义 `JsonSerializerContext`，避免运行时反射序列化
> - 扩展配置 DTO 时不要使用 `required` 修饰（源生成绑定器通过 `new T()` 构造），请在 `Validate()` 方法中完成必填校验

### 核心依赖

| 包                            | 版本             | 说明                                |
| ----------------------------- | ---------------- | ----------------------------------- |
| **Mud.HttpUtils**             | v3.0.8  | HTTP 客户端工具类（含源代码生成器） |
| **Mud.HttpUtils.Generator**   | v3.0.8  | HTTP 客户端代码生成器（编译时）     |
| **Mud.HttpUtils.Resilience**  | v3.0.8  | 弹性策略（重试 / 超时 / 熔断）      |
| **Microsoft.Extensions.Http** | v10.0.12 | HTTP 客户端工厂                    |
| **Microsoft.Agents.AI**       | v1.25.0 | AI 基座（仅 AI 包引用，核心包 MAF-free） |
| **Mud.AI.Agent / Mud.AI.Abstractions** | v1.0.1 | 中立 AI 运行时（独立仓库发包） |

---

## 目录结构

```
MudFeishu/
├── Mud.Feishu/                    # 核心 HTTP API 客户端
├── Mud.Feishu.Abstractions/       # 事件处理抽象层
├── Mud.Feishu.DataModels/         # 强类型数据模型 + 源生成 JsonContext
├── Mud.Feishu.AI/                 # Agent 运行时（会话/流式/知识/HITL）
├── Mud.Feishu.AI.Tools/           # 331 策展工具 + 执行链 + 编译期 Schema
├── Mud.Feishu.AI.Mcp/             # MCP stdio server
├── Mud.Feishu.WebSocket/          # 长连接实时订阅
├── Mud.Feishu.Webhook/            # HTTP 回调事件处理
├── Mud.Feishu.Authentication/     # 用户认证中间件
├── Mud.Feishu.EventCallback/      # 事件回调强类型模型
├── Mud.Feishu.Redis/              # 分布式去重
├── Mud.Feishu.OpenTelemetry/      # 可观测性
├── skills/                        # 31 域 Skills 产物（随包分发）
├── Tests/                         # 11 个测试工程
├── Demos/                         # 示例（含 AI Agent Demo / AOT 验证）
├── documents/                     # 官方文档镜像 + 工具权限对照表
├── scripts/                       # verify-build / audit-config-keys / GenerateJsonContext / ...
└── Directory.Build.props          # 全局 MSBuild 属性（Version 3.0.3 唯一来源）
```

---

## 详细文档

- [Mud.Feishu.Abstractions 详细文档](./Mud.Feishu.Abstractions/README.md) - 事件处理抽象层使用指南
- [Mud.Feishu 详细文档](./Mud.Feishu/README.md) - HTTP API 完整使用指南
- [Mud.Feishu.DataModels 详细文档](./Mud.Feishu.DataModels/README.md) - HTTP API 强类型数据模型使用指南
- [Mud.Feishu.EventCallback 详细文档](./Mud.Feishu.EventCallback/README.md) - 飞书事件回调强类型数据模型使用指南
- [Mud.Feishu.WebSocket 详细文档](./Mud.Feishu.WebSocket/Readme.md) - WebSocket 实时事件订阅指南
- [Mud.Feishu.Webhook 详细文档](./Mud.Feishu.Webhook/README.md) - Webhook HTTP 回调事件处理指南
- [Mud.Feishu.Authentication 详细文档](./Mud.Feishu.Authentication/README.md) - 飞书用户认证中间件使用指南
- [Mud.Feishu.Redis 详细文档](./Mud.Feishu.Redis/README.md) - Redis 分布式去重扩展指南
- [Mud.Feishu.OpenTelemetry 详细文档](./Mud.Feishu.OpenTelemetry/README.md) - OpenTelemetry 可观测性扩展指南
- [Mud.Feishu.AI Tools Readme](./Mud.Feishu.AI.Tools/Readme.md) - 工具面与执行链说明

---

## 演示界面展示

以下是 **FeishuWikiManager**（飞书知识库管理 Demo）的实际运行界面截图，展示了 SDK 在实际项目中的应用效果：

### 用户认证与登录

| 飞书 OAuth 授权                                    | 系统登录界面                                     |
| :------------------------------------------------- | :----------------------------------------------- |
| ![飞书认证授权](./Images/wiki飞书认证授权界面.png) | ![登录界面](./Images/飞书云文档管理登陆界面.png) |

### 知识库管理核心功能

| 主界面                                         | 知识空间                                   |
| :--------------------------------------------- | :----------------------------------------- |
| ![知识库主界面](./Images/Wiki知识库主界面.png) | ![知识空间](./Images/Wiki知识空间界面.png) |

| 搜索功能                                     | 云空间同步                                               |
| :------------------------------------------- | :------------------------------------------------------- |
| ![搜索界面](./Images/Wiki知识库搜索界面.png) | ![云空间同步](./Images/飞书云文档管理云空间同步功能.png) |

### 云文档管理

| 文档管理主界面                                       | 文件上传                                             |
| :--------------------------------------------------- | :--------------------------------------------------- |
| ![文档管理主界面](./Images/飞书云文档管理主界面.png) | ![文件上传](./Images/飞书云文档管理文件上传界面.png) |

> 💡 以上界面均基于 **Mud.Feishu** SDK 开发，完整展示了飞书 OAuth 认证、知识库管理、文档搜索、云空间同步等核心功能。
> [查看 Demo 源码](./Demos/FeishuWikiManager)

---

## 相关链接

### 📖 官方文档

- [飞书开放平台文档](https://open.feishu.cn/document/) - 飞书 API 官方文档和最佳实践
- [NuGet 包管理器](https://www.nuget.org/) - .NET 包管理官方平台

### 📦 NuGet 包

- [Mud.Feishu](https://www.nuget.org/packages/Mud.Feishu/) - 核心 HTTP API 客户端库
- [Mud.Feishu.AI](https://www.nuget.org/packages/Mud.Feishu.AI/) - Agent 运行时
- [Mud.Feishu.AI.Tools](https://www.nuget.org/packages/Mud.Feishu.AI.Tools/) - 策展工具面 + 执行链
- [Mud.Feishu.AI.Mcp](https://www.nuget.org/packages/Mud.Feishu.AI.Mcp/) - MCP stdio server
- [Mud.Feishu.Abstractions](https://www.nuget.org/packages/Mud.Feishu.Abstractions/) - 事件处理抽象层
- [Mud.Feishu.DataModels](https://www.nuget.org/packages/Mud.Feishu.DataModels/) - HTTP API 强类型数据模型库
- [Mud.Feishu.WebSocket](https://www.nuget.org/packages/Mud.Feishu.WebSocket/) - WebSocket 实时事件订阅库
- [Mud.Feishu.Webhook](https://www.nuget.org/packages/Mud.Feishu.Webhook/) - Webhook HTTP 回调事件处理库
- [Mud.Feishu.Authentication](https://www.nuget.org/packages/Mud.Feishu.Authentication/) - 飞书用户认证中间件库
- [Mud.Feishu.EventCallback](https://www.nuget.org/packages/Mud.Feishu.EventCallback/) - 飞书事件回调强类型数据模型
- [Mud.Feishu.Redis](https://www.nuget.org/packages/Mud.Feishu.Redis/) - Redis 分布式去重扩展库
- [Mud.Feishu.OpenTelemetry](https://www.nuget.org/packages/Mud.Feishu.OpenTelemetry/) - OpenTelemetry 可观测性扩展库

### 🛠️ 开发资源

- [项目仓库](https://gitee.com/mudtools/MudFeishu) - 源代码和开发文档
- [示例项目](./Demos) - 完整的使用示例和演示代码
  - [Mud.Feishu.Agent.Demo](./Demos/Mud.Feishu.Agent.Demo) - AI Agent 完整示例（工具 / HITL / 事件驱动对话）
  - [Mud.Feishu.Demo](./Demos/Mud.Feishu.Demo) - HTTP API 完整功能示例（REST API + ASP.NET Core）
  - [Mud.Feishu.Webhook.Demo](./Demos/Mud.Feishu.Webhook.Demo) - Webhook HTTP 回调事件处理示例（**原生支持 AOT 发布**）
  - [Mud.Feishu.WebSocket.Demo](./Demos/Mud.Feishu.WebSocket.Demo) - WebSocket 实时事件订阅演示（含 Redis 分布式去重）
  - [FeishuWikiManager](./Demos/FeishuWikiManager) - 飞书知识库管理 Demo（Vue3 + .NET）
  - [AotVerification](./Demos/Mud.Feishu.AotVerification) - Native AOT 兼容性验证工程（win-x64 / linux-x64 双 RID）
- [测试项目](./Tests) - 完整的单元测试和集成测试

### 🤝 社区支持

- [问题反馈](https://gitee.com/mudtools/MudFeishu/issues) - Bug 报告和功能请求
- [贡献指南](./CONTRIBUTING.md) - 如何参与项目贡献
- [更新日志](./CHANGELOG.md) - 版本更新记录和变更说明

---

## 许可证

本项目遵循 [MIT 许可证](./LICENSE)，允许商业和非商业用途。

---

<div align="center">

**如果觉得 MudFeishu 对你有帮助，请给个 ⭐Star 支持一下！**

Made with ❤️ by Mud Studio

</div>
