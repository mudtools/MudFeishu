# Mud.Feishu 更新日志

本文件记录 Mud.Feishu 各版本的显著变更。格式遵循 [Keep a Changelog]，版本号遵循 [语义化版本]。

[Keep a Changelog]: https://keepachangelog.com/zh-CN/1.1.0/
[语义化版本]: https://semver.org/lang/zh-CN/

## [Unreleased]

> 主题：**适配 `Mud.HttpUtils` 3.0.9**（在 3.1.0 已随 3.0.8 完成 `BC-30` 三成员移除的基础上，
> 回迁官方令牌恢复入口并更正过时论断）。
> 本版为**破坏性依赖变更**：`Mud.HttpUtils` / `Mud.HttpUtils.Generator` 由 `3.0.8` 升至 `3.0.9`
> （3.0.9 批次以「删自建件 + 更正论断」为主，无新增破坏性变更）。
> 同时并入 v3.1 线批次：Webhook 语义修复、AI 流式与工作流工具、工具面收口（351 工具 / 34 域）、
> 多应用管理加固与 `Mud.AI.*` 1.0.2（详见下文各节）。
> **上线前提（已满足）**：上游 `3.0.9` **已发布至 nuget.org**；`nuget.config` 中的本地源（指向
> Release 产物 `artifacts`）**已移除**，引用改由公共源还原并复跑全门禁。

### 依赖升级

- `Mud.HttpUtils` / `Mud.HttpUtils.Generator`：`3.0.8` → **`3.0.9`**（两者必须同版本）。
  7 个源工程的 10 处 `Mud.HttpUtils*` 声明统一，各 README 依赖表与 `AGENTS.md` 同步；
  `Mud.Feishu.AI*` 与示例工程的 `Mud.HttpUtils.Attributes` / `Mud.HttpUtils.Abstractions`
  引用同批升级。
- 验证期经本地 Release 包渠道还原（`mudhttputils-local` → `D:/Repos/MudHttpUtils/artifacts`，
  由 `pack.ps1 Release` 产出并逐字节校验）；**禁止**使用 `artifacts-debug` 的 Debug 包。
  上游 3.0.9 发布至 nuget.org 后本地源**已移除**，并按「清缓存 → `restore --no-cache` → `clean`
  → `build`」纪律重新还原自公共源（0 错误、190 warning 与升级前基线逐码一致），全门禁复跑通过。
- `Mud.AI.Abstractions` / `Mud.AI.Agent` / `Mud.AI.Generator` 由 1.0.1 升至 **1.0.2**
  （中立工具遥测端口 `IAgentToolTelemetry`、`AmbientScope<T>` 运行期装配扩展、`TurnScopeContext.Facts`、
  `DelegatingToolFunction` 工具函数装饰基座、会话存储门面与回退错误截断修复）。1.0.2 已由上游发布至
  nuget.org，联调用的本地 folder 源 `mudai-local` 已从 `nuget.config` 删除（还原后重新 restore 验证通过）。

### 变更（Changed）

- **令牌恢复配置改走组件官方入口**：注册链中的「DTO 投影 + `Apply` + 手工
  `ConfigurationChangeTokenSource` + 校验器 + 可解析实例」共 38 行自建块，整块替换为
  `AddMudHttpTokenRecoveryFromConfiguration(configuration)`。绑定语义（DTO 投影、节缺失保留默认值、
  非法值在选项解析期抛错）与热更新语义（配置重载 ⇒ `IOptionsMonitor` 缓存失效）逐项等价，
  已由既有等价性 / 热更新用例覆盖；无 `IConfiguration` 的代码式注册路径保留选项管线、
  校验器与可解析实例（不含节绑定）。
- **JSON 上下文接入改为合并入口**：`AddMudHttpClientJsonContext(FeishuApiResultJsonContext.Default)`
  改为 `AddMudHttpClientJsonContexts(FeishuJsonContext.Default, FeishuApiResultJsonContext.Default)`
  （net8+）。DI 管道的解析器链同时覆盖事件/Webhook 类型与响应包装/认证 DTO；非 DI 路径依赖的
  `FeishuJsonDefaults` 静态合并链**保持不变**。
- **注释与用例更正**：删除「`TokenRecoveryEnhancedClient` 未重写 `WithBaseAddress`、强转抛
  `InvalidCastException`」的过时论断（组件 3.0.9 起该方法返回同类型实例），热更新注释改为
  明确「每 `CreateClient` 重跑配置委托」而非「克隆实例」；`PurgeGateTokenStoreDecorator` 的读降级
  论断改为「组件已降级，装饰器位于 store 层仍保留降级作为纵深防御」（行为不变）。
- **新增回归用例**：`TokenRecoveryClientCapabilityTests`（`WithBaseAddress` 返回同类型 + 基地址生效）、
  `AddFeishuApp_ShouldRegisterFeishuDomains_SoNoBlankAllowlistWarning`（注册入口已登记飞书域名，
  严格模式客户端不产生「空白名单」启动期提示）。

### 修复（Fixed）

- `scripts/verify-build.ps1` 步骤 0（CACHE-1）的本地源比对改为「**仅当 `nuget.config` 显式声明
  本地文件夹源时**执行」，未声明时输出 `[SKIP]`：组件发布到公共源后本地源已移除，
  此时缓存来自公共源，继续比对本机打包目录会把「公共源包与本地同版本重打包不一致」误报为下游缓存陈旧。
  本地联调周期（声明了该源）的比对语义不变。
- **更正旧文档的一处错误描述**：`GetWebApi<T>(appKey)` 曾被称为「返回独立实例」，实际与
  `UseAppScope` / 旧的 `UseApp()` 共用同一个基于 `AsyncLocal` 的 `IAppContextHolder`，
  **不存在「独立实例」语义**；正确做法是先切换、再从 DI 解析。
- 决策档案登记执行状态：`弃用成员移除方案.md`（§2 档 A 门禁、§5 风险 7 已消解）、
  `迁移与弃用史.md`、`编号索引.md`、`上游契约适配史.md`、`注释精简归档.md`。

### 修复（Fixed）— Mud.Feishu.Webhook

- **验签"无法判定"不再伪装成"验签失败"（P0）**：`IEncryptKeyProvider` 故障（KeyVault 抖动返回空）、验签链内部异常、
  热更后配置构建失败，现在抛出 `FeishuWebhookUnavailableException` 并映射 **HTTP 503 + `Retry-After`**
  （请飞书重推）。此前这些情况返回 `false` → 403 终态，事件永久丢失且不入失败存储。
  - 语义契约：`HandleEventAsync(request, body)` 的 `false` **仅**表示"签名已被判定为无效"。
  - 新增 `FeishuWebhookUnavailableException`；中间件 4 处 503 写点收敛为 `WriteServiceUnavailable`（统一带 `Retry-After`）。
- **配置热更不再可能导致进程终止**：所有 `IOptionsMonitor.OnChange` 回调统一经
  `SafeOptionsChangeExtensions.OnChangeSafe` 注册（回调异常只记日志、绝不上抛到配置重载线程），
  且回调内不再读取 `CurrentValue`（改为与构造期播种的快照比较）。
  限流中间件的 `System.Threading.Timer` 被删除，窗口清理改为请求路径上的机会式清扫
  （无定时器 ⇒ 无回调异常面、无 `IDisposable`、无孤儿资源）。
- **请求体读取预算化**：移除 `EnableBuffering()`（不再对 >30KB 报文落临时文件），改为 64KB 块读取 + 按
  `ContentLength` 预分配 + 贯穿 `RequestAborted`，并去掉一次全量复制。
  **`MaxRequestBodySize` 默认值由 10MB 调整为 1MB**（超限 413；确有超大卡片事件请显式调大）。
- **`UseFeishuWebhook()` 在 `Apps` 为空时启动失败**：此前表现为"启动成功、端点静默 404、飞书持续重推"。
  错误消息给出两条修复路径；仅使用 `IFeishuWebhookService` 手动驱动事件的宿主不受影响（不挂中间件即可）。
- **`GlobalRoutePrefix` 归一化**：`"/feishu"`、`"feishu/"`、`" /feishu/ "` 等写法不再导致端点静默失效
  （归一化在 `PostConfigure` 完成，所有消费方读到生效值）。
- **实例化处理器/拦截器改为单例注册**：`AddHandler(instance)` / `AddInterceptor(instance)` 此前以 Scoped 注册
  共享实例，DI 会在**首个请求 scope 结束时释放**它；现改为 Singleton（容器关停时释放），
  XML 明确"实例必须线程安全；需按请求隔离状态请用 `AddHandler<T>()`"。
- **删除中间件的容器注册**：`UseMiddleware` 恒自建管道实例，容器注册会产生第二个"幻影实例"
  （重复的配置变更订阅 + 孤儿定时器）。
- **解密阶段取密钥贯穿 `CancellationToken`**：此前 `DecryptEventAsync` 未透传 CT，
  导致 `DecryptionTimeoutMs` 无法中断一次挂起的密钥获取调用。
- **`InMemoryFailedEventStore.GetPendingRetryEventsAsync` 尊重 `beforeTime` 入参**（此前恒用当前时刻）。
- **请求体预分配封顶，消除超大 `Content-Length` 的内存放大与整数溢出**：此前按声明的
  `Content-Length` 直接 `(int)` 预分配（`MaxRequestBodySize` 为 `long` 且无上限）——
  >2GB 声明会让 `(int)` 溢出为负并触发 500，且未认证请求只需写一个大 `Content-Length`
  即可让服务端立即分配同等内存。现生效预算封顶 `min(MaxRequestBodySize, int.MaxValue)`
  （产物是 `string`，长度受 `int` 约束），预分配容量 = `min(声明长度, 生效预算, 1MB)`，
  超出部分由 `MemoryStream` 按需增长，字节预算仍逐块校验。
- **失败事件重试不再刷新 `FailedAt`**：此前每轮重试都把它改成当前时刻，使
  `InMemoryFailedEventStore.GetOldestPendingAge()`（→ 健康检查 `oldest_pending_retry_age_seconds`，
  >5 分钟判 `Degraded`）对"处理器持续失败"恒定看不出问题。现 `FailedAt` 保留**首次失败时刻**，
  退避进度仅由 `NextRetryAt` 表达。⚠️ 运维可见变化：长期失败的事件现在会真的让健康检查转为 `Degraded`。
- **健康检查严重度不再倒挂**：此前"并发利用率 ≥80% → `Degraded`"排在"失败事件积压 ≥500 → `Unhealthy`"之前，
  "利用率高 + 积压爆表"会被报成较轻的 `Degraded`。现统一按 **`Unhealthy` > `Degraded`** 判定
  （槽位耗尽 → 积压硬阈值 → 利用率 → 最旧待重试年龄 → 积压软阈值），所有数据项始终完整回写。

### 变更（Changed）— Mud.Feishu.Webhook

- 解密热路径：去掉 `Task.Run`（AES 为微秒级，切换开销大于计算），派生密钥按 `EncryptKey` 缓存。
- `SecurityAuditService` 的成功审计日志由 `Information` 降为 `Debug`（失败仍为 `Warning`）；
  中间件的"收到请求/请求处理完成"降为 `Debug`，成功事件日志由 4 条降为 2 条。
- 启动自检日志（`WebhookOptionsStartupValidator`）增补生效参数：`MaxRequestBodySize`、`DecryptionTimeoutMs`、
  限流的启用状态与窗口/上限。

### 移除（Removed）— Mud.Feishu.Webhook

- 删除 4 个**无运行时抛出点**的公共异常类型（假公共面）：`FeishuWebhookDecryptionException`、
  `FeishuWebhookProcessingException`、`FeishuWebhookSecurityException`、`FeishuWebhookSerializationException`。
  保留 `FeishuWebhookException`（基类）、`FeishuWebhookValidationException`（413）、
  `FeishuWebhookUnavailableException`（503）——每个类型都有真实抛出点与明确状态码映射。

### 文档（Docs）

- `documents/ErrorHandling.md` 新增「Webhook ACK 决策表」（与 `WebhookAckSemanticsContractGuards` 同源）
  与「Webhook 部署前置条件」（跨实例需 Redis 去重、内存失败队列重启即丢、明文事件一律拒绝、
  Nonce 消费顺序、请求体上限、路由前缀写法）。
- `Mud.Feishu.Webhook/README.md` / `README_EN.md`：修正 `MaxRequestBodySize` 默认值，补充
  `GlobalRoutePrefix` 写法、限流与重试的语义说明。
- `documents/index.md` 新增「Webhook 排障速查」：503 = 密钥服务不可用（**不是**签名不匹配）、
  403 / 404 / 413 的判定与处置。

### 新增（Added）

- **`Mud.Feishu.AI` —— 会话内流式回复（CardKit 卡片实体 + 会话气泡）**：
  `CardKitStreamMessageChannel` 成为 `AddFeishuStreamingChannel()` 装配的降级链首选
  （CardKit → `EditMessageChannel`）。协议三步「建实体（`streaming_mode`）→ 投气泡 → 推全量文本」，
  平台边界与失败语义见 `documents/AIAgent/decisions/ADR-0013-cardkit-streaming-boundaries.md`。
  - **目标语义由通道自解释**（实现 `IChannelTargetResolver`）：会话事件取 `ChatId`（`receive_id_type=chat_id`）；
    业务事件（审批 / 表格记录变更，单聊）取操作人 `open_id`（`receive_id_type=open_id`）——
    **p2p 事件因此重新具备流式能力**，且不新增任何平台 API 调用与权限点。
  - 终态失败**可观测**：`Error` 日志 + 降级原因 `channel_terminal_failed`（与「单次增量失败」
    `channel_update_failed` 分级）；投放失败产生的残留卡片实体单独计数 `channel_begin_orphaned`。
  - 同文去重（终态文本与上次成功推送相同时不再多打一次 PUT）与终态后迟到写入丢弃（不重放、不重建状态）。
- **`Mud.Feishu.AI` —— 会话键政策外移**：新增 internal `ConversationKeyPolicy` 作为「会话键怎么拼」的单一真相
  （业务事件注入 `event-{EventKey}` 前缀段，聊天会话沿用默认前缀；五段键位与 `TryParse` 回灌不变）。
- **Gate 补强**：`CardKitRouteContractGuards` 反射锁定 cardkit 建实体 / 流式更新两条路由模板，
  防止「AI 通道打错端点」再次只能在线上被发现。

- **Mud.Feishu.AI.Tools —— 复合工作流（S-4，+2 个只读工具；域数不变）**
  - `workflow.meeting_summary`（`minutes.get` → `minutes.get_statistics`（可关）→ `minutes.get_artifacts`）
    与 `workflow.standup_report`（`calendar.agenda` → `task.list_my_tasks`）：把"取哪几段、怎么拼"固化在
    代码里，模型只给起点参数；返回步骤轨迹（每步工具/状态/耗时/错误）+ 聚合报告。
  - **失败即停 + 断点续跑**：中途失败返回失败结果与 `resume_from`（步骤号），以 `start_at` 重调即从
    失败步续跑，已成功的前序步骤标 `skipped` 不重复取数；`dry_run=true` 只列执行计划不取数。
  - **白名单不得旁路**：步骤工具经注册表解析且必须落在启用集内，未启用即在该步失败（不静默跳过）。
  - 只读链优先：首批两条链全只读；写工作流需"半途失败补偿"语义（平台无事务/补偿原语），登记为后续项。
  - 新公开入口 AddFeishuWorkflowTools()（与全域入口 AddFeishuReadonlyToolCores 同进同出）。
  - 同批：Guidance/workflow.md（L1）、skills/feishu-workflow 产物、权限对照表 +2 行（v1.15）、
    golden 重固化 2 行、FeishuWorkflowRunnerTests（6 例：顺序/失败续跑/跳过/预演/白名单边界/定义契约）。

### 变更（Changed）

- **`Mud.Feishu.AI` —— 业务事件会话键带事件命名空间（一次性，不迁移历史）**：业务事件
  （审批任务 / 表格记录变更 / 任务更新）的会话键前缀段为 `event-{EventKey}`，与同一用户的聊天会话
  彻底隔离（防双向上下文污染）。键位仍是五段、`TryParse` 回灌不变。**本库尚未发布，故不提供迁移**：
  升级到含本变更的版本后，业务事件会话从空历史开始（聊天会话不受影响）。`object_key`
  （`instance_code` / `file_token+table_id`）**不进键位**，只经 `EventFacts` 注入 prompt。
- **Mud.Feishu.AI.Tools —— 工具面重复度收口（365 → 351 个工具 / 34 域）**：合并 14 枚能力重叠的工具——
  `mail.send_message` + `mail.send_draft` → `mail.send`；`mail.rule_create` + `mail.rule_update` → `mail.rule_save`；
  `mail.template_create` + `mail.template_update` → `mail.template_save`；`drive.grant_permission` +
  `drive.update_permission_member` → `drive.set_permission`；`bitable.add_record` 并入
  `bitable.record_batch_create`（入参 `fields` → `records`）；`bitable.get_records_by_ids` 并入
  `bitable.query_records`（`record_ids` 按 ID 精取）；另有 `vc.list_reserves` → `vc.get_reserve`。
  读写分布 185 只读 / 166 写类（其中高危 39），公开面 458 → 449，affordance 221 → 207 份（100204 字符），
  Source 声明 358 → 344。描述面同批做兄弟对首句互指（bitable 工作流、docx 三粒度读、contact 解析↔详情、
  board 读↔建、application 可见范围读↔写）与 174 处写类权限模板句去重（授权约束由执行器强制，
  不依赖模型读描述；描述总量 54,891 → 47,495 字符）；golden 重固化、skills 重导出、权限对照表 v1.19（351 行）。
- **工具面规模数字校正**：README.md / README_EN.md / Mud.Feishu.AI.Tools/Readme.md /
  .docs/AI/状态基线.md 中漂移的 "344 工具 / 176 只读 / 31 域" 等数字按真相源
  FeishuToolSchemas.golden.txt 校正为 **351 工具（185 只读 + 166 写类 + 39 高危）/ 34 域**。
- **`Mud.Feishu.AI.Mcp` —— 装配诊断**：空工具面在**构造期告警一次**并点名成因
  （`registry_absent` / `factory_absent` / `whitelist_empty`）与可修配置节；非空时输出一条
  `MCP 工具面就绪：tools=… domains=… readOnly=… write=… resources=…` 读数（与
  `initialize.capabilities._meta` 同口径）。**行为契约不变**：`tools/list` 返回空仍是合法状态，
  本包不新增第二条启用通道。
- **`Mud.Feishu.AI.Mcp` —— 版本闭集补"加版唯一判据"**：新修订进入 `SupportedVersions` 的唯一条件是
  `initialize` / `tools/list` / `tools/call` / `resources/*` 四项形态无变化（任一项变化须先补适配，
  否则"声明的版本"与"实际语法"脱钩、失败不可观测）。`tools/call` 结果形态与进度通道的边界
  （不产出 `structuredContent`、不实现 `notifications/progress`）登记为
  `documents/AIAgent/decisions/ADR-0012-mcp-output-and-progress-scope.md`。
- **`Mud.Feishu.AI.Mcp` —— 注释去规模数字**：`FeishuMcpServerOptions` 注释不再写死工具数
  （规模以 `tools/list` 实测为准）；Runbook §4.1/§5 补 stdio 日志通道的正确形态与现行 Demo 入口，
  §6 增结果形态决策索引。

### 修复（Fixed）

