
# 幻灯片（slides.*）

## 路由优先级
- **XML 演示文稿**（链接含 `/slides/`）→ `slides.*`；普通文档正文属 docx 域，云盘文件本体属 drive 域。
- 要看或改**某一页** → 先 `slides.list_slides` 取 id，再 `slides.get_slide`；只有核对整体结构才读整份。
- 要**图片交付** → `slides.list_slide_images`；先在稿外看效果（不落库）→ `slides.render_slide_image`。

## 前置链
- 改页：`slides.list_slides`（页码↔slide_id）→ `slides.get_slide`（完整单页 XML）→ 改写 → `slides.replace_slide`。
- 新建：`slides.create_presentation`（整份 XML）→ 用返回的 slide_ids 逐页 `slides.add_slide`。
- 回滚：`slides.list_histories` → `slides.revert_history` → `slides.get_revert_status` 确认落地。

## 避坑
- **整份是窗口化读的**：正文可达数百 KB，按 `content_offset`/`content_limit` 分窗口，续读取 `next_offset`。
- **单页才给全文**：截断过的 XML 回填平台会静默丢内容，改页一律用 `slides.get_slide`。
- 剥属性（`remove_attr_id=true`）只适合阅读：没有元素 id 就做不了元素级替换。
- `history_version_id` 不是 `revision_id`，回滚要的是前者（正整数字符串）。
- 平台告警必须转达：`issues`（已写入但 XML 有偏差）、`failed_part_index`（部分动作失败）。

## 安全规则
- 5 枚写工具默认不启用（`FeishuAgent:WriteAllowList` + 宿主授权），首次一律 `dry_run=true`；
  其中 `slides.delete_slide` 属**高危写**（不可恢复），`slides.revert_history` 会整副覆盖后续编辑。
- 截图字节落宿主盘、只回 `local_path`；字节不进上下文。
- 只用本域真实工具名；未策展的能力先用 `feishu.capability_lookup` 确认「是没有还是没启用」。

## 示例
- 「把第 2 页标题改成 X」→ `slides.list_slides` → `slides.get_slide` → `slides.replace_slide`。
- 「只要前三页的截图」→ `slides.list_slide_images`（page_start=1、page_end=3）。
