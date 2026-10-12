// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bot;

/// <summary>
/// 机器人信息。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Bot")]
public class BotInfo
{
    /// <summary>
    /// <para>应用的当前状态</para>
    /// <para>必填：否</para>
    /// <para>可选值：</para>
    /// <para>0：初始化，等待租户安装</para>
    /// <para>1：停用</para>
    /// <para>2：启用</para>
    /// <para>3：待安装启用</para>
    /// <para>4：已升级，待启用</para>
    /// <para>5：因许可证过期停用</para>
    /// <para>6：因飞书套餐过期或降级停用</para>
    /// <para>示例值：2</para>
    /// </summary>
    [JsonPropertyName("activate_status")]
    public int? ActivateStatus { get; set; }

    /// <summary>
    /// <para>应用名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：name</para>
    /// </summary>
    [JsonPropertyName("app_name")]
    public string? AppName { get; set; }

    /// <summary>
    /// <para>应用图片的 URL</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://s1-imfile.feishucdn.com/static-resource/v1/da5xxxx14b16113</para>
    /// </summary>
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// <para>应用的 IP 白名单地址</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ip_white_list")]
    public List<string>? IpWhiteList { get; set; }

    /// <summary>
    /// <para>机器人的 open_id</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_e6e14f667cfe239d7b129b521dce0569</para>
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }
}
