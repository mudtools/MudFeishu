// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;

namespace Mud.Feishu.WebSocket.Tests.ContractGuards;

/// <summary>
/// WebSocket 连接级应用绑定守卫（多应用审查）。
/// </summary>
/// <remarks>
/// 拦截的真实回归：<c>FeishuWebSocketOptions.AppKey</c> 原仅作指标维度且默认 <c>"default"</c>，
/// 连接始终取默认应用。多应用装配下，连接会静默绑定默认应用——非默认应用的事件被默认应用凭据处理，
/// 去重键与指标维度也全部错位；且配置重载会把已绑定应用切回默认应用。
/// <para>
/// 不变量：一条 WebSocket 连接只绑定一个应用，绑定键在组合期解析、在客户端构造期固定，
/// 运行期配置重载<b>不得</b>改写绑定。
/// </para>
/// </remarks>
[Trait("Category", "ContractGuard")]
public class WebSocketAppBindingContractGuards
{
    private const string ModuleDirectoryName = "Mud.Feishu.WebSocket";

    /// <summary>
    /// 组合期必须解析并校验绑定键：多应用且未显式配置时 fail-fast。
    /// </summary>
    [Fact]
    public void Composition_ShouldResolveAndValidateAppBindingKey()
    {
        var source = ReadModuleSource("Extensions/FeishuWebSocketServiceBuilder.cs");

        source.Should().Contain("ResolveAppKeyAndValidate(",
            "WebSocket 客户端工厂必须在构造前解析应用绑定键");
        source.Should().Contain("ConfiguredAppKeys",
            "绑定校验必须基于已配置的应用键集合");
        source.Should().Contain("多应用模式下必须显式配置",
            "多应用且未配置绑定键时必须 fail-fast，而不是静默绑定默认应用");
    }

    /// <summary>
    /// 客户端必须在构造期固定绑定键，指标/去重维度不得随配置重载漂移。
    /// </summary>
    [Fact]
    public void Client_ShouldFreezeResolvedAppKeyAtConstruction()
    {
        var source = ReadModuleSource("FeishuWebSocketClient.cs");

        source.Should().Contain("_resolvedAppKey",
            "客户端必须缓存构造期解析的绑定键");
        source.Should().NotContain("Options.AppKey",
            "客户端运行期不得再从 IOptionsMonitor 读取 AppKey——配置重载会把连接切回默认应用");
        source.Should().Contain("ResolveAppKey()",
            "指标/去重维度必须消费构造期固定的绑定键");
    }

    /// <summary>
    /// 管理器必须缓存绑定键，不得在重载后重新按默认应用解析。
    /// </summary>
    [Fact]
    public void Manager_ShouldKeepBoundAppKeyAcrossReloads()
    {
        var source = ReadModuleSource("FeishuWebSocketManager.cs");
        var body = ExtractMethodBody(source, "private IFeishuAppContext ResolveCurrentContext()");

        body.Should().Contain("_boundAppKey",
            "解析结果必须缓存，否则配置重载后会重新走默认应用分支");
        body.Should().Contain("ConfiguredAppKeys.Count > 1",
            "多应用且未显式配置时必须在解析处 fail-fast");
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

    private static string ReadModuleSource(string relativePath)
    {
        var path = Path.Combine(GetRepositoryRoot(), ModuleDirectoryName, relativePath.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(path).Should().BeTrue($"模块源码必须存在：{relativePath}");
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
