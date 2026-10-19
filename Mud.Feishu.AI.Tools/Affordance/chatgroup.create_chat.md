## WHEN
- 为推进某件事拉起一个新群，且要一次带上若干成员。
- 需要新群自带可见性与入群审批设定，避免事后补配置。

## Avoid when
- 往已有群里补人——用 `chatgroup.add_member`。
- 改已有群的名称或权限——那是 `chatgroup.update_chat`。
- 撤掉刚建错的群——用 `chatgroup.delete_chat`。

## Prerequisites
- `user_id_list` 不传则新群里只有机器人自己；带人时 ID 口径跟 `user_id_type` 对齐。
- 宿主需启用群管理 API 并放行该写工具，否则只会拿到"未注册客户端"的说明。

## Tips
- 建群后群主就是机器人自身身份；群主转让没有对应工具，不要向用户承诺。
- 拉人有无效时本投影只回 chat_id，不列失败明细——要核对成员用 `im.list_chat_members`。
- 默认值偏保守：chat_type 是 private、edit_permission 是 all_members，要换必须显式传。
