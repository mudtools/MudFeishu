// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Application;

/// <summary>
/// ApplicationAppAbility
/// <para>当前版本下应用开启的能力</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ApplicationAppAbility
{
    /// <summary>
    /// <para>小程序能力</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("gadget")]
    public ApplicationGadget? Gadget { get; set; }

    /// <summary>
    /// <para>网页能力</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("web_app")]
    public ApplicationWebApp? WebApp { get; set; }

    /// <summary>
    /// <para>机器人能力</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("bot")]
    public ApplicationBot? Bot { get; set; }

    /// <summary>
    /// <para>小组件能力</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("workplace_widgets")]
    public ApplicationWorkplaceWidget[]? WorkplaceWidgets { get; set; }

    /// <summary>
    /// <para>主导航小程序</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("navigate")]
    public ApplicationNavigate? Navigate { get; set; }

    /// <summary>
    /// <para>云文档应用</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("cloud_doc")]
    public ApplicationCloudDoc? CloudDoc { get; set; }

    /// <summary>
    /// <para>云文档小组件</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("docs_blocks")]
    public ApplicationDocsBlock[]? DocsBlocks { get; set; }

    /// <summary>
    /// <para>消息快捷操作</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("message_action")]
    public ApplicationMessageAction? MessageAction { get; set; }

    /// <summary>
    /// <para>加号菜单</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("plus_menu")]
    public ApplicationPlusMenu? PlusMenu { get; set; }
}
