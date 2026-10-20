// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Slides;

/// <summary>
/// 页面元素级局部替换响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Slides")]
public class ReplaceSlideResult
{
    /// <summary>
    /// <para>演示文稿的最新版本号</para>
    /// <para>必填：否</para>
    /// <para>示例值：100</para>
    /// </summary>
    [JsonPropertyName("revision_id")]
    public long? RevisionId { get; set; }

    /// <summary>
    /// <para>失败的操作在 parts 数组中的索引，整个请求失败时返回</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("failed_part_index")]
    public int? FailedPartIndex { get; set; }

    /// <summary>
    /// <para>失败原因描述</para>
    /// <para>必填：否</para>
    /// <para>示例值：pattern not found</para>
    /// </summary>
    [JsonPropertyName("failed_reason")]
    public string? FailedReason { get; set; }
}
