// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>
/// 生成 CalDAV 配置 响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Calendar")]
public class GenerateCaldavConfSettingResult
{
    /// <summary>
    /// <para>CalDAV 密码。</para>
    /// <para>必填：否</para>
    /// <para>示例值：A67h23sd8</para>
    /// </summary>
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    /// <summary>
    /// <para>CalDAV 用户名。</para>
    /// <para>必填：否</para>
    /// <para>示例值：ZhangSan</para>
    /// </summary>
    [JsonPropertyName("user_name")]
    public string? UserName { get; set; }

    /// <summary>
    /// <para>服务器地址</para>
    /// <para>必填：否</para>
    /// <para>示例值：caldav.domain.com</para>
    /// </summary>
    [JsonPropertyName("server_address")]
    public string? ServerAddress { get; set; }

    /// <summary>
    /// <para>设备名。与你发送请求时传入的设备名一致。</para>
    /// <para>必填：否</para>
    /// <para>示例值：iPhone</para>
    /// </summary>
    [JsonPropertyName("device_name")]
    public string? DeviceName { get; set; }
}
