// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// 飞书多应用配置（<c>FeishuApps</c> 节）的进程内合成：由统一节 <c>FeishuDemo</c> 的凭证合成单应用，
/// 不落盘、不入日志。工具冒烟直接整段消费；文档智能体在「配置未显式提供 <c>FeishuApps</c>」时增量合并。
/// </summary>
internal static class DemoAppConfig
{
    /// <summary>SDK 的飞书多应用节名（<c>AddFeishuApp</c> 读它）。</summary>
    public const string SectionName = "FeishuApps";

    /// <summary>
    /// 生成单应用的键值对（索引 0；标记为默认应用）。
    /// </summary>
    /// <param name="appKey">应用键。</param>
    /// <param name="appId">飞书 AppId。</param>
    /// <param name="appSecret">飞书 AppSecret。</param>
    /// <returns>键值对（供 <c>AddInMemoryCollection</c> 消费）。</returns>
    public static IReadOnlyDictionary<string, string?> SingleAppKeys(string appKey, string appId, string appSecret)
        => new Dictionary<string, string?>
        {
            [$"{SectionName}:0:AppKey"] = appKey,
            [$"{SectionName}:0:AppId"] = appId,
            [$"{SectionName}:0:AppSecret"] = appSecret,
            [$"{SectionName}:0:IsDefault"] = "true",
        };

    /// <summary>
    /// 构造只含单应用的独立配置根（AppSecret 只来自配置文件解析结果，不经环境变量、不落盘）。
    /// </summary>
    /// <param name="appKey">应用键。</param>
    /// <param name="appId">飞书 AppId。</param>
    /// <param name="appSecret">飞书 AppSecret。</param>
    /// <returns>配置根。</returns>
    public static IConfigurationRoot CreateSingleApp(string appKey, string appId, string appSecret)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(SingleAppKeys(appKey, appId, appSecret))
            .Build();
}
