---
name: feishu-chatgroup
description: 飞书 chatgroup 域工具集：16 个工具（只读 4 / 写 12）。「我们有哪些群 / 这个 chat_id 对应哪个群」→ `chatgroup.list_chats`（机器人所在的群）
version: 1.0.0
package_version: 3.1.1
---

# Chatgroup · 群管理

## 路由优先级
- 「我们有哪些群 / 这个 chat_id 对应哪个群」→ `chatgroup.list_chats`（**机器人所在的群**）
- 「按名字找群 / 群详情 / 群成员」→ `im.search_chats` / `im.get_chat` / `im.list_chat_members`（归 im 域）
- 「谁能发言 / 发言权限怎么配」→ `chatgroup.list_moderators`
- 「发消息前确认在不在群里」→ `chatgroup.is_in_chat`
- 「群的快捷菜单有哪些」→ `chatgroup.get_menu`
- 「群公告读/写」→ `docx.get_chat_announcement` / `docx.set_chat_announcement`（**归 docx 域**）

## 前置链
1. `chat_id`（`oc_xxx`）来自 `chatgroup.list_chats` 或 `im.search_chats`
2. 发消息前先 `chatgroup.is_in_chat` 预检；不在群里要先让用户把应用拉进群
3. 分页：`has_more=true` 带 `page_token`；一次拿全用 `fetch_all=true`
4. 写操作前先确认宿主已授权（写工具**默认不启用**），并先 `dry_run=true` 预演

## 避坑
- **本域只管"机器人所在的群"**：`list_chats` 不返回机器人未加入的群；按名字找群用 `im.search_chats`。
- **群公告不在本域**：走 `docx.*`（两套 SDK 接口同一批后端路径，只保留 docx 那套）。
- `list_moderators` 返回空 ≠ 没人能说话：先看 `moderation_setting`（`all` 表示全员可发言）。
- `is_in_chat` 判断的是**当前身份**在不在群，不是"某个成员在不在群"。
- `get_menu` 返回空表示"未配置菜单"，不是失败。
- `update_chat` 是**增量更新**（未传字段保持原值），与互通规则的覆盖式语义不同。
- `update_moderation` 选 `moderator_list` 时必须给名单，否则等于把全群静音。
- 群主转让（`owner_id`）**不暴露**——高危语义，当前工具面不可用。

## 写面
- 12 个写工具，覆盖群设置 / 成员 / 管理员 / 置顶 / 菜单删除五类操作。
- **scope 全部待核对**：SDK 注释无字面量，按官方文档取 `im:chat`（⚠️ 控制台开通前按名称搜索确认）。
- 12 个写工具各有 affordance 资产（执行前先读），其中 5 个高危：`delete_chat` / `delete_top_notice` / `delete_manager` / `remove_member` / `delete_menu`。
- `add_member` / `remove_member` 回执带**无效 ID 清单**：非空即有人没进/没出群（`add_manager` 给管理员全量名单）。
- `me_join_chat` 加的是**当前应用/机器人身份**，不是某个用户。
- **新增/修改群菜单暂未纳入**：请求体是嵌套树，扁平参数无法表达。

## 安全规则
- 4 只读 + 12 写；权限点见《工具权限对照表》#143~154。
- 只读需 `im:chat:readonly`；写需 `im:chat`（⚠️ 待核对）——写工具默认不启用，要过宿主授权门禁。
- 出站净化：不返回 `tenant_key`；名单只给 `user_id` 与 ID 类型，姓名走通讯录域解析。
- 高危写一律先 `dry_run` 预演，并向用户复述后果。
- 软缺席：宿主未启用群管理 API 时返回"未注册客户端"说明。

## 示例
- 「我们有哪些群」→ `chatgroup.list_chats(fetch_all=true)`
- 「拉个群沟通」→ `chatgroup.create_chat(name="…", user_id_list=[…], dry_run=true)` → 确认后 `dry_run=false`

