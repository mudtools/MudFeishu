---
name: feishu-slides
description: 飞书 slides 域工具集：12 个工具（只读 7 / 写 5）。XML 演示文稿（链接含 `/slides/`）→ `slides.*`；普通文档正文属 docx 域，云盘文件本体属 drive 域。
version: 1.0.0
package_version: 3.1.1
---

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

## 命令（工具）清单
- `slides.add_slide`（写）：在已有演示文稿中插入一页 SML XML，返回新页 slide_id 与版本号。默认追加到末尾；before_slide_id 指定插到某一页之前（留空=末尾，⚠️ 传空字符串会被判为「未知页面」而不是「末尾」）。同内容多页请逐页调用，本工具不接页面数组。返回的 issues 是 XML 语法告警，须如实转达。写操作：可用 dry_run=true… 〔需 slides:presentation:update 与 slides:presentation:write_only〕
- `slides.create_presentation`（写）：以整份 SML 2.0 XML 新建一个演示文稿，返回新文稿 token、链接、版本号与页面 ID 列表。content 是 <presentation xmlns="http://www.larkoffice.com/sml/2.0" width="960" height="540">…</presentation> 整份文档；平台不单独收标题… 〔需 slides:presentation:create 与 slides:presentation:write_only〕
- `slides.delete_slide`（写）：删除演示文稿中的指定页面（⚠️ 不可恢复：平台不提供恢复被删页面的接口，误删只能靠 slides.revert_history 回到历史版本）。slide_id 必填且必须来自 slides.list_slides 的实际结果，不要凭页码猜测。写操作（高危）：可用 dry_run=true 预演；需 slides:presentation:update 与 slides:presentation:write_only。
- `slides.get_presentation`（只读）：按 xml_presentation_id 获取 XML 演示文稿的整份 SML 2.0 内容（分窗口返回：content_offset/content_limit，默认 1 万字符，回填 content_total_length 与 has_more/next_offset 可续读）。revision_id=-1 表示最新版；remove_attr_id=true 会剥掉 XML 的 id 属性（只适合阅读… 〔需 slides:presentation:read〕
- `slides.get_revert_status`（只读）：查询历史版本回滚任务的执行状态（status：done/partial_failed/running/failed，失败时给出 failed_block_tokens）。slides.revert_history 是异步任务，必须用它确认结果；按返回的 poll_after_ms 间隔轮询，不要连续空轮。只读，需 slides:presentation:read。
- `slides.get_slide`（只读）：获取演示文稿中某一页的 SML 2.0 XML（改这一页前先读它）。slide_id 与 slide_number 二选一必填（slide_number 从 1 起，page 1 = 第一页）；两者同传或都不传都会在本地被拒绝，不消耗下游。单页 XML 通常几 KB，直接进上下文；整副文稿请用 slides.get_presentation。只读，需 slides:presentation:read。
- `slides.list_histories`（只读）：分页列出 XML 演示文稿的历史版本（revision_id、编辑时间、类型、名称、编辑者与可回滚的 history_version_id）。用于「改坏了想回滚」前先看清有哪些版本。只读，需 slides:presentation:read。
- `slides.list_slide_images`（只读）：把演示文稿的既有页面渲染为服务端截图并交宿主落盘，结果只回 local_path/size/content_type——图片字节不进上下文（no-bytes-in-context）。选择器二选一：slide_ids（来自 slides.list_slides）或 page_start/page_end 连续页码（从 1 起），单次最多 10 页。要看「这页讲了什么」用… 〔需 slides:presentation:screenshot〕
- `slides.list_slides`（只读）：列出 XML 演示文稿的页面清单（序号、slide_id、页数与正文文本预览）——编辑某一页前的第一步。平台无「列页面」接口，本工具在本地解析整份 XML 得到清单（不返回 XML 本身）。remove_attr_id=true 或平台剥属性时 slide_id 可能为空，此时改用 slide_number 定位。只读，需 slides:presentation:read。
- `slides.render_slide_image`（只读）：把一段 SML 2.0 页面 XML 渲染成截图并交宿主落盘（只回 local_path/size/content_type，字节不进上下文）。不写入任何演示文稿、也不产生历史版本——适合「先看效果再决定要不要落稿」。content 是单页 <slide> 片段；要导出已有页面请用 slides.list_slide_images。只读，需 slides:presentation:screenshot。
- `slides.replace_slide`（写）：替换演示文稿页面的内容（parts 为 JSON 数组字符串，1~200 项，每项 action 取 block_replace 或 block_insert）。整页重建：block_replace + block_id 填页面 ID 本身、replacement 给整份 <slide> XML（⚠️ 新 XML 里没写的内容会从页面上消失）… 〔需 slides:presentation:update 与 slides:presentation:write_only〕
- `slides.revert_history`（写）：把整副演示文稿回滚到指定历史版本（⚠️ 异步任务 + 整副覆盖：回滚点之后的所有编辑都会消失，包括其他人刚改的内容）。history_version_id 必须来自 slides.list_histories（正整数字符串，不是 revision_id）。调用成功只代表任务已提交，须用 slides.get_revert_status 按… 〔需 slides:presentation:update 与 slides:presentation:write_only〕
