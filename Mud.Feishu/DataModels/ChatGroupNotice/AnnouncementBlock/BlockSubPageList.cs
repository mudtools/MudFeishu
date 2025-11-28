// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroupNotice;

/// <summary>
/// <para>新版 Wiki 子目录 Block</para>
/// </summary>
public class BlockSubPageList
{
    /// <summary>
    /// <para>知识库节点 token，仅支持知识库文档创建子页面列表，且需传入当前页面的 wiki token</para>
    /// <para>必填：是</para>
    /// <para>示例值：Ub47wVI7AikG9wkgnpSbFyabcef</para>
    /// <para>最大长度：27</para>
    /// <para>最小长度：27</para>
    /// </summary>
    [JsonPropertyName("wiki_token")]
    public string WikiToken { get; set; } = string.Empty;
}