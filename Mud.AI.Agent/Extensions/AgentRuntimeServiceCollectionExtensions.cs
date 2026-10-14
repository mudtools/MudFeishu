// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mud.AI.Abstractions.Approvals;
using Mud.AI.Abstractions.Conversations;
using Mud.AI.Abstractions.Observability;
using Mud.AI.Agent.Sessions;

namespace Mud.AI.Agent.Extensions;

/// <summary>
/// <b>中立运行期 DI 骨架</b>（N2-g）：注册"任何生态都需要、且都没有产品语义"的默认件。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它</b>：中立的机制（会话门面、待确认快照、遥测端口）都需要默认实现才能在
/// 「宿主什么也没配」时开箱可用。若这些默认注册散落在各产品的 <c>AddXxxAgent</c> 里，
/// 每上一个新生态就要重抄一遍装配顺序——装配顺序错（如遥测注册在依赖它的服务之后）不报错、
/// 只表现为"遥测缺失"，属最难查的一类问题。
/// </para>
/// <para>
/// <b>注册语义全是 <c>TryAdd</c></b>：产品的薄壳（如飞书侧的 <c>MemoryConversationStore</c>）
/// 与宿主的自定义实现都可通过<b>先注册</b>覆盖默认件——这是「先注册者生效」的既有纪律，
/// 也是本骨架不引入第二套可替换槽位的原因。
/// </para>
/// <para>
/// <b>⛔ 不含会话闸门</b>（方案 §5.4.1 定稿）：闸门端口与实现全部留在产品侧
/// （产品的 <c>IConversationGate</c> 是<b>已发布 SDK 的宿主可替换契约</b>），中立管线经
/// ③ 式委托消费。把闸门塞进中立骨架会同时制造「第二个可替换槽位」与「中立包依赖产品契约」。
/// </para>
/// <para>
/// <b>TTL 口径</b>：默认会话存储用 <see cref="MemorySessionStore.DefaultTtl"/>；产品薄壳必须
/// 显式传入自己的 TTL 单一阈值源（避免出现两份可漂移的字面量）。
/// </para>
/// </remarks>
public static class AgentRuntimeServiceCollectionExtensions
{
    /// <summary>
    /// 注册中立运行期的默认件（幂等；已注册的实现保持不变）。
    /// </summary>
    /// <remarks>
    /// 注册清单（顺序即依赖顺序）：
    /// <list type="number">
    /// <item>遥测端口 → 空实现（无宿主观测设施时的零成本默认）；</item>
    /// <item>会话存储 <see cref="ISessionStore"/> → <see cref="MemorySessionStore"/>（单实例默认）；</item>
    /// <item>待确认快照 <see cref="IPendingApprovalStore"/> → 进程内实现（软缺席契约的默认件）。</item>
    /// </list>
    /// </remarks>
    /// <param name="services">服务集合。</param>
    /// <returns>服务集合（可串联）。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> 为 null。</exception>
    public static IServiceCollection AddAgentRuntimeCore(this IServiceCollection services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // ① 遥测：空实现在最前——后续注册的机制可选地依赖它（构造参数可空即可，不强制）。
        services.TryAddSingleton<IAgentTelemetry>(NoopAgentTelemetry.Instance);

        // ② 会话存储：进程内默认（分布式部署由宿主先注册自己的实现覆盖）。
        services.TryAddSingleton<ISessionStore>(static _ => new MemorySessionStore());

        // ③ 待确认快照：进程内默认（R7 / C4a 的软缺席契约，见其类型注释）。
        services.TryAddSingleton<IPendingApprovalStore, Mud.AI.Agent.Approvals.InMemoryPendingApprovalStore>();

        return services;
    }
}
