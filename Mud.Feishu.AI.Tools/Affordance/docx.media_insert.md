## WHEN

- 文档需要一张来自公网 URL 的图片，且希望它成为可单独选中、移动的图片块。
- 图片还没有飞书素材 token，只能从 URL 现取。

## Avoid when

- 图片需要登录态或防盗链才能下载——落盘会失败，应先把图片转到公开可访问的地址。
- 只是给文字改样式、加粗或加链接——图片块表达不了行内格式。
- 想一次插入多张——逐张调用；每次只处理一个 URL 与一个落点。

## Prerequisites

- 宿主须注册附件落盘器（`IFeishuAttachmentStager`）与 Drive API 客户端；缺任一项会返回结构化错误而不是静默跳过。
- 调用方须有 `docx:document` 与 `drive:drive`；写工具默认不在启用名单内，需宿主授权。
- `document_id` 与可选的 `parent_block_id` 必须同篇；父块缺省为文档根块。

## Tips

- 两步链路：上传返回 `image_token` 后再建图片块。建块失败会标记 `partial_failure`——图片已进素材库但文档未变，回执里的 `image_token` 可用于人工排查。
- `idempotency_key` 只覆盖建块一步（媒体上传本身不幂等）；重试时固定同一个键，避免同一落点插出两张图。
- `width` / `height` 单位是像素，留空由平台按原图比例展示；`align` 仅接受 `left` / `center` / `right`。
- `caption` 是图注文本，不是替代文本；需要无障碍描述时写进正文。
