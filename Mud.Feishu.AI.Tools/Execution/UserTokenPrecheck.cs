// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Metrics;

namespace Mud.Feishu.AI.Tools.Execution;

/// <summary>
/// 用户令牌就绪探测器（T1 / D1 核心）。
/// </summary>
/// <remarks>
/// <para>
/// <b>零令牌持有</b>：只读令牌状态、不缓存结果——授权/登出随时改变状态，缓存会导致 stale 判定。
/// </para>
/// <para>
/// <b>三态判定</b>（纯委托 <see cref="IFeishuTokenManagerResolver"/>）：
/// <c>HasValidTokenAsync</c> ⇒ <see cref="UserTokenPrecheckResult.Ready"/>；
/// <c>CanRefreshTokenAsync</c> ⇒ <see cref="UserTokenPrecheckResult.Refreshable"/>；
/// 否则 <see cref="UserTokenPrecheckResult.AuthRequired"/>。
/// </para>
/// <para>
/// <b>模式开关</b>（<see cref="FeishuAgentOptions.UserTokenPrecheck"/>）：
/// <list type="bullet">
/// <item><see cref="UserTokenPrecheckModes.Off"/>：跳过探测（改造前行为）。</item>
/// <item><see cref="UserTokenPrecheckModes.CacheOnly"/>（默认）：只调 <c>HasValidTokenAsync</c>，内存镜像命中即零 IO。</item>
/// <item><see cref="UserTokenPrecheckModes.Full"/>：先 <c>HasValidTokenAsync</c>，未命中再 <c>CanRefreshTokenAsync</c>。</item>
/// </list>
/// </para>
/// <para>
/// <b>探测异常 fail-open（放行）+ Warning + OTel 标记</b>：
/// Redis 抖动不得被放大为「全员 AuthRequired」（R8 DP-T2-1 论证）；
/// fail-open 后由下游 401 + <c>TokenRecoveryExecutor</c> 兜底。
/// </para>
/// <para>
/// <b>触发约束</b>：仅 <c>identity == user</c>；<c>dry_run=true</c> 跳过；tenant 路径零触碰。
/// </para>
/// </remarks>
internal sealed class UserTokenPrecheck
{
    private readonly IFeishuTokenManagerResolver? _resolver;
    private readonly FeishuAgentOptions _options;
    private readonly ILogger? _logger;

    /// <summary>
    /// 初始化 <see cref="UserTokenPrecheck"/>。
    /// </summary>
    /// <param name="resolver">
    /// 令牌管理器解析器（可空——未注册时探测降级为 fail-open 放行）。
    /// <b>注意</b>：MS DI 不把可空引用注解当作"可缺席"，本类型的"未注册即 fail-open"
    /// 由 <c>AddFeishuToolInfrastructure</c> 的**工厂注册**兜住（显式传 <c>null</c>）——
    /// 若改成 <c>TryAddSingleton&lt;UserTokenPrecheck&gt;()</c>，未注册 resolver 的宿主
    /// 会在构建注册表时抛激活期异常，fail-open 语义随之消失。
    /// </param>
    /// <param name="options">Agent 配置（<see cref="FeishuAgentOptions.UserTokenPrecheck"/> 消费点）。</param>
    /// <param name="logger">日志（可空）。</param>
    public UserTokenPrecheck(
        IFeishuTokenManagerResolver? resolver,
        IOptions<FeishuAgentOptions> options,
        ILogger? logger = null)
    {
        _resolver = resolver;
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    /// <summary>
    /// 探测当前用户的令牌就绪状态。
    /// </summary>
    /// <param name="appKey">应用键（令牌管理器查找键）。</param>
    /// <param name="userId">用户标识（open_id——令牌缓存查找键，由 <c>IFeishuCurrentUserContext.SetUser</c> 写入）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>探测结果（四态闭集）。</returns>
    /// <remarks>
    /// <b>不缓存探测结果</b>——授权/登出随时改变状态。
    /// </remarks>
    public async Task<UserTokenPrecheckResult> ProbeAsync(
        string appKey,
        string userId,
        CancellationToken cancellationToken = default)
    {
        // Off 模式：跳过探测（改造前行为，A15 零差异）。
        if (_options.UserTokenPrecheck is UserTokenPrecheckModes.Off)
        {
            return UserTokenPrecheckResult.Skipped;
        }

        // Resolver 缺席：fail-open（装配期应有诊断兜底，运行期降级为放行）。
        if (_resolver is null)
        {
            _logger?.LogWarning(
                "用户令牌探测失败：IFeishuTokenManagerResolver 未注册——fail-open 放行（appKey: {AppKey}）", appKey);
            FeishuToolDiagnostics.RecordDegraded(appKey, FeishuMetrics.DegradedReasons.UserTokenPrecheckError);
            return UserTokenPrecheckResult.FromError;
        }

        // TryGet 三态版本（不吞异常）：null = 应用不存在/初始化失败/已退休。
        var userTokenManager = _resolver.TryGetUserTokenManager(appKey);
        if (userTokenManager is null)
        {
            _logger?.LogWarning(
                "用户令牌探测失败：无法解析 appKey '{AppKey}' 的用户令牌管理器——fail-open 放行", appKey);
            FeishuToolDiagnostics.RecordDegraded(appKey, FeishuMetrics.DegradedReasons.UserTokenPrecheckError);
            return UserTokenPrecheckResult.FromError;
        }

        // 第一态：HasValidTokenAsync（CacheOnly / Full 都调）
        try
        {
            var hasValid = await userTokenManager.HasValidTokenAsync(userId, cancellationToken)
                .ConfigureAwait(false);
            if (hasValid)
            {
                return UserTokenPrecheckResult.Ready;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex,
                "用户令牌探测异常：HasValidTokenAsync 失败——fail-open 放行（appKey: {AppKey}）", appKey);
            FeishuToolDiagnostics.RecordDegraded(appKey, FeishuMetrics.DegradedReasons.UserTokenPrecheckError);
            return UserTokenPrecheckResult.FromError;
        }

        // CacheOnly 模式：不调 CanRefreshTokenAsync，直接放行（由下游 401 + TokenRecoveryExecutor 兜底）
        if (_options.UserTokenPrecheck is UserTokenPrecheckModes.CacheOnly)
        {
            // CacheOnly 未命中 ⇒ 不判 AuthRequired，放行（避免 store 读穿透）
            return UserTokenPrecheckResult.Refreshable;
        }

        // Full 模式：第二态 CanRefreshTokenAsync
        try
        {
            var canRefresh = await userTokenManager.CanRefreshTokenAsync(userId, cancellationToken)
                .ConfigureAwait(false);
            return canRefresh
                ? UserTokenPrecheckResult.Refreshable
                : UserTokenPrecheckResult.AuthRequired;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex,
                "用户令牌探测异常：CanRefreshTokenAsync 失败——fail-open 放行（appKey: {AppKey}）", appKey);
            FeishuToolDiagnostics.RecordDegraded(appKey, FeishuMetrics.DegradedReasons.UserTokenPrecheckError);
            return UserTokenPrecheckResult.FromError;
        }
    }
}
