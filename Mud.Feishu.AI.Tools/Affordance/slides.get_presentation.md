## WHEN

- 要看整份 XML 演示文稿的原始 SML 结构（整体核对、导出改写、比对页序）。

## Avoid when

- 只想改某一页——先 `slides.list_slides` 取 slide_id，再用 `slides.get_slide`；整份读入会白占窗口。
- 要导出图片——`slides.list_slide_images`。

## Prerequisites

- `xml_presentation_id` 来自幻灯片链接的 `/slides/` 段，宿主须已启用幻灯片（Slides）API。
- 需要 `slides:presentation:read`。

## Tips

- 正文按窗口返回（默认 1 万字符）：续读带 `content_offset=next_offset`，`has_more=false` 即读完。
- `remove_attr_id=true` 会剥掉元素 id，**只适合阅读**，之后无法做元素级替换。
- `revision_id=-1` 或缺省即最新版。
