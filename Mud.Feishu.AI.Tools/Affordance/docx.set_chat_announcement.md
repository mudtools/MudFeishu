## WHEN

- 群公告里已有的某些块要换文案（改值班人、改时间地点），改完全群即刻可见。

## Avoid when

- 公告要增删块或调整层级——本工具只替换块的文本元素，做不到。
- 想往群里发条消息而不是改公告——那是 `im.send_message`，公告与消息是两个对象。
- 不确定公告现有内容与块 ID——先 `docx.get_chat_announcement`。

## Prerequisites

- `chat_id` 来自 `chatgroup.list_chats` 或 `im.search_chats`，本工具不需要也不接受文档 ID。
- 待改的块 ID 必须取自 `docx.get_chat_announcement`：公告自带一套块结构，别拿普通文档的块 ID 来改。

## Tips

- 广播面无草稿态：写下去就是全员生效，先 `dry_run` 核对将下发的条数。
- 整批在调用下游前逐条校验，一条缺文本就整批拒绝，不会出现改了一半。
- 返回只给下发条数，不逐块回执；要确认结果就再读一次公告。
