// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Mail;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// F-6 同款（模型零 JSON、零平台代码）：邮件收件规则的窄 DSL 编译器 —— 把
/// <c>field|operator|value</c> / <c>action</c> 结构化文本编译为平台代码，并把平台代码反向渲染回简化式。
/// </summary>
/// <remarks>
/// <para>
/// 代码表单一来源：<c>Mud.Feishu.DataModels.Mail</c> 下 <c>RuleConditionItem</c> / <c>RuleActionItem</c>
/// 的 XML 注释（平台定义）。本类是这批表的唯一编译/渲染入口：
/// <c>mail.rule_create/rule_update</c> 编译入参，<c>mail.rule_list</c> 反渲染出参。
/// </para>
/// <para>
/// 反向渲染保留未知代码（<c>unknown_field_{code}</c> 等）：平台未来新增代码经 rule_list
/// 透出时不丢失信息，模型可据此决定是否原样回写。
/// </para>
/// </remarks>
internal static class MailRuleDsl
{
    /// <summary>条件/动作数组的单项数上限（平台上限 32，宁紧勿松以尽早拦下失控输入）。</summary>
    internal const int MaxItems = 30;

    /// <summary>合法语法提示（出现在所有错误消息里，让模型可自我纠正）。</summary>
    internal const string LegalForms =
        "条件每项形如 'field|operator|value'（如 \"subject|contains|发票\"）；旗标型字段单独出现（all/is_external/is_spam/not_spam/has_attachment）；"
        + "field 可选 sender/recipient/cc/recipient_or_cc/subject/body/attachment_name/attachment_type/any_address/all/is_external/is_spam/not_spam/has_attachment；"
        + "operator 可选 contains/not_contains/starts_with/ends_with/is/is_not/contains_me/empty（contains_me/empty 不带 value）；"
        + "动作每项为单名（archive/delete/mark_read/to_spam/not_spam/add_flag/no_notify）或 'move_to_folder|文件夹ID'。";

    private const int MoveToFolderCode = 11;

    private static readonly (int Code, string Name)[] FieldTable =
    [
        (1, "sender"),
        (2, "recipient"),
        (3, "cc"),
        (4, "recipient_or_cc"),
        (6, "subject"),
        (7, "body"),
        (8, "attachment_name"),
        (9, "attachment_type"),
        (10, "any_address"),
        (12, "all"),
        (13, "is_external"),
        (14, "is_spam"),
        (15, "not_spam"),
        (16, "has_attachment"),
    ];

    private static readonly (int Code, string Name)[] OperatorTable =
    [
        (1, "contains"),
        (2, "not_contains"),
        (3, "starts_with"),
        (4, "ends_with"),
        (5, "is"),
        (6, "is_not"),
        (7, "contains_me"),
        (10, "empty"),
    ];

    private static readonly (int Code, string Name, bool Unsupported)[] ActionTable =
    [
        (1, "archive", false),
        (2, "delete", false),
        (3, "mark_read", false),
        (4, "to_spam", false),
        (5, "not_spam", false),
        (8, "add_user_label", true),
        (9, "add_flag", false),
        (10, "no_notify", false),
        (11, "move_to_folder", false),
        (12, "auto_forward", true),
        (13, "share_to_chat", true),
    ];

    /// <summary>编译条件数组为平台 <see cref="RuleCondition"/>；<paramref name="matchAll"/> 缺省为满足全部（match_type=1）。</summary>
    internal static RuleCondition CompileConditions(string[] conditions, bool? matchAll)
    {
        if (conditions is null || conditions.Length == 0)
        {
            throw new ArgumentException($"conditions 不能为空 —— 至少给出 1 个条件。{LegalForms}");
        }

        if (conditions.Length > MaxItems)
        {
            throw new ArgumentException($"条件数 {conditions.Length} 超过上限 {MaxItems}，请精简。{LegalForms}");
        }

        var items = new RuleConditionItem[conditions.Length];
        for (var i = 0; i < conditions.Length; i++)
        {
            items[i] = CompileCondition(conditions[i]);
        }

        return new RuleCondition
        {
            MatchType = matchAll == false ? 2 : 1,
            Items = items,
        };
    }

