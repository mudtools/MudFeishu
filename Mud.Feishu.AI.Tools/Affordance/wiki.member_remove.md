## WHEN

- 有人转岗、离职或项目收尾，不该再看到某个知识空间。

## Avoid when

- 只想降权（admin 改 member）——本工具只撤不降，需用 `wiki.member_add` 以新角色重加。
- 只想让某个人看不到某一页——撤整个空间是过度反应，应改那个子树的权限。
- 还没核对身份三元组——先 `wiki.member_list` 拿准再动手。

## Prerequisites

- 调用身份须是该空间的管理员，否则平台直接报权限不足。
- 定位靠 member_id + member_type + member_role 三项，必须与 `wiki.member_list` 返回的一字不差。
- 建议先 `dry_run=true` 预演，确认三项与 space_id 后再实删。

## Tips

- 角色填错不会报错，只会「删不到人」：回执 removed=true 不等于对方真的失去访问权，撤完再 `wiki.member_list` 复验一次。
- 一次只撤一个身份，没有「清空整个空间成员」的入口；批量撤人须逐次调用并向用户报进度。
