# ADR-0004：引擎 / 契约 / 外壳三段分离（第七式 · 第八式）

- 状态：**已决策（三段分离工艺与异常语义归属冻结）**
- 日期：2026-10-15（由 v3 中立化重构方案 §4.2 的两式 + §7.2/§7.4 的落地事实收敛）
- 影响面：`Mud.AI.Agent/Conversations/{KeyedAsyncGate,ConcurrencyLimiter}.cs`、
  `Mud.AI.Agent/Agents/{AgentAssemblyFactory,ObservedDelegatingAgent}.cs`、
  `Mud.Feishu.AI/Conversations/{KeyedConversationGate,GlobalConcurrencyGate}.cs`、
  `Mud.Feishu.AI/Agents/FeishuAgent.cs`

## 背景

一个类型同时满足「实现已发布的产品契约」+「主体是通用机制」时，只有两条退化路径：

1. **机制留在产品侧** ⇒ 无法上游化，中立包永远长不出可复用能力；
2. **契约与实现一起搬去中立侧** ⇒ 中立包必须引用产品类型（契约、异常、配置），
   双向中立锁当场破裂。

两者都编译通过、都能跑，**而破坏是全静默的**（此前后续任何一次"顺手搬代码"都会让它恶化）。
因此需要一条**可机械断言的工艺**，把"契约留产品、实现去中立、中间加一层"固定成决策，
而不是散落在几个文件的注释里。

## 决策

### 第七式：引擎 / 契约 / 外壳三段分离

- **契约**（产品侧，已发布、字节不动）：如 `IConversationGate`。
- **外壳**（产品侧，薄适配，≈40–130 行）：同名同签名实现原契约，**逐方法委托**引擎；
  仍留在原命名空间 ⇒ 宿主导出面零变化。
- **引擎**（中立侧，零产品依赖）：无接口或自有中立接口；公开签名里**不出现任何产品类型**
  （形参只用中立标量 / 委托 / `CancellationToken`）。

落地形态：

| 引擎（`Mud.AI.Agent`） | 外壳（`Mud.Feishu.AI`） | 契约（产品侧） |
|---|---|---|
| `Conversations/KeyedAsyncGate.cs` | `Conversations/KeyedConversationGate.cs`（44 行） | `IConversationGate` |
| `Conversations/ConcurrencyLimiter.cs` | `Conversations/GlobalConcurrencyGate.cs`（132 行） | `IConversationGate` |
| `Agents/AgentAssemblyFactory.cs` | `Agents/FeishuAgent.cs` 的装配调用点 | `AIAgent`（MAF） |
| `Agents/ObservedDelegatingAgent.cs` | `Agents/FeishuAgent.cs`（继承该基类） | `AIAgent`（MAF） |

> `AgentAssemblyFactory` / `ObservedDelegatingAgent` 属**基类/工厂式**的三段分离：
> 机制（装配、遥测编织、摘要先于 Run）在中立类里，产品侧只保留产品政策
> （空值守卫、`Validate()`、`FeishuGuidanceBudget`、审批/幂等补偿）。

**验收判据（可机械断言）**：

1. 产品外壳的每个方法体只有**一条语句**（`=> await _engine.XxxAsync(...)` / `=> new Handle(...)`）——
   即"委托而非重写"；
2. 中立引擎的公开签名里**不出现任何产品类型**；
3. 产品外壳的同名类型**仍存在于原命名空间**（导出面不变）；
4. 引擎源码**不含**引擎词汇的实现自持（`SemaphoreSlim` / `ConcurrentDictionary` / `WaitAsync(` /
   `Task.Delay` 等），这些必须只在引擎里出现。

判据 1/4 由 `Tests/Mud.Feishu.AI.Tests/ContractGuards/EngineShellContractGuards.cs` 双向锁定
（壳↔引擎：壳不含引擎词汇且真出现引擎类型名；引擎位于指定中立工程目录且不含产品词汇）。

### 第八式：中立引擎返回"中立结果"，产品外壳决定异常语义