- **`Mud.Feishu.AI` 门禁恢复（P0）**：`Tests/Mud.Feishu.AI.Tests` 曾因泛型基类上的静态成员被无实参调用
  而 6 × `CS0305` 编译失败——该工程的全部守卫（Golden / 审批 / 摘要 / 注释预算 / 诊断契约）本轮**一条都没执行**。
  会话键构造上移为 `ConversationKeyPolicy` 后恢复编译，并同批修掉被遮蔽的三条红：
  `PublicSurfaceRationale`（新通道未登记消费方）、`CommentConvention`（台账 `vanished`/`unfrozen` 双向脱节 +
  新文件 26 行注释块越 20 行硬上限）、以及两条**用例自身的断言错误**
  （`Sequence_ShouldRestartPerCard` 用两次 `Setup` 覆盖同一桩、`Channel_ShouldSwitchTenantScope` 把
  「Begin 一个作用域覆盖建实体+投放」误算成两次）。
- **通道测试成功桩恒走失败分支**：流式更新的桩只写 `Code = 0`、`Data` 为空，而解包器把空 `Data` 判为失败——
  「成功路径」从未被覆盖。桩改为带 `Data`，并补「成功无 Warning」的正向断言。
- **注释预算台账与注释体**：CardKit 通道的长注释块（平台边界五条）外移 ADR-0013，源码只留摘要，
  台账同批重固化（新增通道与 `ConversationKeyPolicy`、删除已下线通道条目）。

多应用管理与事件分发审查后的整批修复。除「连接级应用绑定」在**多应用装配**下属行为变更（未显式配置
`FeishuWebSocket:AppKey` 将启动失败）外，其余各条对单应用部署语义不变。

- **WebSocket —— 连接级应用绑定**：`FeishuWebSocketOptions.AppKey` 由「仅指标维度」升级为**连接绑定键**。
  单应用装配留空即自动解析为该应用的键；多应用装配必须显式配置，未配置或配置了未注册的键在启动期
  `InvalidOperationException` 失败——此前多应用部署下连接静默绑定默认应用，事件以错误身份收发。
  绑定键在客户端构造期解析一次（`FeishuWebSocketClient` / `FeishuWebSocketManager` 各自缓存），
  并贯穿指标、去重、失败事件落盘；配置热更新不会把已绑定连接切回默认应用（换绑需重启或重建客户端）。
- **WebSocket —— 统一去重中间件的应用维度贯通**：新增 `IAppScopedUnifiedDeduplicationMiddleware`
  （`UnifiedDeduplicationMiddleware` 实现），把 `appKey` 透传到事件去重器；旧接口方法等价于
  `appKey = null`，二进制兼容。此前 WebSocket 事件经统一中间件去重时丢失应用维度，多应用部署下相同
  EventId 会跨应用互相判重。`FeishuEventMessageHandler` 的检查 / 完成 / 回滚三处调用同批改为传实际应用键。
- **Webhook —— 专属处理器与全局处理器的分发策略**：新增 `FeishuWebhook:HandlerDispatchMode`
  （`AppScopedOnly` 默认 / `MergeWithGlobal`）。默认语义与此前一致（有专属处理器即不再走全局），
  `MergeWithGlobal` 让全局处理器同时参与；未注册专属处理器时始终回退全局。合并模式下一个事件已被
  专属处理器处理时，全局未注册不再记 `unhandled`。分发逻辑拆为专属 / 全局两条路径，原「专属处理器
  全部不匹配事件类型」的 Warning 与 unhandled 指标保留；非法枚举值在 `Validate()` 拒绝。
- **Webhook / WebSocket —— 后置拦截器韧性**：`AfterHandleAsync` 改为逐条 try/catch 并统一传
  `CancellationToken.None`。此前单个后置拦截器抛异常会跳过其余拦截器，并把成功的业务处理改判为失败
  （Webhook 侧还会顶替原始处理异常）；外部取消时传入的已取消令牌会让收尾逻辑被立即短路。
  拦截器失败现在只记 Error 日志（拦截器类型 / 事件类型 / EventId），不影响原始处理结果。
- **多应用管理 —— `RemoveApp` 生命周期收口**：注册表摘除、配置快照同步、墓碑写入、世代递增与
  清库门打门在 `_registryLock` 内原子完成，清库在锁外异步执行并在 `finally` 撤销租约。三处修复：
  ①**墓碑**——显式移除的键不再被「先于移除开始、晚于移除提交」的热更新静默复活（配置源不再声明该键
  或显式 `AddApp` 才撤销）；②**ABA**——移除后立即重新添加时，迟到的清库以世代校验跳过，不会删除新
  上下文刚写入的令牌；③**快照同步**——此前 `RemoveApp` 不同步配置快照，后续热更新会把已移除应用误判
  为仍在线。默认应用提升的结果因此确定：按剔除后快照顺序，兜底 AppKey 序数排序。
- **多应用管理 —— 热更新逐应用失败隔离**：新增 / 移除 / 更新三个提交阶段各自隔离异常，单个应用失败
  不再中断其余应用的变更。提交顺序修正为「`RegisterApp` 成功后再退休旧上下文」——此前先入退休队列后
  注册，注册失败会让旧上下文在宽限期后被 Dispose 却仍被注册表引用。更新失败的键回滚其 `Lazy` 引用交换；
  失败的新增键不入配置快照（否则下次热更新会误判「未变更」而永不重试），失败的移除键保留原快照条目。
  预清库计划随之收敛为只含 `WriteBackRiskKeys`（fire-and-forget 下「预清库超时」分支不可达）。
- **多应用管理 —— 构造期 / 热更新 / `AddApp` 三入口校验统一**：重复 `AppKey`、空 `AppKey`、多个
  `IsDefault = true` 一律硬失败。此前重复键只告警并让后注册者静默覆盖，多个默认应用让默认身份随注册
  顺序漂移、热更新反复切换。
- **用户令牌 —— 访问令牌失效写穿持久化**：新增公开能力接口 `IUserTokenStoreAccessTokenInvalidation`
  （只删 access、保留 refresh），`FeishuUserTokenStore` / `RedisUserTokenStore` 实现，
  `EncryptedUserTokenStore` 条件透传（内层无能力时 no-op，不降级为 `RemoveAsync`——那会连 refresh
  一起删掉）。`UserTokenManager.InvalidateUserTokenAsync` 在清本地镜像后写穿存储，失败只记 Warning
  不抛出（失效仍为尽力而为）；能力探测落在**未装饰的原始 store** 上，门装饰器刻意不声明该能力以避免
  假命中。此前失效只清本地缓存，多实例部署与冷启动会从 store 恢复出本应失效的 access token。
- **临界区自检与作用域说明**：配置热更新的 `_lazyRebuildLock` 提交段加 50ms 持有时间 Debug 自检
  （提示锁内混入了 IO 或重计算）；`TokenStorePurgeGate` 补「只拦读、写/删直达、进程内生效」的作用域
  说明；`TokenKeyBuilder` 注明键中明文 userId 的取舍。新增 ADR-0009（用户标识与存储键隐私）、
  ADR-0010（热更新锁序与临界区预算）、ADR-0011（清库门作用域）。
- **`Mud.Feishu.AI.Mcp` —— HTTP 传输两处语义对齐**：
  - `Accept` 只声明 `text/event-stream` 的请求回 **406**（此前放行后返回 `application/json`，
    等于返回一个客户端明确拒绝过的媒体类型）；规范围 MCP 客户端同时携带两者，**不受影响**。
  - 以**通知形态**到达的 `initialize` 回 **202 空正文**（此前回 `200 {}`——既非合法 JSON-RPC
    响应也无信息量的信封），会话标识仍随 `MCP-Session-Id` 头下发（否则客户端拿不到续约凭据）。
- **`Mud.Feishu.AI.Mcp` —— 线协议序列化配置单一化**：stdio 宿主不再私配一份编码器实例，与协议层 /
  HTTP 传输共用同一份（两处各配一份必然漂移：同一个中文字符串在 stdio 里是原文、在 HTTP 里是 `\uXXXX`）。
- **CI 测试阶段「testhost 握手超时 90s ⇒ 3 个工程整段中止」的假红根治**（2026-10-23 windows-latest）：
  单条 `dotnet test <slnx>` 让 MSBuild 按 4 核同时起 4 个测试宿主，首批 4 个里 3 个
  （Abstractions / AI / AI.Tools）在 90s 内未完成与 `vstest.console` 的握手，各落一个
  `total=0` 的 TRX 后 `Test Run Aborted`（同一批次其余 8 个工程全绿，含后续波次的 4 个并发
  net8.0 宿主 ⇒ 冷启动预算问题，非用例失败）；VSTestTask 返回 false 却不记 MSBuild 错误
  （MSB4181），最终只表现为「Build FAILED / 0 个错误」。修复：`Run tests` 改为**逐工程串行**
  执行（与 `scripts/verify-build.ps1` 步骤 4 的 GATE-4 同口径），每个工程独占结果目录与 TRX
  （不再靠 `[1]/[2]` 后缀猜工程名）；对 `total=0`（宿主启动/握手失败指纹）**单工程自动重试一次**，
  重试仍为空则点名该工程失败，不再以整体退出码一句带过；`VSTEST_CONNECTION_TIMEOUT` 提到 300s
  （workflow `env` 与本地门禁同值）。
- **审批 / 考勤接口与官方文档逐字段对齐**（核对 `Interfaces/Approval`、`Interfaces/Attendance` 全部接口）：
  - 审批：`ApprovalBotMessageRequest` 补 `sender_user_id` / `text`；`ApprovalBotMessageUpdateRequest.I18nResources`
    改为非空数组；`AddSignTaskRequest.AddSignType` 与 `RollbackTaskRequest.NodeIds` 按文档改为必填；
    审批文件上传的 multipart 字段名由 `file` 纠正为 `content`（服务端按该字段名取文件）。
  - 考勤：考勤组 `free_punch_cfg` 补 `free_clock_setting`（`clock_mode` / `clock_internal_hhmm`）；
    `user_stats_fields/query` 补必填查询参数 `employee_type`；归档规则列表返回改为单层
    `FeishuApiResult<ApiPageListResult<ArchiveReportMeta>>`——此前多包一层信封，`data.items` 无法反序列化。
  - 清理文档未声明的成员：`CommentsPageListResult` 不再继承分页基类（响应只有 `comments`）；
    `AttendanceGroupsInfo` 删除仅请求侧存在的 `effect_now`；审批文件接口文件按命名规范由
    `IFeishuV4ApprovalFile_Tenant.cs` 更正为 `IFeishuV2ApprovalFile_Tenant.cs`（接口名与文档同步）。

## [3.1.0] - 2026-10-19

**重大功能版本 —— 新增 AI 能力。** `Mud.Feishu.AI`（Agent 运行时）、`Mud.Feishu.AI.Tools`（工具面）与
`Mud.Feishu.AI.Mcp`（MCP 服务器）三个新包随本版首次发布：在 .NET 服务里嵌入飞书智能体、让模型安全地
调用飞书 API，或把飞书工具经 MCP 接给任意 Agent 框架。AI 系列包为首次发包、**无破坏性变更**；
核心 SDK 包（`Mud.Feishu`、`Mud.Feishu.Abstractions`、`Mud.Feishu.Redis` 等）可直接升级，唯一迁移点是
随 `Mud.HttpUtils` 3.0.8 移除的三个上下文切换成员（见「变更」）。

### 亮点

- **飞书智能体运行时**：飞书消息 / 事件 → 会话 → 模型 → 回复，一行装配即可上线；Bot 自激过滤、
  群聊 @ 过滤等安全语义内建，回复支持消息卡片流式输出。
- **365 个策展工具（34 个业务域）**：即时消息、邮件、任务、多维表格、电子表格、云文档、知识库、
  日历、审批、通讯录、妙记、幻灯片等飞书高频操作以编译期 Schema 暴露给模型；只读与写类分离，
  40 个高危写工具默认关闭、启用前须经宿主授权。
- **人工确认（HITL）**：写操作可停在「等待确认」，经宿主审批通道放行后续跑；未确认绝不自动执行。
- **MCP 服务器**（可选包）：stdio 或 Streamable HTTP 接入 Claude Code / Cursor / 自建 harness，
  工具白名单与进程内 Agent 同源。
- **会话记忆与知识检索**：条数 + token 双窗口记忆、超窗摘要压缩；内置知识接入与自建向量检索（RAG）
  两种形态，检索结果回链引用。

### 新增（Added）

- **`Mud.Feishu.AI` —— Agent 运行时**
  - `FeishuAgent`（基于 `Microsoft.Agents.AI`）：会话管理、模型调用、工具循环、流式输出；
  - `AddFeishuImConversationHandler` 一行接入内置 IM 会话处理器（群聊 @、单聊均可配置）；
  - 会话串行化闸门：同会话排队、跨会话并行，提供进程内与 Redis 两种实现；
  - 流式回复通道：消息卡片流（创建 → 更新 → 终态），失败自动降级为消息编辑通道；
  - 事件上下文装配：审批、多维表格记录变更等事件进入对话上下文（`ConversationRequest` 新增事件事实）；
  - 交付可靠性：重投递命中「已生成但未确认送达」的回复时直接补发，不重跑模型。
- **`Mud.Feishu.AI.Tools` —— 工具面（365 个策展工具 / 34 域，188 只读 + 177 写类）**
  - 幻灯片（`slides` 新域，12 枚）：XML 演示文稿的读、建、改、删与回滚——`slides.get_presentation` /
    `slides.list_slides` / `slides.get_slide` 读取 XML 正文（按 `content_offset` / `content_limit`
    窗口化，默认 25000 字符，回填编辑的原文**不截断**）；`slides.create_presentation` /
    `slides.add_slide` / `slides.replace_slide` 建稿与改写；`slides.delete_slide`（高危写，默认关闭）；
    `slides.list_histories` 列历史版本、`slides.revert_history` 提交异步回滚并由
    `slides.get_revert_status` 轮询确认（「提交成功」不等于「已回滚完成」）；
    `slides.list_slide_images` / `slides.render_slide_image` 渲染页面图片（base64 解码后经宿主
    暂存落盘，结果不含字节）。写类五枚均支持 `dry_run` 预演、校验先于下发。
  - Markdown 生命周期（`markdown` 新域，5 枚）：`markdown.create`（Markdown 上传为 `.md` → 导入任务转在线
    文档 → 有界轮询 → 成功终态后清理源文件，失败保留源文件凭证供排查）、`markdown.fetch`（docx 正文按
    窗口读回 Markdown；`.md` 资源文件走受控下载通道，严格 UTF-8 校验）、`markdown.patch`（唯一精确匹配
    替换，0 处不写、多处直接拒绝）、`markdown.overwrite`（整篇正文重建，先建后删故删除失败时新旧并存）
    与 `markdown.diff`（只读比对，输出行级 unified diff）。三个写工具均支持 `dry_run` 且默认不启用；
    `markdown.create` 的 `file_extension` 必须与上传后缀一致，否则平台拒绝导入。
  - 复合工作流（`workflow` 新域，2 枚只读）：`workflow.meeting_summary` 一键汇总一段时间内的会议
    纪要（`minutes.search` → `minutes.get_artifacts`，按场次裁剪摘要/章节/待办），
    `workflow.standup_report` 生成站会日报（`calendar.agenda` → `task.list_my_tasks`）。编排定义是
    编译期常量，每步仍走完整授权 / 审计 / 脱敏链路（回执 `steps[]` 带逐步骤 `audit_id`）；
    依赖工具未启用时返回可执行的 `tool_not_allowed`，`dry_run=true` 只回步骤计划不发起嵌套调用，
    长链路可携带 `resume_token` 断点续跑（Base64 内联状态，服务端无会话）。两者刻意**不含任何写入
    步骤**（只读设计，见 `documents/AIAgent/decisions/ADR-0007-workflow-layer.md`）；
    `workflow.meeting_summary` 含他人发言与待办，**默认不启用**。
    `Mud.Feishu.Agent.Demo` 新增「复合工作流」模式（第 7 项）：先对两个工作流各做一次 `dry_run` 预演，
    再真实执行会议纪要汇总；站会日报在配置 `CalendarId` / `UserId` 时真实执行，否则打印跳过提示。
    该模式不调用模型，共享只读白名单同步剔除 PII 工具（仅在该模式自己的白名单里显式启用）。
  - 收尾补齐（会议 / 知识库 / 审批）：会议机器人 `vc.user_active_meeting` / `join_meeting` /
    `leave_meeting` / `send_meeting_message`（9 位会议号入会、19 位会议 ID 离会与会中消息，双 ID
    不可互换）；知识库空间与成员 `wiki.space_list` / `space_get` / `member_list` / `member_add` /
    `member_remove`（高危写，默认关闭）/ `node_copy`；审批 `approval.cancel_instance`（提交人撤销）与
    `cc_instance`（抄送，**user 身份**）。
  - 表情回复、妙记与 OKR 指标补齐：即时消息表情 `im.list_reactions` / `batch_query_reactions` /
    `add_reaction` / `delete_reaction`（后者为高危写，默认关闭）；妙记逐字稿 `minutes.get_transcript`
    （受控落盘，结果不含字节）与 **user 身份** `minutes.clip` / `upload` / `subscribe` / `unsubscribe`
    （剪辑走「起始|结束」文本式时间区间，强制带时区、不做本地时区静默偏移，且校验先于 `dry_run`）；
    OKR 指标 `okr.get_objective_indicator` / `get_key_result_indicator` / `update_indicator`
    （写回执与读侧共用同一投影口径，改完即可原样读回）；知识库 `wiki.space_create`
    （**user 身份**——平台的空间创建只有用户身份形态，且不支持幂等键，重复调用即创建多个同名空间）。
  - 文档历史与素材补齐：`docx.history_list`（只读，云文档版本列表——版本能力在云空间侧、Docx 无对应
    端点，故 Source 跨域指向 Drive 文件版本接口；`obj_type` 为闭集 `docx` / `sheet`，走统一分页信封，
    只回版本元信息不含正文）与 `docx.media_insert`（写，图片两步链：素材上传 `parent_type=docx_image`
    → 建图片块；建块失败时回 `partial_failure` 与已上传的 `image_token` 供续用，`idempotency_key`
    只覆盖建块一步（素材上传不幂等），依赖宿主附件暂存器、默认不启用，支持 `dry_run` 预演）。
    同批把「平台无接口故无法策展」的核验结论（日历改期转让、IM 收藏与 Feed 流、知识库删空间 /
    删节点、OKR 评论、多维表格 upsert 与分享链接、妙记替换发言人、会议截图、邮件回复线程语义等）
    登记于《工具权限对照表》不策展清单，而不是提供绕开策展的调用通道。
  - 强类型参数 Schema、统一分页信封与错误码；写操作支持 `dry_run` 预演；文件下载受控落盘，
    结果不含字节（不污染上下文）；工具结果被截断时如实向模型标记；
  - 权限模型：逐工具声明调用身份（tenant / user）与 OAuth scope，权限对照表随包分发；
  - 逐工具 affordance 决策指导（何时用 / 何时别用 / 前置条件 / 技巧），模型可见；
    写类工具面已**满覆盖**（40 个高危写 + 137 个普通写），只读面按高频读取路径补齐
    （im / bitable / docx / drive / mail 的 `list` / `get` 共 33 份 + slides 7 份 + markdown 2 份 +
    workflow 2 份，只读合计 44 份，全面 **221 份资产**），
    覆盖率由 `AffordanceCoverageContractGuards` 按风险分级锁定（写类两级 100%、只读级 18% 起步），
    新增写类工具不同批补资产即构建报红；
  - 结构化审计出口（`IToolExecutionAuditSink`）与 OpenTelemetry 指标（工具执行、耗时、模型调用耗时）；
  - 自建知识检索（分块 / 索引 / 向量检索）与文本工具（`ai.translate_text`、`ai.detect_language`）；
  - Skills 产物导出（`skills/feishu-*`），可直接分发给 Agent harness；另含单入口路由
    `skills/feishu-suite/SKILL.md`（34 行域条目 + 规模数字，行体与 MCP 域路由摘要共用同一次推导，
    不复述任何域内安全规则）。