    /// <summary>编译动作数组为平台 <see cref="RuleAction"/>。</summary>
    internal static RuleAction CompileActions(string[] actions)
    {
        if (actions is null || actions.Length == 0)
        {
            throw new ArgumentException($"actions 不能为空 —— 至少给出 1 个动作。{LegalForms}");
        }

        if (actions.Length > MaxItems)
        {
            throw new ArgumentException($"动作数 {actions.Length} 超过上限 {MaxItems}，请精简。{LegalForms}");
        }

        var items = new RuleActionItem[actions.Length];
        for (var i = 0; i < actions.Length; i++)
        {
            items[i] = CompileAction(actions[i]);
        }

        return new RuleAction { Items = items };
    }

    /// <summary>反向渲染平台条件为简化式数组（rule_list 用）；空条件返回空数组。</summary>
    internal static string[] DescribeConditions(RuleCondition? condition)
    {
        if (condition?.Items is null || condition.Items.Length == 0)
        {
            return [];
        }

        return condition.Items.Select(DescribeCondition).ToArray();
    }

    /// <summary>反向渲染平台动作为简化式数组（rule_list 用）；空动作返回空数组。</summary>
    internal static string[] DescribeActions(RuleAction? action)
    {
        if (action?.Items is null || action.Items.Length == 0)
        {
            return [];
        }

        return action.Items.Select(DescribeAction).ToArray();
    }

    /// <summary>平台 match_type 是否"满足全部条件"（rule_list 用）。</summary>
    internal static bool IsMatchAll(int matchType) => matchType != 2;

    private static RuleConditionItem CompileCondition(string? raw)
    {
        var text = (raw ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            throw new ArgumentException($"条件不能为空字符串。{LegalForms}");
        }

        var segments = text.Split('|');
        var fieldName = segments[0].Trim();
        if (!TryFindFieldCode(fieldName, out var fieldCode))
        {
            throw new ArgumentException($"未知的条件字段 '{fieldName}'。{LegalForms}");
        }

        if (IsFlagField(fieldCode))
        {
            if (segments.Length > 1)
            {
                throw new ArgumentException($"旗标型字段 '{fieldName}' 不带操作符/值，应单独出现（如 \"{fieldName}\"），实际 '{text}'。{LegalForms}");
            }

            return new RuleConditionItem { Type = fieldCode };
        }

        if (segments.Length < 2 || segments[1].Trim().Length == 0)
        {
            throw new ArgumentException($"条件 '{text}' 缺少操作符（应为 'field|operator|value'）。{LegalForms}");
        }

        var operatorName = segments[1].Trim();
        if (!TryFindOperatorCode(operatorName, out var operatorCode))
        {
            throw new ArgumentException($"未知的条件操作符 '{operatorName}'。{LegalForms}");
        }

        if (IsValuelessOperator(operatorCode))
        {
            if (segments.Length > 2)
            {
                throw new ArgumentException($"操作符 '{operatorName}' 不带值，应为 '{fieldName}|{operatorName}'，实际 '{text}'。{LegalForms}");
            }

            return new RuleConditionItem { Type = fieldCode, Operator = operatorCode };
        }

        if (segments.Length < 3)
        {
            throw new ArgumentException($"操作符 '{operatorName}' 需要值（应为 '{fieldName}|{operatorName}|value'）。{LegalForms}");
        }

        // 值可能与 | 相邻出现（如正文里带竖线）：第三段起的全部内容用 | 重新拼接，仅去首尾空白。
        var value = string.Join("|", segments, 2, segments.Length - 2).Trim();
        if (value.Length == 0)
        {
            throw new ArgumentException($"条件 '{text}' 的值为空。{LegalForms}");
        }

        return new RuleConditionItem { Type = fieldCode, Operator = operatorCode, Input = value };
    }

