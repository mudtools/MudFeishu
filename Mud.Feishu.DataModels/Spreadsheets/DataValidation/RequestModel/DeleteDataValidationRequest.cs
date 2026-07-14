// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>
/// 删除数据验证请求
/// </summary>
public class DeleteDataValidationRequest
{
    /// <summary>
    /// <para>指定要删除的下拉列表的范围。可指定多个范围。</para>
    /// <para>**注意**：</para>
    /// <para>- 删除某个范围失败不影响其它范围的执行。响应体中将返回每个范围的执行结果。</para>
    /// <para>- 单个范围指定的单元格不可超过 5,000 个，范围的总数不可超过 100 个。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("dataValidationRanges")]
    public DeleteDataValidationRange[] DataValidationRanges { get; set; } = [];


}