- **`Mud.Feishu.AI.Mcp` —— MCP 服务器（可选包）**
  - stdio 与 Streamable HTTP 两种传输把白名单飞书工具与资源暴露给进程外 Agent；与进程内 Agent 共用同一
    工具定义与执行链，写工具同样走人工确认。HTTP 形态的租户身份只取自宿主认证层，逐请求 fail-closed。
- **人工确认（HITL）管线**：`IFeishuToolApprovalChannel` 宿主审批通道、待确认快照持久化
  （`IFeishuPendingApprovalStore`）、批准后续跑幂等；未注册审批通道时写工具停在「等待确认」。
- **示例程序**：`Demos/Mud.Feishu.Agent.Demo` 以启动菜单选择运行模式（裸模型 / IM 会话 /
  文档智能体 / 工具冒烟 / 领域事件等），非交互环境下自动采用配置默认项。
- **装配自检面（`Mud.Feishu.AI.Tools`）**：`IFeishuDiagnosticsProbe` 把「启动后第一次调用才暴露的装配
  缺陷」变成一次可调用、可读、可断言的报告——`Probe()` 回 `FeishuDiagnosticsReport`（结论
  `FeishuDiagnosticsFinding`（严重度 info / warning / error + 稳定机读码 + 处置建议）与事实读数
  `FeishuDiagnosticsFact`），`ToText()` 排序全确定，可作发布前冒烟与健康检查的断言面。覆盖配置有效性、
  注册表装配、契约↔注册对账、风险词表、写工具缺授权器、HITL 通道缺席、审计出口缺席、内容安全开关、
  user 身份接线与令牌管理器可达性、应用配置、guidance 预算余量等。
  **零副作用契约**：同步、不发起任何网络调用、不刷新令牌、不实例化应用（令牌维度只报「管理器可达性」，
  不报「有效期」——读有效期会让一条诊断命令变成一次真实调用）；自检自身出错时降级为 `probe_fault`
  结论而非抛出。由 `AddFeishuTools()` 注册为单例，宿主按需解析。
- **成本核算出口（`Mud.Feishu.Abstractions` 指标面）**：新增 `feishu.agent.llm.tokens`（Counter，
  维度只有 `direction` = input / output / total）与 `feishu.tool.result.tokens`（Histogram，
  维度 `tool` + `app_key`，读数为** shaping 后**回填模型的文本 token 数）。前者与既有的 Span 用量属性
  **相互独立**：没有 Activity 监听器时宿主仍拿到成本读数；供应商未回传 usage、零值与负值一律不计数
  （宁缺不造）。高基数纪律不变：`conversation_key` / `chat_id` / `user_id` 只进 Span 属性，绝不进指标 tag。
  两项指标仍是零依赖的 `Meter` 发射，SDK 不接 Exporter。
- **MCP 运行时自描述**：`initialize` 响应的 `capabilities._meta` 新增装配读数
  `tools` / `domains` / `readOnlyTools` / `writeTools` / `resources` / `protocolVersions`，
  全部由**实际服务中的工具面**（受同一白名单约束）与协议版本闭集推导——外部 Agent 因此在第一次握手后
  就知道「这个 server 到底给了我多大一面」，而不必靠 `tools/list` 回读后再自行分类。
- **MCP Streamable HTTP 传输（`Mud.Feishu.AI.Mcp`）**：在 stdio 之外提供第二条传输。宿主把自己的 HTTP 端点
  （`POST` / `DELETE`）映射到 `FeishuMcpHttpHandler`，请求与响应是框架无关快照（`McpHttpRequest` /
  `McpHttpResponse`），本包**不引入任何 Web 框架依赖**（最小 API / MVC / FastEndpoints / 云函数皆可适配）。
  **执行链零变化**：工具面、白名单、授权、出站净化与审计仍在协议层之下，传输层新增的判据全部是**拒绝**形态
  （不合格请求宁可 4xx，也不会被交给执行链）。
  - **租户身份的唯一来源是宿主认证层**：`AppKey` / `UserId` 只能由认证层写入请求快照，绝不从 URL、请求体或
    自定义头推导；会话在 `initialize` 时绑定租户与协商后的协议版本，后续请求给出不同身份即 403——
    **会话标识不是切换租户的凭据**。一个进程服务多租户需显式声明 `FeishuMcp:Http:TenantBoundPerRequest=true`
    （默认关闭，此时 HTTP 与 stdio 同为单租户形态）。
  - **会话目录在进程内**：`MaxSessions`（默认 64，超限 503）与 `SessionIdleTimeoutSeconds`（默认 900，
    惰性回收、不起后台线程）；`DELETE` 终止会话回 204，未知或已回收的会话标识回 404（客户端重新握手）。
    多实例部署需会话粘滞，或把上限理解为**单实例**上限。
  - **跨源防护 fail-closed**：`AllowedOrigins` 为空时**拒绝一切带 `Origin` 的请求**（`Origin` 只由浏览器发出，
    服务端 Agent 与 CLI 不发，故默认形态对它们零影响）；拒绝文案不回显来源值。`AllowedAppKeys` 是认证之后的
    第二道名单，不是身份来源。
  - **明确取舍（不是未完成）**：只做 JSON 响应，不做 `text/event-stream`（`GET` 一律 405）、不支持批量请求
    （400）、通知回 202 无正文；请求体上限归宿主的 Web 层。决策与理由见
    `documents/AIAgent/decisions/ADR-0008-mcp-http-transport.md`，部署与排障对照表见
    `documents/AIAgent/MCP多租户部署Runbook.md` §2。
- **`Demos/Mud.Feishu.Agent.Demo` 新增 `doctor` 模式**：零模型、零网络打印上述诊断报告。凭证留空即以
  **占位凭证**把容器装配成完整形态（占位凭证永不被使用，该模式不触网），从而能读出真实装配结论而不是一堆
  「未配置」噪音。

### 变更（Changed）

- **长尾能力从「不可达」改为「可申请」**：`feishu.capability_lookup` 现按分组回策展缺口
  （`curated_methods` / `uncurated_methods`）与候选策展名 `suggest_tools`，`feishu.schema_read` 对
  未策展方法回 `suggested_tool`，两者共用同一条申请通道文案（`apply_channel`）。候选名是宿主策展的
  **候选**，不构成调用通道——实测对既有策展面模块前缀一致率 95.6%、全名一致率 23.1%，故命名最终以
  策展人为准（决策与口径：`documents/AIAgent/decisions/ADR-0006-capability-apply-loop.md`）。
  工具面**仍不提供**任意方法兜底调用；未策展期间的正确处置是如实告知用户。
- **OpenTelemetry 装配内核统一**：非法配置由静默忽略改为注册期即抛 `OptionsValidationException`；
  请择一注册 OpenTelemetry 装配（与 `AddMudHttpOpenTelemetry` 并用会重复导出）。
- **上下文切换成员随 `Mud.HttpUtils` 3.0.8 移除**：`FeishuAppManager` 的 `GetWebApi` / `GetDefaultWebApi` /
  `RegisterSwitcherFactory` 已删除（上游 3.0.8 物理移除基类成员，本仓重写同批删除）。
  迁移路径：DI 解析 `IAppScopeSwitcher` + `UseAppScope(appKey)` / `UseDefaultAppScope()`（自动归还上下文），
  或 `IAppContextHolder.SwitchToApp(appKey, appManager, sp)`（受控切换并保持）。
- **文档与示例改写**：调用点统一为「先切换上下文、再从 DI 解析 API 客户端」形态（README 中 / 英、
  `Mud.Feishu` README、多应用示例控制器）。

### 修复（Fixed）

- **Skills 产物的两处「只有真实外部 Agent 才会暴露」的缺陷**（由一次真实 harness 加载实证抓到，
  发布的工具面零变化、golden 逐字节零漂移）：
  - 命令清单行的 240 字符**按字符硬切**会把权限串与身份要求切成半截（实测 27 行受影响，
    含 `im:message:send_as_bot` 整段丢失、`minutes.upload` 的 user 身份被切）。现改为
    **按标点边界截断 + 行尾补契约尾注**（`〔user 身份；需 minutes:minutes.upload:write〕`），尾注取自
    Schema 的 `x-feishu` 元数据而非散文（散文会漏），且只要发生截断就回填**全量**契约；短描述逐字节不变。
  - 正文里的 `feishu.guidance_read(域, 主题)` 细则指针是**进程内工具**形态，只拿到 `skills/` 文件的
    Agent 无法执行（实测 14 个域包 14 条全部落空）。导出层不改写正文（避免第二份真相源），改为在每个
    有细则的包末尾追加一条**解码规则**——指针的「域, 主题」即本包 `references/主题.md`。
- 未应答的人工确认不再阻塞后续会话（自动愈合）；流式回退路径不再丢失待确认审批。
- 下发失败的重投递改为补发既有回复，不再重跑模型重复计费；空回复不再下发。
- 多应用宿主下回复不再以默认应用身份误发到其他租户。
- `minutes.get_artifacts` 逐字稿分窗口参数此前被丢弃（恒返回预览），现真实生效。
- 知识召回的 `has_answer` 不再被中间事件重置导致答案被丢弃。
- `chatgroup.create_chat` / `chatgroup.add_manager` 的工具描述与执行器回执不一致（描述承诺
  「返回无效 ID 列表」，实际一个不投影、一个返回管理员全量名单）——模型据此会向用户误报拉人失败明细，
  现已按实现改写；同批校正 `Guidance/chatgroup.md` 的回执口径。
- `im.get_message_read_users` 声明了 `page_token` / `fetch_all` 入参，单页结果却用无翻页契约的信封
  投影——模型既看不到 `has_more` 也拿不到续翻游标，等于「能传游标却翻不了页」；现改走统一分页信封，
  与输出 Schema 声明一致。
- MCP 协议层读取 `method` 字段用了 `GetValue<string>()`，且该调用落在 try/catch 之外：客户端把 `method`
  写成非字符串（数字 / 对象）时，stdio 链抛出 `InvalidCastException` 而不是回 `-32600`，一条畸形请求就能
  打断整条会话。现统一经 `McpJson.ReadOptionalString`（字段缺席与非字符串同判为「没有方法」），HTTP 形态
  同样回 200 + 协议错误信封。
- Token 计数精确化（此前词表缺失恒走字符估算）。
- 安全：邮件头 CR/LF 注入被拒；IPv6 回环地址白名单校验生效；AOT 严格模式下消除全部
  `IL2026` / `IL3050` 裁剪告警。

### 文档与测试

- **新增工具面三层评测体系**（`Tests/Mud.Feishu.AI.Tools.Tests/Eval/`，8 个类 + 3 份 JSON 资产、26 条执行用例；
  发布的工具面**零变化**，未新增工具、未改任何声明）：① 工具选择 192 条真实用户说法 / 34 个域；② 参数生成 41 条
  （20 条按 JSON Schema 校验 + 21 条交真实执行链判定，两层都含「应通过」与「应被拒」两种极性）；③ 端到端场景
  9 个 / 24 步，覆盖只读默认、强制授权、放开授权、风险上限、人工确认五种装配剖面与 7 类结局桶。评测集是
  **资源文件而不是代码**——改工具面只动 JSON。全程离线：零网络、零新增依赖、无 sleep，写类工具一律以 `dry_run`
  预演断言（只有无下游调用的元工具断言真实成功）。测试装配工厂同步增加一个可选的服务配置钩子，供按剖面追加注册。
- **报告可复现**：同一命令两次独立运行的报告**逐字节相同**（由用例锁定），内含未中首位清单、参数双层判定、
  场景结局桶，以及四类失败分类（选错工具 / 参数错 / 幻觉工具名 / 授权拒）。首轮基线：pass@1 **47.9%**、
  pass@3 **71.9%**、pass@5 **80.2%**、幻觉工具名 **0**；阈值以「只涨不跌」的棘轮下界进门禁，空集一律报红。
- **口径说明（防数字被误读）**：选择层是确定性词汇检索（TF-IDF），度量的是**工具描述的判别度**，不是任何大模型的
  准确率。实测加 per-tool affordance 视图后词汇级 pass@1 由 47.9% 变为 45.3% ⇒ affordance 的收益不体现在字面检索上，
  「补 affordance 即提升选择准确率」不得作为承诺。
- **评测顺带核实、登记为后续待改的三条事实**（本版不改产品行为）：365 枚工具中**仅 2 枚**在 JSON Schema 上声明
  枚举取值（其余闭集由执行器判定 ⇒ 参数质量只能靠执行链层度量）；`markdown.patch` / `markdown.overwrite` 的
  `idempotency_key` 在 Schema 标为可选、执行链按必填拦截（两层分歧）；多维表格筛选/排序文法的拒绝走
  **未分类的第二错误出口**（回执文本以 `[tool_error]` 开头而错误分类字段为空），机器可分辨性待收口。
- 验证：标准档门禁全绿（**5,496 通过 + 25 门控跳过**；`Mud.Feishu.AI.Tools.Tests` 1638 通过，净增 26 条
  全部来自评测），golden **逐字节零漂移**（未新增工具、未改任何声明 ⇒ 无需重固化，skills 与权限对照表零差异）。
- **装配自检与成本出口的配套用例**：`FeishuDiagnosticsProbeTests`（探针逐码触发条件、严重度分档、
  重复调用逐字节相同、`probe_fault` 不抛出）、`DoctorDemoTests`（占位凭证容器的装配读数与报告渲染）、
  `FeishuAgentTelemetryTests` 增 5 条（2 条：token 计数三方向、无 Activity 时仍计数；3 行理论：缺项/零/负值不计数）、
  `FeishuToolResultTokensTests`（读数为 shaping 后文本、shaper 改写的内容才是被计数的那一份、拒绝路径不计数）、
  `FeishuMcpToolServerTests` 增 3 条（`_meta` 自描述与**实际服务对象**同源、资源读数随开关变化）。
  发布的工具面**零变化**（365 枚 / 34 域 / affordance 221 份全部不变，未新增工具、未改任何 `[AgentTool]`
  声明 ⇒ golden 无需重固化），公开面 449→**454**（自检契约的 5 个 public 类型，消费方理由同批登记）。
- 验证：标准档门禁全绿（**5,539 通过 + 25 门控跳过 = 5,564 总计**；净增 43 条全部来自本批：
  `Mud.Feishu.AI.Tools.Tests` 1638→1665、`Mud.Feishu.AI.Tests` 448→453、`Mud.Feishu.Agent.Demo.Tests` 165→176）。
- **Skills 产物的真实消费实证与配套守卫**（`documents/AIAgent/Skills外部消费实证报告.md`）：把一个装了产物副本的
  外部 Agent harness 当真实消费者跑了一遍——三层渐进披露（发现列表只带 `description`、正文按需注入、
  `references/*.md` 再读一次）实测可用，非标准 frontmatter 键 `package_version` 被接受。产物 34→**35** 个
  `SKILL.md`（新增单入口路由）、51→**52** 个 md；清单行 365 条 ≡ golden、超预算 0 条。守卫 **+8**：域路由表
  逐行重算并与工具集合双向闭合、每条清单行的权限与身份判据可达性、截断策略自证（含「短描述逐字节不变」与「无契约可回填时不挂空壳尾注」两极）、
  细则指针必须能落到 `references/` 文件（`scanned > 0` 防假绿）。**诚实边界**：只覆盖 1 个 harness，
  Claude Code / Cursor 未做真机记录；顺带核实并登记 6 枚工具的**描述散文与 Schema 元数据不符**
  （4 枚漏写 `required_scopes`、2 枚 user 身份未在散文声明）——属策展层内容缺陷，根治要改 `[AgentTool]`
  描述并触发 golden 重固化，不在本版范围。
- 验证：标准档门禁全绿（**5,603 通过 + 25 门控跳过 = 5,628 总计**；净增 8 条全部来自上条，
  `Mud.Feishu.AI.Tools.Tests` 1721→1729，其余 10 个程序集逐项不变），golden **逐字节零漂移**
  （未新增工具、未改任何 `[AgentTool]` 声明），公开面仍 458。

### 依赖（Dependencies）

- 新增 `Microsoft.Agents.AI` **1.25.0**（Agent 运行时底座），配套升级 `Microsoft.Extensions.*`
  **10.0.12**、`Microsoft.Extensions.AI.Abstractions` **10.10.1**、`System.Text.Json` **10.0.12**。
- 新增 `Microsoft.ML.Tokenizers.Data.O200kBase`：精确 token 计数词表。
- `Mud.HttpUtils*` 全仓单一版本升级 **3.0.7** → **3.0.8**（`Mud.HttpUtils` 与 `Mud.HttpUtils.Generator`
  必须同版本；含 `Mud.HttpUtils.Resilience` 弹性策略，随包传递）。

## [3.0.4] - 2026-10-18

> 主题：**源码注释治理落地**（注释纪律规范 + `audit-comments` 审计脚本 + L3 决策档案）与
> **WebSocket 心跳/观测修正**。
> 本版**无对外契约变更**：公开 API 签名、运行时行为与包依赖版本均不变；改动集中在注释面、
> 内部文档与示例工程依赖对齐。

### 新增（Added）

**注释纪律（Comment discipline）**

- 新增规范性文档 `documents/代码注释规范.md`：注释三条判据、L0~L4 分层模型、R-01~R-12 硬性规则、
  `[Obsolete]` 消息模板、术语表、逐行豁免机制，以及「必须保留的约束」白名单（附录 A）与
  规则 → 脚本模式映射（附录 B）。
- 新增审计脚本 `scripts/audit-comments.ps1`（报告 / `-Strict` 门禁 / `-ListAll` 三种运行模式），
  接入点写定为 `scripts/verify-build.ps1` 步骤 1.5 与 CI 流水线。
- 新增存量基线 `scripts/comment-baseline.txt`：命中按**内容哈希**冻结（重排版与行号漂移不使基线失效），
  **只减不增**（`-UpdateBaseline` 在基线变大时以 exit 2 拒绝）。本次治理后基线已**清零**
  （初始 980 条 / 1020 命中 → **0 条 / 0 命中**）。
- 新增逐行豁免标记 `// comment-allow: <原因>`（原因必填，禁止整文件豁免）。
- 新增 **L3 决策档案** `documents/决策档案/`：`README.md`（落点约定）、`编号索引.md`（内部编号反查表）、
  `上游契约适配史.md`、`迁移与弃用史.md`、`注释精简归档.md`——版本适配史、缺陷复盘与验收记录
  统一迁出源码注释。
- `AGENTS.md` 增补「注释纪律（Comment discipline）」章节（判据、门禁接入、决策档案落点）。

### 变更（Changed）

- **全仓手写源码注释收敛为「契约 + 约束」**：覆盖 8 个交付项目 **172 个源文件**
  （删除 2037 行、净减 482 行）。移出内容：内部分支/任务编号（`BC-*`、`U-*`、`C-*`、`F-*`、
  `TMA*`、`TMF*`、`TMR*`、`WHF-*`、`WS-*`、`P0-/P1-/P2-*` 等）、
  「修复前 / 改造前 / 原实现」式缺陷复盘、`documents/…` 与 `.docs/…` 内部文档路径、
  `// ─────` 式装饰分节线；保留不可从代码推断的约束（锁序、原子性、幂等、顺序依赖、
  AOT 抑制理由、安全语义）。
  涉及 `Mud.Feishu.Abstractions`（多应用管理、令牌存储与编解码、服务注册、事件解析、指标）、
  `Mud.Feishu.Redis`、`Mud.Feishu.Webhook`、`Mud.Feishu.WebSocket`、`Mud.Feishu.Authentication`、
  `Mud.Feishu.DataModels`（查询 DTO）与 `Mud.Feishu/Interfaces/**`（接口契约注释）。
  令牌存储相关约束（存储形态只经 `TokenStoreHelper` 编解码、CAS 能力面探测必须落在未装饰的原始 store）
  措辞压缩但语义本体保留。
