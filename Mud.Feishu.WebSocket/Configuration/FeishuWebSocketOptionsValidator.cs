// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// FeishuWebSocketOptions 配置验证器
/// </summary>
/// <remarks>
/// 实现 IValidateOptions 接口，在依赖注入时自动验证配置。
/// 内部调用 FeishuWebSocketOptions.Validate() 方法，保持验证逻辑一致性。
/// </remarks>
public class FeishuWebSocketOptionsValidator : IValidateOptions<FeishuWebSocketOptions>
{
    /// <summary>
    /// 验证配置选项
    /// </summary>
    /// <param name="name">配置名称</param>
    /// <param name="options">配置选项实例</param>
    /// <returns>验证结果</returns>
    public ValidateOptionsResult Validate(string? name, FeishuWebSocketOptions options)
    {
        if (options == null)
        {
            return ValidateOptionsResult.Fail("FeishuWebSocketOptions 配置不能为 null");
        }

        try
        {
            options.Validate();
            return ValidateOptionsResult.Success;
        }
        catch (InvalidOperationException ex)
        {
            return ValidateOptionsResult.Fail($"FeishuWebSocketOptions 配置验证失败: {ex.Message}");
        }
    }
}
