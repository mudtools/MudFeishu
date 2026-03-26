// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;


/// <summary>
/// 插入数据请求体中的范围值
/// </summary>
public class RangeValues
{
    /// <summary>
    /// <para>指定范围，用于写入数据。格式为 `&lt;sheetId&gt;!&lt;开始位置&gt;:&lt;结束位置&gt;`。其中：</para>
    /// <para>- `sheetId` 为工作表 ID。</para>
    /// <para>- `&lt;开始位置&gt;:&lt;结束位置&gt;` 为工作表中单元格的范围，数字表示行索引，字母表示列索引。如 `A2:B2` 表示该工作表第 2 行的 A 列到 B 列。</para>
    /// <para>**注意**：`range` 所指定的范围需要大于等于插入的数据所占用的范围。但最终增加的行数由 `value` 决定。</para>
    /// <para>**示例值**：`8fe9d6!A2:B5`。该示例值表示在 ID 为 `8fe9d6` 的工作表的第二行（由起始行 A2 决定）上方新增至多四行，在新增行的 A 列和 B 列插入数据。若插入的数据仅有三行，则最终将在第二行上方新增三行。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("range")]
    public string Range { get; set; } = string.Empty;

    /// <summary>
    /// <para>插入的数据。如要写入公式、超链接、email、@人等，可参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/ugjN1UjL4YTN14CO2UTN">支持写入数据类型</see>]。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("values")]
    public object[][] Values { get; set; } = [];
}