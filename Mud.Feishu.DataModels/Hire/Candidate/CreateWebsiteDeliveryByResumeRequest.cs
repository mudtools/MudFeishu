// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 按简历创建官网投递请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class CreateWebsiteDeliveryByResumeRequest
{
    /// <summary>
    /// <para>职位广告 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：6960663240925956636</para>
    /// </summary>
    [JsonPropertyName("job_post_id")]
    public string? JobPostId { get; set; }

    /// <summary>
    /// <para>人才简历信息（基本信息、教育/工作/实习经历、自定义模块等）</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("resume")]
    public WebsiteDeliveryResume? Resume { get; set; }

    /// <summary>
    /// <para>官网用户 ID，需先通过创建官网用户接口获取</para>
    /// <para>必填：是</para>
    /// <para>示例值：6960663240925956634</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>意向投递城市列表，可从获取职位信息接口返回的工作地列表获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("application_preferred_city_code_list")]
    public string[]? ApplicationPreferredCityCodeList { get; set; }

    /// <summary>
    /// <para>官网推广渠道 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6891560630172518670</para>
    /// </summary>
    [JsonPropertyName("channel_id")]
    public string? ChannelId { get; set; }
}
