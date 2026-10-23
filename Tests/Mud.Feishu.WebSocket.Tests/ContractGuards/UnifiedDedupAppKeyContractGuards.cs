// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;

namespace Mud.Feishu.WebSocket.Tests.ContractGuards;

/// <summary>
/// 统一去重路径的应用维度贯通守卫（多应用审查）。
/// </summary>
/// <remarks>
/// 拦截的真实回归：<c>UnifiedDeduplicationMiddleware</c> 调用事件去重器时不传 <c>appKey</c>，
/// 多应用宿主即使注入了统一中间件，EventId 去重也退化为跨应用共享命名空间——
/// A 应用已处理的事件 ID 会让 B 应用的同名事件被静默跳过。
/// 不变量：WebSocket 事件/二进制两条路径的统一去重调用必须携带连接绑定键。
/// </remarks>
[Trait("Category", "ContractGuard")]
public class UnifiedDedupAppKeyContractGuards
{
    private const string WebSocketModule = "Mud.Feishu.WebSocket";
    private const string AbstractionsModule = "Mud.Feishu.Abstractions";

    /// <summary>
    /// 中间件必须实现应用维度接口，并把 appKey 透传给事件去重器。
    /// </summary>
    [Fact]
    public void Middleware_ShouldImplementAppScopedContract_AndForwardAppKey()
    {
        var source = ReadSource(AbstractionsModule, "Services/UnifiedDeduplicationMiddleware.cs");

        source.Should().Contain("IAppScopedUnifiedDeduplicationMiddleware",
            "中间件必须实现应用维度接口，否则扩展方法只能回退到无 appKey 的旧路径");
        source.Should().Contain("TryMarkAsProcessingAsync(eventId!, appKey",
            "去重检查必须把 appKey 透传给事件去重器");
        source.Should().Contain("MarkAsCompletedAsync(eventId!, appKey",
            "完成标记必须带 appKey，否则完成态写入错误的命名空间");
        source.Should().Contain("RollbackProcessingAsync(eventId!, appKey",
            "回滚必须带 appKey，否则失败重发会被其他应用的 processing 态判重");
    }

    /// <summary>
    /// WebSocket 事件处理器与二进制处理器的统一去重调用必须携带绑定键。
    /// </summary>
    [Fact]
    public void WebSocketPaths_ShouldPassBoundAppKeyToUnifiedDedup()
    {
        var eventHandler = ReadSource(WebSocketModule, "Handlers/FeishuEventMessageHandler.cs");
        var handleBody = ExtractMethodBody(eventHandler, "public override async Task HandleAsync(string message");
        handleBody.Should().Contain("_options.ResolvedAppKey",
            "事件路径的统一去重调用必须携带应用绑定键");
        handleBody.Should().NotContain("seqId: null, cancellationToken)",
            "检测到未带 appKey 的统一去重调用（旧三重载形态）——多应用下会跨应用判重");

        var rollbackBody = ExtractMethodBody(eventHandler, "private async Task RollbackDeduplicationAsync(string eventId");
        rollbackBody.Should().Contain("_options.ResolvedAppKey",
            "回滚同样必须携带应用绑定键");

        var binary = ReadSource(WebSocketModule, "Core/BinaryMessageProcessor.cs");
        var checkBody = ExtractMethodBody(binary, "private async Task ProcessCompleteBinaryMessageAsync");
        checkBody.Should().Contain("_options.ResolvedAppKey",
            "二进制路径的统一去重调用必须携带应用绑定键（同一连接的帧与事件必须同维度）");
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Mud.Feishu.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"未能从 {AppContext.BaseDirectory} 向上定位仓库根目录（Mud.Feishu.slnx）——守卫无法工作，宁可失败也不要假绿");
    }

    private static string ReadSource(string module, string relativePath)
    {
        var path = Path.Combine(GetRepositoryRoot(), module, relativePath.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(path).Should().BeTrue($"模块源码必须存在：{module}/{relativePath}");
        return File.ReadAllText(path);
    }

    private static string ExtractMethodBody(string source, string signatureFragment)
    {
        var sanitized = StripCommentsAndStringLiterals(source);
        var index = sanitized.IndexOf(signatureFragment, StringComparison.Ordinal);
        index.Should().BeGreaterThanOrEqualTo(0,
            $"守卫无法定位签名『{signatureFragment}』——源码结构已被改动，请同步更新守卫（宁可失败也不要假绿）");

        var openBrace = sanitized.IndexOf('{', index);
        openBrace.Should().BeGreaterThanOrEqualTo(0, $"签名『{signatureFragment}』之后未找到方法体起始大括号");

        var depth = 0;
        for (var i = openBrace; i < sanitized.Length; i++)
        {
            if (sanitized[i] == '{')
            {
                depth++;
            }
            else if (sanitized[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return sanitized.Substring(openBrace, i - openBrace + 1);
                }
            }
        }

        throw new InvalidOperationException($"签名『{signatureFragment}』的方法体未能闭合——守卫无法工作");
    }

    private static string StripCommentsAndStringLiterals(string source)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            source,
            @"//.*?$|/\*.*?\*/|@""(?:[^""]|"""")*""|""(?:\\.|[^""\\])*""",
            " ",
            System.Text.RegularExpressions.RegexOptions.Multiline
            | System.Text.RegularExpressions.RegexOptions.Singleline);
    }
}
