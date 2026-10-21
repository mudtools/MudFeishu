---
name: feishu-suite
description: 飞书工具面单入口路由：34 个域技能包、365 个工具（只读 188 / 写 177）。先按域路由表选定技能包，再进包读工具清单与前置链。
version: 1.0.0
package_version: 3.0.3
---

# 飞书工具面 · 单入口路由

## 读法（三层渐进披露）
1. 在本文件的域路由表里按**对象与动作**选定一个域。
2. 加载该域技能包：路由优先级、前置链、避坑、安全规则与逐工具清单都在包内。
3. 只有需要结构化语法（卡片 DSL、筛选排序、话题与回复判别等）时再读包内 `references/*.md`。

## 域路由表（34 个域）
- `feishu-ai`（2 工具：只读 2 / 写 0）：AI 文本能力（ai.*）｜**跨语言沟通**：把一段文本翻译成另一种语言 → `ai.translate_text`
- `feishu-aily`（3 工具：只读 3 / 写 0）：Aily · 智能伙伴数据知识｜「这个应用接了哪些知识库」→ `aily.list_data_assets`（**本域第一跳**，`data_asset_id` 从这来）
- `feishu-application`（9 工具：只读 7 / 写 2）：Application · 飞书应用管理｜不知道有哪些应用 / 不知道 `app_id` → `application.list_apps`（**本域第一跳**）
- `feishu-approval`（8 工具：只读 2 / 写 6）：审批（approval.*）｜出现「待办 / 待批」时先分域：来自**审批流程**才用 `approval.*`；来自**任务清单**用 `task.*`
- `feishu-attendance`（8 工具：只读 7 / 写 1）：考勤（attendance.*）｜**查打卡记录**（"我昨天几点打的卡"）→ `attendance.query_my_flow`（user 身份，只能查本人）；
- `feishu-bitable`（28 工具：只读 13 / 写 15）：多维表格（bitable.*）｜**多维表格**（记录/字段/视图）→ `bitable.*`；**电子表格**（A1 单元格区域）→ `sheets.*`
- `feishu-board`（6 工具：只读 2 / 写 4）：画板（board.*）｜**要"把推理结果画出来"**（架构图 / 流程图 / 时序图 / 思维导图）→ 一律走 `board.render_dsl`：
- `feishu-calendar`（9 工具：只读 4 / 写 5）：日历（calendar.*）｜**日程 / 会议室忙闲** → `calendar.*`；**任务截止时间** → `task.*`
- `feishu-chatgroup`（16 工具：只读 4 / 写 12）：Chatgroup · 群管理｜「我们有哪些群 / 这个 chat_id 对应哪个群」→ `chatgroup.list_chats`（**机器人所在的群**）
- `feishu-contact`（8 工具：只读 8 / 写 0）：通讯录（contact.*）｜**把「人」换成 open_id / 查部门** → `contact.*`；拿到 open_id 之后的动作（发消息、建任务、发邮件）
- `feishu-docx`（19 工具：只读 9 / 写 10）：云文档（docx.*）｜**新版文档正文（块结构）** → `docx.*`；**电子表格** → `sheets.*`；**知识库节点定位** → `wiki.*`；
- `feishu-drive`（17 工具：只读 5 / 写 12）：云空间（drive.*）｜**文件在哪 / 叫什么 / 多大 / 谁所有** → `drive.*`；**取正文** → `docx.*`（文档）或 `sheets.*`（表格）；
- `feishu-feishu`（4 工具：只读 4 / 写 0）：能力出处与元工具（feishu.*）｜「这个能力有没有」→ `feishu.capability_lookup`（**分组级**存在性）；
- `feishu-helpdesk`（10 工具：只读 6 / 写 4）：Helpdesk · 飞书服务台｜「这个问题有没有现成答复」→ `helpdesk.search_faqs`（服务台 FAQ 是**客服口径**的知识源）
- `feishu-hire`（7 工具：只读 7 / 写 0）：招聘（hire.*：职位 / 需求 / 候选人 / 面试 / Offer）｜**职位**（对外岗位）→ `hire.list_jobs`、`hire.get_job`（非 PII）
- `feishu-im`（24 工具：只读 10 / 写 14）：消息（im.*）｜**给人发消息 / 读聊天记录** → `im.*`；**给人发邮件** → `mail.*`；**给人建待办** → `task.*`
- `feishu-knowledge`（1 工具：只读 1 / 写 0）：知识库问答（knowledge.search）｜「文档里写过什么、流程怎么走」→ `knowledge.search`；**「数据现在是多少」** → docx / sheets / bitable
- `feishu-lingo`（11 工具：只读 6 / 写 5）：Lingo · 企业词典｜不知道租户有哪些词库 → `lingo.list_repos`（**本域第一跳**，`repo_id` 来源）
- `feishu-mail`（24 工具：只读 10 / 写 14）：邮件（mail.*）｜**邮件**（mailbox / thread）→ `mail.*`；**即时消息** → `im.*`
- `feishu-markdown`（5 工具：只读 2 / 写 3）：Markdown 生命周期（markdown.*）｜**新建**一份在线文档 → `markdown.create`（产物是 docx，可继续被本域改写）
- `feishu-mdm`（1 工具：只读 1 / 写 0）：主数据 · 国家/地区（mdm.*）｜**国家 / 地区 / ISO 代码 / 国际区号 / 大洲** → `mdm.*`；**时区换算、日期本地化**不属于本域，
- `feishu-minutes`（10 工具：只读 6 / 写 4）：妙记（minutes.*）｜**会议纪要 / 妙记内容** → `minutes.*`；**会议本身**（参会人、录制、预约、会议室）属会议域，
- `feishu-okr`（18 工具：只读 11 / 写 7）：OKR（okr.*）｜**目标 / 关键结果 / 进展** → `okr.*`；**任务待办** → `task.*`；**审批待办** → `approval.*`
- `feishu-org`（8 工具：只读 8 / 写 0）：组织元数据（org.*：职务 / 职级 / 职务族 / 工作城市）｜拿到的是**组织元数据 ID** → 本域：`job_title` → 职务；`job_level_id` → 职级；`job_family_id` → 职务族；`city` → 工作城市
- `feishu-search`（1 工具：只读 1 / 写 0）：搜索（search.doc_wiki）｜**不知道东西在哪、先定位** → `search.doc_wiki`；**已知位置、要取数** → docx / sheets / bitable
- `feishu-security`（2 工具：只读 2 / 写 0）：安全与合规 · 行为审计日志（security.*）｜**"谁动了这份文档 / 谁的权限被改了 / 谁删了什么 / 某人的异常操作"** → `security.*`；
- `feishu-sheets`（25 工具：只读 8 / 写 17）：电子表格（sheets.*）｜**A1 单元格区域（行列网格）** → `sheets.*`；**多维表格（字段 / 记录 / 视图）** → `bitable.*`
- `feishu-slides`（12 工具：只读 7 / 写 5）：幻灯片（slides.*）｜**XML 演示文稿**（链接含 `/slides/`）→ `slides.*`；普通文档正文属 docx 域，云盘文件本体属 drive 域
- `feishu-spark`（10 工具：只读 5 / 写 5）：妙搭（spark.*）｜**建应用 / 改应用**（名称、描述、图标）→ `spark.create_app` / `spark.patch_app`
- `feishu-task`（20 工具：只读 5 / 写 15）：任务（task.*）｜「待办」来自**任务清单** → `task.list_my_tasks`；来自**审批流程** → `approval.*`
- `feishu-trustparty`（11 工具：只读 8 / 写 3）：Trustparty · 跨组织互通｜「和哪些组织互通」→ `trustparty.list_collaboration_tenants`（互通面）
- `feishu-vc`（14 工具：只读 7 / 写 7）：视频会议（vc.*）｜**会议本身**（谁参加了、开了多久、有没有录制、会议室）→ `vc.*`；**会议纪要内容** → `minutes.*`；
- `feishu-wiki`（12 工具：只读 5 / 写 7）：知识库（wiki.*）｜**知识库节点树的定位与移动** → `wiki.*`；**取正文** → `docx.*`（文档）或 `sheets.*`（表格）；
- `feishu-workflow`（2 工具：只读 2 / 写 0）：复合工作流（workflow.*）｜要做「多源汇总成一份报告」→ `workflow.*`；单次查询/写入直接用对应域工具

## 尾注约定
- 域包清单里被截断的长描述会在行尾补 `〔…〕` 尾注，回填该工具的权限串与非默认身份要求。
- 域包正文里的 `feishu.guidance_read(域, 主题)` 指针，在文件形态下就是那个包的 `references/主题.md`。
- 写工具默认不启用，须经宿主授权（`IToolExecutionAuthorizer`）；判据与预演方式在各域包的安全规则小节给出。
