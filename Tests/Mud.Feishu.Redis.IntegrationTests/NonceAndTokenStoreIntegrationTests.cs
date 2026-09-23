// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Redis.IntegrationTests;

/// <summary>
/// Nonce 去重集成测试——覆盖 R-12（TTL 校验）与 SET NX EX 原子性。
/// </summary>
[Collection("Redis")]
public class NonceDeduplicatorIntegrationTests : RedisIntegrationTestBase
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    private readonly RedisFeishuNonceDistributedDeduplicator _sut;

    /// <summary>
    /// 初始化测试。
    /// </summary>
    /// <param name="fixture">共享 Redis 夹具。</param>
    public NonceDeduplicatorIntegrationTests(RedisFixture fixture) : base(fixture)
    {
        _sut = new RedisFeishuNonceDistributedDeduplicator(
            Fixture.Redis,
            NullLogger<RedisFeishuNonceDistributedDeduplicator>.Instance,
            DefaultTtl,
            "feishu:nonce:");
    }

    [RedisFact]
    public async Task Nonce_SetNX_Should_Return_True_Once_And_False_After()
    {
        // Arrange
        var nonce = Guid.NewGuid().ToString("N");

        // Act
        var first = await _sut.TryMarkAsUsedAsync(nonce);
        var second = await _sut.TryMarkAsUsedAsync(nonce);

        // Assert
        first.Should().BeFalse("首次标记返回未使用（false）");
        second.Should().BeTrue("二次标记返回已使用（true，重放攻击）");
    }

    [RedisFact]
    public async Task Nonce_With_Zero_Ttl_Should_Throw_ArgumentOutOfRange()
    {
        // Arrange
        var nonce = Guid.NewGuid().ToString("N");

        // Act
        var act = () => _sut.TryMarkAsUsedAsync(nonce, ttl: TimeSpan.Zero);

        // Assert：R-12——非正 TTL 显式抛参，而非 SET EX 0 被服务端拒绝/静默失效
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithMessage("*Nonce TTL*");
    }

    [RedisFact]
    public async Task Nonce_Concurrent_Only_One_Should_Succeed()
    {
        // Arrange
        var nonce = Guid.NewGuid().ToString("N");

        // Act：20 个并发标记同一 nonce
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => _sut.TryMarkAsUsedAsync(nonce))
            .ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert：恰一个 false（成功标记），其余 true（已使用）
        results.Count(r => r == false).Should().Be(1);
        results.Count(r => r == true).Should().Be(19);
    }
}

/// <summary>
/// 令牌存储集成测试——覆盖 R-09/R-10/R-23 与 R2-08（异步 SCAN + 批删）。
/// </summary>
[Collection("Redis")]
public class TokenStoreIntegrationTests : RedisIntegrationTestBase
{
    /// <summary>
    /// 初始化测试。
    /// </summary>
    /// <param name="fixture">共享 Redis 夹具。</param>
    public TokenStoreIntegrationTests(RedisFixture fixture) : base(fixture)
    {
    }

    [RedisFact]
    public async Task TokenStore_SetAccessToken_With_NonPositive_Expiry_Should_Throw()
    {
        // Arrange
        var store = new RedisTokenStore(
            Fixture.Redis, NullLogger<RedisTokenStore>.Instance, "feishu:test:token");

        // Act
        var act = () => store.SetAccessTokenAsync("app_token", "token_value", 0);

        // Assert：R-23——非法过期时间显式抛参（与内存实现 FeishuTokenStore 一致）
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>().WithMessage("*令牌过期时间*");

        var key = "feishu:test:token:app_token:access";
        (await Fixture.Database.KeyExistsAsync(key)).Should().BeFalse();
    }

    [RedisFact]
    public async Task TokenStore_Clear_Should_Delete_Tenant_And_User_Keys()
    {
        // Arrange：预置租户令牌与用户令牌键
        var prefix = "feishu:clear:token";
        var store = new RedisTokenStore(Fixture.Redis, NullLogger<RedisTokenStore>.Instance, prefix);

        await Fixture.Database.StringSetAsync($"{prefix}:app_token:access", "1");
        await Fixture.Database.StringSetAsync($"{prefix}:app_token:refresh", "1");
        await Fixture.Database.StringSetAsync($"{prefix}:user:user1:app_token:access", "1");

        // Act
        await store.ClearAsync();

        // Assert：ClearAsync 作用域 = 该前缀下全部租户 + 用户令牌键（文档化契约）
        (await Fixture.Database.KeyExistsAsync($"{prefix}:app_token:access")).Should().BeFalse();
        (await Fixture.Database.KeyExistsAsync($"{prefix}:app_token:refresh")).Should().BeFalse();
        (await Fixture.Database.KeyExistsAsync($"{prefix}:user:user1:app_token:access")).Should().BeFalse();
    }

    [RedisFact]
    public async Task TokenStore_GetTokenTypes_With_Colon_TokenType_Should_RoundTrip()
    {
        // Arrange
        var prefix = "feishu:rt:token";
        var store = new RedisTokenStore(Fixture.Redis, NullLogger<RedisTokenStore>.Instance, prefix);

        // tokenType 含冒号（TM-04 回归）
        await store.SetAccessTokenAsync("tenant:cli_xxx", "token_value", 3600);

        // Act（真实 SCAN，R2-08 已异步化）
        var types = await store.GetTokenTypesAsync();

        // Assert
        types.Should().Contain("tenant:cli_xxx");
    }

    [RedisFact]
    public void PerAppFactory_Create_With_Null_AppKey_Should_Throw()
    {
        var factory = new PerAppRedisTokenStoreFactory(Fixture.Redis);

        var act = () => factory.Create(null!);

        act.Should().Throw<ArgumentException>().WithMessage("*appKey*");
    }
}
