// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Mud.Feishu.Webhook.Utils;

namespace Mud.Feishu.Webhook.Configuration;

/// <summary>
/// FeishuWebhookOptions 配置验证器
/// </summary>
/// <remarks>
/// 实现 IValidateOptions 接口，在依赖注入时自动验证配置。
/// 内部调用 FeishuWebhookOptions.Validate() 方法，保持验证逻辑一致性。
/// </remarks>
public class FeishuWebhookOptionsValidator : IValidateOptions<FeishuWebhookOptions>
{
    private readonly IEnvironmentService? _environmentService;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="environmentService">环境服务（可选，用于生产环境安全项锁定）</param>
    public FeishuWebhookOptionsValidator(IEnvironmentService? environmentService = null)
    {
        _environmentService = environmentService;
    }

    /// <summary>
    /// 验证配置选项
    /// </summary>
    /// <param name="name">配置名称</param>
    /// <param name="options">配置选项实例</param>
    /// <returns>验证结果</returns>
    public ValidateOptionsResult Validate(string? name, FeishuWebhookOptions options)
    {
        if (options == null)
        {
            return ValidateOptionsResult.Fail("FeishuWebhookOptions 配置不能为 null");
        }

        try
        {
            options.Validate();
        }
        catch (InvalidOperationException ex)
        {
            return ValidateOptionsResult.Fail($"FeishuWebhookOptions 配置验证失败: {ex.Message}");
        }

        // 生产环境安全项锁定（ADR-4）：兑现 XML 文档中「系统会在生产环境自动检测并拒绝」的承诺
        if (_environmentService?.IsProduction == true)
        {
            if (!options.EnforceHeaderSignatureValidation)
            {
                return ValidateOptionsResult.Fail(
                    "生产环境禁止 EnforceHeaderSignatureValidation=false（将导致缺少 X-Lark-Signature 的请求被直接放行，" +
                    "攻击者可伪造事件）。如确需在非生产环境关闭，请设置 ASPNETCORE_ENVIRONMENT=Development。");
            }

            // WHF-01：应用级覆盖同样受生产锁定约束，与 SignatureValidator 的
            // GetEffectiveEnforceHeaderSignatureValidation 解析路径对齐，
            // 防止应用级显式 false 绕过全局锁定（校验面与运行时面视野一致）
            // 用 KeyValuePair 而非 Values：应用标识就是字典键（R5.2/X8 起 AppKey 由键派生），
            // 直接取 Key 可避免读取已 Obsolete 的派生字段。
            var violatedApp = options.Apps
                .FirstOrDefault(a => a.Value.EnforceHeaderSignatureValidation == false);
            if (!string.IsNullOrEmpty(violatedApp.Key))
            {
                return ValidateOptionsResult.Fail(
                    $"生产环境禁止应用 {violatedApp.Key} 设置 " +
                    "EnforceHeaderSignatureValidation=false（应用级覆盖将绕过签名强制验证）。" +
                    "如确需在非生产环境关闭，请设置 ASPNETCORE_ENVIRONMENT=Development。");
            }
        }

        return ValidateOptionsResult.Success;
    }
}
