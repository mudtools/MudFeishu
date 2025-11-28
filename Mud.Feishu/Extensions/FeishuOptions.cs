// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;

/// <summary>
/// 飞书 API 配置选项类。
/// <para>用于配置飞书 API 访问所需的基本参数，包括应用凭证、网络设置和重试策略。</para>
/// <para>在 ASP.NET Core 应用中，通过配置文件（如 appsettings.json）进行配置，并通过依赖注入使用。</para>
/// </summary>
/// <example>
/// 以下是在 appsettings.json 中的配置示例：
/// <code>
/// {
///   "Feishu": {
///     "AppId": "cli_a1b2c3d4e5f6g7h8",
///     "AppSecret": "dskLLdkasdjlasdKK",
///     "BaseUrl": "https://open.feishu.cn",
///     "TimeOut": "30",
///     "RetryCount": 3
///   }
/// }
/// </code>
/// </example>
/// <remarks>
/// <para><strong>重要提示：</strong></para>
/// <list type="bullet">
/// <item><description>AppId 和 AppSecret 是飞书应用的身份凭证，请妥善保管，不要在代码中硬编码</description></item>
/// <item><description>建议使用环境变量或安全的配置管理系统来存储敏感信息</description></item>
/// <item><description>在生产环境中，建议使用 HTTPS 协议以确保通信安全</description></item>
/// </list>
/// </remarks>
public class FeishuOptions
{
    /// <summary>
    /// 飞书应用唯一标识，创建应用后获得。
    /// <para>示例值： "cli_a1b2c3d4e5f6g7h8"</para>
    /// <para>该标识用于识别你的飞书应用，在调用飞书 API 时必须提供。</para>
    /// <para>可在飞书开发者后台的应用详情页面中找到此值。</para>
    /// </summary>
    /// <exception cref="ArgumentNullException">当 AppId 为 null 或空字符串时抛出</exception>
    public
#if NET7_0_OR_GREATER
        required
#endif
  string? AppId
    { get; set; }

    /// <summary>
    /// 应用秘钥，创建应用后获得。
    /// <para>示例值： "dskLLdkasdjlasdKK"</para>
    /// <para>该秘钥用于应用身份验证，与 AppId 配合使用以获取访问令牌。</para>
    /// <para>请在飞书开发者后台的应用详情页面中找到此值，并确保其安全性。</para>
    /// </summary>
    /// <exception cref="ArgumentNullException">当 AppSecret 为 null 或空字符串时抛出</exception>
    public
#if NET7_0_OR_GREATER
        required
#endif
  string? AppSecret
    { get; set; }


    /// <summary>
    /// 飞书 API 基础地址。
    /// <para>默认值： "https://open.feishu.cn"</para>
    /// <para>用于自定义飞书服务的访问地址，通常在生产环境中使用默认值即可</para>
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// HTTP 请求超时时间（秒）。
    /// <para>默认值：30秒</para>
    /// <para>用于设置API调用的超时时间，网络环境较差时可适当增加此值</para>
    /// <para>建议值：10-120秒，根据网络环境调整</para>
    /// </summary>
    /// <remarks>
    /// 注意：此值目前使用字符串类型以便于配置文件读取，内部会自动转换为整数。
    /// </remarks>
    public string? TimeOut { get; set; }

    /// <summary>
    /// 失败重试次数。
    /// <para>默认值：3次</para>
    /// <para>当API调用失败时的自动重试次数，提高请求的成功率和稳定性</para>
    /// </summary>
    public int? RetryCount { get; set; }


    /// <summary>
    /// 是否启用日志记录，默认为true
    /// </summary>
    public bool EnableLogging { get; set; } = true;
}