- **注释审计脚本修正**：`<remarks>` 的**开闭同行**单行块不再遗留 `$remarksOpen` 悬挂状态——
  此前会把其下方 8 行块并入统计并误报为「82 行」长度（同类单行块 25 处，影响命中行号与长度）。
- **`scripts/verify-build.ps1` 本地源判空修正**：无本地源时改为「先判空再 `Test-Path`」，
  消除 `Test-Path ''` 抛出的「Path 参数为 null 或为空集合」（CI 与已移除本地源的开发机均会命中）。

### 修复（Fixed）

- **心跳循环捕获调用方 `SynchronizationContext` 导致 Ping 被推迟**（`HeartbeatManager`）：
  循环内 `await` 一律改为 `ConfigureAwait(false)`——后台循环的续体不得 Post 回调用方上下文
  （UI 线程 / 旧 ASP.NET 请求上下文 / 测试进程内 xUnit 并发闸门），该上下文被占满时发 Ping 的续体
  会排队等待配额（实测 5s 间隔在 10s 预算内仍未送达）。新增回归守卫
  `HeartbeatManagerProtoBufTests.StartHeartbeatAsync_ShouldNotCaptureCallerSynchronizationContext`，
  并放宽 WebSocket 集成测试握手预算。
- **删除恒为 `null` 的 `ProcessingTask` 分支**（`BinaryMessageProcessor`）：该属性从未赋值、
  分支从不执行，属纯死代码；二进制路由结果由 `RouteBinaryMessageWithResultAsync` 的返回值处理。

### 构建（Build）

- 示例工程 `Mud.Feishu.*` 包引用对齐：`FeishuFileServer` / `FeishuOAuthDemo` / `FeishuWikiManager` /
  `TaskManageDemo` → **3.0.3**；`Mud.Feishu.Webhook.Demo` → **3.0.1**。
- `FeishuWebhookServiceBuilder` 为「已删除配置键名」与过渡期 `[Obsolete]` 开关补 `audit-allow` 逐行标记，
  使 `audit-config-keys.ps1` 可精确放行而无需整文件豁免。

## [3.0.3] - 2026-10-15

> 主题：**适配 `Mud.HttpUtils` 3.0.7**（上游弃用标记传播 + 元包解耦适配）。
> 本版**无对外契约移除**；`FeishuAppManager.GetWebApi` / `GetDefaultWebApi` 随上游标记弃用
> （3.x 编译告警，4.0 移除），签名与行为不变。

### 依赖升级

- `Mud.HttpUtils` / `Mud.HttpUtils.Generator`：`3.0.5` → **`3.0.7`**（两者必须同版本）。
  6 个源工程的 8 处 `Mud.HttpUtils*` 声明统一，各 README 与 `AGENTS.md` 依赖表同步。
- **新增 `Mud.HttpUtils.Resilience` 3.0.7 显式依赖**：上游元包不再传递弹性策略包，本仓
  `AddFeishuApp` 装配链（per-app 重试 / 超时 / 熔断）依赖之，故显式安装；该依赖随包流传给消费方
  （运行期加载所需），不设 `PrivateAssets`。

### 新增（Added）

- **双读令牌存储编解码**：新增公开 `FeishuTokenStoreCodec`（实现上游 `ITokenStoreCodec`）。
  写入格式改为官方相对秒 `{expiresInSeconds}|{token}`；读取双读兼容旧「绝对毫秒戳」存量值
  （按条目自然过期收敛，无需回灌）；消费方「绝对毫秒戳」契约不变。同时修正 Redis 侧 refresh
  TTL 的「无 TTL 信息」边界（前导数 ≤ 0 回落 30 天，不再误判为已过期删键）。
  **回滚须知**：回滚 3.0.2 及更早版本时新格式值会被判为过期（重新获取令牌；Redis refresh 键
  被旧逻辑删除 → 用户需重新授权），不产生错误令牌。
  双读判别复用官方 `MigrationTokenStoreCodec`（`FeishuTokenStoreCodec` 为其薄包装，显式传飞书阈值
  10^10），本仓不再维护私有双读实现。
- **外部标识反查**：`FeishuAppContext` 实现 `IAppExternalIds`（`app_id → appKey`），
  `FeishuAppManager` 支持经内置索引与 DI 注入的宿主 `IAppLookupProvider` 反查 appKey
  （fail-closed，结果必须命中已注册应用）——可把只携带 `app_id` 的事件载荷解析到对应应用。
- **跨进程刷新去重租约**：三个令牌管理器（Tenant/App/User）接入
  `TokenManagerBase.RefreshLease` 钩子。`Mud.Feishu.Redis` 注册官方 `RedisRefreshLease`
  （`SET NX PX` + Lua compare-and-del，fail-open），键前缀 `{TokenKeyPrefix}:lease` 随环境段隔离；
  `DefaultFeishuTokenManagerFactory` 经 DI 解析注入。未启用 Redis 时行为与单实例逐位一致；
  多实例下未抢到租约的实例限时轮询读穿透（250ms 间隔 / 10s 超时 fail-open 放行本机刷新）。
- **按应用清库能力面**：`Mud.Feishu.Redis.RedisTokenStore` 实现官方
  `ITokenStoreAppPurge`（`EnumerateKeysAsync` 物理键快照枚举 / `PurgeAppAsync` 返回实际删除数，
  节点失败直接上抛）；清库门与加密装饰器透传该能力面——宿主可统一走
  `TokenStorePurgeExtensions.PurgeAppTokensAsync(store, appKey)`（能力探测穿透装饰器栈）。
- **多命中外部标识反查**：`FeishuAppManager.TryResolveAllAppKeys`
  重写——宿主提供者实现 `IMultiAppLookupProvider` 时返回全部已注册 appKey（过滤未注册/去重、
  不物化懒加载），仅单结果面时回退单命中反查语义。
- **用户维度原生 CAS**：`RedisUserTokenStore` 实现
  `IUserTokenStoreCompareExchange`——Lua 单脚本内完成「取存储值 → 明文比较 → 写入/删除」，
  凭据变更清库不再有「读取 → 比较 → 删除」的竞态窗口；`UserTokenManager` 按原始 store
  能力分派（无能力 store 保留解码明文比较路径，语义与升级前一致）。
- **v2 信封读取兼容**：`TokenStoreHelper` 支持 v2 版本化信封读取
  （`v2:{n}|{token}` 三态归一、`v2:expired|` 投影为无令牌、Redis 侧 TTL 推导同步兼容）。
  写侧仍单写 v1 相对秒形态（默认存储形态不得漂移）。

### 变更（Changed）

- **`FeishuAppManager.GetWebApi` / `GetDefaultWebApi` 传播上游弃用标记**：基类成员在
  Mud.HttpUtils 3.0.7 标 `[Obsolete]`（4.0 移除），两个重写同步标记以保持上下游口径一致并消除
  CS0672。迁移路径：DI 解析 `IAppScopeSwitcher` + `UseAppScope` / `UseDefaultAppScope`（自动归还），
  或 `IAppContextHolder.SwitchToApp(appKey, appManager, sp)`（受控切换并保持，语义等价）。
  签名与行为在本大版本内不变；测试中的守卫性调用以 `#pragma warning disable CS0618` 精确豁免。
- **凭据变更清库 CAS 改为令牌明文比较**（随相对秒存储格式连带）：相对秒格式下存储编码值随时刻
  漂移，整值重编码比较必然失配；改为解码明文比较后时间无关，并消除旧实现「同 refresh_token
  不同持久化时间戳误跳过清库」的保守代价。
- **`Mud.Feishu.Redis` 接入官方 `Mud.HttpUtils.Redis` 包**：官方 store 与飞书
  管线为两种 `ITokenStore` 使用约定（明文入参 + 内部 codec vs 直存编码值），不可互换直插；
  `FeishuTokenStoreCodec` 作为互操作锚点，供官方组件对接飞书形态存量数据。

### 文档与测试

- 新增 `.docs/MudFeishu-MudHttpUtils-3.0.7-适配说明.md`：上游契约变更逐条对账、
  不迁移项决策（令牌键布局 / 自建叠层 / 私有 codec）及理由、发布前检查清单。
- `AGENTS.md` 增补上游契约变更传导通道（上游 CHANGELOG「契约变更」专章 + 下游约束对账清单）与
  本次适配注记。
- `scripts/verify-build.ps1` 步骤 0 本地源解析改为按「**实际包含 csproj 声明版本包**」判定优先，
  修复 `artifacts`（旧版）与 `artifacts-debug`（联调包）并存时选中前者并跳过缓存哈希校验的问题。
- 验证：质量门禁全绿（编译 0 错误、CS1750 / NU1603 / HTTPCLIENT / MUD / FORM / AOT 白名单全 0、
  AotStrictMode net8.0 逐源项目 0 违规、单元测试 6298/6348——跳过项均为 net6.0 运行时缺失）；
  升级前后唯一警告集合 diff 为 0（零新增警告）；
  新增用例：双读矩阵 14 例、外部标识反查 10 例、Redis refresh TTL 双读行为 5 例。

## [3.0.2] - 2026-10-12

> 主题：**事件回调大规模补齐**（业务域 10 → 24，事件总数 205）与 **适配 `Mud.HttpUtils` 3.0.5**。
> 本版**无对外契约移除**；含三处行为修正（授权域名并集注册、令牌恢复配置热更新链路、用户令牌移除语义）。

### 依赖升级

- `Mud.HttpUtils` / `Mud.HttpUtils.Generator`：`3.0.0` → **`3.0.5`**（两者必须同版本）。
  6 个源工程的 8 处 `Mud.HttpUtils*` 声明统一，`README.md` / `README_EN.md` / `Mud.Feishu/README.md` /
  `Mud.Feishu.OpenTelemetry/README.md` 与 `AGENTS.md` 依赖表同步。
- 示例项目 `Mud.Feishu.*` 包引用：`3.0.0` / `3.0.0-rc3` → **`3.0.1`**（`Demos/Directory.Build.props` 与 8 个 Demo 工程）。

### 新增（Added）

**事件回调（`Mud.Feishu.EventCallback`）**

- **业务域 10 → 24、事件总数 205 个**：新增 14 个业务域 —— `Application`（应用）、`Auth`、
  `MeetingRoom`（会议室）、`Moments`（动态）、`ELearning`（学习）、`SecurityCompliance`（安全合规）、
  `Compensation`（薪酬）、`Payroll`（发薪）、`CoreHr` / `CoreHrV2`（核心人事 v1 / v2）、`Hire`（招聘）、
  `Performance`（绩效）、`Acs`（智能门禁）、`Helpdesk`（服务台）。
- 新增 **219 个事件模型文件**、**124 个 `FeishuEventTypes` 常量**，18 个 `Generated/*JsonContext.g.cs`
  增量注册 443 项 `[JsonSerializable]`（AOT 源生成链路同步，无 AOT005 遗留）。
- 主要事件族：
  - **招聘**：`hire.application.deleted_v1` / `stage_changed_v1`、`hire.offer.status_changed_v1`、
    `hire.ehr_import_task.imported_v1` 与 `.for_internship_offer.imported_v1`、`hire.talent.deleted_v1` /
    `tag_subscription_v1`、`hire.eco_account.created_v1`、`hire.eco_background_check.created_v1` / `canceled_v1`、
    `hire.eco_exam.created_v1`、`hire.referral_account.assets_update_v1`。
  - **核心人事**：`corehr.*` v1（部门 / 职务 / 任职 / 合同 / 预入职 / 调岗 / 离职 / 权限）与 v2
    （地点 / 公司 / 成本中心 / 自定义组织 / 职族职级职位 / 试用期 / 入职任务 / 离职清单 / 流程节点与审批抄送 /
    签署文件等 40+ 事件）。
  - **审批与任务**：`approval.task.status_changed_v4`、`task.task.comment.updated_v1` 等。
  - **通讯录与群组**：`contact.scope.updated_v3`、`im.chat.access_event.bot_p2p_chat_entered_v1`、群成员变动。
  - **会议与会议室**：`vc.bot.meeting_started_v1`、会议室创建 / 更新 / 删除。
  - **安全合规**：设备登记与设备变更事件。另含邮箱、动态（发帖 / 评论 / 表情回复）、学习（课程报名）、
    薪酬与发薪活动、绩效（`performance.stage_task.open_result_v2`、`performance.review_data.changed_v2`）、
    门禁（`acs.user.updated_v1`、`acs.access_record.created_v1`）、服务台（工单创建 / 更新 / 留言 / 审批通知）。

  - **卡片事件回调（新增 `Card` 业务域）**：`card.action.trigger`（用户点击卡片「请求回调」交互组件）的
    请求体模型 `CardActionTriggerResult`（`operator` / `token` / `action` / `host` / `delivery_type` / `context`，
    覆盖 `value`、`form_value`、`input_value`、`option`、`options`、`timezone`、`name` 等交互字段），
    以及响应体模型 `CardCallbackResponse` / `CardCallbackToast`（含 16 语言 `i18n`）/ `CardCallbackCard`
    （`raw` 与 `template` 两种形态）+ `CardCallbackTemplateData`；同步落实「3 秒内响应、禁止 3xx 重定向」约束。

**适配上游（`Mud.HttpUtils` 3.0.5）**

- **`FeishuHttpClientFactory` 改用上游 `EnhancedHttpClientOptions.Clone()`**：删除本仓手写「字段清单式 Clone」，
  上游新增属性（如 3.0.5 的 `JsonEncoder`）不再需要在此同步补拷贝，消除「新属性被静默丢弃」这一类漂移。
- **`UrlValidator` 域名注册改增量并集**：由整体替换语义的 `ConfigureAllowedDomains` 改为逐项 `AddAllowedDomain`
  （清单提取为 `FeishuAllowedDomains`），宿主在 `AddFeishuApp` 之前注册的自定义域名不再被静默丢弃。

### 变更（Changed）

- **用户令牌移除语义改由基类承担**：`Mud.HttpUtils` ≥3.0.4 起桥接器 `RemoveAsync` 无条件写穿持久层，
  `UserTokenManager` **不再覆写** `RemoveTokenAsync`，移除 / 登出在多实例与冷启动下同样落到持久层
  （`PurgeGate` 语义不变），并补冷启动对照测试与注释约束。
- **`TokenRecoveryOptions` 配置绑定改为「捕获 section + 现场 Bind + Apply」单级方案**：不再经
  `IOptionsMonitor<TDep>` 二级管线间接读取（实测配置重载后该管线会返回陈旧 DTO），热更新只依赖单个
  `ChangeTokenSource`；「禁止 `Configure<T>(IConfiguration)` 反射绑定」的 AOT 约束不变。
- `FeishuAppManager` 默认拒绝对应的提示更新为「注册表白名单」授权器
  （`AppKeyAllowListAuthorizer.FromPredicate` + `IFeishuAppManager.HasApp`），仅错误信息与注释表述变化。

### 修复（Fixed）

- **事件模型「文件名与主类名不一致」8 处重命名**：`ApprovalResult` → `ApprovalTaskResult`、
  `DriveFilePermissionMemberAdded` → `DriveFilePermissionMemberAddedResult`、
  `ChatMemberUserAddedResult` → `ChatMemberUserDeletedResult` / `ChatMemberUserWithdrawnResult`、
  `Mail` 目录下误置的 `MeetingMeetingEndedResult` → `MessageReceivedResult`、
  `DepartmentCreatedEventResult` → `DepartmentCreatedResult`、`DepartmentDeleteResult` → `DepartmentUpdateResult`、
  `RoomCreatedResult` → `RoomUpdatedResult`。类名漂移会让 `Generated/*JsonContext.g.cs` 的 `[JsonSerializable]`
  与实类脱节，现已由契约守卫拦截。
- **示例工程 `FeishuDriveService` 删除文件 / 文件夹**：显式指定 `cancellationToken` 实参
  （此前参数位置错位导致取消令牌传递错误）。
- `.gitignore` 忽略 `/.scratch` 本地暂存目录。

### 文档与测试

- `Mud.Feishu.EventCallback/README.md` 重建为全量事件表（205 事件 / 24 域，54 → 205 行），
  表格由源码 `[GenerateEventHandler]` 标注直接汇总，新增事件必须同步该表。
- 新增契约守卫 `EventCallbackContractGuards`（5 条）：常量必须被 `[GenerateEventHandler]` 接线（防止死常量）、
  文件名必须与主类名一致、每个域必须有 `Generated/{域}JsonContext.g.cs` 且已挂进 resolver 链、
  每个 `*Result` 必须能反射找到生成的处理器、已接线事件类型必须出现在 README 事件表。
- 新增事件负载冒烟测试 `EventPayloadSmokeTests` + `TestData/EventCallback/event-examples.json`
  （官方文档「事件体示例」原文回放，对示例中每个非空键做 1:1 建模覆盖校验，字段名错配当场失败）。
- `FeishuHttpClientFactoryTests` / `UserTokenManagerTests` / `FeishuServiceCollectionExtensionsTests`
  随上游适配同步扩充（字段等价性、配置热更新语义与冷启动对照回归）。

## [3.0.1] - 2026-10-07

> 主题：**适配 `Mud.HttpUtils` 3.0.0**（上游移除 `UseApp` / `UseDefaultApp` / `BeginScope(string)`，改以作用域式切换）与 **事件 v1.0 / v2.0 双格式解析修复**。
> 本仓对外契约**无移除**（三个旧成员由 `IFeishuAppContextSwitcher` 接续声明并标 `[Obsolete]`）；
> 含少量行为修正（`EventData.CreateTime` 单位、`EventData.Event` 写侧类型），升级前请阅读「变更（Changed）」与「迁移提示」。

### 依赖升级

- `Mud.HttpUtils` / `Mud.HttpUtils.Generator`：`2.0.9` → **`3.0.0`**（两者必须同版本）。
- 上游 3.0.0 起，非 HttpClient 模式的生成类**默认不再发射** `UseApp(string)` / `UseDefaultApp()` / `BeginScope(string)`，
  且 `IAppContextSwitcher` 不再声明它们；生成类**自动附加** `IAppScopeSwitcher`（`UseAppScope` / `UseDefaultAppScope`）。

### 新增（Added）

- **`IFeishuAppContextSwitcher` 继承 `IAppScopeSwitcher`**：下游可直接以推荐面（作用域式、释放时自动归还上下文）编程。
- **`IFeishuAppContextSwitcher` 接续声明三个旧成员并标注 `[Obsolete]`**：保持对外契约不变，
  188+ 个生成实现类**继续发射**这三个成员，行为与 2.0.9 完全一致。
  三个成员将在本 SDK 的下一个大版本随上游一并移除，请按提示迁移到 `UseAppScope` / `UseDefaultAppScope`。
- **共享事件解析器 `FeishuEventDataParser`**（Abstractions，单一真源）：v2.0 / v1.0 官方形态 / `data` 包裹兼容形态统一解析，
  WebSocket 与 Webhook 双通道删除各自平行实现。
- **事件失败落盘**：WebSocket 通道处理失败可经 `AddFailedEventStore<T>()` / `AddFailedEventStore(instance)` 注册 `IFailedEventStore`（TryAdd 语义，不默认注册，未注册零行为变化；业务失败分支落盘，取消/拦截终态不落盘）。
  配套新增 `FeishuWebSocketOptions.FailedEventInitialRetryDelaySeconds`（默认 10，非正数回退默认）。
- **读取扩展 `GetEventRawJson()`**：统一读取 `EventData.Event` 携带的 JSON 原文。

### 变更（Changed）

