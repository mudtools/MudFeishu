# 通讯录（contact.*）

## 前置链
`resolve_user` / `search_user` → open_id → im / mail / task 等写工具。

## 避坑
- **发给张三不是一步**，必须先解析 open_id。
- 解析不到先确认可见范围，别反复换关键字重试。

## 示例
发给张三 → `search_user` → `im.send_message`。