// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  本项目基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// Redis 操作失败类型分类，用于消费侧区分"连接不可用"与"服务端/配置错误"。
/// </summary>
public enum FeishuRedisFailureKind
{
    /// <summary>
    /// 连接不可用（网络中断、DNS 不可达、认证失败等）
    /// </summary>
    Connection,

    /// <summary>
    /// 操作超时
    /// </summary>
    Timeout,

    /// <summary>
    /// 服务端错误（配置错误、Lua 脚本失败、非法参数等）
    /// </summary>
    Server,

    /// <summary>
    /// 无效参数（调用方传入非法值）
    /// </summary>
    InvalidArgument
}

/// <summary>
/// 飞书 Redis 去重/令牌存储操作的统一异常类型。
/// <para>ADR-6：三个去重器与两个令牌存储统一包装 Redis 异常为可分类的 <see cref="FeishuRedisException"/>，
/// 使消费侧（如 <c>NonceValidator</c>）能区分"连接不可用"（可降级）与"服务端错误"（应抛出）。</para>
/// </summary>
public class FeishuRedisException : InvalidOperationException
{
    /// <summary>
    /// 失败类型分类
    /// </summary>
    public FeishuRedisFailureKind FailureKind { get; }

    /// <summary>
    /// 初始化 <see cref="FeishuRedisException"/>
    /// </summary>
    /// <param name="failureKind">失败类型分类</param>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">原始异常（可选）</param>
    public FeishuRedisException(
        FeishuRedisFailureKind failureKind,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        FailureKind = failureKind;
    }
}