- **`FeishuAppManager.GetWebApi` / `GetDefaultWebApi` 内部实现迁移**：由生成类的
  `UseApp(appKey)` / `UseDefaultApp()` 改为 `IAppContextHolder.SwitchToApp(appKey, this, serviceProvider)` /
  `SwitchToDefaultApp(this)`（**语义等价**：完整守卫 + 立即切换 + 不归还 + 返回上下文）。
  对外行为不变（仍返回 DI 解析到的服务实例，且其 `Current` 已绑定目标应用）。
- **`EventData.Event` 写侧统一为 JSON 原文**字符串\*\*（原 WebSocket 通道写 `JsonElement`）。
  属性类型保持 `object?` 不变；读取请改用扩展 `GetEventRawJson()`，下一 major 收敛为 `string?`。
- **行为变更**：`EventData.CreateTime` 语义由「秒」修正为**毫秒**（与 XML 注释对齐，v1.0 `ts` 一并纳入统一启发式）。
  按秒消费的宿主请 ×1000 或改用 `DateTimeOffset.FromUnixTimeMilliseconds`。
- **`IgnoreUnknownEventTypes` 默认值两通道保持差异**（Webhook=true / WS=false，不改默认）：
  两侧 Options XML 已注明差异原因，WS=false 时启动期输出一次性对齐告警。

### 修复（Fixed）

- **Webhook 通道 v1.0 事件字段映射错误**导致事件被空字段 fail-closed 400 拒绝；
  **WebSocket 通道 v1.0 官方帧**（根级 `uuid`/`token`/`ts`、事件字段在 `event` 内）被整帧静默丢弃。
  现两通道均正常路由 v1.0 事件（事件类型取 `event.type` 原值，handler 按此注册）。
- **v1.0 事件根级 `token` 现解析进入合成 Header**（`Schema == null`，字段取自根级 `uuid`/`token`/`ts` 与 `event.*`）：
  判定 v1.0 请用 `Schema == null`，不要再用 `Header == null`；来源校验与依赖 Header 的幂等逻辑在 v1.0 下恢复可用。

### 迁移提示

- 以 `IFeishuAppContextSwitcher` 类型调用 `UseApp` / `UseDefaultApp` / `BeginScope(string)` 会产生 **`CS0618`** 警告（有意引导）。
  若宿主启用 `TreatWarningsAsErrors`，请改用 `UseAppScope` / `UseDefaultAppScope`（守卫相同，额外自动归还上下文），或临时 `NoWarn CS0618`。
- 语义提醒：`UseAppScope` 在 `using` 结束时**自动回切**；`UseApp` 会一直保持到下次切换（长生命周期宿主须显式切回）。

## [3.0.0] - 2026-09-28

> 3.0 是一次面向**生产可靠性与性能**的全面升级：原生 AOT 一等支持、令牌与多应用管理重构加固、Webhook 安全基线、Redis / WebSocket 稳定性专项，并统一配置结构。包含较多破坏性变更，升级前务必阅读「升级要点」；逐项明细见下方 rc2 / rc3 记录。

### 🌟 核心升级

- **原生 AOT 全面支持**：net8.0+ 全链路源生成 JSON 序列化与配置绑定，AOT 严格模式门禁保证 0 反射告警，附带端到端验证工程（`Demos/Mud.Feishu.AotVerification`）。配置 DTO 不再使用 `required`，校验统一由 `Validate()` 承担。
- **令牌与多应用管理重构**：多应用配置热更新（`BaseUrl`/`TimeoutSeconds` 运行期生效、按 AppKey 增量应用）、per-app 认证客户端与端点隔离、凭据变更即清库、401 令牌恢复真正生效（级联清 store）、Memory/Redis 令牌键布局统一（`TokenKeyBuilder` 单一真相源）、OAuth 失败可重试性分类、令牌存储加密（可选）。
- **Webhook 安全基线**：生产环境强制分布式去重、多应用必须声明 `ExpectedAppId`、拦截语义与默认处理器路由收敛、可恢复故障一律 503 触发飞书重推——杜绝跨应用串扰与事件永久丢失。
- **WebSocket 可靠性专项**：连接生命周期与调用方取消令牌解耦、事件处理失败回 ACK `code=500` 触发服务端重投、并发闸门与重连熔断、僵尸连接消除、消息按 UTF-8 字节统一计量、背压前移到接收路径。
- **Redis 加固**：`rediss://` 真正启用 TLS、四类键统一构造与转义（`RedisKeyBuilder`）、去重竞态 Lua 原子化、Cluster 全节点覆盖、异常可分类（`FeishuRedisFailureKind`）、SeqID 去重改容量窗口、新增运维诊断门面与指标。
- **质量门禁**：`verify-build.ps1` 全新门禁（缓存自检、全 TFM 构建 + 诊断白名单、AOT 严格模式冒烟、TRX 测试计数断言），CI 同步接入。

### ⚠️ 升级要点（破坏性变更）

**配置结构**

- 配置统一为嵌套分组：`FeishuWebSocketOptions` → `Reconnect.*` / `Certificate.*`；`FeishuAppConfig` / `RedisOptions` → `HttpRetry.*` / `CircuitBreaker.*` / `Connection.*`。JSON 旧扁平键仍可自动回填，**C# 代码必须改用嵌套 API**；日志开关统一为 `Logging:LogLevel:*`。
- `nuget.config` 收紧为包来源锁定；依赖钉住 `Mud.HttpUtils` 2.0.7。

**令牌与多应用**

- Redis 令牌键布局变更（`feishu:token:*` → `feishu:{appKey}:token*`），旧键不再读取；`SingletonFeishuTokenStoreFactory` 废弃，改用 `IFeishuTokenStoreFactory.Create(appKey)`；`AddFeishuRedisTokenStore` 不再注册 `ITokenStore`/`IUserTokenStore` 单例。
- 令牌失效级联清除持久层；租户 401 恢复不再回退用户级；`SetDefaultApp` 运行期切换真正生效。
- 含特殊字符（`* ? [ ] : \`）的 AppKey 旧令牌键不再可达，受影响部署建议升级后轮换 AppSecret。
- 注入的 `IFeishuAppContext` 变为无状态转发代理（每次取当前默认应用）；需实例快照语义请设 `FeishuAppOptions.ForwardDefaultAppContext=false`。
- 配置热更新默认开启（`EnableConfigReload=true`）；如需「变更需重启」的旧语义请显式关闭。
- OAuth 刷新失败按可重试性分类：`invalid_grant` 等将清除 refresh token 并要求重新授权。

**Webhook**

- 生产环境未注册分布式去重将启动失败（单实例可设 `FeishuWebhook:AllowInMemoryNonceDedupInProduction=true`）；多应用必须配置 `ExpectedAppId`；`Build()` 要求至少注册一个全局默认处理器。
- `BeforeHandleAsync` 返回 `false` 由 500（可重试）改为 200（已消费）；需「拦截后重推」设 `InterceptionAckMode=Retryable`。拦截器组合默认 `Merge`（旧行为设 `InterceptorFallbackMode=AppOnly`）。
- 解密超时由 400 改为 503（可恢复故障让飞书重投）。

**WebSocket**

- 终止连接请调用 `DisconnectAsync()` / `DisposeAsync()`（`ConnectAsync` 的令牌只约束建连 + 认证）；`StartReceivingAsync` 已弃用；`MessageReceived` 改为并发派发（可能乱序），需顺序保护请用 `IMessageHandler`。
- 同步 `Dispose()` 不再停止服务；配置上界收紧并在启动期校验（重连预算、超时、消息大小等）；构造签名变更（移除 `seqIdDeduplicator` / `ProcessingTask` 等死参数）。
- 指标 API 迁移：`FeishuMetrics.WebSocketConnectionObserver` 等静态可写属性移除，改用 `RegisterWebSocketMetricsSource(...)`。
- 连接默认白名单 `*.feishu.cn;*.larksuite.com`，自建端点请配置 `AllowedHostSuffixes`。

**DTO 重命名（修复源生成同名冲突 SYSLIB1031）**

- `DepartmentsV1.DepartmentLeader` → `DepartmentLeaderV1`、`DepartmentDetail` → `DepartmentDetailV1`、`ApprovalExternal.ApprovalCreateViewers` → `ExternalCreateViewers` 等 7 组，完整列表见 3.0.0-rc2。

**Redis 去重**

- SeqID 去重键增加 scope 隔离维度（默认 `AppKey|MachineName`），窗口由 TTL 改为容量（`FeishuRedis:SeqIdWindowCapacity`，默认 100000）；`GetCacheCount()` 等语义收窄为窗口内真实值。
- `RedisOptions` 非法值改为启动期校验失败；`NonceFailureMode` 仅对 Redis 连接类故障生效。

### 🐛 重点修复

- **事件永久丢失**：事件处理失败 ACK 恒 200、Nonce 基础设施故障伪装 403、处理器类型不匹配静默丢失、解密超时吞成 400 等已全部修复，可恢复故障统一 503 触发飞书重推。
- **令牌正确性**：401 恢复复用被拒令牌、租户重试被注入用户令牌、Redis 过期令牌 TTL=0 永不过期、凭据变更清库在内存后端无效等。
- **稳定性**：去重工厂自解析 `StackOverflowException`、空前缀清库误删全库、WebSocket 僵尸连接 / 死锁 / `ObjectDisposedException` 竞态、Redis 连接串日志泄漏口令等。
- 同轮完成 200+ 项次级修复与加固，明细见下方 rc2 / rc3 记录。

## [3.0.0-rc3] - 2026-09-23

> 本版聚焦 **Webhook 多地部署安全加固、令牌/多应用热更新稳定性、Redis 去重与令牌存储正确性、WebSocket 连接可靠性**。包含若干破坏性变更，升级前请务必阅读「升级须知」。

### 🌟 本版亮点

- **Webhook 防重放与多应用隔离**：生产环境强制分布式去重、多应用必须声明 `ExpectedAppId`、拦截语义与默认处理器路由收敛，杜绝跨应用串扰与事件永久丢失。
- **令牌与多应用热更新稳定性**：默认应用桥接改为「解析桥接」（DI 注入立即跟随运行时切换）、per-app 认证客户端真正生效、Memory/Redis 令牌键布局逐字节对齐、`OnConfigurationChanged` 不再阻塞配置回调线程。
- **Redis 去重与令牌存储正确性**：SeqID 改为容量窗口、`ClearCacheAsync` 真正删除、`rediss://` 真正启用 TLS、令牌键前缀可配置、运维诊断门面与指标补齐。
- **WebSocket 连接可靠性**：连接生命周期与调用方 `CancellationToken` 解耦、僵尸连接消除、分片消息边界保护、并发派发与存活/丢弃指标。

### ⚠️ 升级须知（破坏性变更 / 必须处理）

**Webhook**

- **生产环境默认禁止内存 Nonce 去重**：未注册分布式去重（`AddFeishuRedisDeduplicators()`）时，生产环境（`ASPNETCORE_ENVIRONMENT=Production`）启动直接失败。单实例部署请显式设置 `FeishuWebhook:AllowInMemoryNonceDedupInProduction=true`（会输出风险告警）。
- **多应用必须配置 `ExpectedAppId`**：`FeishuWebhook:Apps` 条目 > 1 时缺失该键将启动失败——它是防止 `EncryptKey` 误配导致跨应用串扰的唯一兜底。单应用部署仍可选。
- **拦截语义变更**：`BeforeHandleAsync` 返回 `false` 由原本的 `500`（可重试）改为 `200`（已消费并落去重标记，飞书不再重推）。如需「拦截后重推」，设置 `FeishuWebhook:InterceptionAckMode=Retryable`（响应 503，不落去重标记）。
- **必须注册全局默认处理器**：`Build()` 现在要求至少一个不带 appKey 的 `AddHandler<T>()`，否则启动失败（此前会静默用应用专属处理器作默认，导致其它应用事件被错误路由）。
- **应用专属拦截器不再静默屏蔽全局拦截器**：组合策略默认改为 `Merge`（全局先行 → 应用专属）。旧行为（某应用一旦注册专属拦截器就**完全丢弃**全局拦截器，使安全/审计横切在该应用上静默失效）可通过 `FeishuWebhook:InterceptorFallbackMode=AppOnly` 保留（此时启动期会 Warning 列出被屏蔽的全局拦截器）；另提供 `AppThenGlobal`（应用专属先行）。
- **解密超时由 400 改为 503**：解密超时此前被吞成 `null` → 400（终态，飞书不重推）；超时属可恢复的服务端问题，现按 503 让飞书重投，解密 CTS 同时链接 `RequestAborted`。
- **`IEnvironmentService` 缺失即启动失败**：它是「生产环境内存 Nonce 去重阻断」的唯一判据来源，缺失会导致生产锁**静默失效**，故要求其必然可解析。
- **空 `appKey` 的去重键不再退化为裸键**：改用固定哨兵前缀，消除跨应用同 ID 事件的碰撞面。

**令牌与多应用**

- **默认应用上下文桥接语义变更（默认生效）**：注入的 `IFeishuAppContext` 变为无状态转发代理，每次访问都取当前默认应用。若代码强转为具体 `FeishuAppContext`，请设 `FeishuAppOptions.ForwardDefaultAppContext=false` 恢复实例快照语义（需重启生效）。
- **含特殊字符的 AppKey 键布局变化**：`TokenKeyBuilder` 现转义 glob 元字符（`* ? [ ] : \`）。升级前请确认 AppKey 不含这些字符；否则旧令牌键在热更新后不再匹配。
- **自定义 `UserTokenStoreBase` 子类**：若覆写了 `KeyPrefix`，需改为委派 `TokenKeyBuilder.BuildKeyPrefix(appKey)`，与 Memory/Redis 端保持一致。
- **`OnConfigurationChanged` 不再同步等待清库完成**（此前最多约 10s）；需等待的宿主请以清库门撤除为完成信号自行轮询。清库完成前不恢复旧令牌的语义不受影响。
- **运行时添加的应用**：一旦由配置声明，其「运行时添加」标记会被撤销，之后从配置删除该应用不再被永久忽略。

**Redis**

- **新增 `FeishuRedis:SeqIdWindowCapacity`**（默认 100000，非正值启动失败）：SeqID 去重窗口由「TTL 时间窗口」改为「容量窗口」，`GetCacheCount()` / `GetMaxProcessedSeqId()` 语义收窄为窗口内真实值，不可用于推断剩余去重空间。
- **新增 `FeishuRedis:TokenKeyPrefix`**（默认 `feishu`）：令牌键前缀由硬编码改为可配置，多环境共用 Redis 时可隔离键空间。
- 含 `:` / `\` 的 AppKey 旧令牌键不再可达，建议受影响部署轮换 AppSecret（升级后首次取令牌会重新落库）。

**WebSocket**

- **连接生命周期不再跟随调用方 `CancellationToken`**：`ConnectAsync` 的令牌只约束「建连 + 认证」；终止连接请调用 `DisconnectAsync()` / `DisposeAsync()`，恢复接收请调用 `ReconnectAsync()`。
- **`IFeishuWebSocketClient.StartReceivingAsync` 已弃用**：接收循环由 `ConnectAsync` 统一管理；已有循环时为幂等 no-op，未连接时抛 `InvalidOperationException`。
- **`MessageReceived` 线程契约变更**：由「接收循环同步串行派发」改为「并发租约内派发，可能并发、可能乱序」。需要顺序/超时保护请改用 `IMessageHandler`。
- **`PingPongMessageHandler` / `HeartbeatMessageHandler` 构造函数移除了 `FeishuWebSocketOptions` 参数。**
- 配置上界收紧（启动期 fail-fast）：`Reconnect.TotalBudget ≤ 7 天`、`Reconnect.BaseDelayMs/MaxDelayMs ≤ 1 小时`、`MessageSizeLimits.MaxTextMessageSize ≤ 10MB`、`ConnectionTimeoutMs/AuthTimeoutMs/AuthGateTimeoutMs ≤ 5 分钟`。
- 入站报文不再全文入日志（改为长度 + 200 字符脱敏预览），连接 URL 日志整体剥离 query。

### ✨ 新增

- **Webhook**：`FeishuWebhook:AllowInMemoryNonceDedupInProduction`、`FeishuWebhook:InterceptionAckMode`、`FeishuWebhook:InterceptorFallbackMode`（默认 `Merge`）、`FeishuWebhook:NonceTtlSeconds`（显式配置时强制 `> TimestampToleranceSeconds` 的重放窗口不变量）；启动期选项校验（宿主启动失败而非首个请求 500）；`intercepted` / `intercepted_retryable` 指标标签；未匹配事件类型与软超时可观测（Warning + `unhandled` / `timeout_overshoot` 指标）；健康检查 `nonceDedup` 形态数据项（生产 + 内存 = `Degraded`）；启动 Summary 日志（去重形态、时间戳容差、Nonce TTL、重放窗口不变量、各应用处理器/拦截器注册自检）。
- **令牌/多应用**：清库链路可观测性 `PurgeTokenStoreFailureEvent`（EventId 5601，宿主可据此建告警）；热更新竞态门闸测试基建 `HotReloadRaceHarness`。
- **Redis**：运维诊断门面 `IRedisDeduplicationDiagnostics.GetSnapshotAsync` → `RedisDeduplicationDiagnosticsSnapshot`；Redis 指标 `feishu.redis.operation` / `feishu.redis.operation.duration` / `feishu.redis.scan.keys`（挂在既有 `Mud.Feishu` Meter）；健康检查注册可选（`registerHealthCheck=false`）。
- **WebSocket**：连接存活探针 `ConnectionLiveness`（`ReceiveLoopAlive` / `LastReceiveUtc` / `IdleMs` / `IsZombie`）；存活/丢弃指标 `feishu.websocket.receive.idle_ms` / `.loop_alive` / `.zombie` / `feishu.websocket.frames.discarded`（四个受控丢弃点全部接入计数）；健康检查 `data` 新增 `receive_loop_alive` / `last_receive_utc` / `idle_ms` / `is_zombie`；架构不变量守卫与 7 条契约守卫测试；`FeishuWebSocketServiceBuilder` 不再静默丢弃直接注册的 `IFeishuEventInterceptor`。

### 🐛 修复

- **进程崩溃**：`FeishuDeduplication:Mode=Distributed` 且未注册 Redis 时，去重工厂自解析导致 `StackOverflowException` 已修复；改为正常构建并告警「事件去重仍为内存实现」。
- **事件永久丢失**：Nonce 去重基础设施故障（Redis 连接/超时）不再伪装成 403 验签失败，改由 `FeishuDeduplicationFatalException`（`FailureKind=Server`）转 **503** 触发飞书重推；客户端断开（`OperationCanceledException`）不再被吞成「验签失败 403」写向已中止连接；处理器 `SupportedEventType` 不匹配不再静默丢失事件；解密超时不再被吞成 400。
- **Webhook 技术债**：内存 Nonce 去重补上容量上限（原为无界增长）；多处理器同时失败时补记**全部**异常（此前仅重抛首个）；删除不可达的补偿分支；消除处理器注册表的并发注册竞态；失败事件重试服务每轮结束显式清除 AppKey 上下文。
- **令牌/多应用**：默认应用 DI 桥接改为解析桥接（修复继续用旧凭据 / 指向已释放上下文）；per-app 认证客户端编译期直引，装配失败显式失败（修复「凭据发往错区域」）；Redis 令牌键前缀去预转义 + SCAN glob 字面量转义（修复清库/枚举/全用户清库永不命中）；OAuth 刷新失败分类收紧（瞬时故障不再误清 refresh token）；运行时添加的应用可被配置正确接管；凭据变更清库不再阻塞配置回调线程；未实例化应用的凭据变更也能被检出。
- **Redis**：`rediss://` 现在真正启用 TLS（此前明文连 TLS 端口）；`RedisFeishuEventDistributedDeduplicator` 补声明 `IDisposable`；事件去重 Lua 补 `tonumber(timestamp)` 护栏；`GetStatusAsync` 改用服务端 `TIME` 求差；令牌 SCAN 类 API 改为异步分批删除（500/批）；健康检查 PING 成功即 `Healthy`；`FeishuRedisFailureKind.InvalidArgument` 首次真实产生；清零 4 处编译警告。
- **WebSocket**：取消退出导致「连接正常但收不到事件」的僵尸连接；`StartReceivingAsync` 幂等守卫可创建第二条接收循环；分片超限丢弃不排空致边界失步；重连窗口未钳制致自动重连失效；入站完整报文未脱敏入日志；修复释放与事件派发期间的 `ObjectDisposedException` 及死锁竞态；socket 类型收敛到抽象 `WebSocket`；`ResolveMaxTextMessageBytes` 整型溢出；`IsConnected` 双真源。

