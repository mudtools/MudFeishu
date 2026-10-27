# ADR-0010：热更新锁序与临界区预算

- 状态：**已决策（锁序与预算冻结；运行期自检已落地）**
- 日期：2026-10-23
- 影响面：`Mud.Feishu.Abstractions/Authentication/MultiApp/FeishuAppManager.cs`

## 背景

配置热更新（`IOptionsMonitor.OnChange` 回调）同时涉及：配置快照交换、Lazy 上下文重建、
基类注册表变更、默认应用键切换、旧上下文退休，以及凭据变更时的令牌清库（Redis 为 SCAN + 逐键删除）。
这些动作分布在四个锁上；若在其中任一锁内执行 IO、等待异步任务或派发用户事件，
会阻塞回调线程、放大线程池饥饿，并与并发 `AddApp` / `RemoveApp` 形成锁序反转的死锁面。

## 决策

### 锁序（禁止反向获取）

```
_configApplyLock → _lazyRebuildLock
                 → _registryLock
                 → _defaultAppLock
```

- `_configApplyLock` 是热更新提交的最外层锁；进入后**不再**获取任何会获取它的锁。
- `_lazyRebuildLock`、`_registryLock`、`_defaultAppLock` 之间互不嵌套获取。
- `RemoveApp` 在 `_registryLock` 内完成「摘除 → 墓碑/世代 → 快照写回」，
  默认应用提升在 `_defaultAppLock` 内单独完成，退休入队与清库派发在全部锁外。

### 临界区预算

- 锁内**只允许**内存引用交换与字典读写；禁止：store IO、HTTP 调用、`Task.Wait`/`.Result`、
  用户事件派发、`Dispose`、退休扫描。
- 两阶段提交：Phase-P 清库（IO）在 `_configApplyLock` **之前**完成（fire-and-forget + 门）；
  Phase-A 预构造在锁内（纯内存装配）；Phase-B 提交在 `_lazyRebuildLock` 内**仅做引用交换**，
  `RegisterApp`、退休入队、二次清库派发全部在锁外。
- `_lazyRebuildLock` 单次持有超过 **50ms** 时记 `Debug` 自检日志（`LockHoldWarningThresholdMs`），
  作为「IO 混入临界区」的回归信号；该告警只观测、不改变行为。

### 为什么不阻塞清库

在 `OnChange` 回调上同步等待清库完成（`Task.Run(...).Wait(10s)`）会造成回调线程最长阻塞 10s、
宿主有 `SynchronizationContext` 时存在死锁面、线程池受限时占用双线程。
因此预清库只做「同步打门 + 异步派发」，实质时序由恢复路径的门短路承担（见 ADR-0011）。

## 后果

- 热更新回调的同步段为微秒级；重建与清库的耗时不再传导给配置系统。
- 单应用提交失败逐应用隔离（回滚引用交换、退休新上下文），不阻断其余应用；
  Phase-A 构造失败整体中止且不动注册表。
- 50ms 自检仅覆盖 `_lazyRebuildLock` 的主提交段；其余锁的临界区由代码评审按本决策检查。

## 如何应用

- 新增锁或新增锁内逻辑前，先声明其在上述锁序中的位置；无法归位说明设计需要拆分。
- 任何可能阻塞的操作（IO、等待、回调）必须移到锁外，或改为「先派发、后由门/队列收敛时序」。
- 修改 `_lazyRebuildLock` 临界区时同步复核 50ms 自检的日志语义。
