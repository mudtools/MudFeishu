// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Abstractions.Conversations;

using Mud.Feishu.Abstractions.Conversations;

namespace Mud.Feishu.AI.Conversations;

/// <summary>
/// 把飞书侧的会话存储契约（<see cref="IConversationStore"/>，位于<b>已发布</b>的
/// <c>Mud.Feishu.Abstractions</c>）桥接到中立端口 <see cref="ISessionStore"/> 的防腐层。
/// </summary>
/// <remarks>
/// <b>为什么需要这一层（不是冗余）</b>：会话门面（<c>Mud.AI.Agent.Sessions.AgentSessionStore</c>）住在<b>产品中立</b>的 AI 运行期包里，
/// 而它的存储契约两处都搬不动——搬进 <c>Mud.AI.Abstractions</c> 会让核心 SDK 反向依赖 AI 包族（撞中立性守卫），
/// 让门面直接引用 <see cref="IConversationStore"/> 则让中立包反向依赖产品契约，上游化（迁独立包）不可能。
/// <para>
/// 两者接口形状相同，但<b>生命周期与版本纪律完全不同</b>（一个随已发布 SDK 冻结、一个在未发布窗口可自由变更）⇒ 这里是<u>有意的接缝</u>，
/// 不是可以合并的重复类型。
/// </para>
/// <para>
/// 本类型是 <c>internal</c>：它是实现细节，宿主看到的仍是 <see cref="IConversationStore"/>（<c>FeishuAgent</c> 的公开构造签名未变）。
/// </para>
/// </remarks>
internal sealed class ConversationStoreSessionAdapter : ISessionStore
{
    private readonly IConversationStore _inner;

    /// <summary>初始化 <see cref="ConversationStoreSessionAdapter"/>。</summary>
    /// <param name="inner">飞书侧会话存储实现（内存 / Redis / 宿主自定义）。</param>
    /// <exception cref="ArgumentNullException"><paramref name="inner"/> 为 <see langword="null"/>。</exception>
    public ConversationStoreSessionAdapter(IConversationStore inner)
        => _inner = inner ?? throw new ArgumentNullException(nameof(inner));

    /// <inheritdoc />
    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
        => _inner.GetAsync(key, cancellationToken);

    /// <inheritdoc />
    public Task SaveAsync(string key, string serializedSession, CancellationToken cancellationToken = default)
        => _inner.SaveAsync(key, serializedSession, cancellationToken);

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        => _inner.DeleteAsync(key, cancellationToken);
}
