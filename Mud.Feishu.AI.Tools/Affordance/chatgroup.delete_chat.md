## WHEN
- 一个临时群完成使命（项目收尾、活动结束），且群内没有需要保留的沟通记录。
- 建错的群需要撤掉（名字写错、拉错人、重复建群）。

## Avoid when
- 只是想改群名或权限——用 `chatgroup.update_chat`，群与记录都保留。
- 只是想清人——用 `chatgroup.remove_member` 逐个移出，群还在。
- 记录可能还有用——先确认群内没有待办/附件/结论，解散后无法找回。

## Prerequisites
- 先用 `chatgroup.list_chats` 或 `im.get_chat` 核对 chat_id，确认目标群无误。
- 与群主/管理员确认过：解散影响全群成员，历史消息随群消失。
- 宿主已授权该写工具（默认不启用），且 `MaxToolRisk` 未把它挡掉。

## Tips
- **解散不可逆**：平台没有"恢复已解散群"入口，只能重建并重新拉人（新 chat_id）。
- 务必先 `dry_run` 预演，核对 chat_id 后再以 `dry_run=false` 执行。
- 执行后可用 `chatgroup.list_chats` 复核：目标群应已不在列表里。
- 只想暂停沟通而非清场——用 `chatgroup.update_chat` 改群名或收紧权限，群与记录都留下。
