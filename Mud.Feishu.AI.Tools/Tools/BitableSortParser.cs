// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Bitable;

namespace Mud.Feishu.AI.Tools.Tools;

/// <summary>
/// bitable.query_records <c>sort</c> 简化文法解析器（AI-FD-D12 P1D-1b 批次 A）：
/// 每个子句形如 <c>字段:asc</c> / <c>字段:desc</c>（方向大小写不敏感；允许元素内逗号分隔多个子句），
/// 最多 3 个——复杂嵌套排序文法仍延后（入参推断稳定性原则，Phase 1 §3.3.1）。
/// </summary>
/// <remarks>解析失败以 <c>invalid_args</c> 语义结构化拒绝（对齐 <see cref="BitableFilterParser"/> 先例）。</remarks>
internal static class BitableSortParser
{
    /// <summary>排序子句上限。</summary>
    public const int MaxClauses = 3;

    public static bool TryParse(string[]? sort, out RecordQuerySort[] parsed, out string? error)
    {
        parsed = [];
        error = null;
        if (sort is null || sort.Length == 0)
        {
            return true;
        }

        var clauses = new List<RecordQuerySort>();
        foreach (var raw in sort)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            foreach (var segment in raw.Split(','))
            {
                var clause = segment.Trim();
                if (clause.Length == 0)
                {
                    continue;
                }

                var separator = clause.IndexOf(':');
                if (separator == 0)
                {
                    error = $"sort 子句 '{clause}' 字段名不能为空";
                    return false;
                }

                if (separator < 0 || separator == clause.Length - 1)
                {
                    error = $"sort 子句 '{clause}' 语法不支持——须为 字段:asc 或 字段:desc（≤{MaxClauses.ToString(CultureInfo.InvariantCulture)} 个）";
                    return false;
                }

                var fieldName = clause.Substring(0, separator).Trim();
                var direction = clause.Substring(separator + 1).Trim();
                if (fieldName.Length == 0)
                {
                    error = $"sort 子句 '{clause}' 字段名不能为空";
                    return false;
                }

                if (!string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase))
                {
                    error = $"sort 子句 '{clause}' 方向不支持——仅 asc / desc（大小写不敏感）";
                    return false;
                }

                clauses.Add(new RecordQuerySort
                {
                    FieldName = fieldName,
                    Desc = string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase),
                });
            }
        }

        if (clauses.Count > MaxClauses)
        {
            error = $"sort 子句最多 {MaxClauses.ToString(CultureInfo.InvariantCulture)} 个，实际 {clauses.Count.ToString(CultureInfo.InvariantCulture)} 个";
            return false;
        }

        parsed = [.. clauses];
        return true;
    }
}
