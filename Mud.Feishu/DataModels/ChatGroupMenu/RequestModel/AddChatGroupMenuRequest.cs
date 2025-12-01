// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroupMenu;

/// <summary>
/// 添加群菜单请求体
/// <para>在指定群组中添加一个或多个群菜单。成功调用后接口会返回当前群组内所有群菜单信息。</para>
/// </summary>
public class AddChatGroupMenuRequest
{
    /// <summary>
    /// <para>要向群内追加的菜单</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("menu_tree")]
    public
#if NET7_0_OR_GREATER
        required
#endif
        ChatMenuTreeData MenuTree
    { get; set; } = new();

}



/// <summary>
/// <para>要向群内追加的菜单</para>
/// </summary>
public class ChatMenuTreeData
{
    /// <summary>
    /// <para>一级菜单列表</para>
    /// <para>**注意**：一个群内最多有 3 个一级菜单。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("chat_menu_top_levels")]
    public ChatMenuTopLevel[] ChatMenuTopLevels { get; set; } = [];

}