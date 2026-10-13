---
name: feishu-application
description: 飞书 application 域工具集：7 个工具（只读 7 / 写 0）。不知道有哪些应用 / 不知道 `app_id` → `application.list_apps`（本域第一跳）
version: 1.0.0
package_version: 3.0.2
---

# Application · 飞书应用管理

## 路由优先级
- 不知道有哪些应用 / 不知道 `app_id` → `application.list_apps`（**本域第一跳**）
- 问「这应用是干什么的 / 什么状态 / 开了哪些权限」→ `application.get_app`
- 问「谁能看到这个应用」→ `application.get_app_visibility`
- 问「谁能改这个应用」（开发者后台权限）→ `application.list_collaborators`
- 问「应用开通了哪些权限、某权限是否生效」→ `application.get_scopes`
- 问「线上是哪一版 / 有没有待审版本」→ `application.list_app_versions` → `application.get_app_version`

## 前置链
1. `app_id`（`cli_xxx`）来自 `application.list_apps`；可先用状态 / 付费类型 / 应用类型过滤
2. `version_id` 来自 `application.list_app_versions`（同一应用下才有效）
3. 列表分页：`has_more=true` 时带 `page_token` 续取；一次拿全用 `fetch_all=true`（配 `max_items`）
4. 可用范围的**用户列表是独立游标**（`has_more_users` / `user_page_token`），与主分页分开续取

## 避坑
- 本域**只认 `app_id`**：平台没有"按名字查应用"的接口，给名字就先 `list_apps` 匹配。
- `application.get_app` **不返回应用密钥 / 加密配置 / 负责人联系方式**（出站净化）——不要向用户承诺"帮你取 secret"。
- 负责人只有 `owner_id`：要姓名 / 邮箱请拿它走通讯录域工具，本域不解析身份。
- `list_collaborators` 返回空是**结论**（没人被额外授权，只有所有者能进后台），不是失败。
- 可用范围的"可见清单"与"不可见清单"**分别生效、不可见优先**；`is_visible_to_all=1` 时可见清单可能为空（那是全员可见）。
- 要看"线上版本"用 `get_app` 的 `online_version_id`，别在版本列表里猜第一个。
- 与**妙搭（`spark.*`）区分**：`spark` 管妙搭应用，本域管开发者后台里的自建 / 商店应用。

## 安全规则
- 7 个工具全部**只读**：需应用只读权限；两个版本工具另需"应用版本只读"权限（准确 scope 名见《工具权限对照表》#115~121）。
  ⚠️ 本域 SDK 注释只给了自然语言、未给 scope 字面量，故对照表该区段标 ⚠️，开通前请在开发者后台按名称核对。
- 出站净化：应用密钥、加密密钥 / 校验令牌、回调白名单与可信域、负责人邮箱 / 手机号**一律不出站**。
- 可用范围与协作者只返回**标识符**（department_id / group_id / user_id / open_id），不含姓名——按最小必要使用。
- 软缺席：宿主未启用应用管理 API 时返回明确的"未注册客户端"说明，不要反复重试。

## 示例
- 「我们有哪些自建应用」→ `application.list_apps(owner_type=0)`
- 「cli_xxx 是干嘛的、线上跑哪版」→ `application.get_app` → 需要版本细节再 `application.get_app_version`
- 「这个应用谁能看」→ `application.get_app_visibility(app_id=…)`
- 「谁有后台权限」→ `application.list_collaborators(app_id=…)`（拿到 user_id 后再走通讯录域解析姓名）

## 命令（工具）清单
- `application.get_app`（只读）：按 app_id 查询应用详情（名称/描述/状态/场景类型/主语言/在线与待审版本 ID/移动端与 PC 端默认能力/已申请权限的 scope 名）。⚠️ 不返回应用密钥、加密配置与负责人联系方式（出站净化）。只读，需 application:application:readonly。
- `application.get_app_version`（只读）：按 version_id 查询单个应用版本的详情（版本号/状态/创建与发布时间/备注/该版本申请的能力与可用范围）。只读，需 application:application.app_version:readonly。
- `application.get_app_visibility`（只读）：查询应用可用范围（是否全员可见 + 可见/不可见的部门、用户组、用户列表）。回答「这个应用谁能看到」用它；成员过多时按 has_more_users/user_page_token 继续取用户列表。只读，需 application:application:readonly。
- `application.get_scopes`（只读）：查询本应用已申请的权限点及授权状态（scope_name/grant_status/scope_type）——回答「这个应用现在开了哪些权限」「某权限是否已生效」。只读，需 application:application:readonly。
- `application.list_app_versions`（只读）：分页列出应用的版本历史（version_id/version/status/创建与发布时间）——用于回答「线上是哪一版」「有没有待审版本」。只读，需 application:application.app_version:readonly。
- `application.list_apps`（只读）：分页列出企业自建/商店应用（返回 app_id/名称/状态/场景类型/在线版本 ID）——进入「应用管理」域的第一步：拿到 app_id 才能查详情、可用范围、协作者与版本。可按 status / payment_type / owner_type 过滤。只读，需 application:application:readonly。
- `application.list_collaborators`（只读）：列出应用协作者（谁能进开发者后台改这个应用；返回 type=管理员/开发/运营 + user_id）——用于回答「这个应用谁能改」。只读，需 application:application:readonly。
