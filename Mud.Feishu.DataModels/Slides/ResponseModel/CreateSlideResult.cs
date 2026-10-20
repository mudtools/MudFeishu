// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Slides;

/// <summary>
/// 创建幻灯片页面响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Slides")]
public class CreateSlideResult
{
    /// <summary>
    /// <para>新创建页面的唯一标识</para>
    /// <para>必填：否</para>
    /// <para>示例值：slidecn20m4XJ1hJXXxXXX</para>
    /// </summary>
    [JsonPropertyName("slide_id")]
    public string? SlideId { get; set; }

    /// <summary>
    /// <para>演示文稿的最新版本号</para>
    /// <para>必填：否</para>
    /// <para>示例值：100</para>
    /// </summary>
    [JsonPropertyName("revision_id")]
    public long? RevisionId { get; set; }

    /// <summary>
    /// <para>XML 语法问题（内容被接受但被服务端改写时返回）</para>
    /// <para>必填：否</para>
    /// <para>示例值：XML sanitize issues: </para>
    /// </summary>
    [JsonPropertyName("issues")]
    public string? Issues { get; set; }
}
