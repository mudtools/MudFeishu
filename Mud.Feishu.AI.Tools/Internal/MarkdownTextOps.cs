// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>Markdown 精确匹配的序数文本原语（netstandard2.0 无 <c>StringComparison</c> 替换重载）。</summary>
internal static class MarkdownTextOps
{
    /// <summary>按序数统计非重叠命中次数（命中数决定是否需要 replace_all）。</summary>
    internal static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    /// <summary>按序数全量替换（逐段拼接，不做二次解释）。</summary>
    internal static string ReplaceAllOrdinal(string text, string oldValue, string newValue)
    {
        var builder = new StringBuilder(text.Length);
        var start = 0;
        int index;

        while ((index = text.IndexOf(oldValue, start, StringComparison.Ordinal)) >= 0)
        {
            builder.Append(text, start, index - start);
            builder.Append(newValue);
            start = index + oldValue.Length;
        }

        builder.Append(text, start, text.Length - start);
        return builder.ToString();
    }
}
