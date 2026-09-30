// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;

namespace Mud.Feishu.AI.Knowledge;

/// <summary>
/// <see cref="AilyKnowledgeOptions"/> 的启动期校验器（Options 管线接 <see cref="AilyKnowledgeOptions.Validate"/>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么必须显式注册（R2-10）</b>：<c>AilyKnowledgeOptions</c> 早有 <c>Validate()</c>，但它此前<b>只</b>在
/// 解析处（<c>AddFeishuAilyKnowledge</c> 的工厂里）被调用——即"用到才校验"。而该选项的解析点是
/// <c>AilyKnowledgeProvider</c>，其唯一消费者是"知识检索"，于是<b>配错的宿主会在第一次真实提问时才炸</b>，
/// 而不是启动期。与 <see cref="Agents.FeishuAgentOptions"/>（注册器 + <c>ValidateOnStart</c>）对齐后，
/// 配置错误在启动期响亮失败。
/// </para>
/// <para>
/// net6+ 宿主经 <c>ValidateOnStart</c> 在启动期触发；netstandard2.0 无 ValidateOnStart，
/// 由首次解析 Options 时触发（与 <see cref="Agents.FeishuAgentOptionsValidator"/> 同模式）。
/// </para>
/// </remarks>
public sealed class AilyKnowledgeOptionsValidator : IValidateOptions<AilyKnowledgeOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AilyKnowledgeOptions options)
    {
        if (options is null)
            return ValidateOptionsResult.Fail("AilyKnowledgeOptions 不能为 null");

        try
        {
            options.Validate();
            return ValidateOptionsResult.Success;
        }
        catch (InvalidOperationException ex)
        {
            // 有意静默（守卫白名单）：异常被**转换**为 Options 管线的失败结果（Fail），
            // 由宿主在启动期以 ValidateOnStart 抛出——不是吞掉，而是换了一条上报通道。
            return ValidateOptionsResult.Fail(ex.Message);
        }
    }
}
