// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Security;

/// <summary>
/// OpenAPI 审计日志条目
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Security")]
public class SecurityOpenApiLog
{
    /// <summary>
    /// <para>openapi 日志唯一标识</para>
    /// <para>示例值：10000</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>飞书开放平台定义的 API</para>
    /// <para>示例值：POST/open-apis/demo/v1/example</para>
    /// </summary>
    [JsonPropertyName("api_key")]
    public string? ApiKey { get; set; }

    /// <summary>
    /// <para>日志产生的时间，以秒为单位的时间戳</para>
    /// <para>示例值：1610613336</para>
    /// </summary>
    [JsonPropertyName("event_time")]
    public int? EventTime { get; set; }

    /// <summary>
    /// <para>调用 OpenAPI 的应用唯一标识</para>
    /// <para>示例值：cli_xxx</para>
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// <para>发起调用 api 的 ip 地址</para>
    /// <para>示例值：192.123.12.1</para>
    /// </summary>
    [JsonPropertyName("ip")]
    public string? Ip { get; set; }

    /// <summary>
    /// <para>openapi 调用日志详情</para>
    /// </summary>
    [JsonPropertyName("log_detail")]
    public SecurityOpenApiLogDetail? LogDetail { get; set; }
}
