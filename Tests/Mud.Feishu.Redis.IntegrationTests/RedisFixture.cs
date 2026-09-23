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
            AbortOnConnectFail = false,
            // 必须开启管理命令：FLUSHDB 属管理命令，StackExchange.Redis 会在**客户端**拦截
            // （ConnectionMultiplexer.CheckMessage → RedisCommandException），AllowAdmin=false 时
            // 命令根本不会发往服务端，用例隔离（FlushDbAsync）与夹具初始化会 100% 失败
            // （CI ubuntu-latest 实测 Redis.IntegrationTests 25/25 全挂）。
            // 此处连接的是 Testcontainers 一次性 redis:7 容器（无持久数据、无共享实例），故显式开启；
            // 产品侧 RedisOptions.Advanced.AllowAdmin 的默认值仍为 false。
            AllowAdmin = true
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
/// 基于环境门控的集成测试基类——无 Docker 时全部用例在**发现期**被标记为 skipped
/// （见 <see cref="RedisFactAttribute"/>）。
/// </summary>
/// <remarks>
/// 只使用集合夹具（<see cref="ICollectionFixture{TFixture}"/>），**不再叠加**
/// <c>IClassFixture&lt;RedisFixture&gt;</c>：两者同时声明时 xUnit 会为每个测试类各建一个夹具实例，
/// 造成容器重复启动（一个类一个 Redis 容器）与资源浪费（T-R2-03）。
/// </remarks>
[Collection("Redis")]
public abstract class RedisIntegrationTestBase : IAsyncLifetime
{
    /// <summary>共享的 Redis 容器夹具（由集合夹具注入）。</summary>
    protected readonly RedisFixture Fixture;

    /// <summary>
    /// 初始化基类。
    /// </summary>
    /// <param name="fixture">集合夹具注入的 Redis 夹具。</param>
    protected RedisIntegrationTestBase(RedisFixture fixture)
    {
        Fixture = fixture;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (!RedisFixture.ShouldRun)
            return;

        // 测试间隔离：清空当前数据库
        await Fixture.FlushDbAsync();
    }

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;
}

/// <summary>
/// Redis 集合定义——全部测试类共享同一个容器夹具。
/// </summary>
[CollectionDefinition("Redis")]
public class RedisCollection : ICollectionFixture<RedisFixture> { }
