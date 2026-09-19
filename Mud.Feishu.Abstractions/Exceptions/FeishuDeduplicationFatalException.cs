// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 去重体系致命故障标记（WHF-02）：由事件服务的<b>去重调用点</b>包装抛出，
/// 用于与业务处理器内部使用 Redis 抛出的 <see cref="FeishuRedisException"/> 区分——
/// 前者不回滚、不写失败存储、上抛转 503；后者按业务失败处理（回滚 + ADR-2 + 500）。
/// </summary>
public class FeishuDeduplicationFatalException : FeishuRedisException
{
    /// <summary>
    /// 使用去重致命故障消息与内部异常初始化。
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">原始 Redis 异常</param>
    public FeishuDeduplicationFatalException(string message, Exception innerException)
        : base(FeishuRedisFailureKind.Server, message, innerException)
    {
    }
}
