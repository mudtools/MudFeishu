// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 批量删除背调套餐和附加调查项请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class BatchDeleteEcoBackgroundCheckPackageRequest
{
    /// <summary>
    /// <para>背调账号 ID，可通过「账号绑定」事件获取</para>
    /// <para>必填：是</para>
    /// <para>示例值：6995842370159937061</para>
    /// </summary>
    [JsonPropertyName("account_id")]
    public string? AccountId { get; set; }

    /// <summary>
    /// <para>要删除的套餐 ID 列表；删除套餐不影响已安排的背调</para>
    /// <para>必填：否</para>
    /// <para>示例值：["7230753910687080001"]</para>
    /// </summary>
    [JsonPropertyName("package_id_list")]
    public string[]? PackageIdList { get; set; }

    /// <summary>
    /// <para>要删除的附加调查项 ID 列表；删除附加调查项不影响已安排的背调</para>
    /// <para>必填：否</para>
    /// <para>示例值：["7230753910687080002"]</para>
    /// </summary>
    [JsonPropertyName("additional_item_id_list")]
    public string[]? AdditionalItemIdList { get; set; }
}
