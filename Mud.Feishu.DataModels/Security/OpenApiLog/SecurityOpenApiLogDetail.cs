// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Security;

/// <summary>
/// OpenAPI 调用日志详情
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Security")]
public class SecurityOpenApiLogDetail
{
    /// <summary>
    /// <para>http 请求路径</para>
    /// <para>示例值：/open-apis/demo/v1/example</para>
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>
    /// <para>http 请求方法</para>
    /// <para>示例值：POST</para>
    /// </summary>
    [JsonPropertyName("method")]
    public string? Method { get; set; }

    /// <summary>
    /// <para>http 查询参数</para>
    /// <para>示例值：{}</para>
    /// </summary>
    [JsonPropertyName("query_param")]
    public string? QueryParam { get; set; }

    /// <summary>
    /// <para>http 请求体</para>
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    /// <summary>
    /// <para>http 状态码</para>
    /// </summary>
    [JsonPropertyName("status_code")]
    public int? StatusCode { get; set; }

    /// <summary>
    /// <para>http 响应体，仅返回 code、msg、error 信息等</para>
    /// </summary>
    [JsonPropertyName("response")]
    public string? Response { get; set; }
}
