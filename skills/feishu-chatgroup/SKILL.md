---
name: feishu-chatgroup
description: 飞书 chatgroup 域工具集：4 个工具（只读 4 / 写 0）。「我们有哪些群 / 这个 chat_id 对应哪个群」→ `chatgroup.list_chats`（机器人所在的群，无需关键词）
version: 1.0.0
package_version: 3.0.2
---

# Chatgroup · 群管理

## 路由优先级
- 「我们有哪些群 / 这个 chat_id 对应哪个群」→ `chatgroup.list_chats`（**机器人所在的群**，无需关键词）
- 「按名字找某个群」→ `im.search_chats`（关键词搜群，归 im 域）
- 「这个群的详情（群主 / 成员数）」→ `im.get_chat`（归 im 域）
- 「群成员有哪些」→ `im.list_chat_members`（归 im 域）
- 「这个群谁能发言 / 发言权限怎么配的」→ `chatgroup.list_moderators`
- 「往这个群发消息前先确认在不在群里」→ `chatgroup.is_in_chat`
- 「这个群的快捷菜单有哪些」→ `chatgroup.get_menu`
- 「群公告读/写」→ `docx.get_chat_announcement` / `docx.set_chat_announcement`（**归 docx 域**）

## 前置链
1. `chat_id`（`oc_xxx`）来自 `chatgroup.list_chats` 或 `im.search_chats`
2. 发消息前先 `chatgroup.is_in_chat` 预检；不在群里要先让用户把应用拉进群
3. 群列表与发言名单都分页：`has_more=true` 带 `page_token`；一次拿全用 `fetch_all=true`（配 `max_items`）

## 避坑
- **本域只管"机器人所在的群"**：`chatgroup.list_chats` 不会返回机器人未加入的群；要按名字找别的群用 `im.search_chats`。
- **群公告不在本域**：读/写群公告走 `docx.*`（两套 SDK 接口指向同一批后端路径，工具面只保留 docx 那套，避免同义双轨）。
- `chatgroup.list_moderators` 返回空 ≠ 群没人能说话：先看 `moderation_setting`（`all` 表示全员可发言）。
- `chatgroup.is_in_chat` 判断的是**当前身份**（机器人/用户）在不在群，不是"某个成员在不在群"。
- `chatgroup.get_menu` 返回空表示"未配置菜单"，不是失败。
- 本域**没有解散群 / 改群设置 / 移出成员 / 删除菜单**等写工具（写面待 scope 与授权口径确认后另批交付）。

## 安全规则
- 4 个工具全部**只读**，需 `im:chat:readonly`（与既有 `im.get_chat` / `im.search_chats` / `im.list_chat_members` 同权限）。
- 出站净化：不返回 `tenant_key`（组织标识）；名单只给 `user_id` 与 ID 类型，用户姓名走通讯录域解析。
- 群管理**写面**（建群/改群/加移除成员/管理员/公告/置顶/菜单）尚未纳入工具面——需要这些操作时请如实告知用户"当前不可用"，不要试图用消息类工具绕。
- 软缺席：宿主未启用群管理 API 时返回明确的"未注册客户端"说明，不要反复重试。

## 示例
- 「我们有哪些群」→ `chatgroup.list_chats(fetch_all=true)`
- 「这个群谁能发言」→ `chatgroup.list_moderators(chat_id="oc_xxx")`
- 「发通知前确认机器人在群里」→ `chatgroup.is_in_chat(chat_id="oc_xxx")` → 在群里再 `im.send_message`
- 「群公告写了什么」→ 用 `docx.get_chat_announcement(chat_id="oc_xxx")`

## 命令（工具）清单
- `chatgroup.get_menu`（只读）：查询群的快捷菜单配置（一级/二级菜单的名称、类型与跳转链接）——回答「这个群的快捷入口有哪些」或核对菜单是否配置生效。只读，需 im:chat:readonly。
- `chatgroup.is_in_chat`（只读）：判断当前身份（机器人/用户）是否在指定群内（返回 is_in_chat）——向某个群发消息前的预检：不在群里就别发，先让用户把应用拉进群。只读，需 im:chat:readonly。
- `chatgroup.list_chats`（只读）：分页列出**机器人所在**的群（返回 chat_id/群名/群描述/群主 ID/是否外部群/群状态）——「我们有哪些群」「这个 chat_id 对应哪个群」的第一跳；按关键词找群请用 im.search_chats。只读，需 im:chat:readonly。
- `chatgroup.list_moderators`（只读）：查询群的发言权限设置与可发言成员名单（返回 moderation_setting + user_id 列表，分页）——回答「这个群谁能发言」。与 im.get_chat（群主/管理员）互补。只读，需 im:chat:readonly。
