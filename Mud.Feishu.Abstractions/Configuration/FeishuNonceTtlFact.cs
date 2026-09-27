// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Configuration;

/// <summary>
/// Redis 注册期写入、Webhook 读取的 Nonce TTL 事实（进程内单例）。
/// </summary>
/// <remarks>
/// <para>
/// R5.4/F4（V5 修正）：<c>RedisOptions</c> 属 Redis 包，Webhook 包不可强类型引用。
/// 跨包 Nonce TTL 一致性校验通过本 holder 单向传递：Redis 包在 <c>AddFeishuRedisDeduplicators</c>
/// 注册期写入实际 TTL，Webhook 包在 <c>PostConfigure</c> 读取并与 <c>TimestampToleranceSeconds</c>
/// 做严格 <c>&gt;</c> 校验。
/// </para>
/// <para>
/// 纯 Webhook 宿主（未引用 Redis 包）时 <c>GetService&lt;FeishuNonceTtlFact&gt;()</c> 返回 null，校验跳过。
/// </para>
/// </remarks>
public sealed class FeishuNonceTtlFact
{
    /// <summary>Redis 去重器实际使用的 Nonce TTL；null 表示未由 Redis 包写入。</summary>
    public TimeSpan? NonceTtl { get; set; }
}