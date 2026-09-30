// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Configuration;

/// <summary>
/// 会话存储配置（配置节 <see cref="SectionName"/>）。
/// </summary>
/// <remarks>
/// <para>
/// 会话存储是跨包契约：Memory 实现（Mud.Feishu.AI）与 Redis 实现（Mud.Feishu.Redis）
/// 共同消费本配置——配置类型置于 Abstractions 与 <see cref="FeishuDeduplicationOptions"/>
/// （去重配置被 Memory/Redis 双后端消费）同模式，保证包间只存在纵向引用
/// （各实现包 → Abstractions），不允许横向引用。
/// </para>
/// <para>
/// 配置面治理（R4/R5）：属性必须有真实消费点（两个后端存储的 TTL 读取处）；
/// 配置 DTO 不得使用 <c>required</c>，合法性在 <see cref="Validate"/> 校验。
/// </para>
/// </remarks>
public sealed class FeishuConversationOptions
{
    /// <summary>配置节名称（<c>FeishuConversation</c>）。</summary>
    public const string SectionName = "FeishuConversation";

    /// <summary>
    /// 会话空闲 TTL 的<b>默认值</b>（R2-10：本常量是唯一字面量处）。
    /// </summary>
    /// <remarks>
    /// 消费点：本属性默认值 + <c>MemoryConversationStore</c> 的构造函数缺省值。
    /// 此前两处各写一份 <c>TimeSpan.FromHours(24)</c>，与 <c>IConversationStore</c> 上
    /// 「TTL 单一阈值源为 <see cref="SessionTtl"/>」的契约相矛盾（漂移不会被编译器发现）。
    /// </remarks>
    public static readonly TimeSpan DefaultSessionTtl = TimeSpan.FromHours(24);

    /// <summary>
    /// 会话空闲 TTL（消费点：Memory/Redis 会话存储的过期时长，单一阈值源——两后端不得各设一套）。
    /// </summary>
    public TimeSpan SessionTtl { get; set; } = DefaultSessionTtl;

    /// <summary>
    /// 校验配置合法性。
    /// </summary>
    /// <exception cref="InvalidOperationException">存在非法取值。</exception>
    public void Validate()
    {
        if (SessionTtl <= TimeSpan.Zero)
            throw new InvalidOperationException(
                $"FeishuConversation:{nameof(SessionTtl)} 必须为正数，实际值: {SessionTtl}");
    }
}
