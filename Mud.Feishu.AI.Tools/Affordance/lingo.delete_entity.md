## WHEN
- 一个词条确认作废（项目下线、旧口径被新词条取代），且已与管理后台的词典管理员确认过。
- 建错的词条需要撤掉（拼写错误、重复词条、误建到非目标词库）。

## Avoid when
- 只是想改释义或别名——用 `lingo.update_entity`，词条与 entity_id 都保留。
- 只是想改个名——用 `lingo.update_entity` 覆盖 `main_key`。
- 还没确认这个词条有没有被引用——外部系统里引用该 entity_id 的地方会全部失效，先排查再删。
- 有审核要求——删除走免审通道，不可撤销；若流程要求留痕，请在管理后台操作。

## Prerequisites
- 先用 `lingo.search_entities` / `lingo.get_entity` 核对 entity_id，确认目标词条无误。
- 与词典管理员确认过：删除后**无法恢复**，且引用该 entity_id 的外部系统（如内部 Wiki、IM 机器人）会失效。
- 宿主已授权该写工具（默认不启用），且 `MaxToolRisk` 未把它挡掉。

## Tips
- **删除不可逆**：平台不提供"恢复已删词条"入口，只能重建且会拿到**新的 entity_id**。
- 务必先 `dry_run` 预演，核对 entity_id 后再以 `dry_run=false` 执行。
- 删除后可用 `lingo.search_entities` 复核：原词条名应搜不到。
- 若只是想暂停某个词条被搜索到，可改用 `lingo.update_entity` 关掉 `allow_search`，比删除温和得多。