### 📝 文档与测试

- `Mud.Feishu.Redis/README.md`：键布局改为实测样例（含双冒号与 `\:` 转义）、配置表补齐 `SeqIdWindowCapacity` / `TokenKeyPrefix`、新增「运维诊断」「可观测性」两节。
- `Tests/Mud.Feishu.Redis.Tests/README.md`：删除不存在的 `RedisFeishuEventDistributedDeduplicatorWithFallback` 章节、按实际结构/技术栈刷新。
- 新增契约守卫（7 条）并以「故意违规金丝雀」验证可拦截回归；WebSocket 新增存活/分片排空/装配重连/配置上界/压力等用例；`verify-build.ps1` 步骤 4 追加 `--filter "Category!=Stress"`。
- `AGENTS.md` / README 中多应用与令牌清库相关表述及 AppKey 命名约束同步。

## [3.0.0-rc2] - 2026-09-18

### 🌟 亮点

- ⚡ **原生 AOT 全面适配**：net8.0+ 一等公民支持 Native AOT 发布——全链路源生成 JSON 序列化与
  配置绑定、AOT 严格模式质量门禁保证 0 反射告警，附带端到端验证工程与完整文档。
- 🔐 **令牌与多应用管理三轮专项加固**：用户令牌可续期、401 恢复真正生效、
  凭据变更即清库、Memory/Redis 令牌键统一、配置热更新事务化、OAuth 失败语义分类等 60+ 项修复。
- 🪝 **WebSocket 可靠性**：事件处理失败不再静默吞异常（飞书将重发）、重连熔断、并发闸门、健康检查并发指标。
- 🗄️ **Redis 去重加固**：四类键统一构造与转义、Cluster 全节点覆盖、竞态 Lua 原子化、失败可分类、新增 Testcontainers 集成测试。
- 🛡️ **质量门禁**：`verify-build.ps1` 全新门禁（缓存自检、全 TFM 构建 + 诊断白名单、AOT 严格模式冒烟、TRX 测试计数断言），CI 同步接入。
- 📦 **依赖升级**：`Mud.HttpUtils` / `Mud.HttpUtils.Generator` 统一钉住 **2.0.7** （组件首个正式版系列，生成器修复源生成同名类型冲突 SYSLIB1031）。

### ⚠️ 升级须知（破坏性变更 / 行为变更）

**令牌与多应用**

- 令牌失效现在级联清除持久层存储——401 恢复才真正生效（此前重试仍用被拒旧令牌）。
- Redis 令牌键布局变更：`feishu:token:*` → `feishu:{appKey}:token*`，升级后旧键不再读取；
  `SingletonFeishuTokenStoreFactory` 已废弃，请改用 `PerAppRedisTokenStoreFactory`。
- `AddFeishuRedisTokenStore` 不再注册 `ITokenStore`/`IUserTokenStore` 单例，
  改用 `IFeishuTokenStoreFactory.Create(appKey)`。
- 租户请求的 401 恢复不再回退为用户级恢复（重试不会被注入用户令牌）。
- `GetAllApps()` 不再隐式实例化全部应用；后台令牌刷新默认仅覆盖默认应用，其余应用首次访问时增量注册。
  需要启动期预热请设 `WarmUpAllAppsOnStartup = true`。
- `SetDefaultApp` / `TrySetDefaultApp` / `DefaultAppKey` 现在真正生效，请复核运行期切换默认应用的调用点。
- `TryGet*` 令牌管理器解析器无默认应用时返回 null（不再抛异常）。
- 认证/取令牌请求改用本应用的命名客户端（多区域部署不再取错平台端点）；
  必要时以 `EnablePerAppAuthenticationClient = false` 降级。
- 配置热更新默认开启（`EnableConfigReload = true`），`BaseUrl`/`TimeoutSeconds` 变更无需重启；
  如需「配置变更需重启」的旧语义，显式设为 `false`。
- OAuth 刷新失败按可重试/不可重试分类：`invalid_grant` 等将清除 refresh token 并要求重新授权。
- 凭据变更即清库：热更新检测到 (AppId, AppSecret) 变更，立即清除该应用全部持久化令牌。
- 已删除死配置项：`EnableContextRetirement` / `PurgeStoreOnTokenInvalidation`（默认安全行为保留）。

**配置与 HTTP**

- **配置结构统一为嵌套（C# 代码破坏性）**：扁平属性收敛为分组对象——`FeishuWebSocketOptions` 的重连/证书
  改为 `Reconnect.*` / `Certificate.*`（`AutoReconnect`→`Reconnect.Auto`、`ReconnectDelayMs`→`Reconnect.BaseDelayMs`、
  `ValidateServerCertificate`→`Certificate.ValidateServerCertificate` 等），新增 `Certificate.Mode`（`Strict`/`Dev`/`Custom`）；
  `FeishuAppConfig` 的 `TimeOut`/`RetryCount`/`CircuitBreaker*` 与 `RedisOptions` 连接键同样收敛为
  `HttpRetry.*` / `CircuitBreaker.*` / `Connection.*`。**JSON 旧扁平键仍可绑定**（启动期自动回填到嵌套属性），
  C# 代码必须改用嵌套 API；各模块日志开关统一为 `Logging:LogLevel:*`（`EnableLogging` 已移除）。
- `FeishuAppConfig` 移除 `required`：AOT 源生成绑定要求，校验统一由 `Validate()` 承担。
- 增强 HttpClient 装配基线化：此前手工 `new` 静默取默认值的 10 个字段现与注册路径同源。
- `nuget.config` 收紧为包来源锁定（`<clear />` + `packageSourceMapping`）。

**WebSocket**

- 事件处理失败不再静默吞异常：ACK 返回 `code=500`，飞书服务端将重发——**请确保业务处理器幂等**。
- 同步 `Dispose()` 不再尝试停止服务；确定性停止请 `await StopAsync()` 或 `await DisposeAsync()`。
- 服务端 Pong 下发的 ClientConfig 不再覆盖本地重连策略（`PingInterval` 仍生效，钳制 5–30 秒）。
- 构造签名变更：`FeishuEventMessageHandler` 移除 `seqIdDeduplicator` 参数；
  `WebSocketBinaryMessageEventArgs.ProcessingTask` 移除。
- 重连协调器不再在持锁期间触发事件：订阅者在回调内**同步阻塞等待**重入 `TryReconnectAsync` 不再死锁，
  并发重连请求改为立即返回 `false`。**注意：回调内不得同步阻塞**（会占住重连闸门导致后续重连被跳过），
  耗时操作请自行 `Task.Run` 或改由 `ReconnectFailed`/`ReconnectLimitReached` 触发异步补偿。
- 旧连接接收循环的迟到异常不再误报为"新连接断开"（断线声明绑定 socket 身份）；
  "旧连接已关闭 + 新连接握手失败"场景下 `Disconnected` 事件不再丢失。
- `Dispose()` 之后调用连接/发送 API 现在确定性抛出 `ObjectDisposedException`（此前为随机抛出或偶发成功）；
  内部 `SemaphoreSlim` 不再随 `Dispose` 释放（消除在途 `Release()`/租约归还的 `ObjectDisposedException` 竞态，
  未访问 `AvailableWaitHandle`，无 OS 句柄泄漏）。
- `Reconnect.MaxDelayMs` 不再自动抬升到 `Reconnect.BaseDelayMs`：非法组合改由 `Validate()` 在启动期报错
  （此前赋值结果依赖配置绑定顺序）。
- 文本消息发送与接收统一按 **UTF-8 字节**计量（新增 `MessageSizeLimits.MaxTextMessageBytes`，
  0 = 3 × `MaxTextMessageSize` 自动推导）。默认值下属**放宽**：旧"字符语义"的合法消息全部继续通过；
  二进制发送补齐此前完全缺失的 `MaxBinaryMessageSize` 校验。
- 背压前移到接收路径：并发槽位耗尽（`MaxConcurrentHandlers`，默认 32）时接收循环被阻塞以施加 TCP 反压
  （排队任务数与消息副本数一并受上界约束）。需要旧行为可设 `MaxConcurrentHandlers = 0`。
- 分片文本消息的接收上限由"1MB 字节"改为与发送侧同源（默认 3MB 字节），不再误拒"1MB 字符级"合法消息。
- 关闭握手回显服务端下发的关闭码/描述（RFC 6455 §5.5.1），不再固定 `NormalClosure`；同步 `Dispose()` 的
  关闭握手超时后会强制中止并观察残留任务异常（不再遗留无人观察的任务）。
- **指标 API 变更**：`FeishuMetrics.WebSocketConnectionObserver` / `WebSocketBacklogObserver` 两个静态可写属性**已移除**，
  改为 `FeishuMetrics.RegisterWebSocketMetricsSource(appKeyProvider, activeConnectionsProvider, pendingMessagesProvider)`
  （返回注销令牌，`Dispose` 后停止采集）。原因：静态单值属性在多应用场景互相覆盖，且长期持有已释放的服务实例；
  新形态按注册实例聚合，AppKey 由提供器每次采集时读取（支持热更新）。自定义集成请迁移到新 API。
- **主机白名单（新默认，可能影响自定义网关）**：`ConnectAsync` 新增 `AllowedHostSuffixes` 主机校验，
  默认 `*.feishu.cn;*.larksuite.com`；连接白名单之外的主机会抛 `ArgumentException`。
  连接自建代理/本地测试端点时，请把主机加入该列表（支持 `*.` 通配后缀与精确主机名，分号分隔），
  或将该项置空表示不限制。
- **二进制帧副本入池**：`FeishuWebSocketClient` 的帧私有副本改由 `ArrayPool<byte>` 提供
  （消除每帧一次的 Gen0/LOH 分配，副本以 `(buffer, 0, count)` 三元组传递并在处理完成后归还池）；
  对外 API 不变，`WebSocketBinaryMessageEventArgs.Data` 仍为按帧精确长度的独立副本。

**DTO 重命名（修复 SYSLIB1031，AOT 源生成要求）**

- `DepartmentsV1.DepartmentLeader` → `DepartmentLeaderV1`、`DepartmentDetail` → `DepartmentDetailV1`、
  `DepartmentPathInfo` → `DepartmentPathInfoV1`
- `ApprovalExternal.ApprovalCreateViewers` → `ExternalCreateViewers`；
  `Drive.Folder.FileShortcutInfo` → `FileShortcutTargetInfo`；`TasksSections.TaskSummary` → `TaskSectionSummary`
- 移除嵌套类型 `AppTableViewProperty.AppTableViewPropertyHierarchyConfig`；
  `IFeishuV2TaskSections.GetTaskSectionsPageListByIdAsync` 返回类型变更为
  `FeishuApiPageListResult<TaskSectionSummary>`

**Redis 去重**

- `RedisOptions` 非法值（0/负 TTL/空前缀）改为启动期校验失败；事件去重亚秒 `ttl` 抛异常；
  `MarkAsCompletedAsync` 不再为不存在的键创建永久记录。
- SeqID 去重键增加 `scopeKey` 隔离维度（默认 `AppKey|MachineName`），多实例不再互相判重；
  `GetCacheCount`/`GetMaxProcessedSeqId` 语义收窄为 TTL 窗口内。
- `NonceFailureMode` 仅对 Redis 连接类故障生效；服务端/配置类错误直接抛出。
- 四类 Redis 键（事件/Nonce/SeqID/令牌）统一走 `RedisKeyBuilder` 构造（分段转义 `:`，杜绝跨段碰撞）。

### ✨ 新增

- **令牌存储加密**：`EncryptedTokenStore` 系列 + `FeishuAppOptions.EnableTokenEncryption`
  （默认关闭；未注册加密提供程序时降级明文并告警，解密失败按缓存未命中处理）。
- **多应用配置热更新**：`appsettings.json` 变更按 AppKey 增量应用；`BaseUrl`/`TimeoutSeconds` 运行期热更新，
  多区域切换无需重启。
- **AOT 安全 JSON 入口**：`FeishuJsonAot`（`JsonTypeInfo` 重载）+
  `FeishuJsonDefaults.ConfigureUserResolver` 自定义 Context 注册。
- **多应用 API**：`IFeishuAuthenticationFactory` / `PerAppFeishuAuthenticationFactory`、
  `IFeishuAppManager.ConfiguredAppKeys`、`AppInstantiated` 事件、`FeishuAppContextRetirement` 退休队列、
  `TokenKeyBuilder`、`FeishuOAuthErrorClassifier`。
- **WebSocket**：并发闸门 `FeishuWebSocketConcurrencyService`（`MaxConcurrentHandlers` 默认 32）、
  真实 `backlog` 指标、按 app_key 分组连接指标、`AllowCertificateNameMismatch`、
  `ProtocolKeepAliveInterval`（默认 20s）、统一去重中间件接入、健康检查并发指标与重连熔断态、
  `AckResponse`/`SubscriptionRequest` 强类型 DTO。
- **WebSocket（本轮加固）**：`MessageSizeLimits.MaxTextMessageBytes`（字节维度上限，0=自动推导）；
  已解析帧在处理异常时补 ACK `code=500`（服务端即时重投，不再等超时）；`WebSocketBinaryMessageEventArgs.ReceiveStartTime`
  现在真实赋值（`ReceiveDurationMs` 可用）；`FeishuWebSocketClient` 的 `AppKey`/认证闸门读取支持
  `IOptionsMonitor` 热更新（其余配置项需重启，已在 XML 注释中口径化）。
- **Redis**：`RedisKeyBuilder`、`FeishuRedisException` + `FeishuRedisFailureKind` 可分类失败契约、
  `RedisOptions.ValidateOnStart()`、Cluster 全节点聚合 `GetServers()`、Testcontainers 集成测试工程。
- **文档**：`documents/ErrorHandling.md`（下载方法错误契约与「HTTP 200 + JSON 错误体」残余风险）、
  `documents/ResponseCaching.md`（多应用缓存键隔离约束）；README 多租户部署指引。

### 🐛 修复（按模块摘要）

- **多应用/令牌**：401 恢复拿到同一被拒令牌；租户重试被注入用户令牌；默认应用状态分裂；
  Lazy 异常缓存自愈失效致应用永久毒化；热更新快照并发修改 / 节流漏比对字段 / 缺 IsDefault 校验；
  旧上下文令牌维护 Timer 泄漏；HostedService 启动期强制实例化阻断宿主启动；稳态 store 恢复永不命中；
  无过期存储值编造有效期；重复 `AddFeishuApp` 双重加解密；凭据变更清库在内存后端无效；
  热更新锁内同步阻塞 IO；恢复路径 IssuedAt 缺失；指标缺 appKey 维度；
  Redis 过期令牌 TTL=0 永不过期；Redis 连接串日志含口令等。
- **JSON / AOT**：net8+ 未覆盖类型抛 `NotSupportedException`（补链尾反射兜底 + 幂等 + 加锁）；
  开放泛型误标 `[HttpJsonSerializable]`（AOT006 / SYSLIB1030）；DataModels 7 组同名 DTO 触发
  SYSLIB1031（重命名修复）；低 TFM 7486 条 AOT006 噪音豁免。
- **HTTP / 配置**：`IFeishuAuthentication` 未注册；`BaseUrl`/`TimeoutSeconds` 不参与热更新；
  per-app 弹性策略固化注册期配置；解密失败完全静默（补节流告警）；
  `FailedEventRetryService` 反序列化选项失配；204 条 NU1603 版本漂移。
- **WebSocket**：健康检查并发指标缺失、重连无熔断、配置热更新不一致、协议保活硬编码、
  未接入统一去重中间件、ACK 恒 200 致事件永久丢失。
- **Redis**：空前缀 `ClearCacheAsync` 误删全库；`redis://`/`rediss://` 地址无法连接；
  Rollback/Mark 并发竞态（Lua 原子化）；超时判定依赖客户端时钟；Sorted Set 无界增长；
  `CancellationToken` 全链路失效；Cluster 单节点覆盖；键 `:` 跨段碰撞。

<details>
<summary>内部加固与质量基建明细</summary>

- **AOT 适配**：net8.0+ 全部源工程启用 AOT / 裁剪分析器与源生成 JSON、配置绑定，配置 DTO 改由
  `Validate()` 校验；WebSocket 二进制链路改为编译期协议模型；`verify-build.ps1` 以 AOT 严格模式
  冒烟并断言 0 反射告警；`FeishuJsonAot` / `FeishuJsonDefaults.ConfigureUserResolver` 提供 AOT 安全
  JSON 入口；`Demos/Mud.Feishu.AotVerification` 双 RID 端到端验证（JSON 序列化 / HTTP 客户端 /
  事件处理 / WebSocket 协议消息）。
- **同名 DTO 冲突（SYSLIB1031）**：源生成器按类型简单名生成元数据，DataModels 7 组同名 DTO 曾编译
  报错，且其余同名类型运行时静默退化为反射兜底（AOT 下失效）；已按命名空间语义重命名（见升级须知）
  并移除重复嵌套定义。
- **依赖组件（Mud.HttpUtils）**：打包防呆（Release 打包 + 包内 DLL 逐字节校验）与清单漂移防护；
  修复多处 DI 构造歧义与用户令牌复制丢失签发时间（导致过期提前量失效）等问题；组件侧新增机器护栏
  用例，AGENTS.md / README 依赖表同步。
- **质量门禁**：测试步骤改为逐（工程，TFM）运行并逐组合断言 TRX 计数，不再依赖中文输出；CI 构建
  日志断言诊断白名单全零；移除测试过滤器使架构守卫真实生效。
- **令牌多应用加固**：用户令牌续期独立读取 refresh token；Memory / Redis 令牌键布局统一为单一真相源；
  凭据变更即清库（进程级共享记账 + Redis SCAN 全量清理，Memory 与 Redis 语义统一）；热更新两阶段
  事务化并将凭据检测与清库 IO 移出锁内；OAuth 失败按可重试性分类；应用装配失败确定性失败并释放
  scope；新增键布局单一真相源、包版本唯一、配置属性集契约等守卫测试；令牌存储加密
  （`EncryptedTokenStore` 系列）与解密失败节流告警。
- **WebSocket 加固**：事件处理失败向上传播并回 `code=500` 让飞书重发；并发闸门、重连熔断器与健康
  检查并发指标；服务端 Pong 不覆盖本地重连策略；协议保活间隔可配置；接入统一去重中间件
  （EventId + SeqID 双重去重）；连接指标按 app_key 分组；关闭握手回显服务端关闭码；帧副本改由
  `ArrayPool<byte>` 提供。
- **Redis 加固**：四类键统一经 `RedisKeyBuilder` 构造（分段转义、长度上限、空前缀防护）；Cluster
  全节点覆盖；Rollback / Mark 并发竞态 Lua 原子化；SeqID Sorted Set 写入时刷新 TTL 并裁剪；超时
  判定改用服务端 `TIME`；`CancellationToken` 全链路生效；异常可分类（`FeishuRedisException` +
  `FeishuRedisFailureKind`）；新增 Testcontainers 真实 Redis 集成测试。
