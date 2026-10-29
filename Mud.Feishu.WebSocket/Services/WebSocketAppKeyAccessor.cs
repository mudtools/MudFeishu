// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Services;

namespace Mud.Feishu.WebSocket.Services;

/// <summary>
/// WebSocket 侧应用键访问器：AppKey 是<b>连接级常量</b>（连接绑定单一应用，
/// 经 <c>ResolveAppKeyAndValidate</c> 在启动期校验/回填），非 per-request 上下文。
/// </summary>
/// <remarks>
/// 回归出处：2026-10-28 审查（E，.docs/AI模块功能缺陷与完整性分析报告-2026-10-28.md §2.6）——
/// WebSocket 侧不注册 <see cref="IAppKeyAccessor"/>，<c>ConversationalFeishuEventHandler</c> 的
/// <c>CurrentAppKey</c> 恒空，多应用宿主走 WebSocket 通道全部落入 fail-fast。
/// 经 <see cref="IOptionsMonitor{FeishuWebSocketOptions}"/> 读取 <c>ResolvedAppKey</c>，
/// 与 Webhook 侧的 AsyncLocal 语义不同属<b>有意差异</b>：长连接单通道无请求边界可设。
/// <see cref="SetAppKey"/>/<see cref="Clear"/> 在连接级常量语义下为空操作（键由连接装配确定，
/// 不得被运行期改写——改写即绕过多应用校验）。
/// </remarks>
internal sealed class WebSocketAppKeyAccessor : IAppKeyAccessor
{
    private readonly IOptionsMonitor<FeishuWebSocketOptions> _options;

    public WebSocketAppKeyAccessor(IOptionsMonitor<FeishuWebSocketOptions> options)
        => _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <inheritdoc />
    public string? CurrentAppKey
    {
        get
        {
            var appKey = _options.CurrentValue.ResolvedAppKey;
            return string.IsNullOrEmpty(appKey) ? null : appKey;
        }
    }

    /// <inheritdoc />
    /// <remarks>连接级常量：键由 <c>ResolveAppKeyAndValidate</c> 确定，运行期写入被忽略（多应用校验不可绕过）。</remarks>
    public void SetAppKey(string appKey)
    {
    }

    /// <inheritdoc />
    public void Clear()
    {
    }
}
