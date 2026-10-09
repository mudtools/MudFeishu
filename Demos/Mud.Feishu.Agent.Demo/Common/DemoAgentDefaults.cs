// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.AI.Agents;
using Mud.Feishu.AI.Extensions;

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// 裸模型 / 工具冒烟 / 文档智能体三个模式**共用**的 Agent 装配默认值与扩展：
/// 模型客户端服务键、演示应用键、演示会话用户，以及「模型客户端 + Agent 基础选项」的一步注册。
/// </summary>
/// <remarks>
/// 模型客户端与 <c>AddFeishuAgent</c> 的四连注册在三个模式间逐字重复，
/// 收敛至此处后，模式侧只声明差异项（Agent 名 / 指令 / 白名单等，经 <paramref name="configure"/> 注入）。
/// </remarks>
internal static class DemoAgentDefaults
{
    /// <summary>演示用的默认飞书应用键（工具执行上下文与合成单应用的租户维度事实来源）。</summary>
    public const string DefaultAppKey = "demo-app";

    /// <summary>键控模型客户端的服务键（与 <c>AddFeishuOpenAIChatClient</c> 一致）。</summary>
    public const string ModelServiceKey = "demo-model";

    /// <summary>轻量模式（裸模型 / 工具冒烟 / IM 事件接入）共用的会话 subject 段（不是真实 open_id）。</summary>
    public const string DemoUserId = "ou_demo_user";

    /// <summary>
    /// 一步注册「模型客户端 + Agent 基础选项」：服务键 / Agent 名 / 指令按默认口径装配，
    /// 模式差异项经 <paramref name="configure"/> 追加。
    /// </summary>
    /// <param name="services">服务容器。</param>
    /// <param name="model">模型三参数（统一节 <c>FeishuDemo</c> 解析结果）。</param>
    /// <param name="agentName">Agent 展示名（OTel <c>feishu.agent.name</c> 取值）。</param>
    /// <param name="instructions">宿主指令（system prompt）。</param>
    /// <param name="configure">模式专属选项（白名单、身份闭集、记忆上限等；可空）。</param>
    /// <returns>服务容器（链式）。</returns>
    public static IServiceCollection AddDemoAgent(
        this IServiceCollection services,
        ChatModelSettings model,
        string agentName,
        string instructions,
        Action<FeishuAgentOptions>? configure = null)
        => services
            .AddFeishuOpenAIChatClient(ModelServiceKey, model.ModelId, model.ApiKey, model.Endpoint)
            .AddFeishuAgent(configure: options =>
            {
                options.ModelServiceKey = ModelServiceKey;
                options.Name = agentName;
                options.Instructions = instructions;
                configure?.Invoke(options);
            });
}
