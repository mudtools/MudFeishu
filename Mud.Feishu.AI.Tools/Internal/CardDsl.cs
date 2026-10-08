// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// R5 / F-6：<b>卡片窄 DSL 编译器</b> —— 把结构化文本编译为飞书 <c>interactive</c> 卡片 JSON。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它</b>：`im.reply_message` 的 <c>content</c> 要求模型<b>手造 JSON 字符串</b>，
/// 而官方明确禁止该做法（`skills/lark-im/SKILL.md`：「禁止手写卡片 payload」）。
/// 本编译器让模型只描述"卡片长什么样"，由代码产出 JSON —— <b>模型全程零 JSON 字符串</b>。
/// </para>
/// <para>
/// <b>语法（窄 DSL，D-6：不做通用卡片透传、不做可写表达式 DSL）</b>：
/// <list type="bullet">
/// <item><c>body</c> 每行一个元素：<c>text:内容</c> / <c>quote:内容</c> / <c>code:内容</c> / <c>divider</c></item>
/// <item><c>buttons</c> 每行一个按钮：<c>文本|url|链接</c> 或 <c>文本|value|回调值</c></item>
/// </list>
/// </para>
/// <para>
/// <b>⚠️ 编译目标的选取依据（勿臆造 tag）</b>：本仓 Demo
/// <c>Demos/TaskManageDemo/backend/Services/Feishu/FeishuNotificationService.cs</c>
/// 已有<b>经实战使用</b>的卡片构造（<c>MsgType="interactive"</c> +
/// <c>{zh_cn:{title, elements:[div/lark_md, action/button]}}</c>），故编译目标
/// <b>照抄该已验证形态</b>，不引入未经本仓验证的新 tag。
/// 因此 <c>quote</c>/<c>code</c> 不臆造新元素类型，而是借 <c>lark_md</c> 的
/// <c>&gt;</c> 与 ``` 围栏表达（只依赖已验证的 <c>div</c>+<c>lark_md</c> 一种原语）。
/// </para>
/// </remarks>
internal static class CardDsl
{
    /// <summary>元素关键字（body 行首）。</summary>
    internal static class Keywords
    {
        public const string Text = "text";

        public const string Quote = "quote";

        public const string Code = "code";

        public const string Divider = "divider";
    }

    /// <summary>按钮回调类型（buttons 行第二字段）。</summary>
    internal static class Callbacks
    {
        public const string Url = "url";

        public const string Value = "value";
    }

    /// <summary>合法组合提示（出现在所有错误消息里，让模型可自我纠正 —— 对齐 F-8）。</summary>
    internal const string LegalForms =
        "合法组合：body 每行形如 'text:内容' / 'quote:内容' / 'code:内容' / 'divider'（divider 不带内容）；"
        + "buttons 每行形如 '按钮文本|url|https://…' 或 '按钮文本|value|回调值'。";

    /// <summary>单次卡片的元素数上限（防止模型生成超长卡片把上下文撑爆）。</summary>
    private const int MaxElements = 30;

    /// <summary>单个按钮文本的字符上限（平台约束量级；宁紧勿松）。</summary>
    private const int MaxButtonTextLength = 40;

    /// <summary>
    /// 编译为 <c>interactive</c> 消息的 <c>content</c> 结构。
    /// </summary>
    /// <param name="title">卡片标题（可空：留空则不输出 header 标题）。</param>
    /// <param name="body">正文 DSL。必填且至少产出 1 个元素，否则抛错。</param>
    /// <param name="buttons">按钮 DSL（可空）。</param>
    public static JsonObject Compile(string? title, string body, string? buttons)
    {
        var elements = ParseElements(body);
        var action = ParseButtons(buttons);
        if (action is not null)
        {
            elements.Add(action);
        }

        var card = new JsonObject { ["elements"] = elements };

        if (!string.IsNullOrWhiteSpace(title))
        {
            card["title"] = title!.Trim();
        }

        return new JsonObject { ["zh_cn"] = card };
    }

    /// <summary>解析正文 DSL 为卡片元素数组。</summary>
    private static JsonArray ParseElements(string? body)
    {
        var elements = new JsonArray();

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException($"body 不能为空 —— 卡片至少要有一个元素。{LegalForms}");
        }

        foreach (var raw in SplitLines(body!))
        {
            var line = raw.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (elements.Count >= MaxElements)
            {
                throw new ArgumentException(
                    $"卡片元素超过 {MaxElements} 个（已截断前 {MaxElements} 个），请精简后再发送。{LegalForms}");
            }

            elements.Add(ParseElement(line));
        }

