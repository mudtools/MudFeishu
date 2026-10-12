# MudFeishu AI 模块 vs 官方 lark-cli 对比分析报告

| 项 | 值 |
|---|---|
| 文档日期 | 2026-10-10 |
| 对比对象 | 当前项目 `Mud.Feishu.AI` / `Mud.Feishu.AI.Mcp` / `Mud.Feishu.AI.Tools`（本地 `E:\temp\MudFeishu`）<br>官方 `lark-cli`（本地 `D:\Repos\cli`，Go module `github.com/larksuite/cli`） |
| 分析方法 | 四路并行代码探索（Mud 侧三项目架构 + lark-cli skills/affordance/extension 体系 + lark-cli 命令/协议/事件/sidecar + lark-cli 设计理念文档） |
| 结论一句话 | 两者**生态位互补而非竞品**：lark-cli 是"AI 操作飞书的手"（工具层，进程外，广度优先）；MudFeishu.AI 是"飞书原生的 AI Agent 大脑+手"（运行时层，进程内，深度优先）。 |
| 关联文档 | `documents/AIAgent/lark-cli-同场景对比备忘.md`（2026-09-27 早期备忘，本报告是其展开） |

---

## 一、根本定位差异（理解一切对比的前提）

| 维度 | 当前项目 `Mud.Feishu.AI` 系列 | 官方 `lark-cli` |
|---|---|---|
| 形态 | **.NET 进程内 SDK**（NuGet 包，被宿主应用引用） | **Go CLI 二进制**（npm 分发，被 Agent 当子进程调用） |
| 生态位 | **含 AI Agent 的运行时**（事件→会话→模型→工具一体化） | **被 AI Agent 调用的工具层**（自身不含 Agent） |
| 设计哲学 | 深度优先：进程内直连 + 强类型/AOT + 多租户强制授权执行链 | 广度优先："for humans and AI agents"，机器可读性优先但不牺牲人类 UX |
| 语言/运行时 | C# / .NET（netstandard2.0→net10.0，AOT 友好） | Go 1.23（单二进制，`go:embed` 内嵌内容） |
| 与 MAF 关系 | **组合复用** `Microsoft.Agents.AI 1.20.0`（`FeishuAgent : AIAgent`） | **不依赖任何 AI/MCP SDK**（go.mod 无相关依赖） |

**关键结论**：两者**不是同类竞品，而是互补分层**。lark-cli 是"AI 操作飞书的手"，MudFeishu.AI 是"飞书原生的 AI Agent 大脑+手"。仓库自身在 `documents/AIAgent/lark-cli-同场景对比备忘.md:7,45-49` 已作此判断（分层互补、非替代）。这一根本差异决定了下文所有"优缺点"都不能脱离生态位单独评判。

---

## 二、功能覆盖面对比（能力雷达）

### 2.1 工具/命令面规模

| 项 | MudFeishu.AI.Tools | lark-cli |
|---|---|---|
| 策展工具/命令数 | **163 个工具**（88 只读 + 75 写，21 域 + 4 元工具） | **200+ 命令**（README 宣称） |
| 底层 API 覆盖 | SDK **1228 个方法**，策展为 163 工具 | catalog **15 个服务**，typed 命令 + **`api` 逃生舱覆盖 2500+ API** |
| 通用 API 执行器 | **已删除** `feishu.api_call`（R-12，2026-10-10，理由：双轨路径/最高风险面/零外部消费） | **保留** `api <method> <path>` 作为全覆盖逃生舱 |
| 三层命令架构 | 单层策展工具 + 元工具（capability_lookup/schema_read/guidance_read/tool_search） | **三层**：Shortcuts（`+verb`）→ API Commands（catalog 自动注册）→ Raw API |
| 业务域数 | 21 域（Bitable/Docx/Wiki/IM/Drive/Sheets/Contact/Approval/Calendar/Task/Mail/Minutes/OKR/VC/Knowledge/Board/Attendance/Spark/AI文本/元工具） | 15 服务 catalog + 19 shortcut 域 + 28 skills |

**解读**：lark-cli 在**广度上明显胜出**（2500+ API 全覆盖的逃生舱 + 200+ 命令），MudFeishu 在**策展密度上更高**（1228 方法精炼为 163 工具，每个工具带强类型契约/风险分级/scope）。MudFeishu 删除通用执行器是**有意的安全取舍**（避免双轨调用路径），但代价是**未策展的 API 无法被 AI 调用**——这是覆盖面的一个结构性缺口。

