# 响应缓存使用指南

Mud.Feishu **默认不启用任何响应缓存**（不注册任何缓存拦截器，行为与不使用缓存完全一致）。
缓存是业务决策（TTL、失效语义、是否允许脏读），因此 SDK 只提供接入路径与约束说明，不替使用方决策。

## 1. 能力来源

| 组件 | 作用 |
| --- | --- |
| `[Cache]` 特性（`Mud.HttpUtils.Attributes`） | 声明式标注接口方法启用缓存 |
| `IHttpResponseCache` | 缓存后端抽象（默认实现 `MemoryHttpResponseCache`） |
| `CacheOptions`（生成器产出） | 由 `[Cache]` 参数编译期生成，交给运行时执行器编排 |

`AddMudHttpClient` 内部已通过 `TryAddSingleton<IHttpResponseCache>` 注册进程内
`MemoryHttpResponseCache`，因此**只要给方法加上 `[Cache]` 就会生效**；
如需替换为 Redis 等分布式后端，在 `AddFeishuApp` **之前**注册自定义 `IHttpResponseCache` 抢占即可。

## 2. 基本用法

```csharp
[HttpClientApi("https://open.feishu.cn")]
public interface IMyDirectoryApi
{
    // 3 分钟绝对过期
    [Get("/open-apis/contact/v3/users/{user_id}")]
    [Cache(180)]
    Task<FeishuApiResult<UserData>?> GetUserAsync([Path] string user_id, CancellationToken ct = default);

    // 自定义键模板 + 滑动过期
    [Get("/open-apis/contact/v3/departments/{department_id}")]
    [Cache(600, CacheKeyTemplate = "dept_{department_id}", UseSlidingExpiration = true)]
    Task<FeishuApiResult<DepartmentData>?> GetDepartmentAsync([Path] string department_id, CancellationToken ct = default);
}
```

`[Cache]` 支持 `DurationSeconds`（默认 300）、`CacheKeyTemplate`、`VaryByUser`、`UseSlidingExpiration`。

不适用的场景（生成器会给出编译期诊断，避免"配置静默失效"）：

- 非幂等的 `[Post]` / `[Patch]` / `[Delete]`；
- `byte[]` / `Stream` / `HttpResponseMessage` 等直达返回类型（绕过执行器编排）。

## 3. 多应用场景的强制约束（重要）

> **生成器产出的缓存键不包含 AppKey。** 默认键形如 `"{方法名}|{参数...}"`，
> 仅在 `VaryByUser = true` 时增加 `user:{当前用户}|` 前缀。
> 而 `IHttpResponseCache` 是**进程级单例**，被所有飞书应用共享。

因此，在多应用（`AddFeishuApp` 配置了多个 `FeishuAppConfig`）场景下直接使用默认缓存后端，
会出现 **A 应用命中 B 应用的缓存**（跨应用数据串号，等同越权）。

**必须**使用按应用隔离键空间的缓存后端：

```csharp
using Mud.HttpUtils;

/// <summary>按当前应用（AppKey）隔离键空间的缓存装饰器。</summary>
public sealed class AppScopedResponseCache : IHttpResponseCache
{
    private readonly IHttpResponseCache _inner;
    private readonly IAppContextHolder _appContextHolder;

    public AppScopedResponseCache(IHttpResponseCache inner, IAppContextHolder appContextHolder)
    {
        _inner = inner;
        _appContextHolder = appContextHolder;
    }

    private string Scope(string key) => $"{_appContextHolder.Current?.AppKey ?? "default"}:{key}";

    public bool TryGet<T>(string key, out T? value) => _inner.TryGet(Scope(key), out value);

    public void Set<T>(string key, T? value, TimeSpan absoluteExpirationRelativeToNow)
        => _inner.Set(Scope(key), value, absoluteExpirationRelativeToNow);

    public void Set<T>(string key, T? value, TimeSpan expirationRelativeToNow, bool useSlidingExpiration)
        => _inner.Set(Scope(key), value, expirationRelativeToNow, useSlidingExpiration);

    public void Remove(string key) => _inner.Remove(Scope(key));

    public Task<T?> GetOrFetchAsync<T>(string key, Func<Task<T>> fetchFunc, TimeSpan expiration, CancellationToken cancellationToken = default)
        => _inner.GetOrFetchAsync(Scope(key), fetchFunc, expiration, cancellationToken);

    public Task<T?> GetOrFetchAsync<T>(string key, Func<Task<T>> fetchFunc, TimeSpan expiration, bool useSlidingExpiration, CancellationToken cancellationToken = default)
        => _inner.GetOrFetchAsync(Scope(key), fetchFunc, expiration, useSlidingExpiration, cancellationToken);

    public Task RemoveAsync(string key) => _inner.RemoveAsync(Scope(key));

    public Task ClearAsync() => _inner.ClearAsync();
}
```

注册（**必须早于** `AddFeishuApp`：`AddMudHttpClient` 内部用 `TryAddSingleton<IHttpResponseCache>`
注册默认后端，"先注册者胜出"）：

```csharp
using Microsoft.Extensions.Options;
using Mud.HttpUtils;

// 在 AddFeishuApp 之前注册：把默认的内存后端包上应用隔离装饰器
services.AddSingleton<IHttpResponseCache>(sp =>
{
    var appOptions = sp.GetService<IOptions<MudHttpClientApplicationOptions>>()?.Value;
    var cacheOptions = appOptions?.ResponseCache;

    // 与 AddMudHttpClient 内部的默认注册保持一致的参数口径
    var inner = new MemoryHttpResponseCache(
        cacheOptions?.MaxCacheSize ?? ResponseCacheOptions.DefaultMaxCacheSize,
        cacheOptions?.CleanupIntervalSeconds ?? ResponseCacheOptions.DefaultCleanupIntervalSeconds);

    // IAppContextHolder 由 AddMudHttpClient → AddMudHttpAppContextHolder 注册（工厂为延迟执行，时序无忧）
    return new AppScopedResponseCache(inner, sp.GetRequiredService<IAppContextHolder>());
});

services.AddFeishuApp(configuration, "FeishuApps");
```

若希望跨进程共享（多实例部署），把 `inner` 换成 Redis 实现即可 ——
`Mud.Feishu.Redis` 提供的连接设施可直接复用；装饰器逻辑不变。

> 单应用部署（只配置一个 `FeishuAppConfig`）下不存在跨应用串号问题，可直接使用默认缓存后端。

## 4. 适合缓存的接口

| 适合 | 不适合 |
| --- | --- |
| 通讯录只读查询（用户/部门详情、列表） | 任何写操作（发送消息、创建文档） |
| Wiki / Bitable / Spreadsheet 元信息 | 令牌、鉴权类接口（`/auth/v3/...`） |
| 帮助台、考勤等枚举与配置类接口 | 用户维度且强实时的数据（消息、日程） |
| 低频变更 + 幂等 + 可作为「最终一致」的场景 | 需要强一致读取的场景 |

## 5. 失效与可观测

- 缓存未提供按接口粒度的失效 API；需要主动失效时，请通过自定义 `IHttpResponseCache`
  暴露的 `Remove` / `ClearAsync` 自行实现（键的构成见 `CacheOptions.KeyTemplate`）。
- 缓存命中/未命中不影响令牌获取与 401 恢复链路：缓存只作用于**响应**层。
- 缓存命中时不会产生 HTTP 请求，因此可显著降低飞书 QPS 配额消耗与 P99 延迟。
