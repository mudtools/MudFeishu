// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Tools;

/// <summary>
/// R-3 / BUG-8：模型入参校验失败异常——在人类可读消息之外，额外携带<b>机器可读的恢复候选</b>。
/// </summary>
/// <remarks>
/// <para>
/// v2.3 中立化迁入（原 <c>Mud.Feishu.AI.Tools.Args.ToolArgumentException</c>，internal → public：
/// 骨架 <c>ToolExecutor.RunAsync</c> 的 catch 分支与产品侧 <c>ToolArgs.RequireEnum</c>（抛出点）分处两个程序集）。
/// </para>
/// <para>
/// <b>为什么需要它</b>：参数类错误的恢复信息不能只靠人类可读文本——否则模型要拿到"合法取值有哪些"
/// 必须<b>解析文案</b>，而执行链的 catch 路径只能把整段文案塞进正文。
/// 本类型把候选值做成结构化字段，经 <c>ToolError.Suggestions</c> 进入错误载荷首行 JSON，
/// 模型可直接分支（对齐官方 <c>lark-cli</c> 的 <c>Suggestions []string</c> 语义）。
/// </para>
/// <para>
/// <b>候选只允许是事实</b>：当前唯一生产者是<b>闭集参数的合法取值清单</b>（取值本身就是事实）。
/// 不得把"猜测的原因/缺失权限"写成候选——那会让结构化字段退化成猜测记录。
/// </para>
/// <para>
/// <b>为什么派生自 <see cref="System.ArgumentException"/></b>：分类器
/// （<c>ToolErrorClassifier.Classify</c>）按基类判定 <c>Validation/invalid_args</c>，
/// 派生后<b>零改动</b>即被正确归类（模式匹配对派生类型命中基类分支）。
/// </para>
/// </remarks>
public sealed class ToolArgumentException : System.ArgumentException
{
    /// <summary>构造带恢复候选的参数校验异常。</summary>
    /// <param name="message">人类可读消息（保持不变，老用例的 <c>Contain</c> 断言不受影响）。</param>
    /// <param name="suggestions">机器可读恢复候选（非空、非空白项由载荷序列化器过滤）。</param>
    public ToolArgumentException(string message, IReadOnlyList<string> suggestions)
        : base(message)
    => Suggestions = suggestions;

    /// <summary>机器可读的恢复候选（如闭集参数的合法取值清单）。</summary>
    public IReadOnlyList<string> Suggestions { get; }
}
