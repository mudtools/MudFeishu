// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 错误恢复结果，包含错误分析信息和恢复建议
/// </summary>
public class ErrorRecoveryResult
{
    /// <summary>
    /// 获取或设置异常信息
    /// </summary>
    public Exception Exception { get; set; } = null!;

    /// <summary>
    /// 获取或设置错误类型
    /// </summary>
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 获取或设置错误上下文信息
    /// </summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置错误是否可恢复
    /// </summary>
    public bool IsRecoverable { get; set; }

    /// <summary>
    /// 获取或设置恢复建议
    /// </summary>
    public string RecoveryRecommendation { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置建议的延迟时间
    /// </summary>
    public TimeSpan SuggestedDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 获取或设置时间戳
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 返回错误恢复结果的字符串表示
    /// </summary>
    /// <returns>格式化的错误信息字符串</returns>
    public override string ToString()
    {
        return $"{ErrorType}: {Exception.Message} (可恢复: {IsRecoverable}, 建议: {RecoveryRecommendation})";
    }
}
