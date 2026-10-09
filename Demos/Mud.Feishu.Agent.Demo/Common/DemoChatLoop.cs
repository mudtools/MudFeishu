// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// 裸模型与工具冒烟模式共用的控制台问答循环：逐行读入，<c>exit</c>（大小写不敏感）或 EOF 退出。
/// </summary>
/// <remarks>
/// 两个模式的原循环逐字重复（读入判空 + exit 判定 + 逐轮执行），差异只在"每一轮做什么"——
/// 收敛为单一循环 + 轮次委托；文档业务智能体不适用（斜杠命令 / 审批挂起态需要 <see cref="AgentConsoleLoop"/> 全量交互面）。
/// </remarks>
internal static class DemoChatLoop
{
    /// <summary>
    /// 运行问答循环。
    /// </summary>
    /// <param name="title">进入循环前打印的标题行。</param>
    /// <param name="onUserInput">每一轮用户输入的处理委托（保存会话由委托自行负责）。</param>
    public static async Task RunAsync(string title, Func<string, Task> onUserInput)
    {
        ArgumentNullException.ThrowIfNull(onUserInput);

        Console.WriteLine($"{title}（exit 退出）：");
        while (Console.ReadLine() is { } userText
            && userText.Length > 0
            && !string.Equals(userText, "exit", StringComparison.OrdinalIgnoreCase))
        {
            await onUserInput(userText);
        }
    }
}