### 2.2 Agent 运行时（MudFeishu 独有，lark-cli 无）

| 能力 | MudFeishu.AI | lark-cli |
|---|---|---|
| Agent 编排 | ✅ `FeishuAgent : AIAgent`（组合 MAF `ChatClientAgent`） | ❌ 无（它是工具，不是 Agent） |
| 模型接入 | ✅ OpenAI-compatible `IChatClient`（键控，HTTPS 白名单） | ❌ 无 |
| 会话/记忆 | ✅ `IConversationStore` + TTL + 串行闸门 + **渐进式摘要** + token 精确计数（Tiktoken） | ❌ 无 |
| 流式回复 | ✅ 编辑分片 / 卡片流降级链 / `IMessageChannel` | ❌ 无 |
| 事件→会话→模型→回复闭环 | ✅ IM/审批/任务/Bitable 四种处理器 | ❌ 无（事件只 NDJSON 流出，由外部 Agent 消费） |
| RAG 知识问答 | ✅ RAG-A（Aily 托管）+ RAG-B（自建向量检索） | ❌ 无 |
| HITL 人工确认 | ✅ 无令牌版（批准通道 + 待确认快照 + 幂等续跑闭环） | ⚠️ 退出码 10 确认门禁（让外部 Agent 处理） |

**这是 MudFeishu 的护城河**：它提供的是"开箱即用的飞书智能体运行时"，lark-cli 提供的是"让任意 Agent 能操作飞书的工具"。两者面向不同消费者。

### 2.3 AI 协议面（MCP）

| 项 | MudFeishu.AI.Mcp | lark-cli |
|---|---|---|
| MCP Server | ✅ **自研** stdio JSON-RPC Server（`initialize`/`ping`/`tools/list`/`tools/call`） | ❌ 不运行 MCP 服务端 |
| MCP Client | ❌ 无 | ❌ 无（但**解析** MCP 网关错误响应） |
| MCP 风格 tool schema | ✅ `tools/list` 的 `inputSchema` 取自编译期 `AIFunction.JsonSchema` | ✅ `schema` 命令输出 **MCP 风格 envelope**（`name/description/inputSchema/outputSchema/_meta`） |
| 协议版本 | 闭集协商（2024-11-05 / 2025-03-26 / 2025-06-18） | envelope_version "1.0" |
| 传输 | 仅 stdio（刻意不做 SSE/HTTP，防 AsyncLocal 租户串号） | N/A（不充当服务端） |
| 生态定位 | 自立一个 MCP Server 供进程外 Agent 接入 | **被嵌入**：作为 MCP 生态中"可被 Agent 调用的能力层"，通过 skills + schema + 结构化契约供 Agent 使用 |

**解读**：MudFeishu 选择了"自己做 MCP Server"（进程内执行链复用，租户边界=进程），lark-cli 选择了"不做 Server，但产出 MCP 风格 schema 供任意 Agent 自建工具表"。两种路线各有道理：MudFeishu 的 Server 复用同一执行链（授权/租户/净化/审计零旁路），lark-cli 的方式更轻、更易被任意 Agent 集成。

### 2.4 Skills / 可发现性体系

| 项 | MudFeishu | lark-cli |
|---|---|---|
| Skills 数量 | 21 个域产物（`SKILL.md` + references JSON） | **28 个** skill（含 `lark-shared`/`lark-skill-maker`/`lark-openapi-explorer` 元能力） |
| Skills 分发 | 随 NuGet 包 `<None Include="..\skills\**\*" Pack="true">` | `go:embed` 编译进二进制，`lark-cli skills list/read` 暴露 |
| Skills 外部消费者 | ⚠️ 架构报告自评"**尚无外部消费者**"（`.docs/AI/MudFeishu-AI系列-架构分析报告.md:151-155`） | ✅ `npx skills add larksuite/cli`，兼容主流 AI 工具，有版本漂移检测 |
| Suite 聚合路由 | ❌ 无对应物 | ✅ `isolated-skills/lark-suite/` + `skill-template/` 模板合成 |
| 自定义 skill 框架 | ❌ 无 | ✅ `lark-skill-maker` skill 教 AI 创建新 skill |
| Affordance（per-command 决策指导） | ❌ 无独立 affordance 层 | ✅ `affordance/*.md`（8 域），运行时懒加载注入 `--help` 和 `schema` |
| 元工具 | ✅ capability_lookup / schema_read / guidance_read / tool_search | ⚠️ 通过 `schema` + `skills list/read` + `event list/schema` + `whoami` 组合实现 |

