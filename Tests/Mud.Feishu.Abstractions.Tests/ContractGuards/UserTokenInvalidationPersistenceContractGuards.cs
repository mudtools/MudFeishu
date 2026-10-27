// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录下的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Mud.Feishu.Abstractions.Authentication;

namespace Mud.Feishu.Abstractions.Tests.ContractGuards;

/// <summary>
/// 用户访问令牌失效必须落持久层，且能力探测口径与 CAS 一致（落在未装饰的原始 store 上）。
/// </summary>
/// <remarks>
/// 回归事实：<c>InvalidateUserTokenAsync</c> 曾只清内存镜像，本实例之外的读穿透 / 冷启动
/// 仍会从持久层恢复被 IdP 拒绝的访问令牌，使失效被静默撤销。
/// 修复引入 <see cref="IUserTokenStoreAccessTokenInvalidation"/>，本守卫锁定三条结构约束，
/// 防止后续重构把探测移回装饰器（内层未实现时假命中）或把持久化删除误扩为双键删除。
/// </remarks>
public class UserTokenInvalidationPersistenceContractGuards
{
    [Fact]
    public void UserTokenManager_ShouldProbeCapability_OnRawStore()
    {
        var content = ReadProductionSource("UserTokenManager.cs");

        content.Should().Contain("userTokenStore as IUserTokenStoreAccessTokenInvalidation",
            "能力探测必须落在未装饰的原始 store 上（与 CAS 同口径）");
        content.Should().Contain("_accessTokenInvalidationStore",
            "管理器必须持有原始 store 的失效能力");
        content.Should().Contain(".InvalidateAccessTokenAsync(userId, _tokenTypeKey, cancellationToken)",
            "失效必须真正触达持久层的「仅访问令牌」删除");
        content.Should().Contain("catch (Exception ex) when (ex is not OperationCanceledException)",
            "持久层失效失败必须降级为 Warning，不得改变方法的非抛出契约");
    }

    [Fact]
    public void PurgeGateDecorators_ShouldNotDeclareAccessTokenInvalidationCapability()
    {
        typeof(PurgeGateUserTokenStoreDecorator)
            .GetInterfaces()
            .Should().NotContain(typeof(IUserTokenStoreAccessTokenInvalidation),
                "装饰器无条件声明能力会让内层未实现时探测假命中（与 IUserTokenStoreCompareExchange 同因）");
        typeof(PurgeGateTokenStoreDecorator)
            .GetInterfaces()
            .Should().NotContain(typeof(IUserTokenStoreAccessTokenInvalidation));
    }

    [Fact]
    public void InRepoUserStores_ShouldImplementAccessTokenInvalidationCapability()
    {
        typeof(FeishuUserTokenStore)
            .GetInterfaces()
            .Should().Contain(typeof(IUserTokenStoreAccessTokenInvalidation),
                "内存后端必须支持仅失效访问令牌，否则默认部署仍会复活被拒绝的令牌");
        typeof(EncryptedUserTokenStore)
            .GetInterfaces()
            .Should().Contain(typeof(IUserTokenStoreAccessTokenInvalidation),
                "加密装饰器必须声明该能力并按内层能力透传，否则加密部署的持久化失效静默失效");
    }

    [Fact]
    public void EncryptedUserTokenStore_ShouldPassThroughOnlyWhenInnerSupportsIt()
    {
        var content = ReadProductionSource("EncryptedTokenStore.cs");

        content.Should().Contain("_inner is IUserTokenStoreAccessTokenInvalidation invalidatable",
            "加密装饰器必须按 optional-capability 透传，内层未实现时跳过而不是回落到双键删除");
    }

    private static string ReadProductionSource(string fileName)
    {
        var solutionRoot = GetSolutionRoot();
        solutionRoot.Should().NotBeNull("找不到解决方案根目录时本守卫无法工作（宁可失败，也不要假绿）");

        var path = Directory
            .GetFiles(solutionRoot!, fileName, SearchOption.AllDirectories)
            .FirstOrDefault(p => !IsExcluded(p));

        path.Should().NotBeNull($"{fileName} 应存在于生产源码中");
        return File.ReadAllText(path!);
    }

    private static bool IsExcluded(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}Tests{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}Demos{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    private static string? GetSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir) && !File.Exists(Path.Combine(dir, "Mud.Feishu.slnx")))
        {
            dir = Path.GetDirectoryName(dir);
        }
        return dir;
    }
}
