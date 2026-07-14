// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;


/// <summary>
/// 保护范围的维度信息
/// </summary>
public class ProtectedDimension
{
    /// <summary>
    /// <para>要保护的行或列的信息</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("dimension")]
    public SheetRangeDimension Dimension { get; set; } = new();

    /// <summary>
    /// <para>允许编辑保护范围的用户的 ID。ID 类型取决于查询参数 `user_id_type` 的取值。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("users")]
    public string[]? Users { get; set; }

    /// <summary>
    /// <para>保护范围的备注信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("lockInfo")]
    public string? LockInfo { get; set; }

}