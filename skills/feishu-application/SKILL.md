---
name: feishu-application
description: 飞书 application 域工具集：9 个工具（只读 7 / 写 2）。不知道有哪些应用 / 不知道 `app_id` → `application.list_apps`（本域第一跳）
version: 1.0.0
package_version: 3.0.3
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
3. 分页：`has_more=true` 带 `page_token` 续取；一次拿全用 `fetch_all=true`
4. 可用范围的**用户列表是独立游标**（`has_more_users` / `user_page_token`），与主分页分开续取

## 避坑
- 本域**只认 `app_id`**：平台没有"按名字查应用"的接口，给名字就先 `list_apps` 匹配。
- `get_app` **不返回应用密钥 / 加密配置 / 负责人联系方式**（出站净化）——不要承诺"帮你取 secret"。
- 负责人只有 `owner_id`：要姓名 / 邮箱请拿它走通讯录域工具，本域不解析身份。
- `list_collaborators` 返回空是**结论**（只有所有者能进后台），不是失败。
- 可见清单与不可见清单**分别生效、不可见优先**；全员可见时可见清单可能为空。
- 与**妙搭（`spark.*`）区分**：`spark` 管妙搭应用，本域管开发者后台里的自建 / 商店应用。

## 写面
- 2 个写工具：`update_visibility`（可见范围，四类名单**增量**语义）/ `update_management`（启用或停用管理面）。
- **scope 待核对**：SDK 注释无字面量，按官方文档取 `application:application`（⚠️ 控制台开通前请按名称核对）。
- `update_management(enable=false)` 影响面极大（管理员将无法在后台管理该应用），务必先 `dry_run` 并复述后果。
- 其余 17 个写方法**未纳入**（改群主/协作者、v7 `update_config` 直写加密密钥且深嵌套、`upload_app_avatar` 本地路径不可达等）。

## 安全规则
- 7 只读 + 2 写；权限点见《工具权限对照表》#115~123。
- 出站净化：应用密钥、加密密钥 / 校验令牌、回调白名单与可信域、负责人邮箱 / 手机号**一律不出站**。
- 可用范围与协作者只返回**标识符**，不含姓名——按最小必要使用。
- 软缺席：宿主未启用应用管理 API 时返回"未注册客户端"说明，不要反复重试。

## 示例
- 「我们有哪些自建应用」→ `application.list_apps(owner_type=0)`
- 「这个应用谁能看」→ `application.get_app_visibility(app_id=…)`
- 「把应用开放给某人」→ `application.update_visibility(app_id="…", add_visible_user_ids=[…], dry_run=true)` → 确认后执行

## 命令（工具）清单
- `application.get_app`（只读）：按 app_id 查询应用详情（名称/描述/状态/场景类型/主语言/在线与待审版本 ID/移动端与 PC 端默认能力/已申请权限的 scope 名）。⚠️ 不返回应用密钥、加密配置与负责人联系方式（出站净化）。只读，需 application:application:readonly。
- `application.get_app_version`（只读）：按 version_id 查询单个应用版本的详情（版本号/状态/创建与发布时间/备注/该版本申请的能力与可用范围）。只读，需 application:application.app_version:readonly。
- `application.get_app_visibility`（只读）：查询应用可用范围（是否全员可见 + 可见/不可见的部门、用户组、用户列表）。回答「这个应用谁能看到」用它；成员过多时按 has_more_users/user_page_token 继续取用户列表。只读，需 application:application:readonly。
- `application.get_scopes`（只读）：查询本应用已申请的权限点及授权状态（scope_name/grant_status/scope_type）——回答「这个应用现在开了哪些权限」「某权限是否已生效」。只读，需 application:application:readonly。
- `application.list_app_versions`（只读）：分页列出应用的版本历史（version_id/version/status/创建与发布时间）——用于回答「线上是哪一版」「有没有待审版本」。只读，需 application:application.app_version:readonly。
- `application.list_apps`（只读）：分页列出企业自建/商店应用（返回 app_id/名称/状态/场景类型/在线版本 ID）——进入「应用管理」域的第一步：拿到 app_id 才能查详情、可用范围、协作者与版本。可按 status / payment_type / owner_type 过滤。只读，需 application:application:readonly。
- `application.list_collaborators`（只读）：列出应用协作者（谁能进开发者后台改这个应用；返回 type=管理员/开发/运营 + user_id）——用于回答「这个应用谁能改」。只读，需 application:application:readonly。
- `application.update_management`（写）：启用或停用应用的管理面（⚠️ 停用后管理员将无法在后台管理该应用，影响面极大——务必先 dry_run 预演并确认 app_id 与 enable 取值）。写操作：默认不启用，需宿主授权，需 application:application（⚠️ scope 名待控制台核对）。
- `application.update_visibility`（写）：更新应用的可见范围（把应用开放/关闭给指定的用户、部门、用户组，或设为全员可见）——四类名单均为**增量**语义，未传的不动。写操作：默认不启用，需宿主授权（IToolExecutionAuthorizer），需 application:application（⚠️ scope 名待控制台核对）。建议先 dry_run 预演。
