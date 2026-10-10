---
name: feishu-ai
description: 飞书 ai 域工具集：2 个工具（只读 2 / 写 0）。跨语言沟通：把一段文本翻译成另一种语言 → `ai.translate_text`。
version: 1.0.0
---

# AI 文本能力（ai.*）

## 路由优先级（先判断该不该用本域）
- **跨语言沟通**：把一段文本翻译成另一种语言 → `ai.translate_text`。
- **不确定语言**：先 `ai.detect_language` 拿语言代码，再决定翻不翻、翻成哪种语言。
- **图片 / 扫描件 / 语音里的内容**：本域**没有** OCR 与语音转文字工具——模型没有文件系统，
  也不该把 MB 级 base64 塞进参数；如实告知用户"需要宿主侧先转成文本"，不要臆造调用。

## 前置链
目标语言未指定时先问用户；源语言未知时先用 `ai.detect_language`。
需要术语统一时，在 `ai.translate_text` 的 glossary 里给"原文→译文"配对（最多 128 项，仅本次生效）。

## 避坑
- `text` 上限 1000 字符：超长会被本地拒绝并要求分段，不要原样反复重试。
- 语言代码用平台口径（zh / zh-Hant / en / ja / ru / de / fr / it / pl / th / hi / id / es / pt / ko / vi），
  **大小写不敏感**；不要传"中文""English"这类语言名。
- 翻译结果是**机器翻译**：专有名词、代码、金额数字务必复核后再对外使用。
- 术语表只对本次调用生效，不会沉淀为平台的翻译记忆。

## 安全规则
- 本域工具均为只读（无副作用，无 dry_run 语义）；调用需 scope `translation:text`。
- 不要把保密内容（密钥、个人敏感信息）送进翻译——结果会进入第三方模型上下文。
- 只使用本域真实工具名，不臆造不存在的工具或接口。

## 示例
「把这段话翻成英文」→ `ai.translate_text`（目标语言取 en）。
「这段是德语吗」→ `ai.detect_language` → 据返回的语言代码如实回答。

## 命令（工具）清单
- `ai.detect_language`（只读）：识别一段文本的语种（飞书机器翻译语种识别），返回 ISO 639-1 语言代码与可读语言名（如 en / 英语）。用于在翻译、检索、审阅前先确定内容语言。只读，需 translation:text。
- `ai.translate_text`（只读）：把文本翻译成目标语言（飞书机器翻译）。text 上限 1000 字符（超限本地拒绝，不会下发）；source_language / target_language 用语言代码（zh / zh-Hant / en / ja / ru / de / fr / it / pl / th / hi / id / es / pt / ko / vi，大小写不敏感），不确定源语言时先用 ai.detect_language 识别。可选 glossary 传本次生效的术语表（JSON 数组…
