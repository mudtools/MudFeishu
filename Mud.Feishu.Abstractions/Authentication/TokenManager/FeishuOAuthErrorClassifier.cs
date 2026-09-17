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
    /// 不可重试的错误消息关键字集合（不区分大小写）。
    /// </summary>
    private static readonly string[] UnretryableMsgKeywords =
    {
        "invalid_grant",
        "refresh token",
        "已失效",
        "已被吊销",
        "scope",
    };

    /// <summary>
    /// 判定 OAuth 刷新失败是否不可重试。
    /// </summary>
    /// <param name="errorCode">飞书 API 返回的错误码（<c>res.Code</c>）</param>
    /// <param name="errorMsg">飞书 API 返回的错误消息（<c>res.Msg</c>）</param>
    /// <returns>不可重试返回 true，可重试返回 false</returns>
    internal static bool IsUnretryable(int? errorCode, string? errorMsg)
    {
        if (errorCode.HasValue && UnretryableErrorCodes.Contains(errorCode.Value))
            return true;

        if (!string.IsNullOrEmpty(errorMsg))
        {
            var msg = errorMsg!;
            foreach (var keyword in UnretryableMsgKeywords)
            {
                if (msg.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
        }

        return false;
    }
}
