// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Mud.Feishu.EventCallback;
using Match = System.Text.RegularExpressions.Match;

namespace Mud.Feishu.Abstractions.Tests.ContractGuards;

/// <summary>
/// EventCallback 事件覆盖治理守卫：常量接线、JsonContext 覆盖、文件名/类名一致、生成器落地与 README 同步。
/// </summary>
/// <remarks>
/// 事件回调「补齐」最容易回归的不是编译错误，而是**静默不生效**：常量存在却没有 <c>[GenerateEventHandler]</c>
/// 接线、新域忘挂 resolver 导致运行期 AOT005、<c>*JsonContext.g.cs</c> 忘记再生成。
/// 这些失败在编译期看不出来，因此这里同时做源码扫描（接线面）与反射断言（产物面）。
/// </remarks>
public class EventCallbackContractGuards
{
    private const string HandlerNs = "Mud.Feishu.EventCallback";

    // ────────────────────────────────────────────────────────────────────
    // 守卫 A：事件类型常量必须被接线（防止「定义了常量却永远不会触发处理器」）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 每个非 <c>[Obsolete]</c> 的 <see cref="FeishuEventTypes"/> 常量，必须被某个模型的
    /// <c>[GenerateEventHandler(EventType = FeishuEventTypes.X)]</c> 引用。
    /// </summary>
    [Fact]
    public void FeishuEventTypes_ShouldAllBeWired_ByGenerateEventHandler()
    {
        var declared = EventTypeNameConstants(includeObsolete: false);
        var wired = WiredConstants();

        declared.Should().NotBeEmpty("反射未读到常量说明引用了错误的程序集");
        declared.Where(c => !wired.Contains(c)).Should().BeEmpty(
            "未在 [GenerateEventHandler] 中引用的事件常量不会产生处理器（死常量）；" +
            "若确需下线请标 [Obsolete] 并注明替代项。");
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 B：模型文件名必须与类名一致（防止重命名后残留旧类名）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 每个模型文件的**主类**（首个顶层 <c>public class</c>）必须与文件名一致。
    /// </summary>
    /// <remarks>
    /// 只约束主类：历史上 8 个模型「文件名与类名不符」会导致 <c>Generated/*JsonContext.g.cs</c> 里的
    /// <c>[JsonSerializable]</c> 与实类漂移；同文件内的附属类型（如 <c>Mail/MessageReceivedResult.cs</c>
    /// 里的 <c>MailSubscriber</c>）不属该规则，保持原状以免改动公共 API 布局。
    /// </remarks>
    [Fact]
    public void EventModelFile_ShouldDeclareClassWithMatchingFileName()
    {
        var offenders = new List<string>();
        foreach (var file in ModelFiles())
        {
            var m = Regex.Match(File.ReadAllText(file), @"^public class (\w+)", RegexOptions.Multiline);
            if (m.Success && Path.GetFileNameWithoutExtension(file) != m.Groups[1].Value)
                offenders.Add($"{file} → class {m.Groups[1].Value}");
        }

        offenders.Should().BeEmpty("文件名必须与主类名一致（历史上有 8 个模型重命名后类名未同步）。");
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 C：每个 SerializerClassName 都要有 JsonContext 且已挂进 resolver 链
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>[HttpJsonSerializable(SerializerClassName = "X")]</c> 出现的每个域 X，必须存在
    /// <c>Generated/XJsonContext.g.cs</c>，并在 <c>FeishuEventCallbackJsonResolverExtensions</c> 中
    /// 同时具备 <c>using</c> 与 <c>XJsonContext.Default</c>。
    /// </summary>
    [Fact]
    public void JsonContexts_ShouldCoverAllSerializerClassNames()
    {
        var domains = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var file in ModelFiles())
            foreach (var m in Regex.Matches(File.ReadAllText(file), @"SerializerClassName\s*=\s*""([^""]+)""").Cast<Match>())
                domains.Add(m.Groups[1].Value);

        domains.Should().NotBeEmpty("未扫描到任何 [HttpJsonSerializable] 模型，多半是目录定位失效");

        var resolver = File.ReadAllText(Path.Combine(EventCallbackDir, "Extensions", "FeishuEventCallbackJsonResolverExtensions.cs"));
        var missing = new List<string>();
        foreach (var d in domains)
        {
            if (!File.Exists(Path.Combine(EventCallbackDir, "Generated", $"{d}JsonContext.g.cs")))
                missing.Add($"{d}: 缺少 Generated/{d}JsonContext.g.cs（请运行 scripts/GenerateJsonContext.ps1）");
            if (!resolver.Contains($@"using Mud.Feishu.EventCallback.{d};", StringComparison.Ordinal))
                missing.Add($"{d}: resolver 缺少 using");
            if (!resolver.Contains($"{d}JsonContext.Default", StringComparison.Ordinal))
                missing.Add($"{d}: resolver 的 JsonTypeInfoResolver.Combine 未包含该 Context");
        }

        missing.Should().BeEmpty("未挂载的域在 AOT/源生成解析下会在运行期抛 AOT005（嵌套类型不在同一 Context）。");
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 D：生成器必须真的产出处理器（产物面断言）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 程序集中每个 <see cref="IEventResult"/> 实现都必须有对应的 <c>{去 Result}EventHandler</c>。
    /// </summary>
    /// <remarks>
    /// 源码写了 <c>[GenerateEventHandler]</c> 不代表生成成功（属性拼错/常量缺失时源生成器会静默跳过）。
    /// 这里断言的是编译产物，能抓住「编译通过但处理器类不存在」。
    /// </remarks>
    [Fact]
    public void EventResultModel_ShouldHaveGeneratedEventHandler()
    {
        var assembly = typeof(FeishuEventTypes).Assembly;
        var handlers = new HashSet<string>(assembly.GetTypes().Select(t => t.FullName!), StringComparer.Ordinal);

        var missing = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IEventResult).IsAssignableFrom(t))
            .Where(t => !t.Name.EndsWith("Result", StringComparison.Ordinal))
            .Select(t => $"{t.FullName}: 命名未遵循 *Result").ToList();

        foreach (var t in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && typeof(IEventResult).IsAssignableFrom(t)
                     && t.Name.EndsWith("Result", StringComparison.Ordinal)))
        {
            var handler = $"{HandlerNs}.{t.Name.Substring(0, t.Name.Length - "Result".Length)}EventHandler";
            if (!handlers.Contains(handler)) missing.Add($"{t.FullName} → 缺少 {handler}");
        }

