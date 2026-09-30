// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// 租户上下文作用域工厂的四步门禁（R3-5）。
/// </summary>
/// <remarks>
/// <para>
/// 本类是该工厂的<b>唯一</b>执行体测试：门禁的每一步都必须既「抛出正确异常」又「不产生下游副作用」——
/// 只断言异常类型会漏掉「先切换再报错」这类顺序缺陷（那正是跨租户错发的成因）。
/// </para>
/// <para>
/// 第 2 步（授权器缺失）在生产装配下不可达（<c>FeishuServiceCollectionExtensions</c> 默认注册
/// <c>AllowAllAppAccessAuthorizer</c>），保留为手工构造/替换实现场景的防御，故仍须有用例锁定。
/// </para>
/// </remarks>
public class FeishuAppContextScopeFactoryTests
{
    private const string ValidAppKey = "cli_app-a";

    private readonly Mock<Mud.HttpUtils.IAppContextHolder> _contextHolder = new();
    private readonly Mock<IFeishuAppManager> _appManager = new();
    private readonly Mock<Mud.HttpUtils.IAppAccessAuthorizer> _authorizer = new();

    /// <summary>授权通过时必须按「取应用上下文 → 建立作用域」的顺序返回 holder 作用域。</summary>
    [Fact]
    public void BeginScope_ShouldReturnHolderScope_WhenAuthorized()
    {
        var appContext = new Mock<IFeishuAppContext>();
        var scope = new Mock<IDisposable>();
        _authorizer.Setup(a => a.CanSwitchTo(ValidAppKey)).Returns(true);
        _appManager.Setup(m => m.GetApp(ValidAppKey)).Returns(appContext.Object);
        _contextHolder.Setup(h => h.BeginScope(appContext.Object)).Returns(scope.Object);

        var result = CreateFactory().BeginScope(ValidAppKey);

        result.Should().BeSameAs(scope.Object, "必须原样返回 holder 的作用域（释放责任归调用方，工厂不二次包装）");
        _appManager.Verify(m => m.GetApp(ValidAppKey), Times.Once, "应用上下文必须按目标 appKey 解析");
    }

    /// <summary>appKey 格式非法时必须在任何下游动作之前拒绝（含授权查询）。</summary>
    [Fact]
    public void BeginScope_ShouldThrowArgument_WhenAppKeyInvalid()
    {
        var act = () => CreateFactory().BeginScope("app key with space");

        act.Should().Throw<ArgumentException>();
        VerifyNoGateSideEffects();
    }

    /// <summary>授权器缺失时 fail-closed（不静默放行）。</summary>
    [Fact]
    public void BeginScope_ShouldThrowInvalidOperation_WhenAuthorizerMissing()
    {
        var factory = new FeishuAppContextScopeFactory(_contextHolder.Object, _appManager.Object, appAuthorizer: null);

        var act = () => factory.BeginScope(ValidAppKey);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*IAppAccessAuthorizer*", "缺失授权器属装配缺陷，必须给出可操作提示");
        VerifyNoGateSideEffects();
    }

    /// <summary>授权器拒绝时必须在切换之前中止（拒绝不能被"先切换再报错"绕过）。</summary>
    [Fact]
    public void BeginScope_ShouldThrowUnauthorized_WhenCanSwitchToReturnsFalse()
    {
        _authorizer.Setup(a => a.CanSwitchTo(ValidAppKey)).Returns(false);

        var act = () => CreateFactory().BeginScope(ValidAppKey);

        act.Should().Throw<UnauthorizedAccessException>();
        VerifyNoGateSideEffects();
    }

    /// <summary>构造参数缺失必须显式失败，而不是留到运行期空引用。</summary>
    [Fact]
    public void Constructor_ShouldRejectNullDependencies()
    {
        var act1 = () => new FeishuAppContextScopeFactory(null!, _appManager.Object, _authorizer.Object);
        var act2 = () => new FeishuAppContextScopeFactory(_contextHolder.Object, null!, _authorizer.Object);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    private FeishuAppContextScopeFactory CreateFactory()
        => new(_contextHolder.Object, _appManager.Object, _authorizer.Object);

    /// <summary>门禁拒绝路径必须零下游副作用：不解析应用、不建立作用域。</summary>
    private void VerifyNoGateSideEffects()
    {
        _appManager.Verify(m => m.GetApp(It.IsAny<string>()), Times.Never, "拒绝路径不得解析应用上下文");
        _contextHolder.Verify(
            h => h.BeginScope(It.IsAny<Mud.HttpUtils.IMudAppContext>()),
            Times.Never,
            "拒绝路径不得建立作用域（否则租户上下文已被切换）");
    }
}