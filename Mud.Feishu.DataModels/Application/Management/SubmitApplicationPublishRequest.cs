// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 提交发布自建应用请求体。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class SubmitApplicationPublishRequest
{
    /// <summary>
    /// <para>移动端默认应用能力。应用开通了小程序、机器人或网页应用能力时必填</para>
    /// <para>必填：否</para>
    /// <para>可选值：gadget（小程序）、web_app（网页应用）、bot（机器人）</para>
    /// <para>示例值：gadget</para>
    /// </summary>
    [JsonPropertyName("mobile_default_ability")]
    public string? MobileDefaultAbility { get; set; }

    /// <summary>
    /// <para>桌面端默认应用能力。应用开通了小程序、机器人或网页应用能力时必填</para>
    /// <para>必填：否</para>
    /// <para>可选值：gadget（小程序）、web_app（网页应用）、bot（机器人）</para>
    /// <para>示例值：gadget</para>
    /// </summary>
    [JsonPropertyName("pc_default_ability")]
    public string? PcDefaultAbility { get; set; }

    /// <summary>
    /// <para>发布应用原因</para>
    /// <para>必填：是</para>
    /// <para>示例值：更新了移动端默认应用能力</para>
    /// </summary>
    [JsonPropertyName("remark")]
    public string? Remark { get; set; }

    /// <summary>
    /// <para>更新说明</para>
    /// <para>必填：是</para>
    /// <para>示例值：更新了小程序的头像</para>
    /// </summary>
    [JsonPropertyName("changelog")]
    public string? Changelog { get; set; }

    /// <summary>
    /// <para>应用版本号，格式为 X.X.X，不填则自动创建版本</para>
    /// <para>必填：否</para>
    /// <para>示例值：1.1.1</para>
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }
}

/// <summary>
/// 提交发布自建应用响应体数据。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class SubmitApplicationPublishResult
{
    /// <summary>
    /// <para>应用版本 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：oav_d317f090b7258ad0372aa53963cda70d</para>
    /// </summary>
    [JsonPropertyName("version_id")]
    public string? VersionId { get; set; }

    /// <summary>
    /// <para>应用版本号</para>
    /// <para>必填：否</para>
    /// <para>示例值：1.1.1</para>
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }
}
