---
name: feishu-spark
description: 飞书 spark 域工具集：10 个工具（只读 5 / 写 5）。建应用 / 改应用（名称、描述、图标）→ `spark.create_app` / `spark.patch_app`。
version: 1.0.0
---

# 妙搭（spark.*）

## 路由优先级（先判断对象属于哪个域）
- **建应用 / 改应用**（名称、描述、图标）→ `spark.create_app` / `spark.patch_app`。
- **应用能不能被别人看到** → `spark.get_app_visibility`（读）/ `spark.update_app_visibility`（改）。
- **应用里的数据表与记录** → `spark.list_tables`（有哪些表、字段定义）→ `spark.query_table_records`
  （查记录）/ `spark.add_table_records`（写记录）/ `spark.update_table_records`（按条件改记录）。
- **用量数据**（活跃用户、访问量）→ `spark.get_app_analytics`。

## 前置链（闭环顺序）
`spark.list_apps`（已知 app_id 则跳过）→ `spark.create_app` 得 `app_id`
→ `spark.update_app_visibility`（配可用范围）→ `spark.list_tables`（拿表名）
→ `spark.add_table_records` / `spark.update_table_records` 写数据。

## 避坑
- **`table_id` 实际是表名（table_name）**：来自 `spark.list_tables` 的 `items[].name`，
  不是数字 ID；先用 list_tables 取准名字，不要猜。
- **一次性写 500 条以内**：`spark.add_table_records` 的 `records_json` 上限 500 条，超出直接报错，请分批。
- **`spark.update_table_records` 的 filter 必填且会命中多行**：不带 filter 等于全表更新（工具会拒绝），
  条件写法是 PostgREST 语法（如 `age=gt.10`）；不确定命中范围时先用 `spark.query_table_records` 带同样 filter 试查。
- **`upsert=true` 才会合并**：`spark.add_table_records` 默认纯插入，重复调用会产生重复记录。
- **发布站点 / 上传图标 / 对象存储 / 执行 SQL 未策展**（本地文件与二进制通道）：
  需要这些能力时如实告知用户"需在妙搭控制台或宿主侧完成"；宿主集成路径与检查单见
  `feishu.guidance_read` 的键 `spark/publish`。

## 安全规则
- 写操作（`create_app` / `patch_app` / `update_app_visibility` / `add_table_records` / `update_table_records`）
  默认空名单不启用，须宿主授权（`WriteAllowList` + `IToolExecutionAuthorizer`）；支持 `dry_run=true` 预演。
- `spark.update_app_visibility` 会**扩大可达范围**（`scope=Public` 即全员可见）：改之前先
  `spark.get_app_visibility` 读当前值，并向用户确认目标范围。
- user 身份工具（应用与数据表面）要求宿主放行 `user` 并具备当前用户上下文。

## 示例
「建一个叫'周报助手'的应用，只给部门同事用，然后建张表记录提交情况」→ `spark.create_app`
→ `spark.update_app_visibility`（`scope=Range` + `departments`）→ `spark.list_tables` 确认表
→ `spark.add_table_records` 写入记录。

## 命令（工具）清单
- `spark.add_table_records`（写）：向妙搭数据表添加记录（user 身份）。records_json 为记录数组 JSON，单次上限 500 条。upsert=true 时按主键合并（映射请求头 Prefer: resolution=merge-duplicates），缺省为纯插入。⚠️ 非幂等：不带 upsert 时重复调用会产生重复记录。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），可用 dry_run=true 预演；需 spark:table 权限。
- `spark.create_app`（写）：创建妙搭应用（user 身份，闭环首步）。返回新应用的 app_id，后续用 spark.patch_app / spark.list_tables 继续。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），可用 dry_run=true 预演；需 spark:app 权限。
- `spark.get_app_analytics`（只读）：获取妙搭应用运营数据总览（活跃用户/新增用户/页面浏览，tenant 身份）。start_time/end_time 为秒级 Unix 时间戳字符串（如 "1705312800"），须 end_time >= start_time。需 spark:app:readonly 权限。
- `spark.get_app_visibility`（只读）：获取妙搭应用的可用范围配置（scope/users/departments/chats，user 身份）。修改前先用本工具确认当前范围。需 spark:app:readonly 权限。
- `spark.list_apps`（只读）：获取妙搭应用列表（tenant 身份）。分页工具：可选 fetch_all=true 在预算内自动翻页取完全部应用。需 spark:app:readonly 权限。
- `spark.list_tables`（只读）：获取妙搭应用的数据表列表（含字段定义，user 身份）。分页工具：可选 fetch_all=true 自动翻页。拿到表名后再用 spark.query_table_records 取记录。需 spark:table 权限。
- `spark.patch_app`（写）：修改妙搭应用元信息（名称/描述/图标，user 身份；PATCH 语义天然幂等）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），可用 dry_run=true 预演；需 spark:app 权限。
- `spark.query_table_records`（只读）：查询妙搭数据表记录（user 身份）。filter/order 为 PostgREST 语法（filter 如 age=gt.10；order 如 created_at.desc）。分页工具：可选 fetch_all=true 自动翻页。需 spark:table 权限。
- `spark.update_app_visibility`（写）：修改妙搭应用的可用范围（user 身份）。⚠️ 此操作会扩大应用可达范围（scope=Public 即对全员/外部可见），敏感操作，建议先 spark.get_app_visibility 确认当前范围并用 dry_run=true 预演。scope 取值：Public（公开）/ Tenant（本租户）/ Range（指定范围，须配 users/departments/chats）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer…
- `spark.update_table_records`（写）：按条件更新妙搭数据表记录（user 身份）。filter 必填（PostgREST 语法，如 age=gt.10），会更新**所有命中记录**；record_json 为要修改的字段对象。批量/条件更新天然幂等（同 filter 重放结果一致）。写操作：默认空名单不启用，启用前须经宿主授权（IToolExecutionAuthorizer），可用 dry_run=true 预演；需 spark:table 权限。

## 参考文档（细则）
- `references/publish.md`：`spark/publish`
