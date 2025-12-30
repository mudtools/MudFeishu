// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TaskCustomFields;

/// <summary>
/// 要更新的option数据
/// </summary>
public class UpdateSelectOptionData : SelectOptionData
{
    /// <summary>
    /// <para>要放到某个option之前的option_guid</para>
    /// <para>必填：否</para>
    /// <para>示例值：2bd905f8-ef38-408b-aa1f-2b2ad33b2913</para>
    /// </summary>
    [JsonPropertyName("insert_before")]
    public string? InsertBefore { get; set; }

    /// <summary>
    /// <para>要放到某个option之后的option_guid</para>
    /// <para>必填：否</para>
    /// <para>示例值：b13adf3c-cad6-4e02-8929-550c112b5633</para>
    /// </summary>
    [JsonPropertyName("insert_after")]
    public string? InsertAfter { get; set; }
}