- **文档**：`documents/ErrorHandling.md`（统一响应模型与下载方法错误契约）、
  `documents/ResponseCaching.md`（多应用缓存键隔离约束）；README 多租户部署指引、「能力 ↔ 实现 ↔
  测试」映射表与令牌明文存储安全披露；移除不存在的 `RedisFeishuEventDistributedDeduplicatorWithFallback`
  及降级承诺。

</details>

## [2.1.5] - 2026-06-25

### ✨ Added

- **AI 文档处理**: 新增飞书 AI 文档处理模块（智能文档处理能力，支持 17 种证件识别）
  - 添加简历信息解析接口及数据模型
  - 添加名片识别接口及数据模型
  - 添加合同字段识别接口及数据模型
  - 添加营业执照识别接口及数据模型
  - 添加增值税发票识别接口及数据模型
  - 添加驾驶证识别接口及数据模型
  - 添加食品经营许可证识别接口及数据模型
  - 添加食品生产许可证识别接口及数据模型
  - 添加身份证识别接口及数据模型
  - 添加出租车发票识别接口及数据模型
  - 添加火车票识别接口及数据模型
  - 添加车辆行驶证识别接口及数据模型
  - 添加银行卡识别接口及数据模型
  - 添加中国护照识别接口及数据模型
  - 添加台湾居民来往大陆通行证识别接口及数据模型
  - 添加港澳居民来往内地通行证识别接口及数据模型
  - 添加机动车发票识别接口及数据模型
  - 添加健康证识别接口及数据模型
  - 支持租户级别和用户级别调用
- **AI 文字识别 (OCR)**: 新增飞书基础图片 OCR 识别功能
  - 添加基础图片 OCR 识别接口及数据模型
- **AI 语音转文字**: 新增飞书语音转文字（Speech-to-Text）功能
  - 添加飞书语音文件识别接口及数据模型
  - 添加流式语音识别接口及数据模型
- **AI 翻译**: 新增飞书翻译相关接口（语言检测、文本翻译等）
- **搜索模块**: 新增飞书搜索（Search）模块
  - 添加飞书 V2 文档 Wiki 搜索接口及数据模型
  - 添加飞书 V2 套件搜索接口及数据模型
  - 添加飞书 V2 搜索数据源（DataSource）全套接口及数据模型
  - 添加搜索数据源索引管理接口（创建、批量创建等）及数据模型
  - 添加数据模式（Schema）相关 API 接口及数据模型

### 🔧 Changed

- **AI 接口命名空间**: 调整 AI 接口命名空间，统一归入 `Interfaces/AI` 目录结构
- **邮件组**: 移除邮件组接口定义（功能下架，相关 API 不再对外暴露）
- **机动车发票识别**: 重构机动车发票识别数据模型结构，新增健康证识别支持

### 🐛 Fixed

- 修复飞书消息序列化与测试用例相关问题
- **SpeechToText**: 将 `SpeechStreamConfig` 移动到独立文件，便于维护

### 🧪 Tests

- 调整测试项目目标框架与依赖配置，统一测试项目环境

### 📝 Docs

- 修正 README 示例代码中错误的配置变量名

## [2.1.4] - 2026-06-03

### ✨ Added

- **邮箱别名管理**: 新增飞书邮箱别名管理功能
  - 添加邮箱别名创建接口及数据模型
  - 添加邮箱别名查询接口及数据模型
  - 添加邮箱别名删除接口
  - 添加邮箱地址状态查询接口
- **公共邮箱管理**: 新增飞书公共邮箱管理功能
  - 添加公共邮箱创建接口及数据模型
  - 添加公共邮箱查询、更新、删除接口
  - 添加公共邮箱成员管理全套API接口与数据模型
  - 添加公共邮箱别名管理API接口与数据模型
- **邮件组管理**: 新增飞书邮件组管理功能
  - 添加邮件组创建、删除、更新、查询、列表接口及数据模型
  - 添加邮件组成员管理API（创建、删除、获取、分页列表、批量创建、批量删除）
  - 添加邮件组权限成员管理API（创建、删除、获取、批量查询）
  - 添加邮件组别名管理API（创建、删除、获取列表）
  - 添加邮件组管理员批量操作API（批量创建、批量删除、分页列表）
- **邮箱联系人管理**: 新增飞书邮箱联系人管理功能
  - 添加邮箱联系人增删改查分页管理功能
  - 支持租户级别和用户级别的调用
- **邮箱收信规则管理**: 新增飞书邮箱收信规则管理功能
  - 添加收信规则创建、删除、更新、列表、排序接口及数据模型
- **用户邮箱事件订阅**: 新增用户邮箱事件订阅功能
  - 添加订阅、获取订阅状态、取消订阅用户邮箱事件接口
- **用户邮箱邮件管理**: 新增用户邮箱邮件管理功能
  - 添加发送用户邮箱邮件接口及数据模型
  - 添加获取用户邮箱邮件详情接口及数据模型
  - 添加分页列表用户邮箱邮件接口（支持按文件夹、未读状态、标签等查询）
  - 添加按卡片获取用户邮箱邮件接口
- **邮箱文件夹管理**: 新增用户邮箱文件夹管理功能
  - 添加创建、删除、更新、获取、列表邮箱文件夹接口及数据模型
  - 添加批量删除邮箱文件夹接口
- **邮箱标签管理**: 新增用户邮箱标签管理功能
  - 添加创建、删除、更新、获取、列表邮箱标签接口及数据模型
  - 添加批量删除邮箱标签接口
- **邮件会话管理**: 新增用户邮箱会话管理功能
  - 添加获取、更新、列表、移动邮箱会话接口及数据模型
  - 添加批量删除邮件会话接口
- **邮件模板管理**: 新增飞书邮件模板管理功能
  - 添加邮件模板创建、删除、更新、获取、列表接口及数据模型
  - 添加列出可发信邮箱接口及数据模型
  - 添加邮件附件下载相关数据模型
- **日历模块**: 新增飞书日历管理功能
  - 新增租户级/用户级日历管理API
  - 新增日历ACL管理API
  - 新增日程管理API
- **视频会议**: 新增视频会议租户告警记录查询功能
  - 添加告警联系人和告警记录数据模型
  - 添加分页查询告警记录接口

### 🔧 Changed

- **模块注册**: 新增邮箱模块支持，注册邮件模块服务
- **接口命名规范**: 修正邮件组接口方法命名，统一为驼峰命名规范
  - 部分更新接口重命名为UpdateMailGroupPartialAsync
  - 全量更新接口重命名为UpdateMailGroupAsync
- **数据模型结构**: 调整邮件模板相关数据模型结构，移动到对应子目录

### 📝 Docs

- 新增飞书邮箱全套API文档
- 更新飞书视频会议API文档
- 更新日历相关API文档
- 统一API文档标题中的令牌术语
- 修正Exchange绑定接口文档与注释

## [2.1.3] - 2026-05-22

### ✨ Added

- **视频会议导出**: 新增飞书视频会议 V1 导出功能
  - 添加飞书视频会议 V1 导出接口定义
  - 添加会议列表导出 API 及数据模型
  - 添加参会人明细导出接口及数据模型
  - 添加参会人会议质量数据导出接口
  - 添加会议预约数据导出和查询 API
  - 添加下载导出文件 API 接口
- **会议室层级**: 新增飞书会议室层级管理功能
  - 添加会议室层级创建接口及数据模型
  - 添加删除会议室层级接口
  - 添加更新会议室层级接口及请求模型
  - 添加获取会议室层级详情接口
  - 添加批量查询会议室层级详情 API
  - 添加分页查询会议室层级列表 API
  - 添加搜索会议室层级 API 及数据模型
- **会议室**: 新增飞书会议室管理功能
  - 添加飞书租户视频会议室 API 接口定义
  - 添加会议室管理 API 及数据模型
  - 添加删除会议室接口
  - 添加更新会议室接口
  - 添加获取会议室详情接口及数据模型
  - 添加批量获取会议室 API 支持
  - 添加分页查询会议室列表 API
  - 添加搜索会议室 API
- **会议室配置**: 新增飞书会议室配置相关功能
  - 添加飞书会议室配置相关接口和数据模型
  - 添加创建范围配置 API 及请求模型
  - 添加获取范围配置 API 及数据模型
  - 添加获取预约配置表单 API 及数据模型
  - 添加更新预约配置表单 API 支持
  - 添加获取预约配置管理员 API 及数据模型
  - 添加更新预约配置管理员 API 支持
  - 添加获取停用状态变更通知配置 API
  - 添加更新停用通知配置 API 及数据模型
- **视频会议数据查询**: 新增飞书视频会议数据查询功能
  - 添加飞书会议数据查询 API 及数据模型
  - 添加会议质量和预约数据模型及 API

### 🔧 Changed

- **模块注册重构**: 重构模块注册逻辑，新增视频会议和认证授权模块
- **会议室预定配置**: 整理会议室预定配置的数据模型结构

### 📝 Docs

- 修复视频会议文档注释并更新接口参数

## [2.1.2] - 2026-05-11

### ✨ Added

- **视频会议录制**: 新增飞书会议录制 API 接口和相关数据模型
  - 添加会议录制 API 接口及请求模型
  - 添加会议录制 API 和相关数据模型
  - 添加设置会议录制权限 API 和相关模型
  - 添加租户级别和用户级别的会议录制接口
- **视频会议报告**: 新增视频会议报告功能
  - 添加视频会议报告相关模型和接口
  - 添加获取每日报告 API 及响应模型
  - 添加获取 Top 用户报告 API 及响应模型

### 🔧 Changed

- **HTTP 客户端增强**: 增强 FeishuHttpClient 的 HTTP 客户端选项支持

### 🧹 Chore

- 更新 Mud.HttpUtils 相关包到 1.7.1 版本

## [2.1.1] - 2026-05-11

### ✨ Added

- **视频会议**: 新增飞书视频会议 V1 模块支持
  - 新增飞书视频会议接口及实现
  - 添加预约会议相关数据模型和接口
  - 添加删除预约接口并完善预约文档
  - 添加更新预约接口及相关数据模型
  - 添加获取预约详情功能
  - 添加获取活跃会议相关数据模型和接口
  - 添加会议相关数据模型和接口
  - 添加会议分页列表接口及相关模型
  - 添加会议搜索功能及相关模型
  - 添加邀请参会人接口及相关数据模型
  - 添加移除会议用户接口及相关模型
  - 添加设置主持人接口并重构参会人管理

### 🔧 Changed

- **数据模型重构**: 将 Meeting 类重构为继承 MeetingBaseInfo
- **多应用配置重构**: 重构飞书多应用配置注册逻辑
- **视频会议重构**: 重命名预约会议相关类和方法

### 🧹 Chore

- 移除冗余的会议信息代码

## [2.1.0] - 2026-05-01

### ✨ Added

- **日历管理**: 新增飞书日历管理模块支持
  - 添加飞书日历 V4 接口定义
  - 添加日历创建相关数据模型和接口
  - 添加主日历查询接口及相关数据模型
  - 添加批量获取主日历信息功能
  - 添加获取日历信息的接口方法
  - 添加批量查询日历信息接口
  - 添加更新日历接口及数据模型
  - 增加查询日历列表接口及默认分页大小调整
  - 添加删除共享日历接口
  - 添加搜索日历接口
  - 添加日历订阅和取消订阅接口
- **日历 ACL**: 新增日历访问控制列表功能
  - 添加飞书日历 ACL 接口定义
  - 添加日历访问控制列表功能并重构相关类
  - 添加日历 ACL 创建和删除事件支持
- **日历日程**: 新增日历日程管理功能
  - 添加日历日程相关数据模型和接口
  - 添加获取日程接口及相关数据模型
  - 添加获取日程分页列表功能
  - 添加日历日程搜索功能及相关模型
  - 添加更新日程功能并重构响应模型
  - 添加租户和用户日历事件接口
  - 添加回复日程接口及请求模型
  - 添加取消订阅日程变更事件接口
  - 添加获取重复日程实例接口及响应模型
  - 添加获取日程实例视图相关数据模型和接口
- **日程参与人**: 新增日程参与人管理功能
  - 添加日程参与人相关功能接口及数据模型
  - 添加获取日程参与人列表接口
  - 添加获取日程参与群成员列表接口
- **会议群**: 新增会议群管理功能
  - 添加创建会议群接口及响应模型
  - 添加解绑会议群接口方法
  - 添加创建会议纪要和会议群响应模型及接口
- **请假日程**: 新增请假日程管理功能
  - 添加创建请假日程接口及模型
  - 添加删除请假日程接口
- **会议室**: 新增会议室相关功能
  - 添加会议室忙闲查询功能及相关数据模型
  - 添加会议室日程查询相关数据模型和接口
  - 添加会议室日程实例回复接口
  - 新增查询日历忙闲信息接口
  - 添加批量查询日历忙闲信息接口
- **Exchange 集成**: 新增 Exchange 账户绑定功能
  - 添加 CalDAV 配置生成功能
  - 添加 Exchange 账户绑定到飞书账户功能
  - 添加删除 Exchange 绑定接口
- **日历事件回调**: 新增日历相关事件回调支持
  - 添加日历变更事件类型和处理类
  - 添加日程变更事件支持
  - 添加第三方会议室日程变动事件支持
  - 添加会议室状态变更事件支持
- **多维表格**: 添加飞书多维表格工作流接口定义

### 🔧 Changed

- **日历模块重构**: 重构日历模块文件结构，将数据模型按功能分类整理
- **Exchange 重构**: 重命名 Exchange 绑定响应模型并添加查询接口
- **云文档重构**: 重命名评论接口以匹配驱动模块命名规范

### 📦 Build & Config

- 更新所有 demo 项目中的 Mud.Feishu 相关包至 2.1.3 版本
- 更新项目版本号至 2.1.3

### 📝 Docs

- 新增多维表格、电子表格和画板模块文档
- 添加飞书云文档相关接口文档
- 更新多个 README 文档，完善功能说明和配置示例

## [2.0.9] - 2026-04-24

### ✨ Added

- **云文档权限管理**: 新增飞书云文档权限管理功能
  - 添加云文档协作者权限相关模型和接口
  - 添加批量增加协作者权限接口
  - 添加删除云文档协作者权限接口
  - 添加云文档所有者转移功能
  - 添加判断用户云文档权限接口
  - 添加获取云文档权限设置接口
- **云文档密码**: 新增云文档密码功能支持
  - 添加云文档密码设置功能
  - 添加云文档密码刷新和停用接口
- **云文档订阅**: 新增云文档文件订阅功能
  - 添加文件订阅功能相关接口和模型
  - 添加更新文件订阅状态的接口
  - 更新用户订阅接口
- **云文档评论**: 新增飞书云文档评论功能
  - 添加飞书文档评论相关数据模型和接口
  - 支持批量获取评论
  - 添加解决/恢复评论接口
  - 添加文件评论表情回复支持
  - 添加全文评论接口及模型
  - 添加创建文件评论回复的接口
  - 添加更新和删除文件评论回复的接口
  - 添加评论表情回应接口
- **画板 (Board)**: 新增飞书画板功能模块
  - 添加画板主题接口及相关数据模型
  - 添加画板缩略图下载和语法解析接口
  - 添加创建画板节点接口及响应模型
  - 添加画板 V1 租户和用户接口
- **事件回调**: 更新事件处理器以使用新的事件回调模块

### 🔧 Changed

- **画板重构**: 重构画板相关数据模型和接口，优化模型文件结构
- **评论重构**: 统一云文档评论响应模型类名以 Result 结尾，重构评论接口支持批量获取
- **去重器重构**: 重构缓存清理逻辑并添加异步清空方法

### 🐛 Fixed

- 修复 RedisFeishuSeqIDDeduplicator 同步方法阻塞问题

### 📦 Build & Config

- 更新多个演示项目的依赖包版本至 2.0.8
- 更新项目版本号至 2.0.9

### 📝 Docs

- 更新云文档评论接口文档，添加分页获取回复方法和租户/用户实现说明

## [2.0.8] - 2026-04-12

### ✨ Added

- **EventCallback 项目**: 新增独立的事件回调处理项目
  - 重构数据模型结构，将原有抽象层的数据模型迁移至 EventCallback 项目
  - 添加项目文档说明
- **飞书 V2 事件头支持**: 添加对飞书 v2.0 事件 Header 的强类型支持
  - 支持飞书 V2 事件头解析
  - 统一事件处理器默认 Header 类型
- **云文档事件订阅**: 新增飞书云文档事件订阅功能
  - 添加云文档事件订阅接口
  - 添加云文档事件订阅状态查询功能
  - 添加取消用户云文档事件订阅接口
  - 添加查询用户云文档事件订阅状态功能
- **云文档事件回调**: 新增云文档相关事件支持
  - 添加文件已读和文件编辑事件支持
  - 添加文件协作者权限申请和添加事件支持
  - 添加文档协作者移除事件支持
  - 添加文件删除和回收站事件支持
  - 添加文件评论新增事件支持
- **多维表格自动化流程**: 新增自动化流程相关功能
  - 添加自动化流程相关接口及数据模型
  - 添加更新自动化流程状态接口支持
  - 添加获取工作流列表接口
  - 添加多维表格字段和记录变更事件支持
- **审批订阅**: 添加飞书 V4 审批订阅接口
- **多应用隔离**: 实现多应用隔离的事件处理机制
- **配置验证**: 添加配置选项验证器并集成到 DI 容器
- **WebSocket 增强**:
  - 添加配置验证器并优化消息处理性能
  - 添加心跳超时事件并优化连接管理
  - 重构 WebSocket 客户端并添加心跳管理和消息队列功能
- **中间件**: 添加请求耗时日志记录并优化签名验证配置

### 🔧 Changed

- **命名空间重构**: 重构命名空间结构并更新发布脚本
  - 重命名事件处理命名空间并迁移事件类型常量
  - 更新 HandlerNamespace 路径
- **事件处理器重构**:
  - 更新事件处理类并迁移 EventData 文件
  - 更新事件结果类的 HeaderType 配置
  - 更新事件处理类并修正文档链接
  - 移除冗余构造函数
- **Webhook 重构**:
  - 重构签名验证逻辑并扩展应用配置选项
  - 重构多应用键上下文传播机制
  - 移除 ISetAppKeyAware 接口及相关实现
- **配置验证重构**: 重构配置验证逻辑并移除数据注解
- **Drive 重构**: 重构云文档订阅相关模型文件路径
- **代码清理**: 清理冗余代码，移除未使用的命名空间引用

### 🐛 Fixed

- 修复并发测试中的变量捕获问题并简化 .gitignore

### 🧪 Tests

- 添加 FeishuWebhook 配置验证的集成测试和单元测试
- 调整重试延迟测试范围以适应 CI 环境

### 📦 Build & Config

- 更新依赖包版本至 1.7.0 并调整项目引用结构
- 更新 Mud.HttpUtils 依赖至 1.6.6 版本
- 删除 appsettings.example.json 配置文件

### 📝 Docs

- 添加类和方法注释以提升代码可读性
- 更新接口文档链接

## [2.0.7] - 2026-04-07

### ✨ Added

- **WebSocket 增强**: 全面增强 WebSocket 连接管理和错误处理
  - 新增消息处理状态跟踪功能
  - 添加重连状态重置功能
  - 实现重连协调器，优化重连逻辑和心跳间隔
  - 增强连接管理，支持更完善的错误处理机制
- **Webhook 增强**: 新增异步验证方法和 IP 白名单支持
  - 添加异步验证方法以提升性能
  - 支持 IP 白名单配置，增强安全性
