// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Utils;

internal static class WebhookPathHelper
{
    // 与 FeishuWebhookOptions.Validate() 中的 AppKey 正则一致
    private static readonly System.Text.RegularExpressions.Regex AppKeyRegex =
        new(@"^[a-zA-Z0-9_-]{1,64}$", System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// 从路径中提取 AppKey
    /// 前缀大小写不敏感、AppKey 段大小写敏感
    /// </summary>
    /// <param name="path">请求路径</param>
    /// <param name="globalRoutePrefix">全局路由前缀</param>
    /// <returns>合法 AppKey，或 null</returns>
    public static string? ExtractAppKeyFromPath(string path, string globalRoutePrefix)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        var prefix = "/" + globalRoutePrefix;

        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var afterPrefixIndex = prefix.Length;

        if (afterPrefixIndex >= path.Length)
            return null;

        if (path[afterPrefixIndex] != '/')
            return null;

        var start = afterPrefixIndex + 1;
        if (start >= path.Length)
            return null;

        var end = path.IndexOf('/', start);
        if (end == -1)
            end = path.Length;

        var appKey = path.Substring(start, end - start);
        if (string.IsNullOrEmpty(appKey))
            return null;

        // 正则预校验：非法字符直接返回 null（与未知应用同路，避免进入查表）
        if (!AppKeyRegex.IsMatch(appKey))
            return null;

        return appKey;
    }
}
