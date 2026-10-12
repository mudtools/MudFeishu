// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// T1.1 装配期接线诊断守卫：确保 user 身份工具的运行时依赖在装配期被诊断。
/// </summary>
/// <remarks>
/// <para>
/// 守卫三条：
/// <list type="number">
/// <item><c>DiagnoseUserToolWiring</c> 方法存在于 <c>FeishuToolsServiceCollectionExtensions</c>；</item>
/// <item>方法在 <c>BuildRegistry</c> 中被调用（不能被注释掉或删掉）；</item>
/// <item>方法检查了三个依赖：<c>IFeishuCurrentUserContext</c>、<c>IFeishuTokenManagerResolver</c>、<c>UserTokenPrecheckModes.Off</c>。</item>
/// </list>
/// </para>
/// <para>
/// <b>金丝雀</b>：故意移除调用点或检查项，断言必须报红。
/// </para>
/// </remarks>
public class UserToolWiringContractGuards
{
    private const string InfrastructureFilePath =
        "Mud.Feishu.AI.Tools/Extensions/FeishuToolsServiceCollectionExtensions.cs";

    /// <summary>
    /// 守卫 ①：DiagnoseUserToolWiring 方法必须存在于 FeishuToolsServiceCollectionExtensions。
    /// </summary>
    [Fact]
    public void DiagnoseUserToolWiring_ShouldExist_InServiceCollectionExtensions()
    {
        var source = ReadInfrastructureSource();
        source.Should().Contain("DiagnoseUserToolWiring",
            "T1.1 装配期接线诊断方法必须存在于 FeishuToolsServiceCollectionExtensions");
    }

    /// <summary>
    /// 守卫 ②：DiagnoseUserToolWiring 必须在 BuildRegistry 中被调用。
    /// </summary>
    [Fact]
    public void DiagnoseUserToolWiring_ShouldBeCalled_InBuildRegistry()
    {
        var source = ReadInfrastructureSource();
        source.Should().Contain("DiagnoseUserToolWiring(sp, registry, options)",
            "DiagnoseUserToolWiring 必须在 BuildRegistry 中被调用——不能被注释掉或删掉");
    }

    /// <summary>
    /// 守卫 ③：DiagnoseUserToolWiring 必须检查三个依赖（IFeishuCurrentUserContext / IFeishuTokenManagerResolver / Off 模式）。
    /// </summary>
    [Theory]
    [InlineData("IFeishuCurrentUserContext", "用户上下文")]
    [InlineData("IFeishuTokenManagerResolver", "令牌管理器解析器")]
    [InlineData("UserTokenPrecheckModes.Off", "探测关闭模式")]
    public void DiagnoseUserToolWiring_ShouldCheck_AllThreeDependencies(string dependency, string description)
    {
        var source = ReadInfrastructureSource();
        source.Should().Contain(dependency,
            $"DiagnoseUserToolWiring 必须检查 {description}（{dependency}）——漏检会让宿主在运行期逐请求报错");
    }

    /// <summary>
    /// 守卫 ④（金丝雀）：DiagnoseUserToolWiring 方法体必须包含 user 身份工具的触发条件。
    /// </summary>
    [Fact]
    public void DiagnoseUserToolWiring_ShouldOnlyTrigger_WhenUserToolsEnabled()
    {
        var source = ReadInfrastructureSource();
        source.Should().Contain("FeishuToolIdentityNames.User",
            "DiagnoseUserToolWiring 必须只检查 user 身份工具——不应对 tenant 工具触发");
    }

    /// <summary>
    /// 守卫 ⑤：UserTokenPrecheck 必须在 DI 容器中注册（TryAddSingleton）。
    /// </summary>
    [Fact]
    public void UserTokenPrecheck_ShouldBeRegistered_InDI()
    {
        var source = ReadInfrastructureSource();
        source.Should().Contain("TryAddSingleton<UserTokenPrecheck>",
            "UserTokenPrecheck 必须在 AddFeishuToolInfrastructure 中注册——否则 FeishuToolBinding 构造函数解析到 null");
    }

    private static string ReadInfrastructureSource()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root, InfrastructureFilePath);
        File.Exists(path).Should().BeTrue($"基础设施文件必须存在：{InfrastructureFilePath}");
        return File.ReadAllText(path);
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "Mud.Feishu.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        directory.Should().NotBeNullOrEmpty("测试必须能定位仓库根目录（以 Mud.Feishu.slnx 为锚）");
        return directory!;
    }
}
