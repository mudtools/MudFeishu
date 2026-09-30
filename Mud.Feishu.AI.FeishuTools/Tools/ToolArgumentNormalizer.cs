// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// 任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;

namespace Mud.Feishu.AI.FeishuTools.Tools;

/// <summary>
/// 工具参数值的形态归一化中心（AT-B19 / WP1）：
/// 运行时参数值来自模型 tool_call 的 JSON 反序列化，字符串参数实际形态为 <see cref="System.Text.Json.JsonElement"/>。
/// 本类型是该事实的**唯一认知入口**——净化、摘要一律经此遍历，禁止各自 switch。
/// </summary>
/// <remarks>
/// <para>
/// <b>根因（S1）</b>：<c>ToolArgumentSanitizer.ValidateValue</c> 原先只识别 <c>string</c> 与
/// <c>IEnumerable&lt;string&gt;</c>，不识别 <c>JsonElement</c>（运行时真实形态），导致入站净化
/// 在真实主路径**从未生效**。本类型把形态遍历变成单一可复用函数，消除"同一事实在三处被分别发现、第三处漏了"的根因。
/// </para>
/// <para>
/// <b>路径形如</b>：<c>text</c> / <c>texts[1]</c> / <c>fields.name</c>——错误消息可直接用于模型自查。
/// </para>
/// </remarks>
internal static class ToolArgumentNormalizer
{
    /// <summary>
    /// 以 (参数路径, 文本值) 的形式遍历任意形态的参数值。
    /// </summary>
    /// <param name="path">当前参数路径（根参数名）。</param>
    /// <param name="value">参数值（可能为 <c>null</c>/<c>string</c>/<c>JsonElement</c>/<c>IEnumerable&lt;string&gt;</c>）。</param>
    /// <returns>(路径, 文本值) 序列——只产出文本值，数值/布尔/null 被跳过。</returns>
    public static IEnumerable<(string Path, string Text)> EnumerateTexts(string path, object? value)
    {
        switch (value)
        {
            case null:
                yield break;

            case string s:
                yield return (path, s);
                yield break;

            case System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.String } e:
                yield return (path, e.GetString() ?? string.Empty);
                yield break;

            case System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.Array } e:
                {
                    var i = 0;
                    foreach (var item in e.EnumerateArray())
                    {
                        foreach (var pair in EnumerateTexts(
                            $"{path}[{i.ToString(CultureInfo.InvariantCulture)}]", item))
                        {
                            yield return pair;
                        }

                        i++;
                    }

                    yield break;
                }

            case System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.Object } e:
                {
                    foreach (var prop in e.EnumerateObject())
                    {
                        foreach (var pair in EnumerateTexts($"{path}.{prop.Name}", prop.Value))
                        {
                            yield return pair;
                        }
                    }

                    yield break;
                }

            case System.Text.Json.JsonElement: // Number / True / False / Null / Undefined
                yield break;

            case IEnumerable<string> strings: // 宿主侧直接构造的托管形态（测试/内部调用）
                {
                    var i = 0;
                    foreach (var item in strings)
                    {
                        yield return ($"{path}[{i.ToString(CultureInfo.InvariantCulture)}]", item);
                        i++;
                    }

                    yield break;
                }

            default:
                yield break;
        }
    }
}
