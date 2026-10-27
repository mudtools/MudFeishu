// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录下的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Moq;
using Mud.Feishu.Redis.Services;
using StackExchange.Redis;
using Xunit;

namespace Mud.Feishu.Redis.Tests;

/// <summary>
/// <see cref="RedisUserTokenStore"/> 的「仅失效访问令牌」能力测试。
/// </summary>
/// <remarks>
/// 语义约束：只删除 access 键；refresh 键必须保留（唯一的续期凭据）。
/// 若误走 <c>RemoveAsync</c> 的双键删除，用户令牌续期将被彻底切断。
/// </remarks>
public class RedisUserTokenStoreAccessTokenInvalidationTests
{
    [Fact]
    public async Task InvalidateAccessTokenAsync_ShouldDeleteAccessKeyOnly()
    {
        // Arrange
        var deletedKeys = new List<RedisKey>();
        var db = new Mock<IDatabase>();
        db.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Callback<RedisKey, CommandFlags>((key, _) => deletedKeys.Add(key))
            .ReturnsAsync(true);

        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);

        var inner = (RedisTokenStore)new PerAppRedisTokenStoreFactory(redis.Object).Create("cli_a").TokenStore;
        var store = new RedisUserTokenStore(inner, redis.Object, "feishu:token");

        // Act
        await store.InvalidateAccessTokenAsync("user1", "UserAccessToken:cli_a");

        // Assert
        deletedKeys.Should().ContainSingle("仅失效访问令牌，只允许一次单键删除");
        deletedKeys[0].ToString().Should().Contain("user1").And.Contain("access").And.NotContain("refresh",
            "refresh_token 是唯一续期凭据，不得被连带删除");

        db.Verify(
            d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()),
            Times.Never,
            "不得走双键删除重载（会连带删除 refresh_token）");
    }
}
