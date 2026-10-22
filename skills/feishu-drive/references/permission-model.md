# Drive：权限模型（链接分享 vs 成员授权）

## 两条独立通道
1. **公开链接分享**：`drive.get_permission_public`（读） / `drive.update_permission_public`（写）
   ——决定"任何拿到链接的人"能看到什么（link_share_entity / external_access）。
2. **成员授权**：`drive.set_permission`（加协作者） / `drive.set_permission`（改档位）
   / `drive.remove_permission`（移除）——按 openid / email / userid / chatid / departmentid 点名。

## 前置链
1. 改之前先 `drive.get_permission_public` **读当前设置**，否则无法判断这次是收紧还是放开
2. 写操作优先 `dry_run=true` 预演，确认将要下发的调用面

## 避坑
- `update_permission_public` 可能让文档**对组织外可见**：放开前必须先确认合规口径；
  收紧到"仅协作者可见"风险低，放开风险高——不要为了"省事"直接开公开。
- **`grant_permission` 是"加一条"而不是"设为仅此人可见"**：它不回收既有权限，
  要收紧只能配合 `remove_permission`（先查现状再改）。
- `update_permission_member` 是**幂等**改档位（view / edit / full_access）；成员不在时先 grant。
- `transfer_owner` **不可逆**（原所有者降为编辑者）：先确认接手人，且只能由当前所有者发起。
- 以上写操作默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer）。

## 示例
- 「让李四能编辑」→ 先拿到李四的 open_id → `drive.set_permission`（perm=edit）。
- 「改成任何人可读」→ `drive.get_permission_public` → `dry_run` 预演 → `update_permission_public`。