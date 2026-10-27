## WHEN

- 机器人答不上用户的提问，需要把会话切给人工客服——「转人工」的第一跳。
- 尚未建单时也可直接转：按 open_id 发起，回执里会出现新 ticket_id。

## Avoid when

- FAQ 已能回答——用 `helpdesk.answer_user_query` 回传命中即可。
- 要手工写回复——用 `helpdesk.create_ticket_message`。
- 想看工单进展——用 `helpdesk.get_ticket`，本工具是写动作。

## Prerequisites

- open_id 必填：发起转人工的终端用户，不是客服。
- appointed_agents 是指定客服 ID 列表，须是服务台在编客服；技能组配置可先看 `helpdesk.list_agent_skills`。

## Tips

- 指定客服未上线不算失败：服务台会按技能组重新分配，别因没等到指定人就反复重试。
- 转人工后客服接管会话；customized_info 上限 1024 字符，用于交接补充信息。
