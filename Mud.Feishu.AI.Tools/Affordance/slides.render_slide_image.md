## WHEN

- 先在稿外看效果：把一段 SML 页面 XML 渲染成图片，再决定是否落稿。

## Avoid when

- 要导出文稿里已有的页面——`slides.list_slide_images`。
- 只想读文字——`slides.get_slide`。

## Prerequisites

- `content` 是单页片段；需要 `slides:presentation:screenshot` 与宿主落盘器。

## Tips

- 渲染**不写入任何文稿、也不产生历史版本**，试错没有副作用。
- 每个 `<shape>` 必须带一个 `<content/>`，否则平台渲染报错。
- 渲染结果满意后再用 `slides.add_slide` 或 `slides.replace_slide` 落稿。