## 命令（工具）清单
- `chatgroup.add_manager`（写）：把成员设为群管理员（可多人；回执给设置后的管理员全量名单）——「给群加管理员」。写操作：需 im:chat（⚠️ scope 名待控制台核对）。
- `chatgroup.add_member`（写）：把成员拉进群（返回无效成员 ID 列表）——「把人加进群沟通」。写操作：需 im:chat（⚠️ scope 名待控制台核对）。
- `chatgroup.create_chat`（写）：创建一个群（返回 chat_id；群主默认为当前应用身份）——「拉个群沟通」的第一落点。写操作：需 im:chat（⚠️ scope 名待控制台核对）。
- `chatgroup.delete_chat`（写）：解散一个群（⚠️ 不可逆：群内历史消息随群一起消失，且只能重建、会拿到新 chat_id——务必先 dry_run 预演并确认 chat_id 无误）。写操作：需 im:chat（⚠️ scope 名待控制台核对）。
- `chatgroup.delete_manager`（写）：撤销成员的群管理员身份（⚠️ 对方立即失去群管理能力且不会收到通知——务必先 dry_run 预演并确认 manager_ids 无误）。写操作：需 im:chat（⚠️ scope 名待控制台核对）。
- `chatgroup.delete_menu`（写）：删除群的快捷菜单一级入口（⚠️ 二级菜单会随父级一起消失——务必先 dry_run 预演并确认 chat_menu_top_level_ids 无误）。写操作：需 im:chat（⚠️ scope 名待控制台核对）。
- `chatgroup.delete_top_notice`（写）：取消群置顶（⚠️ 全群可见的置顶会消失，消息本身不丢失——务必先 dry_run 预演并确认 chat_id 无误）。写操作：需 im:chat（⚠️ scope 名待控制台核对）。
- `chatgroup.get_menu`（只读）：查询群的快捷菜单配置（一级/二级菜单的名称、类型与跳转链接）——回答「这个群的快捷入口有哪些」或核对菜单是否配置生效。只读，需 im:chat:readonly。
- `chatgroup.is_in_chat`（只读）：判断当前身份（机器人/用户）是否在指定群内（返回 is_in_chat）——向某个群发消息前的预检：不在群里就别发，先让用户把应用拉进群。只读，需 im:chat:readonly。
- `chatgroup.list_chats`（只读）：分页列出**机器人所在**的群（返回 chat_id/群名/群描述/群主 ID/是否外部群/群状态）——「我们有哪些群」「这个 chat_id 对应哪个群」的第一跳；按关键词找群请用 im.search_chats。只读，需 im:chat:readonly。
- `chatgroup.list_moderators`（只读）：查询群的发言权限设置与可发言成员名单（返回 moderation_setting + user_id 列表，分页）——回答「这个群谁能发言」。与 im.get_chat（群主/管理员）互补。只读，需 im:chat:readonly。
- `chatgroup.me_join_chat`（写）：让当前应用/机器人身份加入一个群（公开群可直接加入；私密群需已在群内或被邀请）——用于「机器人进群后先自证在群内」的前置步骤。写操作：需 im:chat（⚠️ scope 名待控制台核对）。
- `chatgroup.put_top_notice`（写）：设置群置顶（action_type=1 置顶某条消息，需配 message_id；action_type=2 置顶群公告，不需 message_id）——「把重要内容钉在群顶部」。写操作：需 im:chat（⚠️ scope 名待控制台核对）。
- `chatgroup.remove_member`（写）：把成员移出群（⚠️ 对方立即失去群内可见性，且不会收到单独通知——务必先 dry_run 预演并确认 id_list 无误）。写操作：需 im:chat（⚠️ scope 名待控制台核对）。
- `chatgroup.update_chat`（写）：更新群设置（群名/描述/编辑权限/拉人权限/分享名片权限/@所有人权限/消息模式）——未传的字段保持原值。写操作：需 im:chat（⚠️ scope 名待控制台核对）。
- `chatgroup.update_moderation`（写）：更新群的发言权限（⚠️ 高危语义：可一句话把全群改成「仅群主与管理员可发言」——moderation_setting=all_members / only_owner / moderator_list；选 moderator_list 时用 moderator_added_list / moderator_removed_list 增删可发言成员）。写操作：需 im:chat（⚠️ scope 名待控制台核对）。建议先 dry_run 预演。
