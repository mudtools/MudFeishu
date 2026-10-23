# ADR-0011：令牌清库门（TokenStorePurgeGate）的作用域

- 状态：**已决策（进程内、仅恢复读路径、fail-open 的计数门）**
- 日期：2026-10-23
- 影响面：`Mud.Feishu.Abstractions/Authentication/TokenStorePurgeGate.cs`、
  `Mud.Feishu.Abstractions/Authentication/TokenManager/PurgeGateTokenStoreDecorator.cs`、
  `Mud.Feishu.Abstractions/Authentication/MultiApp/FeishuAppManager.cs`

## 背景

凭据变更（AppId / AppSecret）热更新要求「清库 → 重建」的严格时序：新上下文不得在清库完成前
从 store 恢复出旧凭据来源的令牌。清库是 IO（Redis 为 SCAN + 逐键 DELETE），不能在热更新回调线程上
同步等待（见 ADR-0010）。需要一个轻量机制在不阻塞回调的前提下守住这条时序。

## 决策

`TokenStorePurgeGate` 是按 `appKey` 的**进程内引用计数租约**：

| 维度 | 口径 |
|---|---|
| 作用对象 | 令牌恢复读路径（租户 / 应用 / 用户的 store 读族） |
| 作用范围 | **本进程**；不跨实例、不持久化 |
| 生效方式 | `Mark` 打门（同步、微秒级）→ 异步清库 → `finally { Release }` 撤门 |
| 计数语义 | 预清库与提交后二次清库各计一次；叠加期间门持续有效，归零才撤门 |
| 读路径行为 | `Pending` ⇒ 视为未命中（不触达 store）；`TimedOut` ⇒ fail-open 并记一次 Warning |
| 写 / 删 / 清库 | **一律透传**，绝不拦截 |
| 失效上界 | 30s（`SafetyTimeoutSeconds`），防止清库挂死导致存储恢复被永久禁用 |

### 为什么不拦写族

清库期间旧上下文仍可能在途写入令牌；拦截写不会消除这些残留，反而会让「提交后二次清库」
失去覆盖对象。清库本身的 `ClearAllUsersAsync` 若被拦截，会使凭据变更清库静默失效。

### 为什么不做分布式门

- 分布式门需要跨实例协调（Redis 键、租约续期、失效判定），把一次本地时序问题升级为可用性问题；
- 跨实例的残余窗口与既有 Redis SCAN 语义同级——其他实例在窗口内可能恢复旧令牌，
  由「清库完成 → 重建 → 提交后二次清库」收敛，不构成回归；
- 组件不据此承诺跨实例的强一致时序；宿主若需要，应在部署层做停写/滚动重启。

### 装饰器口径

管理器的 store 读边界统一经 `PurgeGateUserTokenStoreDecorator` / `PurgeGateTokenStoreDecorator`：
门判定与读故障降级（异常 ⇒ Warning ⇒ 未命中）收敛在装饰器内，管理器不再逐点判定或 try/catch。
装饰器**刻意不声明** `IUserTokenStoreCompareExchange` 与 `IUserTokenStoreAccessTokenInvalidation`——
能力探测必须落在未装饰的原始 store 上，否则内层未实现时探测假命中。

## 后果

- 热更新回调不阻塞；「清库完成前不得恢复旧令牌」由恢复路径短路保证。
- 清库异常/挂死时门最多 30s 后自动失效，存储恢复不会被永久禁用。
- 多实例部署接受与 SCAN 同级的残余窗口；该口径需在部署文档中保持可见。

## 如何应用

- 新增令牌**恢复读**路径必须经过既有装饰器（或显式 `TokenStorePurgeGate.Query`），不得直读 store。
- 新增清库路径必须成对 `Mark` / `Release`，`Release` 放在 `finally`；不得在写/删路径加入门判定。
- 不得把门状态用于跨进程协调或持久化；需要跨实例语义时另立方案并更新本 ADR。
