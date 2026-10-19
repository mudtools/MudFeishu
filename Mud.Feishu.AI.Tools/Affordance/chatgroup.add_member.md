## WHEN
- 项目群、评审群已经建好，要把此前漏掉的同事分批补进去。
- 外部协作方确定人选后，需要把人拉进既有对接群。

## Avoid when
- 要让机器人自己进群——那是 `chatgroup.me_join_chat`。
- 要把人清出群——用 `chatgroup.remove_member`。
- 建群与拉人一步完成——用 `chatgroup.create_chat` 的成员列表参数，不是本工具。

## Prerequisites
- `chat_id` 与 `id_list` 都必填；`id_list` 的口径由 `member_id_type` 声明（默认 open_id）。
- 若群开了入群审批，未即时进群的会落在回执的待审批清单里。

## Tips
- 回执给三份清单：`invalid_id_list` / `not_existed_id_list` / `pending_approval_id_list`，任一非空即有人没进群。
- `succeed_type` 默认 0：出现任一无效 ID 就整体失败；想先进能进的需显式改策略。
- `dry_run` 预演只报 chat_id 与将要拉入的人数，不下发。
