# Bitable filter / sort 语法

## 前置链
1. `bitable.list_fields` 拿到**字段名与字段类型**（filter/sort 的分组项必须与类型匹配）
2. `bitable.query_records` 传 `filter` / `sort` 字符串

## 避坑
- **filter 是受控语法，不是自由 SQL**：解析器只接受工具 Schema 里描述的那套形式，
  写 `WHERE` / `SELECT` / 自由括号表达式会被直接拒绝（错误会指出非法位置）。
- **字段名要用 `list_fields` 的原名**，不要用中文别名或猜测的英文名。
- **类型错配是最高频失败**：文本字段用等值匹配、数字/日期字段用比较，混用会被平台或解析器拒绝。
- 排序字段必须是可排序类型；多字段排序按给定顺序生效。
- 结果分页：`has_more=true` 时带上返回的 `page_token` 续取，不要重发同一请求。

## 按视图取数
- `bitable.list_views` 列出视图（名称 / 类型 / 可见范围）：拿到 `view_id` 后传给 `bitable.query_records`。
- 视图自带的过滤与排序**会一起生效**：不要再传一份表达同一条件的 `filter`——
  条件叠加后更严，最容易"明明有记录却查出空结果"。
- 视图的可见范围可能本身就限制了可见记录；取数前先用 `list_views` 确认。
- 表结构变更后视图可能仍引用旧字段：先 `list_fields` 核对字段名，再取数。

## 示例
- 「状态=进行中 的记录」→ `list_fields` 确认状态字段名与类型 → `query_records(filter=…)`
- 「按截止时间倒序」→ `query_records(sort=…)`
- 「按'我负责的'视图取数」→ `list_views` 拿 `view_id` → `query_records(view_id=…)`（不要再加 filter）