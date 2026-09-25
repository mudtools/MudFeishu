// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 批量更新职位管理人员请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class BatchUpdateJobManagerRequest
{
    /// <summary>
    /// <para>招聘负责人 ID，与入参 user_id_type 类型一致；update_option_list 包含「招聘负责人」时必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("recruiter_id")]
    public string? RecruiterId { get; set; }

    /// <summary>
    /// <para>招聘助理 ID 列表，与入参 user_id_type 类型一致；update_option_list 包含「招聘助理」时必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("assistant_id_list")]
    public string[]? AssistantIdList { get; set; }

    /// <summary>
    /// <para>用人经理 ID 列表，与入参 user_id_type 类型一致；update_option_list 包含「用人经理」时必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("hiring_manager_id_list")]
    public string[]? HiringManagerIdList { get; set; }

    /// <summary>
    /// <para>更新的成员项：1 招聘负责人 / 2 招聘助理 / 3 用人经理</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("update_option_list")]
    public int[] UpdateOptionList { get; set; } = Array.Empty<int>();

    /// <summary>
    /// <para>创建者 ID，与入参 user_id_type 类型一致，不填则为默认系统</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("creator_id")]
    public string? CreatorId { get; set; }
}
