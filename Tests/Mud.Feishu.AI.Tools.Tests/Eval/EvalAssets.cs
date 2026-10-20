// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.Feishu.AI.Tools.Tests.Eval;

/// <summary>工具选择评测用例：一句真实用户说法 + 可接受的目标工具集（任一命中即算选对）。</summary>
internal sealed record SelectionCase(string Id, string Domain, string Intent, IReadOnlyList<string> Expect);

/// <summary>参数生成评测用例：<c>layer</c> = schema（对着 Schema 校）/ handler（交给真实执行链判）。</summary>
internal sealed record ParameterCase(
    string Id,
    string Tool,
    string Layer,
    string Expect,
    string Why,
    IReadOnlyDictionary<string, object?> Arguments);

/// <summary>场景步骤；<paramref name="ApproveAfterHost"/> = 宿主侧模拟「用户已批准」后重调本步。</summary>
internal sealed record ScenarioStep(
    string Tool,
    IReadOnlyDictionary<string, object?>? Arguments,
    string Expect,
    bool ApproveAfterHost);

/// <summary>端到端场景：按 <paramref name="Profile"/> 装配执行链后逐步判定。</summary>
internal sealed record ScenarioCase(string Name, string Profile, IReadOnlyList<ScenarioStep> Steps);

/// <summary>
/// 评测集装载器：三份 JSON 资源（<c>Eval/*_cases.json</c>）→ 强类型用例。
/// </summary>
/// <remarks>
/// 评测集是<b>资产</b>而不是代码：新增工具/改描述时只动 JSON，用例代码不变。
/// 路径经 <see cref="RepoRoot"/> 定位（与 golden 快照同一先例），保证「报告可复现」
/// 不依赖测试运行目录。
/// </remarks>
internal static class EvalAssets
{
    private const string SelectionPath = "Tests/Mud.Feishu.AI.Tools.Tests/Eval/selection_cases.json";
    private const string ParameterPath = "Tests/Mud.Feishu.AI.Tools.Tests/Eval/parameter_cases.json";
    private const string ScenarioPath = "Tests/Mud.Feishu.AI.Tools.Tests/Eval/scenario_cases.json";

    private static readonly Lazy<IReadOnlyList<SelectionCase>> SelectionCases = new(LoadSelection);
    private static readonly Lazy<IReadOnlyList<ParameterCase>> ParameterCases = new(LoadParameters);
    private static readonly Lazy<IReadOnlyList<ScenarioCase>> ScenarioCases = new(LoadScenarios);

    internal static IReadOnlyList<SelectionCase> Selection => SelectionCases.Value;
    internal static IReadOnlyList<ParameterCase> Parameters => ParameterCases.Value;
    internal static IReadOnlyList<ScenarioCase> Scenarios => ScenarioCases.Value;

    /// <summary>把 Schema 形态的 JSON 对象（模型 tool_call 的 arguments）转成执行链的参数字典。</summary>
    internal static Dictionary<string, object?> ToArguments(JsonElement obj)
    {
        var arguments = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in obj.EnumerateObject())
        {
            arguments[property.Name] = ToValue(property.Value);
        }

        return arguments;
    }

    private static object? ToValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        // 三元分支必须逐支装箱成 object：否则 int/int64/double 的公共类型是 double，
        // 整型入参会在 Schema 面被误判成 number（评测集与真实 tool_call 就此脱节）。
        JsonValueKind.Number => value.TryGetInt32(out var int32) ? int32
            : value.TryGetInt64(out var int64) ? (object)int64
            : value.GetDouble(),
        JsonValueKind.Array => value.EnumerateArray().Select(ToValue).ToArray(),
        JsonValueKind.Object => ToArguments(value),
        _ => value.GetRawText(),
    };

    private static JsonDocument Open(string repositoryRelativePath)
    {
        var path = Path.Combine(RepoRoot(), repositoryRelativePath.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(path).Should().BeTrue(
            $"评测集资源必须随仓库存在（缺失即评测静默退化为空集假绿）：{repositoryRelativePath}");
        return JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8), new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });
    }

    private static IReadOnlyList<SelectionCase> LoadSelection()
    {
        using var document = Open(SelectionPath);
        var cases = new List<SelectionCase>();
        foreach (var item in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var expect = item.GetProperty("expect").EnumerateArray()
                .Select(static e => e.GetString()!)
                .ToArray();
            expect.Should().NotBeEmpty($"选择用例 {item.GetProperty("id").GetString()} 必须给出可接受的目标工具");
            cases.Add(new SelectionCase(
                item.GetProperty("id").GetString()!,
                item.GetProperty("domain").GetString()!,
                item.GetProperty("intent").GetString()!,
                expect));
        }

        return cases;
    }

    private static IReadOnlyList<ParameterCase> LoadParameters()
    {
        using var document = Open(ParameterPath);
        var cases = new List<ParameterCase>();
        foreach (var item in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            cases.Add(new ParameterCase(
                item.GetProperty("id").GetString()!,
                item.GetProperty("tool").GetString()!,
                item.GetProperty("layer").GetString()!,
                item.GetProperty("expect").GetString()!,
                item.GetProperty("why").GetString()!,
                ToArguments(item.GetProperty("args"))));
        }

        return cases;
    }

    private static IReadOnlyList<ScenarioCase> LoadScenarios()
    {
        using var document = Open(ScenarioPath);
        var scenarios = new List<ScenarioCase>();
        foreach (var item in document.RootElement.GetProperty("scenarios").EnumerateArray())
        {
            var steps = new List<ScenarioStep>();
            foreach (var step in item.GetProperty("steps").EnumerateArray())
            {
                var hasArgs = step.TryGetProperty("args", out var args) && args.ValueKind == JsonValueKind.Object;
                steps.Add(new ScenarioStep(
                    step.GetProperty("tool").GetString()!,
                    hasArgs ? ToArguments(args) : null,
                    step.GetProperty("expect").GetString()!,
                    step.TryGetProperty("approve", out var approve) && approve.GetBoolean()));
            }

            scenarios.Add(new ScenarioCase(
                item.GetProperty("name").GetString()!,
                item.GetProperty("profile").GetString()!,
                steps));
        }

        return scenarios;
    }

    /// <summary>从测试输出目录向上定位仓库根（与 <c>FeishuToolGoldenTests</c> 同法）。</summary>
    internal static string RepoRoot()
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

        throw new DirectoryNotFoundException($"未能从 {AppContext.BaseDirectory} 向上定位仓库根（Mud.Feishu.slnx）");
    }
}
