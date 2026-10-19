## WHEN

- 发通知/待办/审批提醒：要点一眼可见，对方能直接点按钮。

## Avoid when

- 长篇正文或随口对话——那是 `im.send_message`。
- 要带图片或文件——卡片不带附件，用 `im.send_file` / `im.send_image`。
- 卡片已发出、内容变了——那是 `im.patch_card_message`，别重复发新卡。

## Prerequisites

- `receive_id` 与 `receive_id_type` 配对（缺省 chat_id）；发给个人先用 `contact.search_user` 拿 open_id。
- `body` / `buttons` 按逐行结构化语法写，非法组合在编译期报错并附合法组合，不进下游。

## Tips

- 按钮 value 的点击回调落在宿主端点，工具链里拿不到点击事件——不要把发送当成"等对方点完再继续"。
- 落写前 `dry_run` 核对编译出的卡片结构；`idempotency_key` 防 1 小时内重复送达。
