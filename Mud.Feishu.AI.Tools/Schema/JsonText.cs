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

    /// <summary>把值转义为 C# 字符串字面量（内容安全时用原始字符串字面量，否则退回转义形态）。</summary>
    /// <remarks>
    /// <para>
    /// 原始字符串字面量的适用条件（不满足一律退回 <see cref="Quote"/> 的转义形态）：
    /// <b>非空、无换行、首尾字符不是引号、不含三连引号</b>。
    /// </para>
    /// <para>
    /// <b>为什么空串不能用原始形态</b>：<c>""""""</c>（6 连引号）会被 Roslyn 按"最长引号连跑"
    /// 整体吞成开头定界符，找不到闭合定界符 → CS8997"未终止的字符串字面量"
    /// （WP2 契约表的 <c>SdkSource/HttpMethod/Route</c> 空值首次暴露）。
    /// 首尾引号会被并入定界符扫描，同理不安全；换行会把单行原始字面量变成多行形态
    /// （开引号后必须换行，行内开启即非法）。
    /// </para>
    /// <para>
    /// 对既有产物（Schema 常量 / 工具名）字节级无影响：JSON 内容以 <c>{</c> 开头 <c>}</c> 结尾、
    /// 无原始换行；工具名为 <c>域.动作</c> 纯标识符。
    /// </para>
    /// </remarks>
    public static string ToCSharpLiteral(string value)
        => value.Length > 0
            && value[0] != '"'
            && value[value.Length - 1] != '"'
            && value.IndexOf('\n') < 0
            && value.IndexOf('\r') < 0
            && value.IndexOf("\"\"\"", System.StringComparison.Ordinal) < 0
            ? "\"\"\"" + value + "\"\"\""
            : Quote(value);
}
