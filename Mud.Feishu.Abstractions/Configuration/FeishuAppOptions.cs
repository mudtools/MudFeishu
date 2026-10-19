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
/// <remarks>
/// 本类选项经 <c>IOptions&lt;T&gt;</c> 注入时为<b>启动快照</b>（IOptions 缓存不随 <c>IConfiguration</c> 变更失效），
/// 修改配置源后不热更，需重启进程。<see cref="EnableConfigReload"/> 仅控制「是否订阅配置变更通知」，
/// 开关本身同样只读一次；<see cref="EnablePerAppAuthenticationClient"/> 在每次创建应用上下文时求值，
/// 其余属性在构造函数或首次解析时读取一次。各属性的具体读取时机见其自身 <c>remarks</c>。
/// </remarks>
public class FeishuAppOptions
{
    /// <summary>
    /// 配置节名称。
    /// </summary>
    public const string SectionName = "FeishuAppOptions";

    /// <summary>
    /// 是否启用应用配置热更新。
    /// </summary>
    /// <remarks>
    /// 启用后 <c>FeishuAppManager</c> 订阅 <c>IOptionsMonitor&lt;List&lt;FeishuAppConfig&gt;&gt;.OnChange</c>，按 AppKey 计算差异并增量应用
    /// （新增注册 / 更新重建上下文 / 删除移除 / 默认应用切换同步），旧上下文延迟回收以免在途请求抛 <c>ObjectDisposedException</c>。
    /// <para><b>生效范围</b>：仅当配置经 <c>IConfiguration</c> 绑定（<c>AddFeishuApp(configuration, sectionName)</c>）时才有变更
    /// 通知；纯代码配置（<c>AddFeishuApp(List&lt;FeishuAppConfig&gt;)</c>）无变更源，只在启动期生效。默认 <c>true</c>。</para>
    /// </remarks>
    public bool EnableConfigReload { get; set; } = true;

    /// <summary>
    /// 是否启用令牌存储加密。
    /// </summary>
    /// <remarks>
    /// 启用后 <c>IFeishuTokenStoreFactory</c> 产出的租户/用户令牌存储被 <c>EncryptedTokenStore</c> /
    /// <c>EncryptedUserTokenStore</c> 装饰（写前加密、读后解密），依赖已注册的 <c>IEncryptionProvider</c>，未注册时降级明文并告警。
    /// <para>默认 <c>false</c>。启用后：键布局不变；存量明文无法自动迁移（解密失败按未命中处理并尽力删脏键，属预期自愈）；
    /// 密钥丢失将使全部已存令牌不可恢复；收益主要体现在 Redis 等持久化、跨进程共享的后端。</para>
    /// </remarks>
    public bool EnableTokenEncryption { get; set; }

    /// <summary>
    /// 应用上下文退休宽限期（秒）。
    /// </summary>
    /// <remarks>
    /// 旧上下文退役后进入退休队列，在此宽限期后显式 <c>Dispose</c>（停止其令牌刷新 <c>Timer</c> 并释放资源）——
    /// 该 <c>Timer</c> 会 root 整个对象图，GC 不会自动回收。宽限期用于让持有旧上下文的在途请求安全完成，
    /// 立即 <c>Dispose</c> 会使其抛 <c>ObjectDisposedException</c>。
    /// <b>默认 300 秒</b>（5 分钟），有效范围 1–3600 秒。
    /// </remarks>
    public int ContextRetireDelaySeconds { get; set; } = 300;

    /// <summary>
    /// 是否在启动时预热全部应用。
    /// </summary>
    /// <remarks>
    /// 启用后 <c>FeishuTokenRegistrationService</c> 在启动时逐应用预热并注册到后台刷新服务，单应用失败不阻断宿主启动（仅 <c>LogError</c>）；
    /// 禁用时仅注册默认应用，其余应用在首次访问时增量注册。
    /// <b>默认 <c>false</c></b>：与懒加载策略一致。
    /// </remarks>
    public bool WarmUpAllAppsOnStartup { get; set; }

    /// <summary>
    /// 是否启用 per-app 认证客户端。
    /// </summary>
    /// <remarks>
    /// 启用后认证/取令牌请求使用本应用的命名 HttpClient（per-app 端点）而非默认应用端点，多区域/多 BaseUrl 部署必须开启。
    /// per-app 装配为<b>生成实现类型的编译期直引</b>，不经 <c>ActivatorUtilities</c> 反射构造，因此无 AOT 约束、也不会静默降级到默认应用端点。
    /// 置为 <c>false</c> 时认证请求走默认应用端点（会 <c>LogWarning</c>）。<b>默认 <c>true</c></b>。
    /// </remarks>
    public bool EnablePerAppAuthenticationClient { get; set; } = true;

    /// <summary>
    /// 是否在热更新时移除运行时添加的应用。
    /// </summary>
    /// <remarks>
    /// 启用后，经 <c>AddApp</c> 运行时添加且不在配置文件中的应用会被热更新移除；禁用时运行时应用不被热更新删除。
    /// <b>默认 <c>false</c></b>：运行时应用由调用方管理生命周期，热更新不应越权删除。
    /// </remarks>
    public bool RemoveRuntimeAddedAppsOnReload { get; set; }

    /// <summary>
    /// 默认应用上下文（<see cref="IFeishuAppContext"/>）的 DI 桥接是否跟随运行时默认应用切换。
    /// </summary>
    /// <remarks>
    /// <c>true</c>（默认）：桥接为<b>转发代理</b>——注入方在 <c>SetDefaultApp</c> / 配置热更新后立即使用新默认应用的 <c>Config</c>、令牌管理器与认证客户端。
    /// <c>false</c>：桥接委托返回并缓存 <c>GetDefaultApp()</c> 实例（「实例快照」语义）；仅当宿主把注入的 <see cref="IFeishuAppContext"/> 强转为具体 <see cref="FeishuAppContext"/> 时才需要——此时开启代理会抛 <c>InvalidCastException</c>。
    /// 本开关在桥接工厂首次解析时读取，属<b>启动快照</b>（运行期修改不生效，需重启进程）。
    /// </remarks>
    public bool ForwardDefaultAppContext { get; set; } = true;
}
