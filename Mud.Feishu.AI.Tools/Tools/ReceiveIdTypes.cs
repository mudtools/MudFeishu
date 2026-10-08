// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools.Tools;

/// <summary>
/// receive_id_type 白名单（防注入任意查询参数）。
/// </summary>
/// <remarks>
/// 原定义在 <c>Channels/EditMessageChannel</c>，WP6-W8 上移至 <c>Tools</c> 层以消除
/// <c>Internal</c> → <c>Channels</c> 的反向依赖。<c>EditMessageChannel</c> 反向引用本类型。
/// </remarks>
internal static class ReceiveIdTypes
{
    /// <summary>允许的 receive_id_type 白名单。</summary>
    public static readonly string[] Allowed = ["chat_id", "open_id", "user_id", "union_id", "email"];
}
