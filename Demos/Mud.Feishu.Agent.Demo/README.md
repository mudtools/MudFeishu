# Mud.Feishu 文档业务 AI 智能体 · 控制台 Demo

把 `Mud.Feishu` 的 **402 个飞书 API 接口**经 `Mud.Feishu.AI.Tools` 的工具面暴露给模型，在**单进程控制台**里
完成**飞书文档业务（docx / wiki / drive / sheets / bitable / search）的读写闭环**——具备工具调用可视化、
三道安全闸（框架审批 / 宿主授权 / 执行审计）、命中式剧本与多轮记忆。

> 这不是「固定脚本」，而是**真智能体**：由模型自主决定调用哪些工具、以什么顺序调用。
> 同一条输入在不同模型/不同轮次会产出不同的工具序列。

---

## 1. 四个运行模式（互不干扰）

| 模式 | 开关（配置文件） | 内容 |
| --- | --- | --- |
| Phase 0 裸模型 | 默认（不设任何开关，读 `FeishuChatDemo` 节） | 一问一答，不含任何飞书能力 |
| Phase 1/2 工具冒烟 | `FeishuToolsDemo:Enabled=true` | 全域只读工具，打印工具名 + 只读问答 |
| P2D-5a 事件接入 | `FeishuImHandlerDemo:Enabled=true` | 打印 IM 会话处理器的注册面代码文本 |
| **文档业务智能体** | **`FeishuDocAgent:Enabled=true`** | **本文件描述的模式** |

四个模式**互斥**，按上表顺序判定，命中即返回。**参数与模式开关全部来自配置文件（`appsettings*.json`），
环境变量不再参与**。

---

## 2. 快速开始（Windows / PowerShell）

把参数写进 `appsettings.local.json`（已被 `.gitignore` 忽略）：

```powershell
Set-Location Demos/Mud.Feishu.Agent.Demo
Copy-Item appsettings.json appsettings.local.json    # 模板 → 本地覆盖文件
# 编辑 appsettings.local.json：填目标模式的 Enabled=true 与模型/飞书凭证
Set-Location <仓库根>
dotnet run --project Demos/Mud.Feishu.Agent.Demo
```

