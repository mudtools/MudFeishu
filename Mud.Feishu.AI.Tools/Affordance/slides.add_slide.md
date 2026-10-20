## WHEN

- 往已有文稿插入一页：默认追加到末尾，或用 `before_slide_id` 插到某页之前。

## Avoid when

- 要改现有页——`slides.replace_slide`；整份新建——`slides.create_presentation`。

## Prerequisites

- `xml_presentation_id` + 单页 XML；需要 `slides:presentation:update` + `slides:presentation:write_only`。
- 要插到某页前，先用 `slides.list_slides` 取该页 `slide_id`。

## Tips

- `before_slide_id` **留省**才是末尾；传空字符串会被判为「未知页面」。
- 多页请逐页调用（本工具不接页面数组）。
- 重试用同一个稳定 `idempotency_key`，别用随机值。
