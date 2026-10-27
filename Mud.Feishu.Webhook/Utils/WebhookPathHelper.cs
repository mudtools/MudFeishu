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
    /// 归一化全局路由前缀：去空白、去首尾斜杠（`" /feishu/ "` → `"feishu"`），幂等。
    /// </summary>
    /// <param name="globalRoutePrefix">用户配置的前缀（可能为空/带斜杠）</param>
    /// <returns>可直接拼接到 `/` 之后的规范形态</returns>
    /// <remarks>
    /// <b>W-04：唯一的归一化实现</b>——由 <c>FeishuWebhookServiceBuilder</c> 的
    /// <c>PostConfigure</c> 在每次 Options 构建时调用，因此所有消费方（含第三方读
    /// <c>GlobalRoutePrefix</c>）看到的都是生效值。此处不再由各调用点各自拼接
    /// <c>"/" + prefix</c>（历史缺陷：配置写成 `"/feishu"` 会得到 `"//feishu"`，
    /// 写成 `"feishu/"` 会在前缀后直接比较 AppKey 段，两者都静默失效 → 端点全量 404）。
    /// </remarks>
    public static string NormalizePrefix(string? globalRoutePrefix)
        => (globalRoutePrefix ?? string.Empty).Trim().Trim('/');

    /// <summary>
    /// 从路径中提取 AppKey
    /// 前缀大小写不敏感、AppKey 段大小写敏感
    /// </summary>
    /// <param name="path">请求路径</param>
    /// <param name="globalRoutePrefix">全局路由前缀（应为已归一化的生效值）</param>
    /// <returns>合法 AppKey，或 null</returns>
    public static string? ExtractAppKeyFromPath(string path, string globalRoutePrefix)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        // 防御性再归一化：本方法可能被测试/宿主动态传入未归一化的值。
        // 归一化是幂等的，对已归一化的生效值零副作用。
        var prefix = "/" + NormalizePrefix(globalRoutePrefix);

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
