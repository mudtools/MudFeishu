// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.FeishuTools;

/// <summary>
/// Bitable <c>filter</c> 简化文法解析器（Phase 1 §3.3.3）：
/// <c>字段 = 值</c> / <c>字段 contains 值</c>，<c>and</c> 连接，最多 5 个子句。
/// </summary>
/// <remarks>
/// <para>
/// 值可用双引号包裹（含空格）；裸值不含空白。复杂嵌套 and/or、<c>sort</c> 不进本期
/// （Phase 2 扩文法）；解析失败回填「filter 语法不支持」结构化错误。
/// </para>
/// <para>
/// 注意：<see cref="GeneratedRegexAttribute"/> 仅 net7+，本包面向 ns2.0~net10 多 TFM，
/// 使用经典 <see cref="Regex"/> 静态字段。
/// </para>
/// </remarks>
public static class BitableFilterParser
{
    /// <summary>最大子句数。</summary>
    public const int MaxClauses = 5;

    private const string OperatorEquals = "is";
    private const string OperatorContains = "contains";

    private static readonly Regex ClauseSplitRegex = new(
        @"\s+and\s+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(2));

    private static readonly Regex ClauseRegex = new(
        @"^\s*(?<field>[\p{L}\p{N}_\-]+)\s*(?<op>=|contains)\s*(?:""(?<quoted>[^""]*)""|(?<bare>[^\s""]+))\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(2));

    /// <summary>
    /// 解析简化筛选式。
    /// </summary>
    /// <param name="filter">模型给出的筛选式（可空/空白——视为无过滤）。</param>
    /// <param name="parsed">解析结果（成功且非空输入时非空）。</param>
    /// <param name="error">失败原因（成功时为 null）。</param>
    /// <returns>是否解析成功。</returns>
    public static bool TryParse(string? filter, out Mud.Feishu.DataModels.Bitable.RecordQueryFilterInfo? parsed, out string? error)
    {
        parsed = null;
        error = null;

        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        var trimmed = filter!.Trim();

        // 显式拒绝 or / 嵌套括号（Phase 2 扩文法）——给模型可读的失败原因。
        if (trimmed.IndexOf(" or ", StringComparison.OrdinalIgnoreCase) >= 0
            || trimmed.StartsWith("or ", StringComparison.OrdinalIgnoreCase)
            || trimmed.EndsWith(" or", StringComparison.OrdinalIgnoreCase)
            || trimmed.IndexOf("(", StringComparison.Ordinal) >= 0
            || trimmed.IndexOf(")", StringComparison.Ordinal) >= 0)
        {
            error = "filter 语法不支持：本期仅支持 and 连接的简化筛选式（如 status = \"done\" and owner contains 张三），不支持 or/括号嵌套";
            return false;
        }

        var clauseTexts = ClauseSplitRegex.Split(trimmed);
        if (clauseTexts.Length > MaxClauses)
        {
            error = $"filter 语法不支持：最多 {MaxClauses.ToString(CultureInfo.InvariantCulture)} 个子句，实际 {clauseTexts.Length.ToString(CultureInfo.InvariantCulture)} 个";
            return false;
        }

        var conditions = new List<Mud.Feishu.DataModels.Bitable.RecordQueryCondition>(clauseTexts.Length);
        foreach (var clauseText in clauseTexts)
        {
            var match = ClauseRegex.Match(clauseText);
            if (!match.Success)
            {
                error = $"filter 语法不支持：子句 \"{clauseText}\" 不是「字段 = 值」或「字段 contains 值」形式";
                return false;
            }

            var op = match.Groups["op"].Value;
            var opValue = string.Equals(op, "=", StringComparison.Ordinal) ? OperatorEquals : OperatorContains;
            var value = match.Groups["quoted"].Success
                ? match.Groups["quoted"].Value
                : match.Groups["bare"].Value;

            conditions.Add(new Mud.Feishu.DataModels.Bitable.RecordQueryCondition
            {
                FieldName = match.Groups["field"].Value,
                Operator = opValue,
                Value = [value],
            });
        }

        parsed = new Mud.Feishu.DataModels.Bitable.RecordQueryFilterInfo
        {
            Conjunction = "and",
            Conditions = [.. conditions],
        };
        return true;
    }
}