        if (elements.Count == 0)
        {
            throw new ArgumentException($"body 未产出任何元素。{LegalForms}");
        }

        return elements;
    }

    /// <summary>解析单行元素。</summary>
    private static JsonNode ParseElement(string line)
    {
        // divider 是唯一"不带内容"的元素，必须优先判定（它没有 ':'）。
        if (string.Equals(line, Keywords.Divider, StringComparison.OrdinalIgnoreCase))
        {
            return new JsonObject { ["tag"] = "hr" };
        }

        var separator = line.IndexOf(':');
        if (separator <= 0)
        {
            throw new ArgumentException(
                $"卡片元素 '{line}' 缺少类型前缀（应为 '类型:内容'）。{LegalForms}");
        }

        var keyword = line[..separator].Trim();
        var content = line[(separator + 1)..].Trim();

        return keyword.ToLowerInvariant() switch
        {
            Keywords.Text => TextElement(content),
            Keywords.Quote => TextElement(Quote(content)),

            // 代码块借 lark_md 围栏表达：不臆造 code_block 之类的新 tag（本仓无验证先例）。
            Keywords.Code => TextElement($"```\n{content}\n```"),
            _ => throw new ArgumentException(
                $"未知的卡片元素类型 '{keyword}'。{LegalForms}"),
        };
    }

    /// <summary>引用：lark_md 的 <c>&gt;</c> 语法（逐行加前缀，多行也成立）。</summary>
    private static string Quote(string content)
        => string.Join("\n", SplitLines(content).Select(static l => "> " + l.Trim()));

    private static JsonObject TextElement(string content)
        => new()
        {
            ["tag"] = "div",
            ["text"] = new JsonObject { ["tag"] = "lark_md", ["content"] = content },
        };

    /// <summary>
    /// 解析按钮 DSL 为 <c>action</c> 元素；无按钮时返回 <see langword="null"/>（不输出空 action）。
    /// </summary>
    private static JsonNode? ParseButtons(string? buttons)
    {
        if (string.IsNullOrWhiteSpace(buttons))
        {
            return null;
        }

        var actions = new JsonArray();
        foreach (var raw in SplitLines(buttons!))
        {
            var line = raw.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            actions.Add(ParseButton(line, actions.Count));
        }

        return actions.Count == 0
            ? null
            : new JsonObject { ["tag"] = "action", ["actions"] = actions };
    }

    /// <summary>解析单个按钮：<c>文本|回调类型|值</c>。</summary>
    private static JsonNode ParseButton(string line, int index)
    {
        var fields = line.Split('|');
        if (fields.Length != 3)
        {
            throw new ArgumentException(
                $"按钮 '{line}' 应为 '文本|回调类型|值' 三段（用 | 分隔），实际 {fields.Length} 段。{LegalForms}");
        }

        var text = fields[0].Trim();
        var callback = fields[1].Trim().ToLowerInvariant();
        var value = fields[2].Trim();

        if (text.Length == 0)
        {
            throw new ArgumentException($"按钮文本不能为空：'{line}'。{LegalForms}");
        }

        if (text.Length > MaxButtonTextLength)
        {
            throw new ArgumentException(
                $"按钮文本 '{text}' 超过 {MaxButtonTextLength} 字。{LegalForms}");
        }

        if (value.Length == 0)
        {
            throw new ArgumentException($"按钮的回调值不能为空：'{line}'。{LegalForms}");
        }

        var button = new JsonObject
        {
            ["tag"] = "button",
            ["text"] = new JsonObject { ["tag"] = "plain_text", ["content"] = text },

            // 首个按钮主色、其余默认色：让"主操作"在视觉上可分辨。
            ["type"] = index == 0 ? "primary" : "default",
        };

        switch (callback)
        {
            case Callbacks.Url:
                button["url"] = value;
                break;
            case Callbacks.Value:
                button["value"] = new JsonObject { ["key"] = value };
                break;
            default:
                throw new ArgumentException(
                    $"未知的按钮回调类型 '{fields[1].Trim()}'（只支持 url / value）。{LegalForms}");
        }

        return button;
    }

    /// <summary>按行拆分（同时容忍 \r\n 与 \n；netstandard2.0 无 <c>Split(char, options)</c> 重载）。</summary>
    private static string[] SplitLines(string text)
        => text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
}
