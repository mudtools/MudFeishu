## WHEN

- 需要把已有页面作为**图片交付**（发图、存档、给用户看排版效果）。

## Avoid when

- 想知道「这页讲了什么」——`slides.get_slide` 的文本更省窗口。
- 要渲染尚未落稿的 XML——`slides.render_slide_image`。

## Prerequisites

- 选择器二选一：`slide_ids`（来自 `slides.list_slides`）或 `page_start`/`page_end`，单次最多 10 页。
- 需要 `slides:presentation:screenshot`，且宿主已配置附件落盘器。

## Tips

- 图片字节不进上下文，结果只回 `local_path` / `size` / `content_type`。
- 超过 10 页分批调用，不要试图一次取完整份。
- 交付物落盘后不调用清理钩子，路径可直接交给用户。
