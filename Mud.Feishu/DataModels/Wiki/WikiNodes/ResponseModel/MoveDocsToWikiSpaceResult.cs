// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Wiki;

/// <summary>
/// <para>[移动云空间文档至知识空间]任务结果</para>
/// </summary>
public class MoveDocsToWikiSpaceResult
{
    /// <summary>
    /// <para>移动完成的节点信息</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("node")]
    public MoveResultNode Node { get; set; } = new();

    /// <summary>
    /// <para>节点移动状态码</para>
    /// <para>必填：是</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; set; }

    /// <summary>
    /// <para>节点移动状态信息</para>
    /// <para>必填：是</para>
    /// <para>示例值：success</para>
    /// </summary>
    [JsonPropertyName("status_msg")]
    public string StatusMsg { get; set; } = string.Empty;
}