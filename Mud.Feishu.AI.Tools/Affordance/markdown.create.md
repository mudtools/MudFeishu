## WHEN

- 用户要「在飞书里新建一篇文档」，且内容你已按 Markdown 组织好（纪要、方案、周报）。
- 需要一份后续还能被 `markdown.patch` / `markdown.overwrite` 改写的在线文档载体。

## Avoid when

- 只是往已有文档追加或替换正文——用 `markdown.patch` 或 `markdown.overwrite`，本工具会新建一份。
- 内容 UTF-8 后超过 20 MB——平台导入任务不接，先拆分。
- 要的是云空间里的 `.md` 资源文件——本工具产物是 docx。

## Prerequisites

- 需 drive:drive（写）。默认空名单不启用，须宿主写入 `FeishuAgent:WriteAllowList` 并经授权。
- 未给 `folder_token` 时落云空间根目录；文件夹客户端缺席时报错而非猜位置。
- 需要宿主提供落盘器（临时源文件落盘用）。

## Tips

- 源 `.md` 是临时物：导入终态后自动清理，返回里 `source_file_cleaned` 说明结果。
- 导入失败时保留 `source_file_token`，凭它排查；错误提示按平台 `job_status` 归类。
- 导入是异步的：轮询有界，超时如实报「仍在处理」，不要当成功。
