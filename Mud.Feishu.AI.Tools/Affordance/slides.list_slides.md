## WHEN

- 编辑某一页之前的第一步：拿页面清单（页码、slide_id、页数、正文预览）。

## Avoid when

- 要读正文细节——用 `slides.get_slide` 或 `slides.get_presentation`，清单只给 120 字预览。
- 要导出图片——`slides.list_slide_images`。

## Prerequisites

- `xml_presentation_id`；需要 `slides:presentation:read`。

## Tips

- 平台**没有**「列页面」接口，本工具在本地解析整份 XML，不返回 XML 本身。
- 预览里认不出目标页时逐页 `slides.get_slide` 确认，不要凭页码拼 slide_id。
- 平台剥属性或读到无 id 的 XML 时 `slide_id` 可能为空，此时改用 slide_number 定位。
