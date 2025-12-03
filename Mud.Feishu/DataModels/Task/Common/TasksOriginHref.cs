// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Task;


/// <summary>
/// <para>任务关联的来源平台详情页链接</para>
/// </summary>
public class TasksOriginHref
{
    /// <summary>
    /// <para>来源链接对应的地址，如填写必须以https://或者http://开头。</para>
    /// <para>**说明**：如需调整 PC 端链接打开方式，可在飞书客户端的 **设置** &gt; **效率** &gt; **链接打开方式** 内调整。</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://www.mudtools.cn</para>
    /// <para>最大长度：1024</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>来源链接对应的标题</para>
    /// <para>必填：否</para>
    /// <para>示例值：反馈一个问题，需要协助排查</para>
    /// <para>最大长度：512</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

