// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 批量获取人才 ID 响应人才信息（批量获取人才 ID 响应 talent_list 子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class TalentBatchInfo
{
    /// <summary>
    /// <para>人才 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("talent_id")]
    public string? TalentId { get; set; }

    /// <summary>
    /// <para>手机号区号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mobile_code")]
    public string? MobileCode { get; set; }

    /// <summary>
    /// <para>手机号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mobile_number")]
    public string? MobileNumber { get; set; }

    /// <summary>
    /// <para>邮箱</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// <para>证件号码</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("identification_number")]
    public string? IdentificationNumber { get; set; }

    /// <summary>
    /// <para>证件类型：1 身份证 / 2 护照 / 3 港澳居民来往内地通行证 / 4 台湾居民来往大陆通行证 / 5 其他 / 6 港澳台居民居住证 / 9 台湾居民居住证</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("identification_type")]
    public int? IdentificationType { get; set; }

    /// <summary>
    /// <para>入职状态</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_onboarded")]
    public bool? IsOnboarded { get; set; }
}
