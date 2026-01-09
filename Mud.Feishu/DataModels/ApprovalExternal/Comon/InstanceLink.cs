// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalExternal;

/// <summary>
/// <para>审批实例链接信息。设置的链接用于在审批中心 **已发起** 列表内点击跳转，跳回三方审批系统查看审批详情。</para>
/// </summary>
public class InstanceLink
{
    /// <summary>
    /// <para>PC 端的三方审批实例跳转链接。</para>
    /// <para>**说明**：</para>
    /// <para>- 当用户使用飞书 PC 端查看实例详情时，通过该链接进行跳转。</para>
    /// <para>- pc_link 和 mobile_link 至少填一个。</para>
    /// <para>必填：是</para>
    /// <para>示例值：https://applink.feishu.cn/client/mini_program/open?mode=appCenter&amp;appId=cli_9c90fc38e07a9101&amp;path=pc/pages/detail?id=1234</para>
    /// </summary>
    [JsonPropertyName("pc_link")]
    public string PcLink { get; set; } = string.Empty;

    /// <summary>
    /// <para>移动端的三方审批实例跳转链接。</para>
    /// <para>**说明**：</para>
    /// <para>- 当用户使用飞书移动端查看实例详情时，通过该链接进行跳转。</para>
    /// <para>- pc_link 和 mobile_link 至少填一个。</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://applink.feishu.cn/client/mini_program/open?appId=cli_9c90fc38e07a9101&amp;path=pages/detail?id=1234</para>
    /// </summary>
    [JsonPropertyName("mobile_link")]
    public string? MobileLink { get; set; }
}
