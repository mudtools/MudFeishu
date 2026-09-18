// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书多应用管理器的行为选项。
/// </summary>
public class FeishuAppOptions
{
    /// <summary>
    /// 配置节名称。
    /// </summary>
    public const string SectionName = "FeishuAppOptions";

    /// <summary>
    /// 是否启用应用配置热更新（ARC-1）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 启用后 <c>FeishuAppManager</c> 订阅 <c>IOptionsMonitor&lt;List&lt;FeishuAppConfig&gt;&gt;.OnChange</c>，
    /// 在 <c>appsettings.json</c> 变更时按 AppKey 计算差异并增量应用：
    /// 新增应用注册、更新应用重建上下文、删除应用移除、默认应用切换同步。
    /// 变更的应用会丢弃旧 <c>FeishuAppContext</c> 与其客户端实例（旧上下文延迟回收，
    /// 避免在途请求抛 <c>ObjectDisposedException</c>），令牌经由含 appKey 维度的存储键天然热迁移。
    /// </para>
    /// <para>
    /// <b>默认 <c>true</c></b>：热更新是正确性收益（避免误以为改了配置却仍走旧配置），
    /// 且本项目尚无需要保持的存量行为。如需回滚，显式置为 <c>false</c> 即可恢复「配置变更需重启」的旧语义。
    /// </para>
    /// <para>
    /// <b>ARC-7（已支持 BaseUrl / TimeOut 热更新）</b>：命名客户端的注册委托捕获的是注册期快照，
    /// 因此 <c>AddFeishuAppBaseServices</c> 额外通过 <c>IHttpClientBuilder.ConfigureHttpClient(IServiceProvider, HttpClient)</c>
    /// 追加了一个 DI 感知的配置动作，在每次 <c>CreateClient</c> 时从
    /// <c>IOptionsMonitor&lt;List&lt;FeishuAppConfig&gt;&gt;</c> 读取<b>当前</b> <c>BaseUrl</c> / <c>TimeOut</c>，
    /// 检测到差异时覆盖新客户端的 <c>BaseAddress</c> / <c>Timeout</c>。
    /// 配合本开关触发的「应用上下文重建 → 新建 <c>TokenRecoveryEnhancedClient</c> → 重新 <c>CreateClient</c>」链路，
    /// 多区域切换（feishu.cn ↔ larksuite.com）与超时调整无需重启进程。
    /// 在途请求仍持有旧客户端实例（其 <c>HttpClient</c> 已固化旧基地址），自然完成收敛。
    /// </para>
    /// <para>
    /// <b>生效范围</b>：仅当应用使用 <see cref="FeishuAppOptions.EnableConfigReload"/> 且配置经
    /// <c>IConfiguration</c> 绑定（<c>AddFeishuApp(configuration, sectionName)</c>）时才会有变更通知；
    /// 纯代码配置（<c>AddFeishuApp(List&lt;FeishuAppConfig&gt;)</c>）没有变更源，配置只在启动期生效。
    /// </para>
    /// </remarks>
    public bool EnableConfigReload { get; set; } = true;

    /// <summary>
    /// 是否启用令牌存储加密（ENH-1）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 启用后 <c>IFeishuTokenStoreFactory</c> 产出的租户/用户令牌存储会被
    /// <c>EncryptedTokenStore</c> / <c>EncryptedUserTokenStore</c> 装饰，写入前加密、读取后解密，
    /// 令牌不再以明文落在存储后端。加密能力依赖已注册的 <c>IEncryptionProvider</c>
    /// （通过 <c>AddMudHttpAesEncryption()</c> 或自定义实现注册）；未注册时降级为明文并记录警告。
    /// </para>
    /// <para>
    /// <b>默认 <c>false</c></b>：加密属于存储策略选择，应由使用方显式决策。
    /// </para>
    /// <para>
    /// <b>启用后的注意点</b>：
    /// <list type="bullet">
    /// <item>键布局不变，不影响多应用隔离语义；</item>
    /// <item>已存在的明文令牌无法自动迁移——解密失败会按「缓存未命中」处理并重新获取令牌（预期行为）；</item>
    /// <item>加密密钥丢失会使全部已存令牌不可恢复，需要重新获取；</item>
    /// <item>进程内 MemoryCache 场景下 refresh token 本就随进程重启丢失，加密收益主要体现在
    /// Redis 等持久化、跨进程共享的后端上。</item>
    /// </list>
    /// </para>
    /// </remarks>
    public bool EnableTokenEncryption { get; set; }

    /// <summary>
    /// 应用上下文退休宽限期（秒）（TMA-07 / P1-6 修复，D5 契约）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 当应用配置热更新（<c>ApplyConfigurationChanges</c>，TMF-04：原 <c>RebuildAppContext</c>
    /// 已并入热更新路径）或 <c>RemoveApp</c> 触发旧上下文退役时，
    /// 旧的 <c>FeishuAppContext</c> 不再被引用，但其内部的 <c>Timer</c>（令牌定时刷新）
    /// 会 root 整个对象图，GC 不会自动回收。旧上下文进入退休队列，
    /// 在此宽限期后显式 <c>Dispose</c>，停止其 Timer 并释放资源。
    /// </para>
    /// <para>
    /// 宽限期的目的是允许在途请求安全完成。在途请求持有旧上下文的 <c>HttpClient</c> 引用，
    /// 立即 <c>Dispose</c> 会导致在途请求抛 <c>ObjectDisposedException</c>。
    /// </para>
    /// <para>
    /// <b>默认 300 秒</b>（5 分钟），有效范围 1–3600 秒。
    /// </para>
    /// </remarks>
    public int ContextRetireDelaySeconds { get; set; } = 300;

    /// <summary>
    /// 是否在启动时预热全部应用（TMA-08 / P1-7 修复，D6 契约）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 启用后 <c>FeishuTokenRegistrationService</c> 会在启动时逐应用预热并注册到后台刷新服务，
    /// 单应用失败不阻断宿主启动（仅 <c>LogError</c>）。
    /// 禁用时仅注册默认应用，其余应用在首次访问时增量注册。
    /// </para>
    /// <para>
    /// <b>默认 <c>false</c></b>：与懒加载策略一致。
    /// </para>
    /// </remarks>
    public bool WarmUpAllAppsOnStartup { get; set; }

    /// <summary>
    /// 是否启用 per-app 认证客户端（TMA-09 / P1-8 修复，D7 契约）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 启用后认证/取令牌请求会使用本应用的命名 HttpClient（per-app 端点），
    /// 而非默认应用的端点。多区域/多 BaseUrl 部署必须开启。
    /// </para>
    /// <para>
    /// 若 AOT 门禁不允许该实现，置为 <c>false</c> 会降级为默认应用端点并 <c>LogWarning</c>。
    /// </para>
    /// <para>
    /// <b>默认 <c>true</c></b>。
    /// </para>
    /// </remarks>
    public bool EnablePerAppAuthenticationClient { get; set; } = true;

    /// <summary>
    /// 是否在热更新时移除运行时添加的应用（TMA-11 / P2-6 修复）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 启用后，通过 <c>AddApp</c> 运行时添加的应用若不在配置文件中，会被热更新移除。
    /// 禁用时保持旧行为：运行时应用不被热更新删除。
    /// </para>
    /// <para>
    /// <b>默认 <c>false</c></b>：运行时应用由调用方管理生命周期，热更新不应越权删除。
    /// </para>
    /// </remarks>
    public bool RemoveRuntimeAddedAppsOnReload { get; set; }
}
