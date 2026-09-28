// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.Feishu.AI.Tools.Schema;

/// <summary>
/// 生成器内 JSON 文本拼装的最小工具（零依赖、netstandard2.0 可用）。
/// </summary>
/// <remarks>
/// 全工程<b>唯一</b>的字符串转义实现：此前 <c>SchemaWriter.Quote</c> 与
/// <c>FeishuToolSchemaGenerator.Quote</c> 各写一份，两者一旦漂移会产出非法 JSON
/// （编译期常量损坏 → 运行期 <c>JsonDocument.Parse</c> 失败）。
/// </remarks>
internal static class JsonText
{
    /// <summary>把值转义为 JSON 字符串字面量（含首尾引号）。</summary>
    public static string Quote(string value)
    {
        var sb = new StringBuilder(value.Length + 2);
        sb.Append('"');
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    if (ch < ' ')
                    {
                        sb.Append("\\u").Append(((int)ch).ToString("x4"));
                    }
                    else
                    {
                        sb.Append(ch);
                    }

                    break;
            }
        }

        sb.Append('"');
        return sb.ToString();
    }

    /// <summary>把值转义为 C# 字符串字面量（优先原始字符串字面量，更易读且免转义）。</summary>
    public static string ToCSharpLiteral(string value)
        => value.IndexOf("\"\"\"", System.StringComparison.Ordinal) >= 0
            ? Quote(value)
            : "\"\"\"" + value + "\"\"\"";
}
