// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tests.Tools;

/// <summary>
/// <see cref="FeishuToolContextAccessor"/> 作用域语义（R3-10）：所有权校验阻止
/// 「乱序释放」把内层租户上下文覆盖成外层值。
/// </summary>
public class ToolContextAccessorTests
{
    private static FeishuToolContext Context(string appKey) => new(appKey);

    [Fact]
    public void Begin_ShouldExposeContext_AndRestoreNullOnDispose()
    {
        var accessor = new FeishuToolContextAccessor();

        accessor.Current.Should().BeNull("未 Begin 时无当前上下文");

        using (accessor.Begin(Context("app-a")))
        {
            accessor.Current!.AppKey.Should().Be("app-a");
        }

        accessor.Current.Should().BeNull("释放后恢复到进入前的 null");
    }

    [Fact]
    public void Dispose_InOrder_ShouldRestorePreviousValues_AndBeIdempotent()
    {
        var accessor = new FeishuToolContextAccessor();

        var outer = accessor.Begin(Context("app-outer"));
        var inner = accessor.Begin(Context("app-inner"));

        accessor.Current!.AppKey.Should().Be("app-inner");

        inner.Dispose();
        accessor.Current!.AppKey.Should().Be("app-outer", "LIFO：内层释放后回到外层上下文");

        outer.Dispose();
        accessor.Current.Should().BeNull();

        // 双重释放必须幂等（不得把 null 再写回、也不得抛异常）。
        var act = () => { inner.Dispose(); outer.Dispose(); };
        act.Should().NotThrow();
        accessor.Current.Should().BeNull();
    }

    /// <summary>
    /// R3-10 核心用例：外层作用域<b>先</b>于内层释放（乱序）时，不得把内层写入的
    /// 租户上下文覆盖成外层的 <c>previous</c>——否则内层正在执行的工具会读到错误 appKey。
    /// </summary>
    /// <remarks>
    /// <b>已知残余</b>：内层随后释放时，会恢复它进入前捕获的 <c>previous</c>（即外层上下文），
    /// 而外层此时已释放——作用域栈被乱序释放破坏后无法完全自愈，此处只保证「不覆盖内层」这一
    /// 最小安全不变式。正常使用（严格 LIFO）由上一个用例覆盖。
    /// </remarks>
    [Fact]
    public void Dispose_OutOfOrder_ShouldNotOverwriteInnerScope()
    {
        var accessor = new FeishuToolContextAccessor();

        var outer = accessor.Begin(Context("app-outer"));
        var inner = accessor.Begin(Context("app-inner"));

        outer.Dispose();

        accessor.Current!.AppKey.Should().Be("app-inner",
            "外层乱序释放不得把内层租户上下文回滚成 app-outer（跨租户串号）");

        inner.Dispose();
    }
}