// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;

namespace Mud.Feishu.Abstractions;

/// <summary>
/// FeishuAppConfig 配置验证器
/// </summary>
/// <remarks>
/// 实现 IValidateOptions 接口，在依赖注入时自动验证配置。
/// 支持单个 FeishuAppConfig 和 List&lt;FeishuAppConfig&gt; 两种配置模式的验证。
/// 内部调用 FeishuAppConfig.Validate() 方法，保持验证逻辑一致性。
/// </remarks>
public class FeishuAppConfigValidator : IValidateOptions<FeishuAppConfig>, IValidateOptions<List<FeishuAppConfig>>
{
    /// <summary>
    /// 验证单个 FeishuAppConfig 配置选项
    /// </summary>
    /// <param name="name">配置名称</param>
    /// <param name="options">配置选项实例</param>
    /// <returns>验证结果</returns>
    public ValidateOptionsResult Validate(string? name, FeishuAppConfig options)
    {
        if (options == null)
        {
            return ValidateOptionsResult.Fail("FeishuAppConfig 配置不能为 null");
        }

        try
        {
            options.Validate();
            return ValidateOptionsResult.Success;
        }
        catch (InvalidOperationException ex)
        {
            return ValidateOptionsResult.Fail($"FeishuAppConfig 配置验证失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 验证 List&lt;FeishuAppConfig&gt; 配置选项列表
    /// </summary>
    /// <param name="name">配置名称</param>
    /// <param name="options">配置选项列表实例</param>
    /// <returns>验证结果</returns>
    public ValidateOptionsResult Validate(string? name, List<FeishuAppConfig> options)
    {
        if (options == null || options.Count == 0)
        {
            return ValidateOptionsResult.Fail("FeishuAppConfig 配置列表不能为 null 或空");
        }

        var errors = new List<string>();
        for (int i = 0; i < options.Count; i++)
        {
            try
            {
                options[i].Validate();
            }
            catch (InvalidOperationException ex)
            {
                errors.Add($"应用[{i}] (AppKey: {options[i].AppKey ?? "null"}): {ex.Message}");
            }
        }

        var defaultApps = options.Where(c => c.IsDefault).ToList();
        if (defaultApps.Count > 1)
        {
            var defaultAppKeys = string.Join(", ", defaultApps.Select(c => c.AppKey ?? "null"));
            errors.Add($"存在多个 IsDefault=true 的应用（{defaultAppKeys}），仅第一个默认应用会生效。请确保只有一个应用标记为默认。");
        }

        if (errors.Count > 0)
        {
            return ValidateOptionsResult.Fail($"FeishuAppConfig 配置验证失败:\n{string.Join("\n", errors)}");
        }

        return ValidateOptionsResult.Success;
    }
}
