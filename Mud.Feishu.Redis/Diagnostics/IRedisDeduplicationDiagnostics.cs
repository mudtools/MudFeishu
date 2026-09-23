// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Redis.Diagnostics;

/// <summary>
/// Redis 去重子系统运维诊断门面（R2-22 / E-02）。
/// </summary>
/// <remarks>
/// <para>
/// <b>存在理由</b>：计数类 API（<c>GetCachedCountAsync</c>、<c>GetCacheCount</c>、
/// <c>GetMaxProcessedSeqId</c>）只存在于**具体实现类型**上，而 DI 绑定的是
/// <c>IFeishuEventDeduplicator</c>/<c>IFeishuNonceDistributedDeduplicator</c>/<c>IFeishuSeqIDDeduplicator</c>
/// 接口——宿主若想在不依赖实现类型的条件下做运维诊断，此前没有出口（README 只能建议"解析具体实现类型"）。
/// 本门面把该能力收敛为一个接口，同时避免宿主耦合具体类型。
/// </para>
/// <para>
/// 由 <c>AddFeishuRedisDeduplicators</c> / <c>AddFeishuRedisTokenStore</c> 以单例注册；
/// 三个去重器若被宿主替换为非 Redis 实现，快照对应 <c>*Available</c> 为 <c>false</c>（不抛异常）。
/// </para>
/// </remarks>
public interface IRedisDeduplicationDiagnostics
{
    /// <summary>
    /// 采集一次诊断快照。
    /// </summary>
    /// <param name="cancellationToken">取消令牌（命令之间生效）</param>
    /// <returns>诊断快照</returns>
    /// <remarks>
    /// <b>成本警告</b>：事件/Nonce 计数为全库 SCAN + 服务端 <c>TIME</c>，属运维路径；
    /// 请勿在热路径或高频轮询（如每次请求）中调用。建议用于健康检查端点或人工排查。
    /// </remarks>
    Task<RedisDeduplicationDiagnosticsSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