        missing.Should().BeEmpty("[GenerateEventHandler] 声明后必须能在程序集中找到处理器类。");
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 E：README 事件表与常量表同步（文档不得落后于代码）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 每个已接线的事件类型值都必须出现在 <c>Mud.Feishu.EventCallback/README.md</c> 的事件表中。
    /// </summary>
    [Fact]
    public void EventTypeValues_ShouldBeDocumentedInReadme()
    {
        var readme = File.ReadAllText(Path.Combine(EventCallbackDir, "README.md"));
        var fields = typeof(FeishuEventTypes).GetFields(BindingFlags.Public | BindingFlags.Static);
        var wired = WiredConstants();

        var missing = fields
            .Where(f => f.IsLiteral && f.FieldType == typeof(string) && wired.Contains(f.Name))
            .Select(f => (string)f.GetRawConstantValue()!)
            .Where(v => !readme.Contains(v, StringComparison.Ordinal))
            .ToList();

        missing.Should().BeEmpty("README 的事件表由源码汇总，新增事件后需同步 README.md。");
    }

    // ────────────────────────────────────────────────────────────────────

    private static HashSet<string> WiredConstants()
    {
        var wired = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in ModelFiles())
            foreach (var m in Regex.Matches(File.ReadAllText(file), @"EventType\s*=\s*FeishuEventTypes\.(\w+)").Cast<Match>())
                wired.Add(m.Groups[1].Value);
        return wired;
    }

    private static List<string> EventTypeNameConstants(bool includeObsolete)
    {
        var fields = typeof(FeishuEventTypes).GetFields(BindingFlags.Public | BindingFlags.Static);
        return fields
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Where(f => includeObsolete || f.GetCustomAttribute<ObsoleteAttribute>() == null)
            .Select(f => f.Name)
            .ToList();
    }

    private static readonly string? Root = GetSolutionRoot();
    private static string EventCallbackDir => Path.Combine(Root ?? ".", "Mud.Feishu.EventCallback");

    private static IEnumerable<string> ModelFiles()
    {
        Root.Should().NotBeNull("找不到解决方案根目录时本守卫无法工作（宁可失败，也不要假绿）");
        var sep = Path.DirectorySeparatorChar;
        return Directory.GetFiles(EventCallbackDir, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{sep}bin{sep}", StringComparison.Ordinal)
                        && !p.Contains($"{sep}obj{sep}", StringComparison.Ordinal)
                        && !p.Contains($"{sep}Generated{sep}", StringComparison.Ordinal));
    }

    private static string? GetSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir) && !File.Exists(Path.Combine(dir, "Mud.Feishu.slnx")))
        {
            dir = Path.GetDirectoryName(dir);
        }
        return dir;
    }
}
