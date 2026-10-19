# Affordance 资产写作规范（per-tool 决策指导）

> 适用面：`Mud.Feishu.AI.Tools/Affordance/{工具名}.md`。机械防线：上游诊断 `MUDFT028–031`
> （结构 / 文件名 / 反引号引用存在性 / 字符上界）+ 本仓 `AffordanceContractGuards`
> （接线一致性 / 强制集合 / 规模冻结 / 模型可见性）与 `AffordanceCoverageContractGuards`
> （按风险分级的最低覆盖率）。

## 一、这份资产是什么

affordance 会被生成器**拼进工具描述的模型可见面**（描述里出现 `【决策指导】` 块），
所以它是「模型在决定要不要调这个工具时读到的判断依据」，不是给人看的接口文档。

- 它回答四个问题：**什么时候该用 / 什么时候别用 / 用之前必须有什么 / 用它时会踩什么**。
- 它**不复述** Description 已经说过的话（参数列表、HTTP 语义）；复述就是浪费 token 预算。
- 每一行都要能改变模型的行为：删掉这行，模型的决策会不会变差？不会就删。

## 二、结构（四节，顺序与标题固定）

```markdown
## WHEN

- ……（1–3 条，写触发场景，不写功能定义）

## Avoid when

- ……（2–4 条，与相邻工具/易混对象划界，优先写成「那是 `别的工具`」）

## Prerequisites

- ……（1–3 条，ID 从哪来、身份/权限/前置读取要求）

## Tips

- ……（1–3 条，幂等/不可逆/配额/静默失败等后果事实）
```

- 只有这四节，标题逐字节为 `## WHEN` / `## Avoid when` / `## Prerequisites` / `## Tips`。
- 每条都是 `-` 开头的单行要点；**四节都不得为空**（空节即构建期错误）。
- 语言：简体中文，面向模型的第二人称祈使句；不加称呼语、不加标题外的正文段落、不加链接与表格。

## 三、预算（字符数，按 LF 归一化统计）

| 上界（门禁） | 建议值（实际风格） |
|---|---|
| 单资产 ≤ 1024 字符 | **380–560 字符** |
| 单节正文 ≤ 320 字符 | **每节 60–200 字符** |

超上界是 `MUDFT031`；总字符数与份数由 `AffordanceContractGuards` 冻结，新增资产必须同批改基线数字。

自检命令（仓库根执行，无参数即扫描全目录，越界退出码 1）：

```powershell
powershell -File ./scripts/check-affordance-budget.ps1
powershell -File ./scripts/check-affordance-budget.ps1 -Paths Mud.Feishu.AI.Tools/Affordance/im.send_message.md
```

## 四、事实来源与禁止项

**只写能从下列位置查到的事实**：

1. `Mud.Feishu.AI.Tools/FeishuToolSchemas.golden.txt` —— 该工具的 `description` / `parameters` /
   `x-feishu`（`risk` / `identity` / `required_scopes` / `source.sdk` / `source.http` / `source.route`）；
2. `Mud.Feishu.AI.Tools/Curation/**` —— `[AgentTool]` 声明与 `[ToolParameter]` 说明；
3. `Mud.Feishu.AI.Tools/Internal/**` —— 执行器行为（`dry_run` 是否支持、校验顺序、分页、落盘约定）；
4. `Mud.Feishu.AI.Tools/Guidance/{域}.md` 与 `Guidance/{域}/*.md` —— 域级避坑（**不要照抄**，
   域级事实留在 guidance，资产只写该工具特有的判断）。

**禁止**：

- 编造参数名、字段名、ID 形态、权限点；不确定就不写。
- 反引号里出现**不存在**的名字。允许的反引号内容只有四类：
  ① `golden` 里逐字节存在的工具名；② 该工具自身 `parameters` 里的参数名；③ `dry_run`（且该工具确实支持）；
  ④ 该工具的**回执字段名与枚举值**——须能在实现它的 `Internal/**` 文件或它自己的 `Curation/**` 声明里
  逐字节找到（`MUDFT030` 只校验 `域.工具`，非点号片段**引擎不管**，所以这一类全靠人肉对账）。
  ⚠️ 只在**别的工具**那里出现过的名字不得借用：实测踩到 `okr.delete_key_result` 写 `kr_id`
  （真名 `key_result_id`）、`chatgroup.remove_member` 用 `chatgroup.is_in_chat` 复核成员
  （那是"当前身份在不在群"）——**逐字节存在 ≠ 属于本工具**，这是本规范最容易失守的一条。
- 引用已删除的兜底通道（万能调用通道已移除，不得出现在任何模型可见文案里）。
- 任务编号、轮次编号、提交号、文档路径等过程记录——资产是给模型读的，不是给项目管理者读的。
- 「请谨慎使用」这类没有信息量的兜底话术。

## 五、同域交叉引用的写法

`Avoid when` 的价值在于**把相邻工具划开**。先列出同域相邻工具（读 golden 里同前缀的工具名），
再写「想做 X 时用 `域.工具`，不是本工具」。宁可少写一条，也不要写一个不存在的名字。
