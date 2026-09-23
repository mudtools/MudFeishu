// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Mud.Feishu.Abstractions.Authentication;

namespace Mud.Feishu.Abstractions.Tests.Authentication.TokenManager;

/// <summary>
/// P2-4：FeishuTokenStore.SharedTenantTypes 跨实例共享记账测试。
/// 验证相同 appKey 的多个 FeishuTokenStore 实例共享静态 SharedTenantTypes 注册表
/// （TMF-01 / D10 凭据变更清库契约）。
/// </summary>
public class FeishuTokenStoreSharedTenantTypesTests
{
    private static IMemoryCache CreateCache() => new MemoryCache(new MemoryCacheOptions());

    [Fact]
    public async Task GetTokenTypesAsync_ShouldSeeTokenTypesWrittenByAnotherInstance_WhenSameAppKey()
    {
        // Arrange：两个独立 cache + 同一 appKey 的实例
        var appKey = $"shared-{Guid.NewGuid():N}";
        var storeA = new FeishuTokenStore(CreateCache(), appKey);
        var storeB = new FeishuTokenStore(CreateCache(), appKey);
        const string tokenType = "tenant:cli_a";

        // Act：实例 A 写入令牌
        await storeA.SetAccessTokenAsync(tokenType, "token-from-a", 7200);

        // Assert：实例 B 能看到 A 注册的 tokenType（跨实例共享记账）
        var tokenTypes = await storeB.GetTokenTypesAsync();
        tokenTypes.Should().Contain(tokenType);

        // Cleanup
        await storeB.ClearAsync();
    }

    [Fact]
    public async Task GetTokenTypesAsync_ShouldNotSeeTokenTypes_WhenDifferentAppKey()
    {
        // Arrange：不同 appKey 的两个实例
        var appKey1 = $"isolated-1-{Guid.NewGuid():N}";
        var appKey2 = $"isolated-2-{Guid.NewGuid():N}";
        var storeA = new FeishuTokenStore(CreateCache(), appKey1);
        var storeB = new FeishuTokenStore(CreateCache(), appKey2);
        const string tokenType = "tenant:cli_a";

        // Act
        await storeA.SetAccessTokenAsync(tokenType, "token-from-a", 7200);

        // Assert：实例 B 看不到 A 的 tokenType（按 KeyPrefix 隔离）
        var tokenTypes = await storeB.GetTokenTypesAsync();
        tokenTypes.Should().NotContain(tokenType);

        // Cleanup
        await storeA.ClearAsync();
    }

    [Fact]
    public async Task ClearAsync_ShouldClearSharedTokenTypes_WhenCalledFromAnotherInstanceWithSameAppKey()
    {
        // Arrange
        var appKey = $"clear-{Guid.NewGuid():N}";
        var storeA = new FeishuTokenStore(CreateCache(), appKey);
        var storeB = new FeishuTokenStore(CreateCache(), appKey);
        const string tokenType = "tenant:cli_a";

        await storeA.SetAccessTokenAsync(tokenType, "token-from-a", 7200);

        // 确认共享记账已注册
        var beforeClear = await storeB.GetTokenTypesAsync();
        beforeClear.Should().Contain(tokenType);

        // Act：实例 B 清库
        await storeB.ClearAsync();

        // Assert：实例 A 的 GetTokenTypesAsync 返回空（共享记账被清空）
        var afterClear = await storeA.GetTokenTypesAsync();
        afterClear.Should().NotContain(tokenType);
        afterClear.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearAsync_ShouldNotAffectOtherAppKey_WhenClearedFromOneAppKey()
    {
        // Arrange
        var appKey1 = $"scope-1-{Guid.NewGuid():N}";
        var appKey2 = $"scope-2-{Guid.NewGuid():N}";
        var store1 = new FeishuTokenStore(CreateCache(), appKey1);
        var store2 = new FeishuTokenStore(CreateCache(), appKey2);

        await store1.SetAccessTokenAsync("tenant:cli_1", "token-1", 7200);
        await store2.SetAccessTokenAsync("tenant:cli_2", "token-2", 7200);

        // Act：清库 appKey1
        await store1.ClearAsync();

        // Assert：appKey2 的记账不受影响
        var remaining = await store2.GetTokenTypesAsync();
        remaining.Should().Contain("tenant:cli_2");
        remaining.Should().NotContain("tenant:cli_1");

        // Cleanup
        await store2.ClearAsync();
    }

    [Fact]
    public async Task SetRefreshTokenAsync_ShouldAlsoRegisterInSharedTenantTypes()
    {
        // Arrange
        var appKey = $"refresh-{Guid.NewGuid():N}";
        var storeA = new FeishuTokenStore(CreateCache(), appKey);
        var storeB = new FeishuTokenStore(CreateCache(), appKey);
        const string tokenType = "tenant:cli_refresh";

        // Act：实例 A 仅写 refresh token（不写 access）
        await storeA.SetRefreshTokenAsync(tokenType, "refresh-token-from-a");

        // Assert：refresh 写入也记账（TMA2-15 / P2-5：清库盲区修复）
        var tokenTypes = await storeB.GetTokenTypesAsync();
        tokenTypes.Should().Contain(tokenType);

        // Cleanup
        await storeB.ClearAsync();
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveFromSharedTenantTypes()
    {
        // Arrange
        var appKey = $"remove-{Guid.NewGuid():N}";
        var storeA = new FeishuTokenStore(CreateCache(), appKey);
        var storeB = new FeishuTokenStore(CreateCache(), appKey);
        const string tokenType = "tenant:cli_remove";

        await storeA.SetAccessTokenAsync(tokenType, "token", 7200);
        (await storeB.GetTokenTypesAsync()).Should().Contain(tokenType);

        // Act：实例 B 移除
        await storeB.RemoveAsync(tokenType);

        // Assert：共享记账中已移除
        var tokenTypes = await storeA.GetTokenTypesAsync();
        tokenTypes.Should().NotContain(tokenType);
    }
}
