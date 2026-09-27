// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.MDM;

/// <summary>
/// 分页批量查询国家/地区响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "MDM")]
public class ListCountryRegionsResult
{
    /// <summary>
    /// <para>国家/地区目录列表</para>
    /// </summary>
    [JsonPropertyName("data")]
    public MdmCountryRegion[]? Data { get; set; }

    /// <summary>
    /// <para>总数</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("total")]
    public string? Total { get; set; }

    /// <summary>
    /// <para>下一次分页参数</para>
    /// <para>示例值：token</para>
    /// </summary>
    [JsonPropertyName("next_page_token")]
    public string? NextPageToken { get; set; }
}
