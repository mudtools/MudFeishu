// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Agent.Demo.Tests;

/// <summary>
/// 测试替身与最小装配工厂。
/// </summary>
/// <remarks>
/// <b>设计纪律</b>：所有替身都是零延时的纯内存对象——仓库既有教训明确禁止用固定
/// <c>Task.Delay</c> 上界做"未阻塞"的耗时断言（线程池注入延迟会让这类用例偶发红）。
/// 「立即返回」用 <c>Task.IsCompleted</c> 直接断言，零延时、零抖动。
/// </remarks>
internal static class TestDoubles
{
    /// <summary>
    /// 仓库根目录（以 <c>Mud.Feishu.slnx</c> 为锚）。
    /// </summary>
    /// <returns>仓库根的绝对路径。</returns>
    /// <remarks>
    /// 供"真实文件"类守卫使用（随仓库提交的配置模板、csproj 依赖面等）——断言真实文件才能挡住
    /// "改了代码忘了改模板"这类静默失效；纯内存替身对此无能为力。
    /// </remarks>
    public static string RepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory)
            && !File.Exists(Path.Combine(directory, "Mud.Feishu.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        if (string.IsNullOrEmpty(directory))
        {
            throw new InvalidOperationException("测试必须能定位仓库根目录（以 Mud.Feishu.slnx 为锚）");
        }

        return directory;
    }

    /// <summary>构造默认合法的配置（必填项齐备，其余取默认值）。</summary>
    /// <returns>配置实例。</returns>
    public static DocAgentSettings CreateSettings() => new()
    {
        ModelId = "test-model",
        ApiKey = "sk-test-not-a-real-key",
        AppId = "cli_test",
        AppSecret = "test-secret-not-real",
    };

    /// <summary>构造带 <see cref="StringWriter"/> 的渲染器（同时接收 stdout 与 stderr）。</summary>
    /// <returns>渲染器与输出读取器。</returns>
    public static (ConsoleRenderer Renderer, StringWriter Output) CreateRenderer()
    {
        var writer = new StringWriter();
        return (new ConsoleRenderer(writer, writer), writer);
    }

    /// <summary>
    /// 构造授权器（含可变策略状态）。
    /// </summary>
    /// <param name="policy">初始策略。</param>
    /// <param name="expectedAppKey">期望 appKey。</param>
    /// <returns>授权器与状态。</returns>
    /// <remarks>
    /// <b>刻意不提供工具注册表</b>：授权器的风险事实来自编译期契约表
    /// （<c>FeishuToolContracts</c>），而不是容器里的 <c>FeishuToolRegistry</c>——
    /// 后者会形成运行期循环依赖（注册表 → Binding → 授权器 → 注册表），
    /// 表现为启动期无输出卡死。故用例里的工具名一律取<b>真实</b>工具名。
    /// </remarks>
    public static (ConsoleToolAuthorizer Authorizer, ConsoleToolAuthorizerState State) CreateAuthorizer(
        string policy = DocAgentSettings.PolicyStrict,
        string expectedAppKey = DocAgentSettings.DefaultAppKey)
    {
        var (renderer, _) = CreateRenderer();
        var state = new ConsoleToolAuthorizerState(expectedAppKey, DocAgentSettings.AllowedScopes, policy);
        return (new ConsoleToolAuthorizer(state, renderer), state);
    }

    /// <summary>
    /// 构造与生产链路同形的工具执行上下文（带会话维度）。
    /// </summary>
    /// <param name="appKey">应用键（缺省取宿主期望值）。</param>
    /// <returns>执行上下文。</returns>
    public static FeishuToolContext CreateContext(string appKey = DocAgentSettings.DefaultAppKey)
        => new(
            AppKey: appKey,
            ConversationKey: "feishu:demo-app:conversation:user:ou_console_demo_user",
            ChatId: "local",
            UserId: DocAgentSettings.DefaultConsoleUserId);

    /// <summary>构造框架审批请求（字段与生产投影同形）。</summary>
    /// <param name="requestId">框架请求标识。</param>
    /// <param name="toolName">工具名。</param>
    /// <returns>审批请求。</returns>
    public static FrameworkToolApprovalRequest CreateApprovalRequest(
        string requestId = "ficc_call_1",
        string toolName = "docx.delete_blocks")
        => new(
            RequestId: requestId,
            ToolName: toolName,
            ToolCallId: "call_1",
            AppKey: DocAgentSettings.DefaultAppKey,
            UserId: DocAgentSettings.DefaultConsoleUserId,
            ConversationKey: "feishu:demo-app:conversation:user:ou_console_demo_user",
            ArgumentsDigest: "document_id=<27 字符>, start_index=1, end_index=6",
            RequiredScopes: ["docx:document"],
            ChatId: null);

    /// <summary>构造一条审计记录。</summary>
    /// <param name="toolName">工具名。</param>
    /// <param name="decision">判定。</param>
    /// <param name="isWrite">是否写操作。</param>
    /// <returns>审计记录。</returns>
    public static ToolExecutionAuditRecord CreateAuditRecord(
        string toolName = "docx.delete_blocks",
        string decision = "denied",
        bool isWrite = true)
        => new(
            ToolName: toolName,
            AppKey: DocAgentSettings.DefaultAppKey,
            RequiredScopes: ["docx:document"],
            Decision: decision,
            Reason: "测试原因",
            IsWrite: isWrite,
            ArgsDigest: "document_id=<27 字符>",
            DurationMs: 12,
            ConversationKey: "feishu:demo-app:conversation:user:ou_console_demo_user");
}
