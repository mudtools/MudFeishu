# MudFeishu

<div align="center">

![MudFeishu Logo](icon.png)

飞书生态的 **AI 原生** .NET SDK —— 351 个策展工具 + Agent 运行时 + MCP 服务器 + 人工确认管线，
让 AI Agent 经自然语言直接操作飞书组织、文档、审批、日历、任务、多维表格、幻灯片等 34 个业务域。

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Mud.Feishu](https://img.shields.io/nuget/v/Mud.Feishu?label=Mud.Feishu "Mud.Feishu")](https://www.nuget.org/packages/Mud.Feishu/)
[![Mud.Feishu.AI](https://img.shields.io/nuget/v/Mud.Feishu.AI?label=Mud.Feishu.AI "Mud.Feishu.AI")](https://www.nuget.org/packages/Mud.Feishu.AI/)
[![Mud.Feishu.AI.Tools](https://img.shields.io/nuget/v/Mud.Feishu.AI.Tools?label=Mud.Feishu.AI.Tools "Mud.Feishu.AI.Tools")](https://www.nuget.org/packages/Mud.Feishu.AI.Tools/)
[![Mud.Feishu.WebSocket](https://img.shields.io/nuget/v/Mud.Feishu.WebSocket?label=Mud.Feishu.WebSocket "Mud.Feishu.WebSocket")](https://www.nuget.org/packages/Mud.Feishu.WebSocket/)
[![Mud.Feishu.Webhook](https://img.shields.io/nuget/v/Mud.Feishu.Webhook?label=Mud.Feishu.Webhook "Mud.Feishu.Webhook")](https://www.nuget.org/packages/Mud.Feishu.Webhook/)

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

- **351 个策展工具**把飞书 API 编译期暴露为模型可调用的 Function Call，每个工具都有强类型 Schema、
  风险等级、权限范围、幂等语义——不是把 1279 个 SDK 方法无差别扔给模型。
- **七步执行链**在每次工具调用时依次过：上下文校验 → 入站净化 → 策略轴 → 授权门禁 → 租户切换 →
  内容安全 → 出站净化，**fail-closed 默认**，写工具未经授权一律拒绝。
- **人工确认管线**让高风险操作（发消息、创建审批、删除记录）先经宿主批准再执行，批准状态单一所有权，
  模型无法自批复。
- **MCP 服务器**把同一套工具面经 stdio JSON-RPC 或 Streamable HTTP 暴露给进程外 Agent
  （Claude Code / Cursor / 自建 harness），执行链零旁路；HTTP 形态的租户身份只取自宿主认证层。
- **Skills 产物**随 NuGet 包分发，仓外 Agent 直接 `npx skills add` 即可获得飞书域知识；
  `skills/feishu-suite/SKILL.md` 是单入口路由（先选域、再加载那一个域包）。

AI 能力不是 Sample 层的补丁，是 SDK 的**第一公民**。

---

## AI 底座：五层架构

```
┌─────────────────────────────────────────────────────────────┐
│  ⑤ 生态位  MCP Server stdio/HTTP  ·  Skills 导出  ·  NDJSON │
├─────────────────────────────────────────────────────────────┤
│  ④ Agent     会话管理  ·  流式消息通道  ·  事件驱动对话     │
│              知识检索 (Aily + RAG)  ·  HITL 审批续跑        │
├─────────────────────────────────────────────────────────────┤
│  ③ 工具面    351 策展工具 (34 域)  ·  编译期 Schema         │
│              风险分级  ·  幂等键  ·  affordance 资产         │
├─────────────────────────────────────────────────────────────┤
│  ② 执行链    入站净化 → 策略轴 → 授权 → 租户切换            │
│              → 内容安全 → 出站净化 → 审计                   │
├─────────────────────────────────────────────────────────────┤
│  ① SDK 基座  1279 强类型接口  ·  源生成 JSON  ·  AOT 净零   │
│              多应用作用域  ·  令牌全生命周期托管            │
└─────────────────────────────────────────────────────────────┘
```

### ① SDK 基座：1279 个强类型接口，Agent 看不到，工具面看得到

飞书全部 20+ API 模块（组织架构、消息、群聊、审批、任务、日历、视频会议、文档、知识库、云盘、
画板、电子表格、多维表格、考勤、卡片、AI 能力、邮箱、搜索、服务台、OKR、妙记、妙搭……）
均为声明式接口 + 源生成实现类。Agent 不直接调这些接口——**工具面从中策展 351 个高频操作**暴露给模型。

### ② 执行链：七步不可变，fail-closed 默认

```
① appKey 上下文校验
①' 入站净化        —— 控制字符/危险 Unicode/独立 CR → 拒绝（invalid_args），零调用下游
② 策略轴           —— MaxToolRisk / AllowedIdentities → 拒绝（policy_denied: reason_code）
③ 授权门禁         —— IToolExecutionAuthorizer → 拒绝（authorization_denied）
                     NeedsUserConfirmation → 通知宿主批准通道，中性拒绝（不含任何凭据）
④ 租户上下文切换    —— BeginScope(appKey)
④' 用户上下文       —— 仅 identity=user 工具：写入 AsyncLocal，finally 清理
⑤ 内容安全         —— 4 条注入规则扫描原始结果（off | warn | block）
⑥ 出站净化（强制）  —— ANSI/控制字符剥离 + 凭据与手机号脱敏；无开关、不可绕过
⑦ 整形钩子 + 审计   —— IToolResultShaper（可空）+ IToolExecutionAuditSink（允许/拒绝/错误三类投递）
```

**关键安全属性**：入站是「拒绝」而非「剥离」（剥离会静默改写用户内容）；出站净化无开关不可绕过；
授权器未返回 `Allowed` 的待确认调用**读、写工具同路径**走挂起解析；`dry_run=true` 的写工具不消耗幂等键。

### ③ 工具面：351 个策展工具，不是 1279 个裸方法

| 域 | 只读 | 写类 | 小计 | | 域 | 只读 | 写类 | 小计 |
| --- | ---: | ---: | ---: | --- | --- | ---: | ---: | ---: |
| 多维表格 | 12 | 13 | 25 | | OKR | 11 | 7 | 18 |
| 云文档 | 8 | 8 | 16 | | 视频会议 | 7 | 7 | 14 |
| Wiki | 5 | 7 | 12 | | 知识库 | 1 | — | 1 |
| 搜索 | 1 | — | 1 | | 画板 | 2 | 4 | 6 |
| IM | 9 | 14 | 23 | | 考勤 | 7 | 1 | 8 |
| 云空间 | 5 | 11 | 16 | | 妙搭 | 5 | 5 | 10 |
| 电子表格 | 8 | 17 | 25 | | AI 文本 | 2 | — | 2 |
| 通讯录 | 8 | — | 8 | | 邮件 | 10 | 11 | 21 |
| 审批 | 2 | 6 | 8 | | 妙记 | 6 | 4 | 10 |
| 日历 | 4 | 5 | 9 | | 任务 | 5 | 13 | 18 |
| 组织 | 8 | — | 8 | | 招聘 | 7 | — | 7 |
| 安全 | 2 | — | 2 | | 终端管理 | 1 | — | 1 |
| 元工具 | 4 | — | 4 | | 应用信息 | 7 | 2 | 9 |
| 群聊管理 | 4 | 12 | 16 | | 服务台 | 6 | 4 | 10 |
| 飞书词典 | 6 | 5 | 11 | | 关联组织 | 8 | 3 | 11 |
| Aily 数据知识 | 3 | — | 3 | | 幻灯片 | 7 | 5 | 12 |
| Markdown | 2 | 2 | 4 | | 复合工作流 | 2 | — | 2 |
| **合计** | **185** | **166** | **351** | | | | | |

**策展而非全暴露**：把 1279 个 SDK 方法无差别暴露给模型 = 模型上下文爆炸 + 无法做权限分级。
MudFeishu 按高频工作流策展为 351 个工具，每个工具都有：
- **编译期 Schema**（`FeishuToolSchemas`）：参数、风险等级、权限范围、是否写操作、SDK 源锚点
- **类型化契约**（`FeishuToolContracts`）：零运行期解析，注册器直接构造定义
- **per-tool affordance**（`x-feishu.affordance`）：何时用 / 何时别用 / 前置条件 / 踩坑提示
- **域 guidance**（34 个域 + 子主题 references）：宿主指令之后追加，2 KB 预算截断

模型遇到未策展的能力时，正确动作是问 `feishu.capability_lookup`（判断不存在 vs 未策展），
再**如实告知用户**——而不是臆造一次调用。**不存在万能兜底通道**（已整条删除，双轨调用路径风险最高）。

### ④ Agent 运行时：会话、流式、知识、事件驱动

```csharp
builder.Services.AddFeishuApp(builder.Configuration);
builder.Services.AddFeishuTools();                    // 工具面
builder.Services.AddFeishuAgent(builder.Configuration) // Agent 运行时
    .AddFeishuEditMessageChannel()                    // 流式消息通道（编辑已发消息）
    .AddFeishuImConversationHandler();                // IM 事件 → 对话
```

- **流式消息通道**：`EditMessageChannel`（流式编辑已发消息）、`CardKitStreamMessageChannel`（会话内流式卡片）、
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

### ⑤ 生态位：MCP 服务器 + Skills 导出

**MCP 服务器**（`Mud.Feishu.AI.Mcp`，独立可选包）：

```csharp
builder.Services.AddFeishuTools();
builder.Services.AddFeishuMcpServer(o => o.AppKey = "cli_xxx");
await app.Services.RunFeishuMcpStdioAsync();   // 阻塞到客户端关闭 stdin
```

- 白名单同源（MCP 暴露的工具 == `FeishuAgent:Tools` + `WriteAllowList`，不新增开关）
- 执行链零旁路（每个 tool call 走同一个 `FeishuToolBinding`）
- 租户边界 = 进程（stdio：appKey 只来自配置，协议层传 appKey 等于把切租户交给客户端）
- 租户边界 = 认证层（Streamable HTTP：一进程可服务多租户，`McpHttpRequest.AppKey` 只能由宿主认证层填写，
  会话绑定租户后给出不同身份即 403）
- 协议层自研（不引官方 `ModelContextProtocol` SDK，覆盖 `tools` 能力 + stdio 与 Streamable HTTP）

**Skills 产物**（随 NuGet 包分发）：

```csharp
IToolSchemaExporter.Export(ToolSchemaDialect.Skills);
// => {"dialect":"skills","files":[{"path":"skills/feishu-im/SKILL.md","content":"…"}, …]}
```

34 个域的 `skills/feishu-{domain}/SKILL.md` + `references/{topic}.md`，外加 `skills/feishu-suite/SKILL.md`
单入口路由（34 域条目 + 规模数字，全部由域资产推导）。仓外 Agent 直接加载即获得飞书域知识。
数据源全是编译期常量 ⇒ 零反射、零 IO、确定性。消费实证见 `documents/AIAgent/Skills外部消费实证报告.md`。

**NDJSON 事件外桥**（Demo 参考实现，R-10 起不在库发布面）：把飞书事件以一行一事件的 NDJSON
交给仓外任意进程，`payload` 原样嵌入（不再序列化），避免字段丢失与 AOT 反射破口。
实现见 `Demos/Mud.Feishu.Agent.Demo/Events/FeishuEventNdjsonBridge.cs`（约 370 行、零 SDK 依赖），
并在 `Modes/DomainEventsDemo.cs` 有真实调用点。库侧不再承诺该生态位——需要它的宿主复制 Demo 实现即可。

---

## 完整飞书 API 覆盖

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
| 幂等键 | 100 个写工具中 21 个暴露 `idempotency_key`（其中 11 个对应平台侧 `client_token` 去重）；`dry_run=true` 不消耗幂等键 |
| 多租户 | appKey 上下文校验 → 租户切换 `BeginScope(appKey)`；用户上下文仅 `identity=user` 工具写入 AsyncLocal，finally 清理；跨租户召回即数据泄露 |
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
| 工具暴露 | 无，或反射扫描全部方法（`Type.GetType` + `method.Invoke`，AOT 不可用 + RCE 风险） | 351 个策展工具，编译期 Schema + 类型化契约，零运行期解析 |
| 权限控制 | 无 | 七步执行链 + 风险分级 + 授权门禁 + HITL 审批管线，fail-closed 默认 |
| 多租户 | 全局静态容器，进程级单例 | appKey 作用域切换 + 用户上下文 AsyncLocal + 跨租户召回即数据泄露 |
| 序列化 | `Newtonsoft.Json` 反射（AOT 不可用） | 源生成 `JsonSerializerContext`，AOT strict 净零 |
| MCP 工具 | 无，或反射调用器 | 自研协议层 stdio / Streamable HTTP server，执行链零旁路，租户身份 fail-closed |
| 生态对接 | 无 | Skills 产物随包分发（Claude Code / Cursor 直接加载）+ NDJSON 事件外桥（Demo 参考实现） |
| 知识检索 | 无 | Aily 托管 + 自建 RAG（结构优先切片 + 向量检索），并存不冲突 |
| 流式回复 | 无 | EditMessageChannel / CardKitStreamMessageChannel / StreamingChannelChain |
| 事件驱动 | 回调处理器单轮回复 | 事件 → 上下文装配 → Agent 对话 → 工具调用 → 流式回复 |
| 契约校验 | 运行期发现路由 / 字段漂移 | 编译期 golden 门禁 + Public API 分析器 + 契约守卫 |
| 目标框架 | `net462` + `netstandard2.0` + `LangVersion 10` | `netstandard2.0` / `net6.0` / `net8.0` / `net10.0` + `LangVersion 13` |

---

## 安装

```bash
# 核心 HTTP API
dotnet add package Mud.Feishu
dotnet add package Mud.Feishu.Abstractions      # 事件处理抽象层（自动依赖）

# AI Agent 全栈
dotnet add package Mud.Feishu.AI                 # Agent 运行时（会话/流式/知识/HITL）
dotnet add package Mud.Feishu.AI.Tools           # 351 策展工具 + 执行链 + 编译期 Schema
dotnet add package Mud.Feishu.AI.Mcp             # MCP server（进程外 Agent，stdio + HTTP）

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

### 2. MCP Server（进程外 Agent）

```csharp
builder.Services.AddFeishuTools();
builder.Services.AddFeishuMcpServer(o => o.AppKey = "default");
await app.Services.RunFeishuMcpStdioAsync();
```

Claude Code / Cursor / 自建 harness 经 stdio 连接即可调用 351 个飞书工具，
授权 / 租户切换 / 净化 / 审计与进程内 Agent **完全一致**。

### 3. HTTP API 调用

```csharp
builder.Services.AddFeishuApp(builder.Configuration);
builder.Services.CreateFeishuServicesBuilder().AddAllApis().Build();

// 注入即调用，令牌全生命周期托管
app.MapGet("/users/{userId}",
    (IFeishuTenantV3User userApi, string userId, CancellationToken ct) =>
        userApi.GetUserInfoByIdAsync(userId, ct));

// 多应用作用域切换
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

---

## 包家族

| 包 | 职责 |
| --- | --- |
| `Mud.Feishu.Abstractions` | 事件处理抽象层、策略模式 + 工厂模式、FeishuMetrics 指标收集、事件去重、SSRF 防护 |
| `Mud.Feishu` | 核心 HTTP API 客户端（20+ 模块），自动令牌管理、智能重试、熔断器、多应用上下文 |
| `Mud.Feishu.DataModels` | 全线强类型请求/响应模型 + 源生成 `JsonSerializerContext` |
| `Mud.Feishu.Authentication` | 基于 AsyncLocal 的线程安全用户上下文 + JWT Claims 提取 + 分布式追踪 |
| `Mud.Feishu.EventCallback` | 事件回调强类型模型 + 源生成器自动生成处理器基类 |
| `Mud.Feishu.WebSocket` | 长连接实时订阅、指数退避重连、序号验证、事件去重、SSL/TLS 校验 |
| `Mud.Feishu.Webhook` | HTTP 回调、签名/时间戳/Nonce 三道闸、AES-256-CBC 解密、频率限制、多应用、重试 |
| `Mud.Feishu.Redis` | 分布式去重（事件/Nonce/SeqID）+ 降级策略 + 多应用隔离 + 健康检查 |
| `Mud.Feishu.OpenTelemetry` | 一键开启分布式追踪与指标采集（OTLP gRPC，Jaeger/Tempo/Prometheus） |
| **`Mud.Feishu.AI`** | **Agent 运行时**：会话管理、流式消息通道、事件驱动对话、知识检索（Aily + RAG）、HITL 审批续跑 |
| **`Mud.Feishu.AI.Tools`** | **351 策展工具**：编译期 Schema + 类型化契约 + 七步执行链 + affordance 资产 + Skills 导出 |
| **`Mud.Feishu.AI.Mcp`** | **MCP server**（stdio 与 Streamable HTTP 两种传输）：进程外 Agent 工具面，执行链零旁路，租户身份 fail-closed |

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
| **Mud.HttpUtils**             | v3.0.9  | HTTP 客户端工具类（含源代码生成器） |
| **Mud.HttpUtils.Generator**   | v3.0.9  | HTTP 客户端代码生成器（编译时）     |
| **Mud.HttpUtils.Resilience**  | v3.0.9  | 弹性策略（重试 / 超时 / 熔断）      |
| **Microsoft.Extensions.Http** | v10.0.12 | HTTP 客户端工厂                    |
| **Microsoft.Agents.AI**       | v1.25.0 | AI 基座（仅 AI 包引用，核心包 MAF-free） |
| **Mud.AI.Agent / Mud.AI.Abstractions** | v1.0.2 | 中立 AI 运行时（独立仓库发包） |

---

## 目录结构

```
MudFeishu/
├── Mud.Feishu/                    # 核心 HTTP API 客户端
├── Mud.Feishu.Abstractions/       # 事件处理抽象层
├── Mud.Feishu.DataModels/         # 强类型数据模型 + 源生成 JsonContext
├── Mud.Feishu.AI/                 # Agent 运行时（会话/流式/知识/HITL）
├── Mud.Feishu.AI.Tools/           # 351 策展工具 + 执行链 + 编译期 Schema
├── Mud.Feishu.AI.Mcp/             # MCP server（stdio + Streamable HTTP）
├── Mud.Feishu.WebSocket/          # 长连接实时订阅
├── Mud.Feishu.Webhook/            # HTTP 回调事件处理
├── Mud.Feishu.Authentication/     # 用户认证中间件
├── Mud.Feishu.EventCallback/      # 事件回调强类型模型
├── Mud.Feishu.Redis/              # 分布式去重
├── Mud.Feishu.OpenTelemetry/      # 可观测性
├── skills/                        # 34 域 Skills 产物（随包分发）
├── Tests/                         # 11 个测试工程
├── Demos/                         # 示例（含 AI Agent Demo / AOT 验证）
├── documents/                     # 官方文档镜像 + 工具权限对照表
├── scripts/                       # verify-build / audit-config-keys / GenerateJsonContext / ...
└── Directory.Build.props          # 全局 MSBuild 属性（Version 3.1.1 唯一来源）
```

---

## 许可证

本项目遵循 [MIT 许可证](./LICENSE)，允许商业和非商业用途。

---

<div align="center">

**如果觉得 MudFeishu 对你有帮助，请给个 ⭐Star 支持一下！**

Made with ❤️ by Mud Studio

</div>