**这是 MudFeishu 最大的覆盖缺口**：Skills 生态位导出层尚无外部消费者，而 lark-cli 的 skills 已是面向外部 Agent 的成熟分发体系（含模板合成、版本同步、suite 聚合、自定义框架）。

### 2.5 事件 / 实时能力

| 项 | MudFeishu | lark-cli |
|---|---|---|
| 事件订阅 | ✅ WebSocket 模块（完整长稳/重连/并发模型，I13-I16 守卫） | ✅ `event consume`（WebSocket 长连接，NDJSON 流式） |
| 事件域 | IM/审批/任务/Bitable 处理器 + 事件外桥 NDJSON | 7 域 EventKey（application/approval/im/minutes/task/vc/whiteboard） |
| 事件→AI 闭环 | ✅ **进程内**（事件→会话→模型→回复） | ⚠️ **进程外**（NDJSON 流给外部 Agent，由 Agent 自行闭环） |
| 事件自省 | ✅ `FeishuEventCatalog` | ✅ `event list` / `event schema <key>` |

### 2.6 错误体系

| 项 | MudFeishu | lark-cli |
|---|---|---|
| 错误分类 | 执行链 fail-closed + 授权三态 + 风险分级 + 内容安全三模式 | **9 Category + Subtype wire-stable** 分类法（RFC 7807 对齐） |
| AI 可读契约 | ⚠️ 错误载荷有唯一构造点 `ToolErrorFactory`，但**无系统化 wire-stable 分类** | ✅ `error.type/subtype/code/hint/retryable/retry_after_seconds/missing_scopes`，面向 AI/脚本/MCP 适配器 |
| 可恢复性 | ⚠️ 依赖宿主 `IToolExecutionAuthorizer` | ✅ `hint` 给具体恢复命令，`missing_scopes` 直接列出缺失 scope，`Suggestions` did-you-mean |
| 退出码语义 | N/A（进程内 SDK） | ✅ 0/1/2/3/4/5/6/**10**（10=高风险确认门禁，agent 协议信号） |
| 错误文档 | `documents/ErrorHandling.md` | `errs/ERROR_CONTRACT.md`（明确三大读者：AI/适配器/框架） |

### 2.7 安全 / 多租户 / 扩展

| 项 | MudFeishu | lark-cli |
|---|---|---|
| 多租户 | ✅ AsyncLocal 上下文 + `BeginScope(appKey)` + 缺 appKey fail-closed | ✅ 多 profile + sidecar 多租户扩展 |
| 凭据隔离 | ⚠️ 进程内（配置面） | ✅ **sidecar**（HMAC 签名，沙箱/CI 环境凭据不进不可信环境）+ keyring 跨平台 |
| 执行链门禁 | ✅ **五步**（上下文→净化→策略轴→授权→切租户→内容安全→出站净化→整形→审计） | ⚠️ 命令级 risk/scope + cmdpolicy 引擎 |
| 扩展机制 | ✅ 宿主钩子（`IToolExecutionAuthorizer`/`IToolResultShaper`/`IToolExecutionAuditSink`/`IFeishuAttachmentStager`） | ✅ **plugin SDK**（credential/transport/platform），fork 二进制，skill overlay |
| 内容安全 | ✅ off/warn/block 三模式 | ✅ contentsafety registry |
| 附件边界 | ✅ 禁止二进制穿越（A10），`IFeishuAttachmentStager` 软缺席 | ⚠️ download/fileio 工具 |

### 2.8 治理 / 工程

| 项 | MudFeishu | lark-cli |
|---|---|---|
| 编译期契约 | ✅ **单源契约**（Schema/契约表/名字表/方法目录/能力目录，零运行期反射）+ golden 逐字节门禁 | ✅ catalog sha256 校验 + codemeta 中央注册 |
| AOT | ✅ 严格模式（IL2026/IL3050 净零） | N/A（Go 天然静态） |
| 契约守卫 | ✅ **44+ ContractGuards**（Tools.Tests 一个目录 46 文件） | ✅ qualitygate（命名/引用/skill质量/dryrun/errorfacts）+ lint/errscontract AST |
| PublicAPI 门禁 | ✅ 三重（Shipped/Unshipped） | ⚠️ 无对应（Go 无 PublicAPI 概念） |
| LLM 审查 CI | ❌ 无 | ✅ `semantic-review.yml`（火山方舟 ARK，PR 语义审查） |
| plugin e2e | ❌ 无（有 Demo 守卫） | ✅ 20 个客户 fork 验证 L4 插件契约 |

---

## 三、各自优缺点

### 3.1 当前项目（MudFeishu.AI）优点

1. **进程内一体化闭环**：事件→会话→模型→工具→流式回复全在进程内，无 IPC 开销，租户上下文 AsyncLocal 自然流转——这是 lark-cli 架构上做不到的。
2. **编译期单源契约 + AOT**：163 工具的 Schema/契约/名字/风险/scope 全部编译期生成，零运行期反射，golden 逐字节门禁。lark-cli 的 schema 是运行时从 catalog 渲染。
3. **执行链护城河**：五步不可变门禁（上下文→净化→策略轴→授权→切租户→内容安全→出站净化→整形→审计），出站唯一出口，错误载荷唯一构造点。安全密度极高。
4. **策展质量**：1228 方法精炼为 163 工具，每个带强类型契约 + 风险分级 + scope + 身份轴，避免"工具爆炸"导致模型选择困难。
5. **MAF 复用**：直接站在 `Microsoft.Agents.AI` 肩膀上，会话/摘要/闸门/token 计数用框架成熟实现，不重复造轮子。
6. **治理密度**：44+ 契约守卫 + 三重 PublicAPI 门禁 + AOT 严格模式，工程纪律严苛。

### 3.2 当前项目缺点

1. **Skills 生态位悬空**：21 个域 Skills 产物已导出但"尚无外部消费者"——投入产出比待验证。
2. **覆盖面有硬天花板**：删除 `feishu.api_call` 后，未策展的 API（1228 方法中未被 163 工具覆盖的部分）AI 无法调用。lark-cli 的 `api` 逃生舱可触达 2500+。
3. **错误契约不够"AI 原生"**：无 wire-stable 的 Category/Subtype 分类法，无 `missing_scopes`/`retryable`/`hint` 系统化字段，AI 消费错误时缺少结构化恢复信号。
4. **无 affordance 层**：缺少 per-command 的 WHEN/Avoid/Prerequisites 决策指导注入机制，AI 选工具主要靠 guidance + 元工具，不如 lark-cli 的 affordance 精细。
5. **无 sidecar 凭据隔离**：沙箱/CI 环境下凭据必须进进程，不如 lark-cli 的 HMAC sidecar 安全。
6. **MCP 仅 stdio**：刻意不做 SSE/HTTP（防租户串号），但限制了接入方式灵活性。
7. **结构性张力**（架构报告自认 3 处）：命名空间跨程序集、`[FeishuTool]` 生命周期断链、执行链定义↔处理器循环依赖。
8. **无 LLM 审查 CI / 无 plugin e2e**：工程治理少了两个 lark-cli 有的自动化维度。

### 3.3 官方项目（lark-cli）优点

1. **覆盖广度碾压**：2500+ API 全覆盖（逃生舱）+ 200+ 命令 + 28 skills，AI 能触达任何飞书能力。
2. **Skills 体系成熟**：手写 SKILL.md + references + go:embed 版本锁步 + `skills list/read` + suite 聚合 + `lark-skill-maker` 自定义框架 + 版本漂移检测，已是面向外部 Agent 的完整分发生态。
3. **affordance 分层理念**：WHAT（schema）/ WHEN（affordance）/ 路由（SKILL.md）/ 条件 HOW（references）四层正交，不重复描述，运行时懒加载注入 help/schema。
4. **错误契约 AI 原生**：9 Category + Subtype wire-stable + hint + retryable + missing_scopes + Suggestions + 退出码 10 确认门禁，明确服务 AI/脚本/MCP 适配器三类读者。
5. **sidecar 凭据隔离**：HMAC 签名，沙箱/CI 环境真实凭据不进不可信进程，多租户扩展完备。
6. **plugin SDK 完备**：credential/transport/platform 三扩展点，fork 二进制可替换凭据源/拦截 HTTP/限制命令面/定制 skill overlay，20 个客户 fork e2e 验证。
7. **三层命令架构**：Shortcuts（人类/AI 友好）→ API Commands（typed）→ Raw API（全覆盖），兼顾易用性与覆盖面。
8. **LLM 语义审查 CI**：火山方舟 ARK 对 PR 做语义审查，工程自动化前沿。

### 3.4 官方项目缺点

1. **无 Agent 运行时**：自身不是 Agent，事件/会话/模型/记忆/RAG/HITL 全部甩给外部 Agent 自行实现——消费方集成成本高。
2. **进程外 IPC 开销**：每次工具调用 = 子进程启动 + JSON 序列化，高频调用不如进程内直连。
3. **无强类型/AOT**：Go 静态编译但 schema 运行时从 catalog 反射渲染，无编译期契约门禁。
4. **无策展精炼**：200+ 命令平铺，工具选择压力全压给 AI（靠 affordance/skills 缓解，但无 MudFeishu 式的"1228→163 策展"）。
5. **无进程内多租户强制**：租户隔离靠 profile + sidecar，不如 AsyncLocal + 执行链 fail-closed 强制。
6. **MCP 仅"风格"非"协议"**：产出 MCP 风格 schema 但不运行 MCP Server，消费方仍需自建工具表。

---

## 四、当前项目做得好的地方（值得保持/强化）

1. **进程内一体化闭环**——这是 lark-cli 架构上无法复制的护城河，应作为核心卖点对外讲。
2. **编译期单源契约 + golden 门禁**——工具面漂移零容忍，质量基线远高于运行时渲染。
3. **五步执行链**——安全密度是飞书 AI 工具面里最高的，应继续强化（而非简化）。
4. **策展哲学**——1228→163 的精炼避免了"工具爆炸"，模型选择体验优于 200+ 平铺。
5. **MAF 复用而非自研 Agent**——站在微软框架肩膀上，避免重复造轮子，长期维护成本低。
6. **HITL 无令牌版**——批准状态唯一归宿主，SDK 只通知不持有令牌，安全模型干净。
7. **RAG-A/B 双路**——Aily 托管 + 自建向量检索，覆盖托管与私有化两种部署。

---

## 五、官方项目值得借鉴的地方（按优先级排序）

### P0 — 直接影响 AI 调用成功率与覆盖面

1. **错误契约 AI 原生化**（借鉴 `errs/ERROR_CONTRACT.md`）：引入 wire-stable 的 `Category/Subtype` 分类法 + `hint`(可操作恢复命令) + `missing_scopes` + `retryable/retry_after_seconds` + `Suggestions`(did-you-mean)。当前 `ToolErrorFactory` 是唯一构造点，改造成本可控。**收益**：AI 消费错误时能结构化分支恢复，而非解析自由文本 message。

2. **affordance 决策指导层**：在 guidance 之上增加 per-tool 的 WHEN/Avoid/Prerequisites/Tips 决策上下文，注入 schema 的 `_meta.affordance`。当前 163 工具的"何时用/避免用"散在域 guidance 里，提炼为结构化 affordance 能显著提升模型选工具准确率。

3. **覆盖面逃生舱（可选安全版）**：重新评估是否需要一个**强约束的**通用 API 执行器（非 lark-cli 的裸 `api`，而是带白名单 + 风险升级 + 强制 scope 校验的版本），覆盖未策展 API。当前完全删除虽安全，但断了 AI 触达长尾 API 的路径。

### P1 — 生态位与可发现性

4. **Skills 外部消费者落地**：21 个域 Skills 产物已导出但无消费者，应推动至少一个外部 Agent（如 Claude/Cursor/豆包）实际消费，或自建 Demo Agent 消费，闭环验证 SKILL.md 格式有效性。借鉴 lark-cli 的 `skills list/read` 命令面 + 版本漂移检测。

5. **suite 聚合路由 + 自定义 skill 框架**：借鉴 `isolated-skills/lark-suite/` 的聚合路由层（一个入口路由到各子能力），降低 Agent 的 skill 发现成本；借鉴 `lark-skill-maker` 让宿主能自定义 skill。

6. **`schema` 命令式自省出口**：当前有元工具 `capability_lookup/schema_read`，但缺少一个**面向进程外消费者**的"导出全部工具 MCP envelope"的独立出口（MCP `tools/list` 是协议内方法，非独立 CLI/HTTP 端点）。借鉴 lark-cli `schema` 命令的独立自省面。

### P2 — 安全与扩展

7. **sidecar 凭据隔离模式**：借鉴 lark-cli 的 HMAC sidecar，为沙箱/CI 场景提供"真实凭据不进不可信进程"的选项。当前多租户靠配置面，CI 场景凭据暴露风险高于 sidecar。

8. **plugin SDK 化扩展点**：当前宿主钩子（Authorizer/Shaper/AuditSink/AttachmentStager）是 .NET 接口注入，借鉴 lark-cli 的 credential/transport/platform 三扩展点模型，把"可替换凭据源/可拦截 HTTP/可限制命令面"显式化为扩展契约。

### P3 — 工程自动化

9. **LLM 语义审查 CI**：借鉴 `semantic-review.yml`，对 PR 引入 AI 相关变更（工具面/guidance/SKILL.md）做 LLM 语义审查。

10. **plugin e2e 契约测试**：借鉴 lark-cli 的 20 个客户 fork e2e，对宿主扩展钩子做跨消费者契约验证。

---

## 六、功能覆盖面相对于官方项目的差距清单

| 差距项 | 当前状态 | lark-cli 状态 | 影响程度 |
|---|---|---|---|
| **API 全覆盖逃生舱** | ❌ 已删 `feishu.api_call` | ✅ `api` 覆盖 2500+ | 高（长尾 API 不可达） |
| **Skills 外部消费者** | ❌ 无外部消费 | ✅ `npx skills add` 主流工具兼容 | 高（生态位未闭环） |
| **affordance 决策层** | ❌ 无独立层 | ✅ 8 域 markdown 注入 help/schema | 中高（选工具准确率） |
| **错误 wire-stable 分类** | ❌ 无系统化分类 | ✅ 9 Category + Subtype + 恢复字段 | 中高（AI 错误恢复） |
| **suite 聚合路由** | ❌ 无 | ✅ `lark-suite` 单入口路由 | 中（skill 发现成本） |
| **自定义 skill 框架** | ❌ 无 | ✅ `lark-skill-maker` | 中（宿主扩展性） |
| **sidecar 凭据隔离** | ❌ 无 | ✅ HMAC sidecar + 多租户 | 中（CI/沙箱安全） |
| **LLM 语义审查 CI** | ❌ 无 | ✅ ARK PR 审查 | 低（工程自动化） |
| **退出码 10 确认门禁协议** | ⚠️ 有 HITL 但无退出码协议 | ✅ 退出码 10 + `--yes` | 低（进程内 SDK 不靠退出码） |
| **shell 补全 / whoami** | N/A（SDK 形态） | ✅ completion + whoami | 无（生态位不同，非差距） |

**反向差距**（MudFeishu 有而 lark-cli 无，不算"差距"但值得明确）：Agent 运行时 / 会话记忆摘要 / 流式回复 / RAG / 进程内事件闭环 / 编译期契约 / AOT / 五步执行链 / MAF 复用——这些是生态位差异，不是覆盖缺口。

---

## 七、总结建议

1. **定位讲清楚**：对外不要和 lark-cli 比"命令数"，而要讲"进程内一体化 Agent 运行时 vs 进程外工具层"的生态位差异——两者可共存（lark-cli 做广度兜底，MudFeishu.AI 做深度闭环）。

2. **补 P0 三项**（错误契约 AI 原生化 / affordance 层 / 覆盖面逃生舱评估）是性价比最高的改进，直接提升 AI 调用成功率与覆盖面。

3. **Skills 闭环**：21 个域产物已投入但无消费者，是当前最大的"悬空资产"，应优先推动一个外部消费者落地验证。

4. **保持护城河**：进程内闭环 / 编译期契约 / 五步执行链 / 策展哲学 是 lark-cli 架构上无法复制的优势，改进时不应为追广度而削弱这些深度特性。

---

## 附录 A：当前项目关键文件索引

| 主题 | 文件（相对仓库根） |
|---|---|
| 架构分析报告 | `.docs/AI/MudFeishu-AI系列-架构分析报告.md` |
| 与 lark-cli 同场景对比（早期备忘） | `documents/AIAgent/lark-cli-同场景对比备忘.md` |
| 工具权限对照表（163 工具） | `documents/AIAgent/工具权限对照表.md` |
| scope 权威清单 | `documents/AIAgent/scope-authority.json` |
| 工具面 Readme（528 行，权威） | `Mud.Feishu.AI.Tools/Readme.md` |
| AI 运行时 Readme（HITL 契约） | `Mud.Feishu.AI/Readme.md` |
| Agent 主循环 | `Mud.Feishu.AI/Agents/FeishuAgent.cs:31` |
| 执行链（护城河） | `Mud.Feishu.AI.Tools/Tools/FeishuToolBinding.cs:36` |
| 生成剖面 | `Mud.Feishu.AI.Tools/SdkProfile/FeishuToolProfile.cs:46` |
| MCP 协议层 | `Mud.Feishu.AI.Mcp/FeishuMcpToolServer.cs:51` |
| 工具装配入口 | `Mud.Feishu.AI.Tools/Extensions/FeishuToolsServiceCollectionExtensions.cs:43` |
| Agent 装配入口 | `Mud.Feishu.AI/Extensions/FeishuAgentServiceCollectionExtensions.cs:100` |
| Demo（可运行参照） | `Demos/Mud.Feishu.Agent.Demo/`（含 `DocAgent/`、`Modes/`、`DemoAttachmentStager`） |
| Skills 产物示例 | `skills/feishu-ai/SKILL.md`（共 21 个域） |

## 附录 B：官方 lark-cli 关键文件索引

| 主题 | 文件（`D:\Repos\cli\...`） |
|---|---|
| 项目定位 / AI Agent 快速开始 / JSON 契约 | `README.md` / `README.zh.md` |
| AI/贡献者规范 / Affordance/Skills 分层 / Hard Contracts | `AGENTS.md` |
| AI 能力演进历程 | `CHANGELOG.md` |
| go:embed 白名单 + init 注入 | `content_embed.go:22-39` |
| `lark-cli skills list/read` 命令实现 | `cmd/skill/skill.go:47-183` |
| skill 内容 Reader / frontmatter 解析 | `internal/skillcontent/reader.go:38-209` |
| skill overlay 组合 resolver | `internal/skillpolicy/resolver.go:76-90` |
| affordance 懒加载 Resolver | `internal/affordance/affordance.go:51-158` |
| affordance markdown 解析 | `internal/affordance/mdparse.go` |
| affordance/skill 注入 CLI build | `cmd/build.go:117-121,466-483` |
| affordance 格式规范 | `affordance/README.md` |
| `Shortcut`/`Flag` 类型定义 | `shortcuts/common/types.go:32-103` |
| 全域 shortcut 聚合注册 | `shortcuts/register.go:76-112` |
| credential Provider 接口 | `extension/credential/types.go:96-100` |
| provider chain 注册排序 | `extension/credential/registry.go:21-28` |
| plugin SDK 完整契约 | `extension/platform/README.md` |
| 聚合 skill 母模板 | `skill-template/master-skill-template.md` |
| 单域 skill 模板 | `skill-template/skill-template.md` |
| 路由关键词 + 引用重写配置 | `skill-template/lark-suite-config.json` |
| suite 聚合路由入口 | `isolated-skills/lark-suite/SKILL.md` |
| MCP 风格 tool envelope 类型 | `internal/schema/types.go:14-26` |
| envelope assembler | `internal/schema/assembler.go:22-280` |
| `schema` 命令 | `cmd/schema/schema.go:63` |
| API catalog manifest + 服务分片 | `internal/registry/catalog/manifest.json` + `services/*.json` |
| 错误契约文档 | `errs/ERROR_CONTRACT.md` |
| 9 Category 定义 | `errs/category.go` |
| Subtype 体系 | `errs/subtypes.go` |
| typed error 扩展字段 | `errs/types.go` |
| 退出码映射 | `internal/output/exitcode.go:14-23` |
| Lark code → Category/Subtype 分类 | `internal/errclass/codemeta*.go` |
| 事件总线守护进程 | `internal/event/bus/bus.go:41` |
| WebSocket 事件入口 | `internal/event/adapter/lark/websocket/feishu.go:23` |
| `event consume` 命令 | `cmd/event/consume.go:47` |
| sidecar 线协议 | `sidecar/protocol.go` |
| sidecar HMAC 签名 | `sidecar/hmac.go` |
| sidecar fail-closed 守卫 | `main_noauthsidecar.go:31` |
| LLM 语义审查 CI | `.github/workflows/semantic-review.yml` |
| skill 格式校验脚本 | `scripts/skill-format-check/index.js` |