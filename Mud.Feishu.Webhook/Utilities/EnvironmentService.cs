// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Utilities;

/// <summary>
/// 环境服务接口
/// 提供应用程序环境相关的信息和判断
/// </summary>
public interface IEnvironmentService
{
    /// <summary>
    /// 当前环境名称
    /// </summary>
    string EnvironmentName { get; }

    /// <summary>
    /// 是否为生产环境
    /// </summary>
    bool IsProduction { get; }

    /// <summary>
    /// 是否为开发环境
    /// </summary>
    bool IsDevelopment { get; }

    /// <summary>
    /// 是否为预发布/测试环境
    /// </summary>
    bool IsStaging { get; }
}

/// <summary>
/// 环境服务实现
/// </summary>
public class EnvironmentService : IEnvironmentService
{
    private readonly string _environmentName;

    /// <summary>
    /// 初始化环境服务
    /// </summary>
    public EnvironmentService()
    {
        _environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    }

    /// <inheritdoc />
    public string EnvironmentName => _environmentName;

    /// <inheritdoc />
    public bool IsProduction => string.Equals(_environmentName, "Production", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public bool IsDevelopment => string.Equals(_environmentName, "Development", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public bool IsStaging => string.Equals(_environmentName, "Staging", StringComparison.OrdinalIgnoreCase);
}
