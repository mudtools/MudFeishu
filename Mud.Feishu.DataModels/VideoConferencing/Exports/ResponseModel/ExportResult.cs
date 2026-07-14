// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;

/// <summary>
/// 导出结果
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class ExportResult
{
    /// <summary>
    /// <para>任务状态</para>
    /// <para>必填：是</para>
    /// <para>示例值：3</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：处理中</item>
    /// <item>2：失败</item>
    /// <item>3：完成</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; set; }

    /// <summary>
    /// <para>文件下载地址</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://lf1-ttcdn-tos.pstatp.com/obj/xxx</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>文件token</para>
    /// <para>必填：否</para>
    /// <para>示例值：6yHu7Igp7Igy62Ez6fLr6IJz7j9i5WMe6fHq5yZeY2Jz6yLqYAMAY46fZfEz64Lr5fYyYQ==</para>
    /// </summary>
    [JsonPropertyName("file_token")]
    public string? FileToken { get; set; }

    /// <summary>
    /// <para>失败信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：nopermission</para>
    /// </summary>
    [JsonPropertyName("fail_msg")]
    public string? FailMsg { get; set; }
}
