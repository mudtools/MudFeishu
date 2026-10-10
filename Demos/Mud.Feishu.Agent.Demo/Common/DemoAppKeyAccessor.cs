// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Services;

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// 演示用应用键访问器（R-13）：把"演示应用键"显式提供给会话事件处理器。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么必须有它（不是可选项）</b>：事件处理器的基类在<b>缺少</b> appKey 而<b>又装配了</b>
/// 工具执行链时 <b>fail-closed 抛错</b>（<c>ConversationalFeishuEventHandler.AllowToolsWithoutAppKey</c>）——
/// 因为该形态下所有工具调用与知识注入必然 100% 失败，继续只会每轮白跑一次模型调用。
/// 处理器的文档给出的根治方式是"<b>注册 <c>IAppKeyAccessor</c></b>
/// （多应用/单应用均可显式提供应用键）"，本类即该推荐形态的最小实现。
/// </para>
/// <para>
/// <b>与 WebSocket/Webhook 通道的区别</b>：生产由通道在派发前写入应用键（多应用事实来源）；
/// 本演示不经通道直接投递事件（见 <c>Modes/DomainEventsDemo.cs</c> 的说明），
/// 故由本类提供等价的"当前应用键"事实。
/// </para>
/// <para>
/// <b>多实例/并发语义</b>：真实通道的 <c>IAppKeyAccessor</c> 是 per-request 的（AsyncLocal 承载）。
/// 本类只服务单应用演示（进程内固定键），<b>不可</b>照抄到多应用生产宿主。
/// </para>
/// </remarks>
/// <param name="appKey">演示应用键（不得为空）。</param>
internal sealed class DemoAppKeyAccessor(string appKey) : IAppKeyAccessor
{
    private readonly string _appKey = string.IsNullOrWhiteSpace(appKey)
        ? throw new ArgumentException("演示应用键不得为空", nameof(appKey))
        : appKey;

    /// <inheritdoc />
    public string? CurrentAppKey => _appKey;

    /// <inheritdoc />
    /// <remarks>单应用演示：应用键固定，覆写无效（显式抛错好过静默不生效）。</remarks>
    public void SetAppKey(string appKey)
        => throw new NotSupportedException(
            "演示用访问器的应用键在构造期固定（单应用语义）——多应用宿主请使用通道注册的 per-request 实现");

    /// <inheritdoc />
    /// <remarks>同上：无可清除的 per-request 状态。</remarks>
    public void Clear()
    {
        // 有意为空：单应用演示没有 per-request 状态可清（方法必须实现，但语义上是 no-op）。
    }
}
