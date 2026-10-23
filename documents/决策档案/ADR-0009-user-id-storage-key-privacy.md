# ADR-0009：用户标识（OpenId）在持久化令牌键中的明文取舍

- 状态：**已决策（保持明文键；风险与缓解措施冻结）**
- 日期：2026-10-23
- 影响面：`Mud.Feishu.Abstractions/Authentication/TokenKeyBuilder.cs`、
  `Mud.Feishu.Abstractions/Authentication/TokenManager/FeishuUserTokenStore.cs`、
  `Mud.Feishu.Redis/Services/RedisUserTokenStore.cs`

## 背景

用户令牌键形如 `{env}:{appKey}:token:user:{userId}:{tokenType}:access|refresh`，
其中 `userId`（飞书 OpenId）以明文出现在 Redis 键路径中。OpenId 属可关联到自然人的外部标识，
评审提出两种收敛方向：

1. **哈希化键段**（HMAC(userId) 或 SHA-256）——降低键空间泄露后的可识别性；
2. **保持明文**，把控制点放在存储侧（ACL、网络隔离、静态加密）与日志侧（脱敏）。

## 决策

**保持明文键段**。理由：

- **键布局是运行期契约**：`TokenKeyBuilder` 是 Memory / Redis 两条后端的唯一键出口，
  `GetTokenTypesAsync(userId)`、`ClearUserAsync(userId)`、`ClearAllUsersAsync()` 与
  `TryParseUserTokenType` 都按该布局做前缀匹配或反向解析。哈希化必须同批改造全部读写方，
  并对存量键做「双读 + 迁移」，否则升级即表现为**用户令牌全部失效/清库 no-op**。
- **哈希不构成实质隐私屏障**：无盐哈希对已知 OpenId 集合可字典反查；加盐哈希虽不可反查，
  但运维侧的按用户定位/删除（数据主体请求）会失去可操作性，收益与代价不匹配。
- **既有控制点已覆盖主要风险**：用户标识入日志统一经 `SensitiveDataUtils.MaskSensitiveData`；
  访问令牌键自带 TTL，refresh 键 TTL ≤ 30 天；Redis 侧的 ACL / 网络隔离 / 静态加密属宿主部署面，
  组件不弱化也不替代。

## 后果

- 接受「Redis 键空间可见即 OpenId 可见」的残余风险；安全评审与合规文档按此口径说明。
- 若未来合规要求键级不可识别，必须走**键版本化 + 双读迁移**（新键格式写入、旧键读取兼容、
  清库按两套 pattern 枚举），并同步更新 `TokenKeyBuilder` 的单一出口与全部契约守卫；
  不得只改写入侧（会造成存量令牌不可达）。

## 如何应用

- 新增存储后端一律复用 `TokenKeyBuilder`，不得自行拼接或另起键格式。
- 新增日志点涉及 `userId` / `OpenId` 时继续走统一脱敏出口。
- 本决策不阻止宿主在 Redis 层启用加密或键前缀隔离；组件侧不感知。
