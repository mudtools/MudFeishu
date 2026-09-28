// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Mud.Feishu.AI.Tools.Schema;

/// <summary>
/// 从<b>渲染后的参数 Schema 文本</b>中提取 <c>properties</c> 段的顶层键（AT-B15 的"来源②"）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么解析文本而不是复用对象模型</b>：这是 AT-B15 的<b>全部意义</b>所在。
/// 原 <c>MUDFT015</c> 的 <c>required</c> 与 <c>properties</c> 都取自同一个 <c>entry.Parameters</c>，
/// 条件恒假（假门禁）。要构成真实约束，第二个来源必须是<b>产物</b>（模型最终看到的 JSON 文本），
/// 而不是同一份输入模型——否则无法检出"渲染期丢键/重复键/转义不一致"这类真实缺陷。
/// </para>
/// <para>
/// <b>解析范围刻意最小</b>：只处理 <see cref="SchemaWriter.WriteInputSchema"/> 的既有输出形状
/// （<c>{"type":"object","properties":{…}}</c>），遇到任何非预期结构即返回失败并让上层报诊断
/// （fail-closed）。<b>不</b>引入通用 JSON 解析器——生成器工程为 <c>netstandard2.0</c> 且刻意零依赖
/// （<see cref="JsonText"/> 就是同一取舍的产物）。
/// </para>
/// </remarks>
internal static class RenderedPropertiesKeys
{
    private const string PropertiesMarker = "\"properties\":";

    /// <summary>
    /// 提取渲染产物中 <c>properties</c> 段的顶层键集（含重复项计数——重复键正是要检出的缺陷）。
    /// </summary>
    /// <param name="renderedSchemaJson"><c>SchemaWriter.WriteInputSchema</c> 的产物。</param>
    /// <param name="keys">提取到的键（可含重复项，按出现顺序）。</param>
    /// <param name="failure">失败原因（成功时为 <see langword="null"/>）。</param>
    /// <returns>是否成功提取。</returns>
    public static bool TryExtract(string renderedSchemaJson, out List<string> keys, out string? failure)
    {
        keys = [];

        if (string.IsNullOrEmpty(renderedSchemaJson))
        {
            failure = "渲染产物为空";
            return false;
        }

        var text = renderedSchemaJson;

        var markerIndex = text.IndexOf(PropertiesMarker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            failure = $"未找到 {PropertiesMarker} 标记（Schema 形状与解析器假设不符）";
            return false;
        }

        var index = markerIndex + PropertiesMarker.Length;
        if (index >= text.Length || text[index] != '{')
        {
            failure = "properties 值不是对象";
            return false;
        }

        index++; // 进入 properties 对象

        // 空对象：{"type":"object","properties":{}}
        if (index < text.Length && text[index] == '}')
        {
            failure = null;
            return true;
        }

        while (index < text.Length)
        {
            if (text[index] != '"')
            {
                failure = $"properties 段第 {index} 个字符处期待键的起始引号，实际为 '{text[index]}'";
                return false;
            }

            if (!TryReadJsonString(text, ref index, out var key))
            {
                failure = "properties 段的键不是合法 JSON 字符串";
                return false;
            }

            keys.Add(key);

            if (index >= text.Length || text[index] != ':')
            {
                failure = $"properties 段的键 '{key}' 之后缺少 ':'";
                return false;
            }

            index++; // 跳过 ':'
            if (!TrySkipValue(text, ref index, out failure))
            {
                return false;
            }

            if (index >= text.Length)
            {
                failure = "properties 对象未闭合";
                return false;
            }

            if (text[index] == ',')
            {
                index++;
                continue;
            }

            if (text[index] == '}')
            {
                // properties 对象闭合；required 段（若存在）由调用方以"意图模型"为来源比对，
                // 此处不解析（避免把 required 也变成同一解析器的产物而弱化约束）。
                failure = null;
                return true;
            }

            failure = $"properties 段中出现非预期的分隔符 '{text[index]}'";
            return false;
        }

        failure = "properties 段解析越界";
        return false;
    }

    /// <summary>读取一个 JSON 字符串字面量并反转义（与 <see cref="JsonText.Quote"/> 的转义集对齐）。</summary>
    private static bool TryReadJsonString(string text, ref int index, out string value)
    {
        var builder = new StringBuilder();
        index++; // 跳过起始引号

        while (index < text.Length)
        {
            var ch = text[index];
            if (ch == '\\')
            {
                if (index + 1 >= text.Length)
                {
                    value = string.Empty;
                    return false;
                }

                var escaped = text[index + 1];
                switch (escaped)
                {
                    case '"':
                        builder.Append('"');
                        break;
                    case '\\':
                        builder.Append('\\');
                        break;
                    case 'n':
                        builder.Append('\n');
                        break;
                    case 'r':
                        builder.Append('\r');
                        break;
                    case 't':
                        builder.Append('\t');
                        break;
                    case 'u':
                        if (index + 5 >= text.Length)
                        {
                            value = string.Empty;
                            return false;
                        }

                        var hex = text.Substring(index + 2, 4);
                        if (!int.TryParse(hex, System.Globalization.NumberStyles.HexNumber,
                                System.Globalization.CultureInfo.InvariantCulture, out var code))
                        {
                            value = string.Empty;
                            return false;
                        }

                        builder.Append((char)code);
                        index += 6;
                        continue;
                    default:
                        // 未知转义：原样保留（不猜测语义）。
                        builder.Append(escaped);
                        break;
                }

                index += 2;
                continue;
            }

            if (ch == '"')
            {
                index++;
                value = builder.ToString();
                return true;
            }

            builder.Append(ch);
            index++;
        }

        value = string.Empty;
        return false;
    }

    /// <summary>跳过一个 JSON 值（标量/对象/数组），把 <paramref name="index"/> 停在分隔符 <c>,</c> 或 <c>}</c> 上。</summary>
    private static bool TrySkipValue(string text, ref int index, out string? failure)
    {
        var depth = 0;
        var inString = false;

        while (index < text.Length)
        {
            var ch = text[index];

            if (inString)
            {
                if (ch == '\\')
                {
                    index += 2;
                    continue;
                }

                if (ch == '"')
                {
                    inString = false;
                }

                index++;
                continue;
            }

            switch (ch)
            {
                case '"':
                    inString = true;
                    index++;
                    continue;
                case '{':
                case '[':
                    depth++;
                    index++;
                    continue;
                case '}':
                case ']':
                    if (depth == 0)
                    {
                        failure = null;
                        return true;
                    }

                    depth--;
                    index++;
                    continue;
                case ',':
                    if (depth == 0)
                    {
                        failure = null;
                        return true;
                    }

                    index++;
                    continue;
                default:
                    index++;
                    continue;
            }
        }

        failure = "properties 的值未正常结束（对象未闭合）";
        return false;
    }
}
