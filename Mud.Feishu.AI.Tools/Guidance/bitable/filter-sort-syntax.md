
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

## 示例
- 「状态=进行中 的记录」→ `list_fields` 确认状态字段名与类型 → `query_records(filter=…)`
- 「按截止时间倒序」→ `query_records(sort=…)`
