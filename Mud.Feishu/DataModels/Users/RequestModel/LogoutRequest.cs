// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 退出登录请求体
/// </summary>
public class LogoutRequest
{
    /// <summary>
    /// idp 侧的唯一标识，logout_type = 2 时必填
    /// </summary>
    [JsonPropertyName("idp_credential_id")]
    public string? IdpCredentialId { get; set; }

    /// <summary>
    /// <para>登出的方式，可选值有：<list type="bullet">
    /// <item>1：UserID，使用开放平台的维度登出</item>
    /// <item>2：IdpCredentialID，使用 idp 侧的唯一标识登出</item>
    /// <item>3：Session 标识符，基于session uuid 登出</item></list>
    /// </para>
    /// </summary>
    [JsonPropertyName("logout_type")]
    public int LogoutType { get; set; } = 1;

    /// <summary>
    /// <para>登出的客户端类型，默认全部登出。可选值：
    /// <list type="bullet">
    /// <item>1：PC 端</item>
    /// <item>2：Web 端</item>
    /// <item>3：Android 端</item>
    /// <item>4：iOS 端</item>
    /// <item>5：服务端</item>
    /// <item>6：旧版小程序端</item>
    /// <item>8：其他移动端</item>
    /// </list>
    /// </para>
    /// </summary>
    [JsonPropertyName("terminal_type")]
    public int[] TerminalType { get; set; } = [];

    /// <summary>
    /// <para>开放平台的数据标识，用户 ID 类型与查询参数 user_id_type 一致，logout_type = 1 时必填</para>
    /// <para>示例值："ou_7dab8a3d3cdcc9da365777c7ad535d62"</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    ///  <para>登出提示语，非必填，不传时默认提示：你已在其他客户端上退出了当前设备，请重新登录。可选值：</para>
    ///   <list type="bullet">
    ///   <item>34：您已修改登录密码，请重新登录</item>
    ///   <item>35：您的登录态已失效，请重新登录</item>
    ///   <item>36：您的密码已过期，请在登录页面通过忘记密码功能修改密码后重新登录</item>
    ///   </list>
    /// </summary>
    [JsonPropertyName("logout_reason")]
    public int? LogoutReason { get; set; }

    /// <summary>
    /// <para>需要精确登出的 session 标识符，logout_type = 3 时必填</para>
    /// <para>示例值："AAAAAAAAAANll6nQoIAAFA=="</para>
    /// </summary>
    [JsonPropertyName("sid")]
    public string? Sid { get; set; }
}