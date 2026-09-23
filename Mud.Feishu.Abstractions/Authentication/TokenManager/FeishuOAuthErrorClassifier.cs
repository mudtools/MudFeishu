// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// OAuth 刷新失败的可重试/不可重试分类器（D12 契约 / TMA2-06）。
/// </summary>
/// <remarks>
/// <para>
/// 所有"刷新失败"必须统一进入退避/负缓存，不得因"返回 null vs 抛异常"而分叉。
/// 失败分类显式化：
/// <list type="bullet">
/// <item><b>可重试</b>（网络超时、5xx、系统繁忙）→ 抛异常（保留可见性）；</item>
/// <item><b>不可重试</b>（<c>invalid_grant</c>、refresh token 失效/被吊销、scope 不符）→ 清理存储、返回 null（进入退避）。</item>
/// </list>
/// </para>
/// <para>
/// <b>分类结果驱动销毁性动作</b>（<c>UserTokenManager</c> 会删除 store 中的 refresh token，
/// 销毁用户唯一的续期路径，必须重新走 OAuth 授权）。因此判定顺序为
/// <b>显式错误码优先 → 收紧后的关键字兜底</b>：
/// <list type="number">
/// <item>命中 <see cref="UnretryableErrorCodes"/> ⇒ 不可重试；</item>
/// <item>命中 <see cref="RetryableErrorCodes"/> ⇒ 可重试（<b>不得</b>再被消息关键字推翻）；</item>
/// <item>错误码缺失 / 为 0 / 未列出 ⇒ 才用关键字兜底。</item>
/// </list>
/// </para>
/// <para>
/// TMR2-P1-4 修复：原实现先判错误码、再<b>无条件</b>用关键字兜底，且关键字含
/// <c>"refresh token"</c> / <c>"scope"</c> 这类在任何刷新相关消息中都可能出现的<b>过宽</b>词
/// （例如瞬时故障的"failed to refresh token, please retry later"）——
/// 会把可重试的瞬时故障误判为不可重试，进而清空用户的 refresh token。
/// 现移除这两个歧义词、加入显式可重试码优先级，并保留未知码的关键字兜底（避免漏判不可重试）。
/// </para>
/// <para>
/// 不可重试的错误码与关键字基于飞书 OAuth v2 规范与实际返回值。若飞书 API 新增错误码，
/// 需在此处同步更新。
/// </para>
/// </remarks>
internal static class FeishuOAuthErrorClassifier
{
    /// <summary>
    /// 不可重试的飞书错误码集合。
    /// </summary>
    private static readonly HashSet<int> UnretryableErrorCodes = new()
    {
        // invalid_grant：refresh token 无效或已过期
        40029,
        // access_token invalid（不应出现在 refresh 路径，但防御性处理）
        99991663,
        // refresh token 已被吊销或过期
        99991664,
        // scope 不符
        99991668,
        // 用户未授权此应用
        99991661,
    };

    /// <summary>
    /// 明确<b>可重试</b>的飞书错误码集合（系统繁忙 / 网关与服务端瞬时故障）。
    /// </summary>
    /// <remarks>
    /// TMR2-P1-4：这些码必须优先于消息关键字——否则一条恰好含 <c>"refresh token"</c> 的
    /// 瞬时故障消息会触发销毁性清库。
    /// </remarks>
    private static readonly HashSet<int> RetryableErrorCodes = new()
    {
        // 网关/服务端瞬时故障
        500, 502, 503, 504,
        // 系统繁忙
        99991400,
        // 服务内部错误（防御性：保留可重试语义）
        1061045,
    };

    /// <summary>
    /// 不可重试的错误消息关键字集合（不区分大小写）。
    /// </summary>
    /// <remarks>
    /// TMR2-P1-4：已移除歧义极大的 <c>"refresh token"</c> 与 <c>"scope"</c>；
    /// 仅保留"任何刷新上下文都不会误报"的强语义词。
    /// </remarks>
    private static readonly string[] UnretryableMsgKeywords =
    {
        "invalid_grant",
        "已失效",
        "已被吊销",
        "user not authorized",
    };

    /// <summary>
    /// 判定 OAuth 刷新失败是否不可重试。
    /// </summary>
    /// <param name="errorCode">飞书 API 返回的错误码（<c>res.Code</c>）</param>
    /// <param name="errorMsg">飞书 API 返回的错误消息（<c>res.Msg</c>）</param>
    /// <returns>不可重试返回 true，可重试返回 false</returns>
    internal static bool IsUnretryable(int? errorCode, string? errorMsg)
    {
        if (errorCode.HasValue && errorCode.Value != 0)
        {
            // 显式不可重试码优先。
            if (UnretryableErrorCodes.Contains(errorCode.Value))
                return true;

            // 显式可重试码次之——不得再被消息关键字推翻（TMR2-P1-4）。
            if (RetryableErrorCodes.Contains(errorCode.Value))
                return false;
        }

        // 仅错误码缺失/为 0/未列出时，才允许用**收紧后**的关键字兜底。
        return IsUnretryableMessage(errorMsg);
    }

    /// <summary>
    /// 关键字兜底判定（仅当错误码不可判定时使用）。
    /// </summary>
    /// <param name="errorMsg">飞书 API 返回的错误消息。</param>
    /// <returns>命中强语义不可重试关键字返回 true，否则 false。</returns>
    private static bool IsUnretryableMessage(string? errorMsg)
    {
        if (string.IsNullOrWhiteSpace(errorMsg))
            return false;

        var msg = errorMsg!;
        foreach (var keyword in UnretryableMsgKeywords)
        {
            if (msg.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }
}
