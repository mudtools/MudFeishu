## WHEN

- 改坏了想回滚：先列出历史版本，拿到可回滚的 `history_version_id`。

## Avoid when

- 要的是当前内容——`slides.get_presentation`，历史列表不含正文。
- 只想改某一页——`slides.get_slide` + `slides.replace_slide`，比回滚代价小得多。

## Prerequisites

- `xml_presentation_id`；需要 `slides:presentation:read`。

## Tips

- 回滚要用 `history_version_id`，**不是** `revision_id`，两者别混填。
- 版本较多时用 `fetch_all=true` 在预算内自动翻页，别手翻十几页。
