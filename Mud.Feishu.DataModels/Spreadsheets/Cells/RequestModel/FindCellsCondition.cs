// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>
/// <para>指定查找单元格的条件。</para>
/// </summary>
public class FindCellsCondition
{
    /// <summary>
    /// <para>查找范围。格式为 `&lt;sheetId&gt;!&lt;开始位置&gt;:&lt;结束位置&gt;`。其中：</para>
    /// <para>- `sheetId` 为工作表 ID</para>
    /// <para>- `&lt;开始位置&gt;:&lt;结束位置&gt;` 为工作表中单元格的范围，数字表示行索引，字母表示列索引。如 `A2:B2` 表示该工作表第 2 行的 A 列到 B 列。`range`支持四种写法，详情参考[电子表格概述](https://open.feishu.cn/document/ukTMukTMukTM/uATMzUjLwEzM14CMxMTN/overview)</para>
    /// <para>必填：是</para>
    /// <para>示例值：PNIfrm!A1:C5</para>
    /// </summary>
    [JsonPropertyName("range")]
    public string Range { get; set; } = string.Empty;

    /// <summary>
    /// <para>是否忽略查找字符串的大小写，默认为 false。</para>
    /// <para>- `true`：忽略字符串中字母大小写差异</para>
    /// <para>- `false`：区分字符串中字母大小写</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("match_case")]
    public bool? MatchCase { get; set; }

    /// <summary>
    /// <para>字符串是否需要完全匹配整个单元格，默认值为 false。</para>
    /// <para>- `true`：完全匹配单元格，比如 `find` 参数 取值为 "hello"，则单元格中的内容必须为 "hello" 才会匹配替换</para>
    /// <para>- `false`：允许部分匹配单元格，比如 `find` 取值为 "hello"，则单元格中的内容包含 "hello" 即可匹配替换</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("match_entire_cell")]
    public bool? MatchEntireCell { get; set; }

    /// <summary>
    /// <para>是否使用正则表达式查找，默认值为 false。</para>
    /// <para>- `true`：使用正则表达式</para>
    /// <para>- `false`：不使用正则表达式</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("search_by_regex")]
    public bool? SearchByRegex { get; set; }

    /// <summary>
    /// <para>是否仅搜索单元格公式，默认值为 false。</para>
    /// <para>- `true`：仅搜索单元格公式</para>
    /// <para>- `false`：仅搜索单元格内容</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("include_formulas")]
    public bool? IncludeFormulas { get; set; }
}