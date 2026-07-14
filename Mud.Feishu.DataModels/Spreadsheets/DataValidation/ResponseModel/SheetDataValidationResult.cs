// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>
/// 数据验证信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spreadsheets")]
public class SheetDataValidationResult : SheetDataValidationInfo
{
    /// <summary>
    /// <para>下拉列表所属的范围，按照列进行聚合。</para>
    /// <para>例如 4d30c6 子表中，A1、A2、A4、B1、B2 都是该下拉列表，则该下拉列表对应的 Ranges 为["4d30c6!A1:A2","4d30c6!A4:A4","4d30c6!B1:B2"]</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ranges")]
    public string[]? Ranges { get; set; }
}
