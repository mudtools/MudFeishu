// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Slides;

/// <summary>
/// 页面元素级局部替换请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Slides")]
public class ReplaceSlideRequest
{
    /// <summary>
    /// <para>替换操作列表，每项包含一个元素级别的编辑操作，最少 1 条，最多 200 条</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("parts")]
    public SlideReplacePart[]? Parts { get; set; }

    /// <summary>
    /// <para>操作备注</para>
    /// <para>必填：否</para>
    /// <para>示例值：批量替换文本内容</para>
    /// </summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// <para>幂等标记</para>
    /// <para>必填：否</para>
    /// <para>示例值：xx-slide-01</para>
    /// </summary>
    [JsonPropertyName("idempotency_key")]
    public string? IdempotencyKey { get; set; }
}
