// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 工具执行上下文的「当前值」访问器：把会话级 <see cref="FeishuToolContext"/>
/// 经 <see cref="System.Threading.AsyncLocal{T}"/> 流入工具执行链。
/// </summary>
/// <remarks>
/// <para>
/// 模型 tool_call 由 MAF 在 <c>RunAsync</c> 调用链内自动执行，工具实例为单例、
/// 无法在构造期绑定会话——上下文必须沿异步调用链流动：事件入口
/// （如 <c>ConversationalFeishuEventHandler</c>）在 <c>RunAsync</c> 前
/// <see cref="Begin"/>，工具桥接/绑定层在执行期 <see cref="Current"/> 读取。
/// </para>
/// <para>
/// 缺失上下文即失败（缺 <see cref="FeishuToolContext.AppKey"/> 同义）——多租户隔离
/// 不允许「默认 appKey」兜底（TMA2-20）。
/// </para>
/// </remarks>
public interface IFeishuToolContextAccessor
{
    /// <summary>当前异步流的工具执行上下文（未设置时为 <see langword="null"/>）。</summary>
    FeishuToolContext? Current { get; }

    /// <summary>
    /// 设置当前异步流的工具执行上下文；返回的作用域释放时恢复先前的值。
    /// </summary>
    /// <param name="context">要生效的上下文。</param>
    /// <returns>作用域（<see cref="IDisposable.Dispose"/> 恢复原值）。</returns>
    IDisposable Begin(FeishuToolContext context);
}

/// <summary><see cref="IFeishuToolContextAccessor"/> 的 <see cref="System.Threading.AsyncLocal{T}"/> 实现。</summary>
public sealed class FeishuToolContextAccessor : IFeishuToolContextAccessor
{
    private readonly AsyncLocal<FeishuToolContext?> _current = new();

    /// <inheritdoc />
    public FeishuToolContext? Current => _current.Value;

    /// <inheritdoc />
    public IDisposable Begin(FeishuToolContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        var previous = _current.Value;
        _current.Value = context;
        return new RestoreScope(this, previous);
    }

    private sealed class RestoreScope(FeishuToolContextAccessor owner, FeishuToolContext? previous) : IDisposable
    {
        private FeishuToolContextAccessor? _owner = owner;

        public void Dispose()
        {
            // 双重释放容错：恢复到进入前的值即可（幂等）。
            if (_owner is null)
                return;

            _owner._current.Value = previous;
            _owner = null;
        }
    }
}