    private static RuleActionItem CompileAction(string? raw)
    {
        var text = (raw ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            throw new ArgumentException($"动作不能为空字符串。{LegalForms}");
        }

        var segments = text.Split('|');
        var actionName = segments[0].Trim();
        if (!TryFindAction(actionName, out var actionCode, out var unsupported))
        {
            throw new ArgumentException($"未知的动作 '{actionName}'。{LegalForms}");
        }

        if (unsupported)
        {
            throw new ArgumentException($"动作 '{actionName}'（代码 {actionCode}）飞书平台标记为暂不支持，无法下发。{LegalForms}");
        }

        if (actionCode == MoveToFolderCode)
        {
            if (segments.Length != 2 || segments[1].Trim().Length == 0)
            {
                throw new ArgumentException($"动作 '{text}' 应为 'move_to_folder|文件夹ID' 两段且文件夹 ID 非空。{LegalForms}");
            }

            return new RuleActionItem { Type = actionCode, Input = segments[1].Trim() };
        }

        if (segments.Length > 1)
        {
            throw new ArgumentException($"动作 '{actionName}' 不带参数，应单独出现（如 \"{actionName}\"），实际 '{text}'。{LegalForms}");
        }

        return new RuleActionItem { Type = actionCode };
    }

    private static string DescribeCondition(RuleConditionItem item)
    {
        var field = FieldNameOf(item.Type);
        if (IsFlagField(item.Type))
        {
            return field;
        }

        if (!item.Operator.HasValue)
        {
            return $"{field}|unknown_operator";
        }

        var op = OperatorNameOf(item.Operator.Value);
        var value = item.Input?.Trim();
        if (IsValuelessOperator(item.Operator.Value) || string.IsNullOrEmpty(value))
        {
            return $"{field}|{op}";
        }

        return $"{field}|{op}|{value}";
    }

    private static string DescribeAction(RuleActionItem item)
    {
        var name = ActionNameOf(item.Type);
        return item.Type == MoveToFolderCode && !string.IsNullOrWhiteSpace(item.Input)
            ? $"{name}|{item.Input!.Trim()}"
            : name;
    }

    private static bool IsFlagField(int code) => code is 12 or 13 or 14 or 15 or 16;

    private static bool IsValuelessOperator(int code) => code is 7 or 10;

    private static bool TryFindFieldCode(string name, out int code)
        => TryFind(FieldTable, name, out code);

    private static bool TryFindOperatorCode(string name, out int code)
        => TryFind(OperatorTable, name, out code);

    private static bool TryFindAction(string name, out int code, out bool unsupported)
    {
        foreach (var (candidateCode, candidateName, candidateUnsupported) in ActionTable)
        {
            if (string.Equals(candidateName, name, StringComparison.OrdinalIgnoreCase))
            {
                code = candidateCode;
                unsupported = candidateUnsupported;
                return true;
            }
        }

        code = 0;
        unsupported = false;
        return false;
    }

    private static bool TryFind((int Code, string Name)[] table, string name, out int code)
    {
        foreach (var (candidateCode, candidateName) in table)
        {
            if (string.Equals(candidateName, name, StringComparison.OrdinalIgnoreCase))
            {
                code = candidateCode;
                return true;
            }
        }

        code = 0;
        return false;
    }

    private static string FieldNameOf(int code) => NameOf(FieldTable, code, "field");

    private static string OperatorNameOf(int code) => NameOf(OperatorTable, code, "operator");

    private static string ActionNameOf(int code)
    {
        foreach (var (candidateCode, candidateName, _) in ActionTable)
        {
            if (candidateCode == code)
            {
                return candidateName;
            }
        }

        return $"unknown_action_{code}";
    }

    private static string NameOf((int Code, string Name)[] table, int code, string kind)
    {
        foreach (var (candidateCode, candidateName) in table)
        {
            if (candidateCode == code)
            {
                return candidateName;
            }
        }

        return $"unknown_{kind}_{code}";
    }
}