- **分布式去重服务重构**: 重构分布式去重服务，支持状态机和事件处理生命周期
  - 实现状态机模式，支持 Processing → Completed 状态转换
  - 添加事件处理超时和回滚机制
  - 支持处理状态跟踪和异常恢复
- **指标收集优化**: 使用并发集合优化指标收集性能
  - 采用 `ConcurrentDictionary` 替代传统锁机制
  - 提升高并发场景下的性能表现
- **工具类扩展**: 添加 HttpClient 扩展方法用于 JSON 序列化配置
  - 新增 `ConfigureJsonSerializerOptions` 扩展方法
  - 简化 JSON 序列化配置流程

### 🔧 Changed

- **Token 管理重构**: 将令牌格式化逻辑提取到 TokenUtils 工具类
  - 统一 `FormatBearerToken` 和 `RemoveBearerPrefix` 方法
  - 消除代码重复，提升代码可维护性
- **签名验证优化**: 移除废弃方法并优化签名验证逻辑
  - 清理过时的验证方法
  - 简化验证流程，提升性能
- **WebSocket 重构**: 重构 WebSocket 相关类的命名空间
  - 统一命名空间结构
  - 优化代码组织
- **测试重构**: 重构订阅验证测试以使用加密密钥提供者
  - 更新测试用例以适配新的验证器架构
  - 提升测试覆盖率和代码质量

### 🐛 Fixed

- 修复大量空引用警告，提升代码健壮性
- 修复 `FeishuWebhookService` 异步方法未等待的问题
- 修复重连逻辑中的心跳间隔问题

### 🧪 Tests

- 新增 WebSocket 核心功能测试
  - 添加 Bug 修复验证测试
  - 新增指数退避重连策略测试
  - 添加重连协调器测试
- 新增去重服务测试
  - 添加统一去重中间件测试
  - 新增配置选项测试
  - 添加降级告警服务测试
- 新增指标收集测试
  - 优化并发性能测试用例

### 📦 Build & Config

- **CI/CD 增强**: 增强发布流程并支持多版本 .NET 构建
  - 支持 .NET 6.0、8.0、10.0 多版本并行构建
  - 优化构建和测试配置

## [2.0.6] - 2026-04-05

### ✨ Added

- **多维表格高级权限**: 新增飞书多维表格高级权限管理功能
  - 新增高级权限接口及相关数据模型
  - 添加角色成员管理相关接口及模型
  - 新增批量添加协作者接口及请求模型
  - 添加自定义角色查询、更新和删除接口
- **多维表格仪表盘**: 新增飞书多维表格仪表盘功能
  - 新增仪表盘接口定义
  - 添加仪表盘复制功能及相关数据模型和接口
- **多维表格表单**: 新增表单管理功能
  - 添加表单字段更新相关模型和接口
  - 新增更新表单元数据接口及相关模型
  - 添加表单升级接口及相关数据模型
- **多维表格字段**: 新增字段管理功能
  - 添加创建字段编组功能接口及模型
  - 新增删除字段接口及响应模型
  - 添加更新字段接口
- **多维表格记录**: 新增记录批量操作功能
  - 添加批量新增记录接口及相关数据模型
  - 新增批量获取记录接口及相关模型
  - 添加批量删除记录接口及相关模型
  - 新增删除记录接口及响应模型
- **用户认证**: 添加飞书用户认证中间件及相关服务配置

### 🔧 Changed

- **授权机制重构**: 统一使用 Token 属性声明授权方式
  - 将 Token 属性从 TokenType 枚举改为字符串类型
  - 移除接口中的 Header 属性，统一使用 Token 属性设置授权
- **多维表格重构**: 优化模型和接口结构
  - 重构角色相关请求模型文件结构
  - 重构表单字段相关接口及模型
  - 重构字段操作请求模型并优化目录结构
  - 重构记录操作请求模型并更新接口
- **文档接口**: 将文档块接口移动到主命名空间
- **演示项目**: 将演示项目从 NuGet 包引用改为本地项目引用

### 🐛 Fixed

- 修正 Bitable 注册方法名称
- 更新依赖版本并添加请求体加密方法

### 📚 Documentation

- 更新 NuGet 包下载量徽章样式

### 🧪 Tests

- 添加多维表格接口的单元测试

### 📦 Build & Config

- 更新项目版本至 2.0.6 并调整 token 管理器参数
- 更新演示项目依赖项，添加飞书认证包

## [2.0.5] - 2026-03-27

### ✨ Added

- **电子表格**: 新增飞书电子表格管理模块支持
  - 工作表管理：创建、查询、更新、删除工作表
  - 行列操作：插入、更新、移动、删除行列
  - 单元格功能：合并、查找、样式设置、数据读写
  - 筛选功能：筛选视图创建与管理、筛选条件设置
  - 数据保护：保护范围设置与管理
  - 数据验证：验证规则设置与管理
  - 条件格式：格式规则创建与管理
  - 浮动图片：图片插入、查询、删除
- **多维表格**: 新增飞书多维表格管理模块支持
  - 多维表格元数据获取与更新
- **用户认证**: 新增飞书用户认证模块
  - 用户认证中间件和上下文服务
  - 用户认证测试项目
- **演示项目**: 新增飞书任务管理演示项目

### 🔧 Changed

- **电子表格重构**: 重构飞书电子表格相关模型和接口
  - 重构飞书表格相关模型和接口
  - 调整飞书电子表格接口命名空间并新增租户和用户接口
  - 重构单元格操作相关模型和接口
  - 重命名单元格相关类以更准确描述功能
  - 重构筛选条件相关模型和接口
  - 统一筛选视图响应模型并添加获取接口
  - 重构飞书电子表格数据验证接口
- **认证重构**: 重构用户令牌管理器和依赖项
  - 将 token 格式化方法移至 TokenUtils 工具类
- **Webhook 重构**: 重构验证器基类与工具类
  - 使用环境服务替代直接环境变量访问
- **测试优化**: 使用环境服务模拟替换环境变量设置
- **项目优化**: 移除冗余的 EnsureUserContext 调用并调整项目引用

### �📚 Documentation

- 更新 README 文档说明
- 更新依赖项文档和版本说明
- 更新接口文档注释并添加数据保护相关模型
- 完善 xml 注释

### 📦 Build & Config

- 更新前端依赖版本
- 更新依赖包版本至 10.0.5 并添加认证测试

## [2.0.4] - 2026-03-19

### ✨ Added

- **文档管理**: 新增飞书文档管理模块支持
  - 添加飞书文档接口及数据模型
  - 新增文档块模型及接口支持
  - 添加获取和批量更新文档块接口及模型
  - 新增获取文档块子块分页列表接口
  - 添加批量删除块功能及相关模型
  - 新增内容转换功能支持
- **知识库**: 新增知识库管理功能
  - 添加知识空间和节点管理接口
  - 新增节点标题更新和复制功能
  - 添加节点管理接口和测试
  - 新增移动云文档至知识空间功能
- **云盘管理**: 新增飞书云盘管理 API 服务支持
  - 添加云空间文件夹相关接口及数据模型
  - 新增云空间文件接口及元数据模型
  - 添加文件版本管理功能
  - 新增文件上传功能支持
  - 添加文件导入导出任务相关数据模型和接口
  - 新增创建文件快捷方式相关模型和接口
  - 添加搜索云文档功能及相关数据模型
  - 新增获取云文档点赞者列表接口
- **任务模块**: 添加任务和卡片模块并重构服务注册逻辑
- **认证增强**: 为 FeishuAuthenticationException 添加可恢复标志构造函数
- **枚举配置**: 将枚举和配置类提取到独立文件

### 🔧 Changed

- **项目重构**: 重构请求和响应模型的文件结构
  - 重构文档块模型文件结构，将块模型移动到 Common 目录并删除原 Blocks 目录
  - 重构文件操作接口及模型命名
  - 重构代码以优化文件上传请求模型和 HTTP 客户端
- **测试优化**: 统一测试中数据模型命名空间并优化测试代码
- **数据库优化**: 为收藏节点添加索引优化查询性能

### 🐛 Fixed

- **消息发送**: 修复发送 Multipart/form-data 请求时 request.Content 的值问题
- **Webhook**: 修复飞书回调配置通不过的问题，在 FeishuEventDecryptor 中指定 EventType

### 📦 Build & Config

- **依赖更新**: 更新多个项目的依赖版本

### 🧪 Tests

- **新增测试**: 添加飞书 Webhook 模块的单元测试

## [2.0.3] - 2026-02-26

### ✨ Added

- **考勤管理**: 新增完整的考勤管理功能
  - 休假发放记录相关数据模型和接口
  - 打卡流水记录相关模型和接口
  - 考勤归档报表相关接口及数据模型
  - 补卡相关数据模型和接口
  - 用户人脸照片上传接口
  - 用户设置查询接口
  - 考勤审批相关数据模型及接口
  - 审批状态更新接口及数据模型
  - 审批结果写入功能及数据模型
  - 考勤统计相关数据模型和接口
  - 查询统计数据相关模型和接口
  - 查询统计表头接口及请求模型
  - 用户人脸识别信息管理相关接口和模型
  - 考勤组相关接口及数据模型
- **审批功能**: 重构审批实例预览功能
- **卡片功能**: 更新卡片元素请求字段名
- **任务功能**: 飞书任务自定义字段更新接口返回类型

### 🔧 Changed

- **项目结构**: 重构项目结构，调整命名空间和文件位置
- **接口规范**: 统一接口方法命名规范
- **考勤接口**: 重构考勤组接口继承结构，重命名考勤用户管理接口并添加下载功能
- **令牌管理**: 重构令牌管理模块并更新依赖版本
- **国际化**: 重构国际化资源模型并更新相关引用
- **测试结构**: 重构测试文件目录结构，将相关测试文件移动到对应功能模块目录下
- **Demo 项目**: 更新 demo 项目以使用包引用而非项目引用
- **响应模型**: 重构响应模型

### 🐛 Fixed

- **用户组成员**: 修正类名拼写错误并更新相关引用
- **测试用例**: 修复测试用例并完善测试数据
- **活动订阅**: 修正活动订阅接口返回类型并完善测试用例

### 📚 Documentation

- **代码注释**: 为测试类和缓存方法添加 XML 注释
- **README**: 更新 README 文档结构和内容

### 📦 Build & Config

- **依赖更新**: 更新 Mud.HttpUtils 依赖至 1.5.3 版本
- **构建配置**: 忽略测试项目的构建输出目录

### 🧪 Tests

- **完善测试用例**: 为多个模块添加和完善测试用例
  - 聊天群组和标签页测试
  - 飞书任务单元测试
  - 飞书用户和用户组接口测试
  - 飞书职位和职级接口测试
  - 员工类型相关接口测试
  - 飞书部门接口测试
  - 任务板块相关测试
  - 任务列表接口测试
  - 飞书任务附件和评论测试
  - 群公告相关接口测试
  - 卡片接口测试
  - 飞书审批任务相关测试
  - 审批查询接口测试
  - 审批评论相关测试
  - 飞书审批相关接口测试
  - 审批消息接口测试

## [2.0.2] - 2026-01-30

### ✨ Added

- **性能指标监控**: 添加完整的性能指标收集功能
  - 新增 MeterExtensions、FeishuMetrics 和 FeishuMetricsHelper 类
  - 在 TokenManager、HttpClientUtils 等关键组件中添加指标记录
- **WebSocket 和 Webhook 指标**: 为 WebSocket 和 Webhook 添加专项指标监控
  - WebSocket 连接数统计和认证/事件处理指标
  - Webhook 签名验证、事件解密和处理指标
  - 事件去重命中指标
  - 支持 WebSocketConnectionCountProvider 获取实时连接数
- **调试日志增强**: 增加响应内容调试日志和错误处理
  - 添加调试日志记录原始响应内容
  - 在 JSON 反序列化失败时提供更详细的错误信息
- **测试控制器**: 新增日志测试控制器和网络测试控制器

### 🔧 Changed

- **依赖更新**: 更新 Mud.HttpUtils 依赖至 1.5.2 版本
- **HTTP 客户端优化**:
  - 增强异常处理和日志记录
  - 改进安全配置和连接池设置
- **认证优化**: 优化令牌获取逻辑并移除冗余指标记录
  - 改为直接从缓存获取令牌时记录指标
- **配置简化**: 移除断路器功能及相关代码
  - 移除断路器配置项和文档说明
  - 简化代码结构和 Webhook 服务
- **时间戳验证**: 优化时间戳验证逻辑，优先使用应用特定配置
- **Demo 项目**: 调整 Demo 项目的配置和依赖

### 🐛 Fixed

- **JobLevel 接口**: 将 JobLevel 接口的 name 参数改为可空类型
- **JobFamilies 接口**: 允许 GetJobFamilesListAsync 的 name 参数为 null

### 📚 Documentation

- 更新 README 文档说明指标使用方式
- 添加日志测试相关文档

### 📦 Build & Config

- 更新项目版本至 2.0.2

## [2.0.1] - 2026-01-28

### 🚨 BREAKING CHANGE

- **移除 FeishuOptions 类** - 完全移除旧的配置类，所有场景统一使用 `FeishuAppConfig`
- **多应用架构支持** - API 签名和配置方式发生变化，需要迁移
- **配置系统重构** - 重试机制和 Token 管理配置方式改变

### ✨ Added

- **多应用支持**: 支持配置和管理多个飞书应用
- **应用上下文切换**: 新增 `IFeishuAppContextSwitcher` 接口支持运行时切换应用
- **自动推断默认应用**: 三种自动推断规则简化配置
- **应用级 Token 缓存隔离**: 每个应用拥有独立的 Token 缓存
- **新配置参数**: 新增 `RetryDelayMs` 和 `TokenRefreshThreshold` 配置
- **文档**: 新增配置迁移指南和多应用配置文档

### 🔧 Changed

- **重构 HttpClient 配置**: Polly 重试策略使用配置参数
- **重构 Token 重试逻辑**: 使用配置的 `RetryDelayMs` 替代硬编码值
- **WebSocket 重试**: 使用配置的 `RetryDelayMs` 替代硬编码值
- **依赖更新**: 替换代码生成器为 `Mud.HttpUtils`

### 🐛 Fixed

- **修复 `RetryCount` 配置不一致问题**: 统一应用到 HttpClient 和 TokenManager
- **WebSocket 重试硬编码问题**: 使用配置参数替代硬编码值

### 📚 Documentation

- 新增配置迁移指南
- 更新示例配置文件
- 新增多应用配置文档

---

## [1.2.2] - 2026-01-19

### ✨ Added

- **考勤管理 API**: 新增完整的考勤班次管理能力
- **审批功能**: 新增审批消息 API 和审批任务查询接口
- **演示项目**: 新增飞书 OAuth 登录演示项目

### 🐛 Fixed

- **解密失败处理**: 修复解密失败时空引用问题
- **令牌管理**: 修复用户令牌管理及状态清理问题
- **项目文件**: 修复 PackageTags 标签重复闭合问题
- **Webhook 中间件**: 修复验证请求属性和缩进问题

### 🔧 Changed

- **模型重构**: 班次模型提取公共基类
- **认证服务**: 重构认证服务和接口命名
- **项目结构**: 移动项目文件到 Sources 文件夹
- **工具类**: 重组异常和 HTTP 客户端工具
- **Redis 性能**: 使用 SCAN 替代 KEYS 命令
- **缓存管理**: 优化令牌缓存和格式化逻辑
- **代码清理**: 移除未使用变量和多余引用

### 📚 Documentation

- API 文档更新和完善
- 项目文档链接和版本号更新
- 演示项目文档优化和重组

### 📦 Build & Config

- 版本更新至 1.2.2
- 依赖管理优化
- Git 配置安全加固

---

## [1.2.1] - 2026-01-16

### ✨ Added

- **配置验证**: 新增 AppId、AppSecret、EncryptKey 格式和长度验证
- **敏感信息保护**: 配置类添加敏感信息掩码功能
- **示例配置**: 新增完整的 appsettings.example.json 文件

### 🔒 Security

- **数据注解验证**: 添加 Data Annotations 属性验证必填字段
- **Redis 配置验证**: 新增 ServerAddress 格式和连接参数验证

### 🧪 Tests

- **配置单元测试**: 全面覆盖 AppId/AppSecret 验证、敏感信息掩码等

### 📚 Documentation

- **XML 文档**: 完善配置类参数说明和示例值

### ⚠️ BREAKING CHANGE

- 修改了 `FeishuAppConfig.Validate()` 方法验证规则
- 添加了 `FeishuWebhookOptions.EncryptKey` 长度验证（32 字符）
- 添加了 `RedisOptions.Validate()` 方法验证连接参数

---

## [1.1.2] - 2026-01-10

### ✨ Added

- **Webhook/WebSocket**: 异步验证、重试功能和请求体签名验证
- **审批功能**: 第三方审批实例验证、同步和状态分页接口
- **配置验证**: WebSocket 和 Feishu 配置选项验证

### 🐛 Fixed

- **WebSocket 配置**: 修复配置相关问题

### 🔧 Changed

- **事件处理器**: 调整为异步处理器设计
- **Redis 服务**: 简化服务注册方法
- **服务注册**: 统一方法命名规范

### 📚 Documentation

- **英文文档**: 重构文档结构和内容组织

### 📦 Dependencies

- 更新项目依赖包至 1.1.2 版本

---

## [1.1.1] - 2026-01-06

### ✨ Added

- **审批功能**: 新增多种审批相关接口和常量
- **任务管理**: 自定义字段管理和选项管理功能
- **API 响应**: 统一为 FeishuApiResult 格式

### 🔧 Changed

- **接口设计**: 统一消息发送接口
- **服务注册**: 统一服务注册 API

### 📚 Documentation

- **README**: 优化项目描述和功能说明
- **架构文档**: 移除过时的架构设计文档

---

## [1.1.0] - 2025-11-12

### ✨ Added

- **用户管理**: 完整的用户 CRUD 操作接口
- **用户组管理**: 用户组创建、更新、删除接口
- **部门管理**: 完整的部门管理功能
- **员工类型管理**: 员工类型相关接口

### 🔧 Changed

- **API 结果模型**: 重构为 FeishuApiResult 命名
- **服务注册**: 优化服务注册代码结构

### 📚 Documentation

- **README**: 更新内容和结构
- **功能说明**: 新增项目功能说明和使用示例

---

## [1.0.9] - 2025-11-14

### ✨ Added

- **跨平台支持**: 支持.NET Standard 2.0
- **HTTP 客户端**: 新增飞书 HTTP 客户端扩展方法

### 🐛 Fixed

- **HttpClient 配置**: 修复配置和 API 端点 URL 格式问题

### 🔧 Changed

- **消息和事件 API**: 重构实现以提高可维护性
- **文件下载**: 优化 HTTP 请求方法提高性能

---

## [1.0.7] - 2025-11-12

### ✨ Added

- **任务管理**: 任务评论、附件、活动订阅和成员管理接口
- **JsTicket API**: 新增 JsTicket API 接口支持前端开发

### 🔧 Changed

- **代码生成器**: 升级 Mud.ServiceCodeGenerator 版本
- **依赖管理**: 优化项目依赖配置

### 📚 Documentation

- **任务文档**: 更新任务成员信息注释

---

## [1.0.3-dev] - 2025-11-12

### ✨ Added

- **基础框架**: 飞书 API 基础框架搭建
- **认证服务**: 认证服务和令牌管理
- **通讯录**: 企业通讯录相关接口
- **消息功能**: 消息发送和接收功能
- **事件支持**: Webhook 和 WebSocket 支持
- **演示项目**: Webhook 和 WebSocket 演示项目

### 📚 Documentation

- **项目文档**: 初始 README 和项目结构说明
