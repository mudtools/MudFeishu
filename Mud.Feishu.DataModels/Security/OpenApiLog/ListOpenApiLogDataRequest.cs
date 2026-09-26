// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Security;

/// <summary>
/// 获取 OpenAPI 审计日志数据请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Security")]
public class ListOpenApiLogDataRequest
{
    /// <summary>
    /// <para>飞书开放平台定义的 API，参考 API 列表（最大长度 100）</para>
    /// <para>必填：否</para>
    /// <para>示例值：["POST/open-apis/authen/v1/access_token"]</para>
    /// </summary>
    [JsonPropertyName("api_keys")]
    public string[]? ApiKeys { get; set; }

    /// <summary>
    /// <para>以秒为单位的起始时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1610613336</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public int? StartTime { get; set; }

    /// <summary>
    /// <para>以秒为单位的终止时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1610613336</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public int? EndTime { get; set; }

    /// <summary>
    /// <para>调用 OpenAPI 的应用唯一标识，可在开发者后台应用详情页的凭证与基础信息中获取</para>
    /// <para>必填：否</para>
    /// <para>示例值：cli_xxx</para>
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// <para>分页大小，取值范围 1~100</para>
    /// <para>必填：否</para>
    /// <para>示例值：20</para>
    /// </summary>
    [JsonPropertyName("page_size")]
    public int? PageSize { get; set; }

    /// <summary>
    /// <para>分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</para>
    /// <para>必填：否</para>
    /// <para>示例值：xxx</para>
    /// </summary>
    [JsonPropertyName("page_token")]
    public string? PageToken { get; set; }
}