- **问题**：`GlobalConcurrencyGate` 满额时抛 `ConversationBusyException`（产品类型）。
  引擎若也抛它 ⇒ 中立包引用产品类型；引擎若抛自有异常 ⇒ 外壳必须 try/catch 翻译
  （**异常驱动的控制流**，且捕获面极易漏）。
- **手法**：中立引擎**不抛业务异常**，用**可空返回值 / 结果标记**表达"未获取到"：

```csharp
// 中立：ConcurrencyLimiter —— null 表示满额 / 超时，不含任何产品语义
public Task<IDisposable?> TryAcquireAsync(CancellationToken cancellationToken = default);

// 产品：GlobalConcurrencyGate —— "什么算 busy" 全留产品
var handle = await _limiter.TryAcquireAsync(ct).ConfigureAwait(false)
    ?? throw new ConversationBusyException(key, BuildBusyMessage());
```

- **边界**：**取消**（`OperationCanceledException`）**原样穿透**——它是 .NET 通用语义，
  不是产品业务异常；只有"满额 / 超时"这类业务诉求被建模成可空返回。

### 超时建模：显式超时重载，非 `WhenAny`

引擎队列超时必须用 `SemaphoreSlim.WaitAsync(timeout, ct)` 的**超时重载**（返回 `false` 即超时），
**不得**用 `await Task.WhenAny(_semaphore.WaitAsync(ct), Task.Delay(timeout, ct))`——
`WhenAny` 竞速会**丢弃败者等待任务**，败者稍后仍可能取走许可而无人认领（许可泄漏面）；
超时重载把"等多久"下沉到信号量运行时，语义更紧、无泄漏面。

⚠️ 这是**建模改进，不是行为变更**：P4 复核证明原实现（`CancelAfter` + 外部令牌反推来源）
行为正确、在等价实现下不产生宿主可见差异 ⇒ **不写 `CHANGELOG.md`、不重固化黄金**，
改为在四路径等价性回归用例中断言异常类型与现状逐条一致。
哨兵：`Tests/Mud.AI.Agent.Tests/Conversations/ConcurrencyLimiterTimeoutModelContractGuards.cs`
断言引擎剔注释后不出现 `CancelAfter` 与自造计时的 `Task.Delay`。

## 理由

- **退化方向是单向的**：从"三段分离"退回"机制回产品"只需在壳里顺手写回一行
  `_entries.TryAdd` 或 `WaitAsync` —— 编译通过、行为看上去也对，机械语义却重新分裂成两份
  （此后修引擎必漏改壳），**而没有任何编译期或运行期信号**。故必须机械锁定。
- **异常语义是产品政策**：`BusyModes` / 超时文案 / 在途计数 / 配额口径都是宿主可见面，
  它们属于外壳而非机制；把"未获取到"与"抛什么异常"解耦，等于把产品政策单点化到外壳。
- **取消不是业务异常**：把取消也翻译成产品异常会让"用户取消"与"系统繁忙"在链路上不可分辨，
  破坏 at-least-once + 幂等回滚依赖的异常类型分界。

## 后果

- 修改三段中任一段时必须同时满足上述四条判据，并同步本 ADR 与相关类级注释
  （注释只留工艺摘要 + 本 ADR 链接）。
- 引擎的用例归 `Tests/Mud.AI.Agent.Tests`（`Conversations/` / `Agents/` / `Tools/`），
  壳的行数与委托判据归 `Tests/Mud.Feishu.AI.Tests`——**引擎测试不得住进产品工程**
  （否则"引擎"其实仍在产品侧）。
- 宿主可见面（`KeyedConversationGate` / `GlobalConcurrencyGate` / `BusyModes` /
  `AddFeishuAgent` 注册形态）**零改动**；本轮为未发布窗口期的一次性硬切换，
  不保留 `[Obsolete]` 或转发壳。
- 决策记录的追溯路径：`.docs/AI/MudFeishu-AI-公共功能中立化重构方案-v3.md` §4.2 / §7.2 / §7.4
  与 `documents/AIAgent/AI-Native-实施进度-Phase*.md` 的评审条目。
