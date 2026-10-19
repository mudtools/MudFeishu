// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.CapabilityDemo.Common;

/// <summary>
/// 会话键构造：经官方接缝 <c>ConversationScopeMapper</c>，<b>不内联拼接</b>
/// （键布局变化时这里零改动）。
/// </summary>
internal static class ConversationKeys
{
    public static string Build(string appKey, string act, string userId)
        => ConversationScopeMapper.BuildKey(
            appKey,
            ConversationScope.P2P(),
            $"{act}_{userId}");
}
