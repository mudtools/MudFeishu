// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Exceptions;

namespace Mud.Feishu.WebSocket.Exceptions;

/// <summary>
/// 飞书WebSocket异常基类
/// </summary>
public class FeishuWebSocketException : FeishuException
{
    /// <summary>
    /// 错误类型
    /// </summary>
    public string ErrorType { get; }

    /// <summary>
    /// 是否可恢复
    /// </summary>
    public bool IsRecoverable { get; }

    /// <summary>
    /// 初始化 <see cref="FeishuWebSocketException"/> 的新实例
    /// </summary>
    public FeishuWebSocketException(string errorType, string message, bool isRecoverable = true)
        : base(message)
    {
        ErrorType = errorType;
        IsRecoverable = isRecoverable;
    }

    /// <summary>
    /// 初始化 <see cref="FeishuWebSocketException"/> 的新实例
    /// </summary>
    public FeishuWebSocketException(string errorType, string message, Exception innerException, bool isRecoverable = true)
        : base(message, innerException)
    {
        ErrorType = errorType;
        IsRecoverable = isRecoverable;
    }

    /// <summary>
    /// 初始化 <see cref="FeishuWebSocketException"/> 的新实例（带错误代码）
    /// </summary>
    public FeishuWebSocketException(int errorCode, string errorType, string message, bool isRecoverable = true)
        : base(errorCode, message)
    {
        ErrorType = errorType;
        IsRecoverable = isRecoverable;
    }

    /// <summary>
    /// 初始化 <see cref="FeishuWebSocketException"/> 的新实例（带可空错误代码）
    /// </summary>
    public FeishuWebSocketException(string errorType, string message, int? errorCode, bool isRecoverable = true)
        : base(errorCode ?? 0, message)
    {
        ErrorType = errorType;
        IsRecoverable = isRecoverable;
    }

    /// <summary>
    /// 初始化 <see cref="FeishuWebSocketException"/> 的新实例（带错误代码和内部异常）
    /// </summary>
    public FeishuWebSocketException(int errorCode, string errorType, string message, Exception innerException, bool isRecoverable = true)
        : base(errorCode, message, innerException)
    {
        ErrorType = errorType;
        IsRecoverable = isRecoverable;
    }

    /// <summary>
    /// 初始化 <see cref="FeishuWebSocketException"/> 的新实例（带可空错误代码和内部异常）
    /// </summary>
    public FeishuWebSocketException(string errorType, string message, Exception innerException, int? errorCode, bool isRecoverable = true)
        : base(errorCode ?? 0, message, innerException)
    {
        ErrorType = errorType;
        IsRecoverable = isRecoverable;
    }
}
