// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroupMenu;

/// <summary>
/// 群菜单操作返回结果
/// </summary>
public class ChatGroupMenuResult
{
    /// <summary>
    /// <para>添加群菜单后，该群组内所有群菜单的信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("menu_tree")]
    public ChatMenuTree? MenuTree { get; set; }

    /// <summary>
    /// <para>添加群菜单后，该群组内所有群菜单的信息。</para>
    /// </summary>
    public class ChatMenuTree
    {
        /// <summary>
        /// <para>一级菜单列表</para>
        /// <para>必填：否</para>
        /// </summary>
        [JsonPropertyName("chat_menu_top_levels")]
        public ChatMenuTopLevelInfo[]? ChatMenuTopLevels { get; set; }

    }
}

/// <summary>
/// <para>一级菜单列表</para>
/// </summary>
public class ChatMenuTopLevelInfo
{
    /// <summary>
    /// <para>一级菜单 ID，后续删除、修改、排序等群菜单管理操作均需要使用菜单 ID。</para>
    /// <para>必填：否</para>
    /// <para>示例值：7117116451961487361</para>
    /// </summary>
    [JsonPropertyName("chat_menu_top_level_id")]
    public string? ChatMenuTopLevelId { get; set; }

    /// <summary>
    /// <para>一级菜单信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("chat_menu_item")]
    public ChatMenuItem? ChatMenuItem { get; set; }

    /// <summary>
    /// <para>二级菜单列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("children")]
    public ChatMenuSecondLevelInfo[]? Childrens { get; set; }

}


/// <summary>
/// <para>二级菜单列表</para>
/// </summary>
public class ChatMenuSecondLevelInfo : ChatMenuSecondLevel
{
    /// <summary>
    /// <para>二级菜单 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：7039638308221468675</para>
    /// </summary>
    [JsonPropertyName("chat_menu_second_level_id")]
    public string? ChatMenuSecondLevelId { get; set; }
}