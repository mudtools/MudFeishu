// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 更新应用红点请求体（用于工作台场景）。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class SetAppBadgeRequest
{
    /// <summary>
    /// <para>用户的 ID，类型由 user_id_type 指定</para>
    /// <para>必填：是</para>
    /// <para>示例值：ou_d317f090b7258ad0372aa53963cda70d</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>红点数据版本号，用于并发更新冲突检测</para>
    /// <para>必填：是</para>
    /// <para>示例值：1664360599355</para>
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// <para>红点附加信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：{}</para>
    /// </summary>
    [JsonPropertyName("extra")]
    public string? Extra { get; set; }

    /// <summary>
    /// <para>桌面端各应用能力的红点数量</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pc")]
    public AppBadgeNumber? Pc { get; set; }

    /// <summary>
    /// <para>移动端各应用能力的红点数量</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("mobile")]
    public AppBadgeNumber? Mobile { get; set; }
}

/// <summary>
/// 各应用能力的红点数量。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppBadgeNumber
{
    /// <summary>
    /// <para>网页应用的红点数量</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("web_app")]
    public int? WebApp { get; set; }

    /// <summary>
    /// <para>小程序的红点数量</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("gadget")]
    public int? Gadget { get; set; }
}
