// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  本项目基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Testcontainers.Redis;

namespace Mud.Feishu.Redis.IntegrationTests;

/// <summary>
/// Redis Testcontainers fixture——提供真实 Redis 7 实例，确保 Lua 脚本/TTL/SET NX 语义可验证。
/// <para>环境门控：环境变量 <c>MUDFEISHU_REDIS_TESTS=1</c> 时启动容器；否则跳过（CI 无 Docker 时不阻塞）。</para>
/// <para>ADR-8：所有命令语义类结论只认真实 Redis，Moq 桩无法替代。</para>
/// </summary>
public class RedisFixture : IAsyncLifetime
{
    private RedisContainer? _container;
    private IConnectionMultiplexer? _redis;

    /// <summary>
    /// 是否应执行集成测试（环境门控）
    /// </summary>
    public static bool ShouldRun =>
        Environment.GetEnvironmentVariable("MUDFEISHU_REDIS_TESTS") == "1";

    /// <summary>
    /// Redis 连接多路复用器
    /// </summary>
    public IConnectionMultiplexer Redis => _redis ?? throw new InvalidOperationException("Redis fixture not initialized");

    /// <summary>
    /// 获取数据库实例
    /// </summary>
    public IDatabase Database => Redis.GetDatabase();

    public async Task InitializeAsync()
    {
        if (!ShouldRun)
            return;

        _container = new RedisBuilder()
            .WithImage("redis:7")
            .WithPortBinding(0, 6379)
            .Build();

        await _container.StartAsync();

        var config = new ConfigurationOptions
        {
            EndPoints = { _container.GetConnectionString() },
            ConnectTimeout = 5000,
            SyncTimeout = 5000,
            AbortOnConnectFail = false
        };

        _redis = await ConnectionMultiplexer.ConnectAsync(config);

        // 确保数据库为空
        await _redis.GetDatabase().ExecuteAsync("FLUSHDB");
    }

    public async Task DisposeAsync()
    {
        if (_redis != null)
            await _redis.CloseAsync();

        if (_container != null)
            await _container.DisposeAsync();
    }

    /// <summary>
    /// 清空当前数据库（测试间隔离）
    /// </summary>
    public async Task FlushDbAsync()
    {
        if (_redis != null)
            await _redis.GetDatabase().ExecuteAsync("FLUSHDB");
    }
}

/// <summary>
/// 基于环境门控的集成分测试基类——无 Docker 时跳过全部用例
/// </summary>
[Collection("Redis")]
public abstract class RedisIntegrationTestBase : IClassFixture<RedisFixture>, IAsyncLifetime
{
    protected readonly RedisFixture Fixture;

    protected RedisIntegrationTestBase(RedisFixture fixture)
    {
        Fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        if (!RedisFixture.ShouldRun)
            return;

        await Fixture.FlushDbAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// 如果环境门控未开启，跳过测试（返回 true 表示应跳过）
    /// </summary>
    protected bool ShouldSkip()
    {
        if (!RedisFixture.ShouldRun)
        {
            return true;
        }
        return false;
    }
}

[CollectionDefinition("Redis")]
public class RedisCollection : ICollectionFixture<RedisFixture> { }
