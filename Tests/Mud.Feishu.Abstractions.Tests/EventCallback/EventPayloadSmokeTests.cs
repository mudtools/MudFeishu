// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

#if NET8_0_OR_GREATER
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FluentAssertions;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.EventCallback;
using Mud.Feishu.EventCallback.Extensions;

namespace Mud.Feishu.Abstractions.Tests.EventCallback;

/// <summary>
/// 事件负载冒烟测试：用官方文档「事件体示例」原文回放每个事件模型。
/// </summary>
/// <remarks>
/// 事件建模最容易出的错不是编译失败，而是**字段名对不上**（驼峰/下划线写错、嵌套层放错位置），
/// 这类问题在运行期表现为字段静默为 null。这里把官方示例 JSON 反序列化进模型再序列化回来，
/// 要求示例里出现的每个非空键都能被模型接住，等价于对官方样例的 1:1 覆盖校验。
/// 数据文件 <c>TestData/EventCallback/event-examples.json</c> 由官方文档抓取生成，
/// 值为事件体 <c>event</c> 节点（schema 2.0 的头字段由 <c>FeishuEventHeader</c> 承担，不在建模范围）。
/// </remarks>
public class EventPayloadSmokeTests
{
    private static readonly Dictionary<string, Type> ModelByEventType = BuildMap();

    static EventPayloadSmokeTests() => FeishuEventCallbackJsonResolverExtensions.ConfigureEventCallbackResolver();

    [Fact]
    public void ExamplePayloadFile_ShouldCoverEveryWiredEventType()
    {
        var examples = Examples().Select(kv => kv.Key).ToHashSet(StringComparer.Ordinal);
        var unmodeled = examples.Where(k => !ModelByEventType.ContainsKey(k)).ToList();

        unmodeled.Should().BeEmpty(
            "示例数据里的事件类型必须能在源码中找到对应的 [GenerateEventHandler] 模型；" +
            "反之不要求——存量事件可以没有回放数据，新增事件时补 TestData/EventCallback/event-examples.json 即可。");
    }

    [Theory]
    [MemberData(nameof(EventTypes))]
    public void ExamplePayload_ShouldDeserialize_IntoEventModel(string eventType)
    {
        var model = ModelByEventType[eventType];
        var json = Example(eventType).ToJsonString();

        var result = JsonSerializer.Deserialize(json, FeishuJsonDefaults.SerializerOptions.GetTypeInfo(model));

        result.Should().NotBeNull($"{eventType} 的示例事件体必须能反序列化进 {model.Name}");
        result.Should().BeAssignableTo<IEventResult>();
    }

    [Theory]
    [MemberData(nameof(EventTypes))]
    public void ExamplePayload_KeysShouldAllBeModeled(string eventType)
    {
        var model = ModelByEventType[eventType];
        var input = Example(eventType);

        var node = JsonSerializer.Deserialize(input.ToJsonString(), FeishuJsonDefaults.SerializerOptions.GetTypeInfo(model));
        var output = JsonSerializer.SerializeToNode(node, FeishuJsonDefaults.SerializerOptions.GetTypeInfo(model));

        var missing = new List<string>();
        CollectMissingKeys(input, output, eventType, missing);

        missing.Should().BeEmpty("示例事件体中的键未被模型接收（字段名或嵌套层级与文档不一致）");
    }

    public static TheoryData<string> EventTypes()
    {
        var data = new TheoryData<string>();
        foreach (var key in Examples().Select(kv => kv.Key).OrderBy(k => k, StringComparer.Ordinal)) data.Add(key);
        return data;
    }

    private static JsonNode Example(string eventType) =>
        Examples()[eventType] ?? throw new InvalidOperationException($"示例数据 {eventType} 的事件体为 null");

    // .NET 8 把 JSON null 解析成 C# null，.NET 9+ 引入 JsonNull（JsonValue 派生）——两种表示都要跳过
    private static bool IsNull(JsonNode? node)
    {
        if (node is null) return true;
        try { return node.GetValue<JsonElement>().ValueKind == JsonValueKind.Null; }
        catch (InvalidOperationException) { return false; }
    }

    private static void CollectMissingKeys(JsonNode? input, JsonNode? output, string path, List<string> missing)
    {
        switch (input)
        {
            case JsonObject io:
            {
                if (output is not JsonObject oo)
                {
                    if (io.Count > 0) missing.Add(path);
                    return;
                }
                foreach (var (key, value) in io)
                {
                    if (IsNull(value)) continue;
                    if (!oo.ContainsKey(key)) missing.Add($"{path}.{key}");
                    else CollectMissingKeys(value, oo[key], $"{path}.{key}", missing);
                }
                return;
            }
            case JsonArray ia when ia.Count > 0:
            {
                if (output is not JsonArray oa)
                {
                    missing.Add($"{path}[]");
                    return;
                }
                for (var i = 0; i < ia.Count; i++)
                {
                    if (i >= oa.Count) break;
                    CollectMissingKeys(ia[i], oa[i], $"{path}[{i}]", missing);
                }
                return;
            }
        }
    }

    private static JsonNode? _examples;

    private static JsonObject Examples()
    {
        if (_examples is null)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "TestData", "EventCallback", "event-examples.json");
            _examples = JsonNode.Parse(File.ReadAllText(path)) ?? throw new InvalidOperationException($"示例数据文件为空：{path}");
        }
        return (JsonObject)_examples;
    }

    private static Dictionary<string, Type> BuildMap()
    {
        var root = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(root) && !File.Exists(Path.Combine(root, "Mud.Feishu.slnx"))) root = Path.GetDirectoryName(root);
        var dir = Path.Combine(root ?? ".", "Mud.Feishu.EventCallback");
        var sep = Path.DirectorySeparatorChar;
        var map = new Dictionary<string, Type>(StringComparer.Ordinal);
        var assembly = typeof(FeishuEventTypes).Assembly;
        var constValues = typeof(FeishuEventTypes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .ToDictionary(f => f.Name, f => (string)f.GetRawConstantValue()!, StringComparer.Ordinal);

        foreach (var file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)
                     .Where(p => !p.Contains($"{sep}bin{sep}") && !p.Contains($"{sep}obj{sep}") && !p.Contains($"{sep}Generated{sep}")))
        {
            var text = File.ReadAllText(file);
            var wired = Regex.Match(text, @"EventType\s*=\s*FeishuEventTypes\.(\w+)");
            var cls = Regex.Match(text, @"^public class (\w+)", RegexOptions.Multiline);
            if (!wired.Success || !cls.Success) continue;
            if (!constValues.TryGetValue(wired.Groups[1].Value, out var eventType)) continue;
            // 模型位于 Mud.Feishu.EventCallback.{Domain}，类名带域前缀故在程序集内唯一
            var candidates = assembly.GetTypes()
                .Where(t => t.IsClass && t.Name == cls.Groups[1].Value && typeof(IEventResult).IsAssignableFrom(t))
                .ToList();
            candidates.Count.Should().Be(1, $"{cls.Groups[1].Value} 应在程序集中唯一且实现 IEventResult");
            map[eventType] = candidates[0];
        }

        return map;
    }
}
#endif
