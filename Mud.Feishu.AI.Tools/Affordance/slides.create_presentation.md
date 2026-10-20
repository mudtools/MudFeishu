## WHEN

- 用户要一份**新的** XML 演示文稿（从空白或整份 SML 生成）。

## Avoid when

- 已有文稿要加页——`slides.add_slide`；改某页——`slides.replace_slide`。
- 只要文字大纲——用文档域即可，别为大纲建演示文稿。

## Prerequisites

- 需要 `slides:presentation:create` + `slides:presentation:write_only`，且写工具在宿主授权面内。
- `content` 是整份文档，根元素为 presentation；标题须写进首页 XML（平台不单独收标题）。

## Tips

- 首次调用一律 `dry_run=true` 预演。
- 返回的 `issues` 是 XML 语法告警：已建成但内容有偏差，须如实转达。
- 配图要先经云盘素材上传取 file_token 再写进 img，本工具不搬运字节。
