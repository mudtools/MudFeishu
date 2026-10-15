// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Application;

/// <summary>
/// ApplicationAppVisibilityEvent
/// <para>可见性名单</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ApplicationAppVisibilityEvent
{
    /// <summary>
    /// <para>是否全员可见</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_all")]
    public bool? IsAll { get; set; }

    /// <summary>
    /// <para>可见名单</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("visible_list")]
    public ApplicationAppVisibleListEvent? VisibleList { get; set; }

    /// <summary>
    /// <para>不可见名单</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("invisible_list")]
    public ApplicationAppVisibleListEvent? InvisibleList { get; set; }
}
