// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Conversations;

namespace Mud.Feishu.AI.Conversations;

/// <summary>
/// 进程内会话闸门（默认实现）：把中立引擎 <see cref="KeyedAsyncGate"/> 的键控串行能力
/// 暴露为会话契约 <see cref="IConversationGate"/>。
/// </summary>
/// <remarks>
/// <para>
/// <b>薄壳（第七式「三段分离」的壳段）</b>：本类只有"键的构造/校验归谁"这一产品裁量，
/// 串行机械语义（同键串行、跨键并行、空闲回收、取消不 Release）全部在中立引擎内实现并有自己的用例。
/// </para>
/// <para>
/// <c>AddFeishuAgent</c> 默认注册本实现，多实例部署经 Redis 闸门（<c>AddFeishuRedisConversationGate</c>）替换；
/// 键必须由 <see cref="ConversationKeyBuilder"/> 构造（闸门不自行拼接键）。
/// </para>
/// </remarks>
public sealed class KeyedConversationGate : IConversationGate
{
    private readonly KeyedAsyncGate _engine = new();

    /// <inheritdoc />
    public async Task<IConversationGateHandle> AcquireAsync(
        string conversationKey,
        CancellationToken cancellationToken = default)
    {
        var handle = await _engine.AcquireAsync(conversationKey, cancellationToken).ConfigureAwait(false);
        return new GateHandle(handle);
    }

    /// <summary>把中立引擎句柄适配为会话契约句柄（放行语义 = 释放引擎句柄）。</summary>
    private sealed class GateHandle(IDisposable engineHandle) : IConversationGateHandle
    {
        public void Dispose() => engineHandle.Dispose();
    }
}
