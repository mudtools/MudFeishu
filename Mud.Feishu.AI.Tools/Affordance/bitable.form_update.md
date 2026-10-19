## WHEN

- 改表单整体：名称、描述、开放共享、分享范围、限制「仅可填写一次」。

## Avoid when

- 增删题目、调题目顺序或必填——用 `bitable.form_question_update`，本工具不碰题目。
- 把旧版表单升级成新版布局——用 `bitable.form_upgrade`，与元数据修改无关。
- 只想确认当前分享状态和填写次数——用 `bitable.form_get`。

## Prerequisites

- 先用 `bitable.form_get` 取回现值再改，本工具只覆盖你传入的项。
- `shared_limit` 仅在 `shared` 为 true 时有意义：off / tenant_editable / anyone_editable 三档。

## Tips

- 可选更新项一个都不传会被本地校验拒绝，至少传一项。
- `shared_limit` 设 anyone_editable 等于互联网上拿到链接的人都能填表，对外放开前先向用户确认。
- 关闭 `shared` 会让既有对外填写入口立即失效。