详见 [§3 配置来源](#3-配置来源文件)。

启动后应先看到**能力面横幅**（配置来源 / 工具数 / 身份闭集 / 授权强制 / guidance 装配 / 剧本数），随后进入 `you › ` 提示符：

```
════════════════════════════════════════════════════════════
 Mud.Feishu 文档业务 AI 智能体 · 控制台 Demo
════════════════════════════════════════════════════════════
 配置来源     : appsettings.json + appsettings.local.json
 应用 appKey  : demo-app（AppId cli_***，Secret 已隐藏）
 模型         : glm-4-flash @ https://.../v4/
 已启用工具   : 27 个（只读 14 / 写 13）
 身份闭集     : tenant（文档业务域全部 tenant；按域事实推导）
 风险上限     : high-risk-write（允许 docx.delete_blocks 进入审批管线）
 授权强制     : on（写工具未注册授权器即拒绝 → 本 Demo 已注册 ConsoleToolAuthorizer）
 授权策略     : strict
 工具结果截断 : 4000 字符（MaxToolResultLength，大文档读取必然截断）
 会话记忆     : 50 条 / 8000 token / 摘要阈值 30
 指令装配     : 7 个域 guidance，共 1845 / 8192 字符（未截断）
 回复通道     : 控制台（流式，唯一通道；分片阈值 200 字符 / ≥60ms）
 剧本         : 6 部（/scenario 列表，/scenario kb-digest 直达）
════════════════════════════════════════════════════════════
```

**5 分钟上手路径**：把 `FeishuDocAgent:WikiSpaceId` 设为你可见的知识库空间 ID → 输入 `/scenario kb`
（只读剧本，0 次审批，全自动跑完）→ 再试 `/scenario author`（写剧本，需要你 /approve）。

---

## 3. 配置来源（文件）

### 3.1 优先级（高 → 低）

| # | 来源 | 说明 |
| --- | --- | --- |
| ① | `appsettings.local.json` | 本地覆盖（**已被 `.gitignore` 忽略**）——**真实密钥写这里** |
| ② | `appsettings.json` | 随仓库提交的模板（只留空密钥与默认值） |
| ③ | 代码默认值 | 见下表"默认"列 |

配置根目录固定为 **程序输出目录**（`AppContext.BaseDirectory`，两个 `appsettings*.json` 由 csproj 随产物复制），
与当前工作目录无关：`dotnet run`、`dotnet bin/Debug/net10.0/xxx.dll`、任意 cwd 行为一致。

单个配置项的查找顺序：`FeishuDocAgent:{键}` → `FeishuApps` 的**主应用**（仅租户三项，见 §3.4）。

### 3.2 配置键对照表（节 `FeishuDocAgent`）

| 配置文件键（节 `FeishuDocAgent`） | 必填 | 默认 | 说明 |
| --- | --- | --- | --- |
| `Enabled` | 否 | `false` | 置 `true` 启用本模式 |
| `ModelId` | ✅ | — | 模型 ID（如 `glm-4-flash`） |
| `ApiKey` | ✅ | — | 模型 API Key |
| `Endpoint` | 否 | SDK 默认 | 须 HTTPS 或环回（`localhost` / `127.0.0.1` / `::1`） |
| `AppId` | ✅ | — | 飞书 AppId（`cli_`/`app_` 开头且 ≥20 字符） |
| `AppSecret` | ✅ | — | 飞书 AppSecret（≥16 字符） |
| `AppKey` | 否 | `demo-app` | 应用键（不得含 `:`） |
| `UserId` | 否 | `ou_console_demo_user` | 会话键 subject 段（**不是**真实 open_id） |
| `WikiSpaceId` | 否 | — | 剧本 S1 的知识库空间 ID（留空则引导模型自行列空间） |
| `Policy` | 否 | `strict` | `strict` / `ask` / `readonly` |
| `SummaryThreshold` | 否 | `30` | 摘要触发条数阈值（调低可加速演示摘要） |
| `AuditExportPath` | 否 | `%TEMP%/mud-feishu-docagent-audit-{时间戳}.jsonl` | `/export` 默认落盘路径 |
| `AttachmentMaxMb` | 否 | `25` | `drive.upload_file` 的附件大小上限 |

### 3.3 配置文件示例

`appsettings.local.json`（`appsettings.json` 是同一份模板，键名与结构完全一致）：

```jsonc
{
  "FeishuDocAgent": {
    "Enabled": true,
    "ModelId": "glm-4-flash",
    "ApiKey": "sk-****",                                   // 真实密钥只写本文件
    "Endpoint": "https://open.bigmodel.cn/api/paas/v4/",   // 可留空 = SDK 默认端点
    "AppId": "cli_****",
    "AppSecret": "****",
    "AppKey": "demo-app",
    "UserId": "ou_console_demo_user",
    "WikiSpaceId": "wikcn****",
    "Policy": "strict",
    "SummaryThreshold": 30,
    "AttachmentMaxMb": 25
  }
}
```

配置 JSON **允许 `//` 注释与尾逗号**（`Microsoft.Extensions.Configuration.Json` 的解析器口径），
故模板里可以写就地说明。未知键会被忽略——这正是随仓库提交的模板要有守卫的原因（见 §11）。

### 3.4 与 SDK 标准写法（`FeishuApps`）的关系

`AddFeishuApp` 消费的是 `FeishuApps` 数组，本 Demo 两种写法都支持：

```jsonc
// 写法一（推荐，本节）：由 AppId/AppSecret/AppKey 合成单应用
"FeishuDocAgent": { "AppKey": "demo-app", "AppId": "cli_xxx", "AppSecret": "xxx" }

// 写法二（SDK 标准）：显式声明应用数组；本 Demo 取 IsDefault=true 的那个（无标记则取第 0 个）
"FeishuApps": [ { "AppKey": "demo-app", "AppId": "cli_xxx", "AppSecret": "xxx", "IsDefault": true } ]
```

两种同时存在时**以 `FeishuApps` 为准**（它是真正生效的租户配置）；此时 `AppKey` 也取自它，
以保证"工具执行上下文的 appKey"与"默认应用"一致。

### 3.5 密钥纪律

- 真实密钥写 `appsettings.local.json`（已被 `.gitignore` 忽略，`git status` 不会出现它）；**不要写进 `appsettings.json`**。
- 密钥不写日志、不进审计载荷；横幅与 `/help` 一律掩码（`cli_***`）。

---

## 4. 工具白名单（27 枚）

**只读（14）**：`docx.get_raw_content`、`docx.get_document_blocks`、`docx.import_markdown`、`wiki.get_node`、
`wiki.list_nodes`、`search.doc_wiki`、`drive.list_folder_files`、`drive.get_file_metas`、`sheets.list_sheets`、
`sheets.get_range_values`、`bitable.list_tables`、`bitable.query_records`、`feishu.capability_lookup`、
`feishu.guidance_read`

**写类（13）**：`docx.create_document`、`docx.append_blocks`、`docx.update_blocks`、`docx.delete_blocks`、
`docx.replace_document`、`wiki.create_node`、`wiki.move_node`、`drive.create_folder`、`drive.move_file`、
`drive.upload_file`、`sheets.update_range`、`sheets.append_rows`、`bitable.add_record`

**两个容易误解的点**：

1. `docx.import_markdown` 名字里有「import」，但 `is_write=false`（只做 Markdown→块结构**转换预览**，**不落文档**），
   因此**免审批**。真正的落文档动作必须显式 `docx.create_document` + `docx.append_blocks`（剧本 S3 专门演示）。
2. 文档业务域**全部是 `tenant` 身份**，故 `AllowedIdentities` 保持默认 `["tenant"]` 即正确。
   对比 Phase 1/2 工具冒烟（`FeishuToolsDemo`）那个模式必须写 `["tenant","user"]`（那边含 `task.list_my_tasks`）——
   **身份闭集应按域事实推导，而不是复制粘贴**。

**已知能力缺口**（已写进指令，避免模型无效尝试）：表格/单元格/引用容器不支持写入；
docx 无单块删除接口（只能区间批删）；docx 无重命名/移动接口（走 `drive.move_file`）；
tenant 身份无 wiki 关键词搜索（改用 `search.doc_wiki`）；工具结果默认 4000 字符截断；全链路无自动重试。

---

## 5. 三道安全闸

```
模型决策
   │
闸 1  框架审批管线（SDK 内建，不可绕过）
      写工具被 ApprovalRequiredAIFunction 包装；MAF 在调用**之前**改写为审批请求
      ⇒ 模型永远拿不到"已批准"状态，无法自批
   │ 宿主批准（人）
闸 2  宿主授权器（本 Demo：ConsoleToolAuthorizer）
      appKey 校验 → 策略轴（readonly / strict 高风险）→ 权限面白名单
      未注册授权器 + EnforceToolAuthorization=true ⇒ 写工具 fail-closed（authorization_denied）
   │
闸 3  执行审计（本 Demo：InMemoryAuditSink）
      允许 / 拒绝 / 错误三类都投递；ArgsDigest 由 SDK 脱敏（本 Demo 不接触原始参数）
   │
租户上下文切换 → 飞书 API
```

**必须理解的一句话：批准 ≠ 放行。** 框架审批通过后，续跑轮里工具才真正进入执行链，此时仍会被授权器咨询。
默认 `strict` 策略下 `docx.delete_blocks`（高风险写）会被授权器**独立再拒一次**——这正是
`/policy strict → /approve → 仍被拒 → /policy ask → /approve → 真删` 这条演示链路的价值。

---

## 6. 六部剧本（`/scenario <名或别名>`）

| 别名 | 名称 | 类型 | 审批次数 | 一句话 |
| --- | --- | --- | --- | --- |
| `kb` | `kb-digest` | 只读 | 0 | wiki 解析 → 读正文 → 带来源摘要 |
| `author` | `doc-authoring` | 写 | 2 | 建文档 → 追加块 → 回读校验 |
| `md` | `md-import` | 读+写 | 2 | Markdown 转换（免审批，不落文档）→ 落文档 |
| `sheet` | `sheet-sync` | 读+写 | 1 | 区域读 → 追加行 → 回读尾行 |
| `del` | `safe-delete` | 写 | 1 | dry_run → 审批 → strict 拦截 → `/policy ask` 放行 |
| `heal` | `self-heal` | 只读 | 0 | 错误 token → 错误分类 → 自愈 → 记忆 |

* **只读剧本**（`kb` / `heal`）会自动继续第二轮（剧本应答栈），无需人工干预。
* **写剧本**（`author` / `md` / `sheet` / `del`）**永不自动批准**——出现 🔒 卡片后请手动
  `/approve demo-approval-00X` 或用 `/deny` 拒绝。
* `sheet` / `del` 的引导语含 `{sheet_token}` / `{document_id}` 字面占位符（**刻意不新增配置项**）：
  引导语要求模型"没拿到就问我"，你把真实 token 贴进会话即可。
* `/scenario list` 列清单，`/scenario clear` 清空应答栈。

---

## 7. 斜杠命令

```
/help /?      帮助 + 当前关键配置      /tools /t      分组列出已启用工具
/guidance /g  打印某域 guidance 全文   /pending /p    待人工确认的写操作
/approve /y   批准并续跑（真执行）      /deny /n       拒绝并以中性结果续跑
/abandon      放弃全部挂起项并重置会话  /policy        查看/切换授权策略
/dryrun       dry-run 引导开关         /audit         审计表（最近 20 条）
/export /e    导出全量审计 JSONL        /usage /u      累计 token 与工具调用计数
/history /h   轮次与最近 5 轮摘要       /reset /r      重置会话（有挂起项时被拒）
/scenario /s  剧本（list / clear / 名）  /verbose /v    工具结果全文 + Trace
/exit /quit   退出（有挂起项时需二次确认）
```

**人机边界**：以 `/` 开头且首 token 命中上表才被拦截；`/` 开头但未命中（如 `/mydoc`）**照常发给模型**。

**挂起态守卫（重要）**：出现 🔒 待确认时，输入新问题会被**阻断**，并提示三条出口
（`/approve` / `/deny` / `/abandon`）。原因不是"体验设计"，而是硬事实：发起确认的那一轮会把
「待应答审批请求」写进会话历史，残留未应答的请求会让该会话**后续每一轮直接抛
`InvalidOperationException`**（工具未放行，但会话不可用）。SDK 在**事件层**做了自愈
（新的用户轮次会放弃该待确认项），**控制台没有事件层**，故本 Demo 必须自己收口。

---

## 8. 飞书应用需开通的权限点

| 域 | 只读 | 写 |
| --- | --- | --- |
| docx | `docx:document:readonly` | `docx:document` |
| wiki | `wiki:wiki:readonly` | `wiki:wiki` |
| drive | `drive:drive:readonly` | `drive:drive` |
| sheets | `sheets:spreadsheet:readonly` | `sheets:spreadsheet` |
| bitable | `bitable:app:readonly` | `bitable:app` |
| search | `search:docs:readonly` | — |
| 元工具 | `feishu:base`（`feishu.guidance_read`；`feishu.capability_lookup` 无需权限点） | — |

> 不需要 `im:message`（不做飞书镜像）、不需要 `aily:knowledge:readonly`（不做 Aily 托管知识）。
> 缺任一权限点时对应工具会在运行期返回 `authorization_denied`（**不是**代码缺陷），错误文案会引导模型把 code 交给用户。

---

## 9. 常见排错

| 现象 | 原因 | 处置 |
| --- | --- | --- |
| 启动即报 `请先设置 FeishuDocAgent:XXX` | 配置文件中缺少该必填项 | 对照 §3.2 补齐 |
| 改了 `appsettings.local.json` 但不生效 | ① 本 Demo **不监听文件变更**（`reloadOnChange: false`），必须重启；② 文件是新加的、还没被复制到输出目录 → 重新 `dotnet run` / `dotnet build`；③ 键名拼错（对照 §3.2） | 看横幅"配置来源"行确认文件是否被加载；再核对键名 |
| 配置文件里 `FeishuDocAgent:Enabled=true` 却没进本模式 | 更早优先级的模式开关也被置 `true`（四模式互斥，按 §1 顺序判定：工具冒烟 → IM 事件接入 → 文档智能体） | 检查并关闭其它模式的 `Enabled`，或只保留目标模式 |
| 横幅"配置来源"只显示 `（未发现 appsettings*.json）` | 两个 `appsettings*.json` 都不在**输出目录** | 确认 `Demos/Mud.Feishu.Agent.Demo/bin/<配置>/net10.0/` 下有 `appsettings.json`（csproj 的 `None Update + CopyToOutputDirectory` 负责复制） |
| `AppId 长度无效` / `AppId 格式无效` / `AppSecret 长度必须至少为 16 字符` | SDK 的租户配置校验（在 Demo 的 `Validate()` **之后**） | AppId 须以 `cli_`/`app_` 开头且 ≥20 字符；AppSecret ≥16 字符 |
| `工具白名单与当前注册的域客户端不匹配：… 未注册工具 'x.y'` | 对应域客户端没注册（域缺席 ⇒ 该域工具不注册） | 检查 `DocAgentDemo` 的 `AddFeishuServices` 是否注册了该域 |
| 写工具报 `authorization_denied` | ① 未注册授权器（本 Demo 已注册）② `strict` 策略独立拒绝高风险写 | 看卡片上的 `reason`：`strict` 场景用 `/policy ask` 放开 |
| 模型说「已发起确认，等你批准」 | 框架审批挂起（闸 1 已拦截，工具**未执行**） | `/pending` 查看 → `/approve <关联号>` 放行；或 `/deny`；或 `/abandon` |
| 输入 `/approve` 后仍失败 | 挂起项超过 10 分钟有效期（框架侧已失配） | `/abandon` 重置会话后重新发起该操作 |
| 工具结果被截断（`原文 N 字符`） | `MaxToolResultLength = 4000` | 分批读取（按块区间/区域读），或调整该配置 |
| `guidance` 被丢弃（横幅黄色告警） | 域 guidance 总量超过 8192 字符上限 | 减少同时启用的域（本 Demo 7 域 ≈1845 字符，不会触发） |
| 启动很慢（一次）| `Microsoft.ML.Tokenizers` 首次初始化 gpt-4o 词表需网络 | 完成后会缓存；离线环境会回退字符估算（仅影响摘要触发精度） |

---

## 10. 可观测性

Demo **不接 OTel Exporter**（零额外依赖），采用「控制台 Trace + 本地指标快照」，Trace 内容对齐 SDK 的埋点语义：

| 控制台 Trace | 对应 SDK 埋点 |
| --- | --- |
| `· ▶ run_streaming agent=…`（由 `feishu.agent.run_streaming` Span 承载） | `feishu.agent.name` |
| `· 授权器放行/拒绝 <工具>（policy=…）` | `feishu.tool.decision` + `feishu.tool.risk` |
| `⚙ <工具>  risk=…  ✓ allowed  412ms`（工具卡片） | Span `feishu.agent.tool` + Histogram `feishu.tool.duration` |
| `· 审计 #N <工具> → allowed（412ms, write=True）` | Counter `feishu.tool.executions`（维度 tool/app_key/outcome） |
| `· usage in=1832 out=417` | `feishu.llm.input_tokens/output_tokens` |

**高基数纪律**：`conversation_key` / `chat_id` / `user_id` 只进 Span 属性与审计载荷，**绝不进 Metrics tag**；
审计表里只显示会话键的**后 12 位**。

---

## 11. 自检点（跑通即符合验收）

| 编号 | 自检 |
| --- | --- |
| F1 | `FeishuDocAgent:Enabled=true` 启动后 30 秒内打印能力面横幅（配置来源 / 工具数 / 身份闭集 / 风险上限 / 授权强制 / guidance / 通道） |
| F2 | 四个模式均按配置节开关与参数运行，**无环境变量参与**（参数、模式开关全部来自 `appsettings*.json`） |
| F3 | `/scenario kb` 无人工干预跑通，回答含来源标注 |
| F4 | `/scenario author` 端到端跑通，且出现**恰好 2 次** 🔒 审批 |
| F5 | `/scenario del`：`dry_run` 零副作用 → 🔒 → `strict` 下授权器独立拒绝 → `/policy ask` 后真删 |
| F6 | `/approve` 后写操作真实生效，`/audit` 出现 `allowed` 记录 |
| F7 | `/deny` 后写操作未生效，模型解释拒绝原因 |
| F8 | 挂起态下输入新问题被阻断并提示三条出口 |
| F11 | `/tools` 的工具数与横幅一致；`/guidance docx` 能打印域引导 |
| F12 | `/audit`、`/export` 输出结构完整、参数已脱敏 |
| F13 | **纯配置文件启动**：只写 `appsettings.local.json`（含 `Enabled=true`）即可进入本模式，横幅"配置来源"列出真实加载的文件 |
| F14 | **优先级正确**：同一键在 `appsettings.local.json` 与 `appsettings.json` 同时存在时本地覆盖文件生效（横幅可见，如 `Policy`） |

**自动化验证**：`Tests/Mud.Feishu.Agent.Demo.Tests` 覆盖本 Demo 的可测逻辑（配置来源与优先级、配置 fail-fast、
授权器策略、审批通道非阻塞、审计 sink、渲染器 ANSI 过滤、剧本解析）、**随仓库提交的 `appsettings.json`
模板守卫**（可解析 / 键名不漂移 / 密钥留空 / 默认值自洽），以及 9 条契约守卫（白名单 ⊆ 工具面、写工具审批分类、
权限面覆盖、tenant 身份、命令集、剧本工具链、依赖面、宿主钩子不得依赖工具注册表）。

```powershell
dotnet test Tests/Mud.Feishu.Agent.Demo.Tests
```

---

## 12. 本项目不做什么（已决策）

| 不做 | 理由 |
| --- | --- |
| 飞书镜像（同一条回答同时输出到控制台与飞书群） | 会引入 `IMessageChannel` 装饰器与注册顺序坑；`StreamChatId` 配置项、`AddFeishuStreamingChannel()` 全部移除 |
| OTel Console Exporter | 需额外 `ProjectReference` 与配置，会掩盖主目标；SDK 的 Span/指标仍在发，只是无 Exporter 接收 |
| Aily 托管知识（`AddFeishuAilyKnowledge` + `knowledge.search`） | 需额外域客户端与配置；白名单不含 `knowledge.search` |
| 用户身份（`identity=user`）工具 | 文档业务域全部是 `tenant`；用户身份需 OAuth2 换 token |
| Web 宿主 / DB / Redis | 控制台进程内闭环；`MemoryConversationStore` 足够 |
| 自动重试（Polly 等） | 与 SDK 口径一致：重试语义由模型按 `ToolErrorKind` 自行决定（剧本 S6 演示自愈） |