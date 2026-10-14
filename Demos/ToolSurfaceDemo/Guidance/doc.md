# doc 域指引

doc 域提供文档的列表与删除两类工具。

- `doc.list`：只读，支持按文件夹 ID 过滤；文件夹 ID 形如 `docs/2026`。
- `doc.delete`：写入面（high-risk-write），按文档 ID 删除；调用前必须向用户复述目标文档标题并确认。

## 错误处理

SDK 抛出 `KeyNotFoundException` 表示目标文档不存在——应向模型回传可读错误而不是重试。
