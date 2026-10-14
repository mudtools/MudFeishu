// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Abstractions.Context;
using Mud.AI.Abstractions.Conversations;

namespace Mud.AI.Agent.Context;

/// <summary>
/// 发送者信息装配器（P2D-5b）：发送者 ID 形态注入 Prompt。
/// </summary>
/// <remarks>
/// <para>
/// <b>N4 上移（E8，迁G）</b>：已核实本类零产品语义（只读 <see cref="ConversationRequest.SenderId"/>
/// 与 <see cref="ConversationScope"/>）——机制整体迁入中立包，产品侧经 global using 保持
/// 简单名引用零改动（DI 注册与测试不受影响）。
/// </para>
/// <para>
/// 脱敏边界：只输出 ID 形态（open_id/user_id），<b>不取昵称原文</b>（防 PII 入模型上下文）。
/// 零外部调用。
/// </para>
/// </remarks>
public sealed class SenderInfoContextAssembler : IContextAssembler
{
    /// <summary>默认装配顺序（问题文本之前的基础事实）。</summary>
    public const int DefaultOrder = 10;

    /// <inheritdoc />
    public int Order => DefaultOrder;

    /// <inheritdoc />
    public Task<string?> AssembleAsync(ConversationRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrEmpty(request.SenderId))
        {
            return Task.FromResult<string?>(null);
        }

        var scopeText = request.Scope.IsGroup ? "群聊" : "单聊";
        return Task.FromResult<string?>($"[发送者] 用户 ID: {request.SenderId}（{scopeText}会话）");
    }
}
