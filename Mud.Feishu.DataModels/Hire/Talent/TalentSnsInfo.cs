// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 人才社交账号（获取人才 v1 详情/列表响应 sns_list 子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class TalentSnsInfo
{
    /// <summary>
    /// <para>社交账号 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>社交平台类型：1 LinkedIn / 2 脉脉 / 3 微信 / 4 微博 / 5 Github / 6 知乎 / 7 Facebook / 8 Twitter / 9 WhatsApp / 10 个人网站 / 11 QQ</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sns_type")]
    public int? SnsType { get; set; }

    /// <summary>
    /// <para>社交账号链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("link")]
    public string? Link { get; set; }

    /// <summary>
    /// <para>自定义字段列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_data_list")]
    public TalentCustomizedDataChild[]? CustomizedDataList { get; set; }
}
