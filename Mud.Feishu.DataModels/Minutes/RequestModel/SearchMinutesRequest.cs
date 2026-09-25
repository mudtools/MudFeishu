// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Minutes;

/// <summary>
/// 搜索妙记请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Minutes")]
public class SearchMinutesRequest
{
    /// <summary>
    /// <para>搜索关键词，长度范围 10～50 字符</para>
    /// <para>必填：否（query、filter 至少提供一个过滤条件）</para>
    /// <para>示例值：季度规划会议</para>
    /// </summary>
    [JsonPropertyName("query")]
    public string? Query { get; set; }

    /// <summary>
    /// <para>妙记搜索的过滤条件（所有者、参与者、创建时间）</para>
    /// <para>必填：否（query、filter 至少提供一个过滤条件）</para>
    /// </summary>
    [JsonPropertyName("filter")]
    public SearchMinutesFilter? Filter { get; set; }

    /// <summary>
    /// <para>排序方式</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sorter")]
    public string? Sorter { get; set; }
}
