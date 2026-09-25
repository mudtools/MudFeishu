// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 猎头供应商下的猎头（查询猎头列表响应子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class AgencyAccount
{
    /// <summary>
    /// <para>猎头 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6995312261554538796</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>禁用原因，仅当 status 为 1（已禁用）时填充</para>
    /// <para>必填：否</para>
    /// <para>示例值：这个猎头很不负责</para>
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// <para>添加时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1639992265035</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>猎头状态：0 正常 / 1 已禁用 / 2 猎头自助停用</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>猎头用户信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_info")]
    public AgencyAccountUser? UserInfo { get; set; }

    /// <summary>
    /// <para>角色：0 管理员 / 1 顾问</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("role")]
    public int? Role { get; set; }
}
