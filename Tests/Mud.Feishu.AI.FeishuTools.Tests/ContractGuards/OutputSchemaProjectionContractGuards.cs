// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Mud.Feishu.AI.Tools.Generated;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

/// <summary>
/// WP6/R5 输出投影 Schema 驱动守卫：验证 <c>SchemaProjection</c> 基础设施可用性
/// </summary>
/// <remarks>
/// <para>
/// <b>守卫内容</b>：
/// </para>
/// <list type="number">
/// <item>output_schema 非空的工具可被 <c>SchemaProjection.Project</c> 正确过滤；</item>
/// <item>无 output_schema 的工具（capability_lookup / knowledge.search）不触发推导投影（守卫不强制它们有 output_schema）。</item>
/// </list>
/// <para>
/// <b>不守卫的内容</b>：显式投影 ⊆ output_schema 字段集——这需要在运行期执行投影函数才能比对，
/// 不适合编译期/启动期守卫。该约束由"既有用例 + golden diff"在行为层面锁定
/// （R5 WP6 判据："golden diff 被显式承认"）。
/// </para>
/// </remarks>
public class OutputSchemaProjectionContractGuards
{
    /// <summary>
    /// FeishuToolSchemas 中有 output_schema 的工具，其 output_schema 必须是合法 JSON 对象。
    /// </summary>
    [Fact]
    public void OutputSchemas_ShouldBeValidJsonObjects_WhenPresent()
    {
        var toolsWithSchema = 0;
        var toolsWithoutSchema = 0;

        foreach (var (toolName, schemaJson) in FeishuToolSchemas.SchemaByToolName)
        {
            using var doc = JsonDocument.Parse(schemaJson);
            var root = doc.RootElement;

            // 信封格式：{"name":"...","description":"...","parameters":{...},"x-feishu":{"output_schema":{...}}}
            if (root.TryGetProperty("x-feishu", out var xFeishu) &&
                xFeishu.TryGetProperty("output_schema", out var outputSchema))
            {
                outputSchema.ValueKind.Should().Be(JsonValueKind.Object,
                    $"{toolName} 的 output_schema 必须是 JSON 对象（type=object），实际为 {outputSchema.ValueKind}");
                toolsWithSchema++;
            }
            else
            {
                toolsWithoutSchema++;
            }
        }

        toolsWithSchema.Should().BeGreaterThan(0,
            "至少部分工具应有 output_schema——否则 WP6 Schema 驱动投影无适用面");
    }

    /// <summary>
    /// SchemaProjection.Project 对简单对象应正确过滤掉 output_schema 未声明的字段。
    /// </summary>
    [Fact]
    public void SchemaProjection_ShouldFilterFieldsNotInOutputSchema()
    {
        var outputSchema = """
            {
                "type": "object",
                "properties": {
                    "id": { "type": "string" },
                    "name": { "type": "string" }
                }
            }
            """;

        var data = new System.Text.Json.Nodes.JsonObject
        {
            ["id"] = "rec_001",
            ["name"] = "测试记录",
            ["internal_field"] = "should_be_filtered",
            ["_debug"] = 42,
        };

        var projected = Mud.Feishu.AI.FeishuTools.Internal.SchemaProjection.Project(data, outputSchema);

        projected.ContainsKey("id").Should().BeTrue();
        projected.ContainsKey("name").Should().BeTrue();
        projected.ContainsKey("internal_field").Should().BeFalse();
        projected.ContainsKey("_debug").Should().BeFalse();
    }

    /// <summary>
    /// SchemaProjection.Project 对数组应递归过滤每个元素的字段。
    /// </summary>
    [Fact]
    public void SchemaProjection_ShouldRecurseIntoArrays()
    {
        var outputSchema = """
            {
                "type": "object",
                "properties": {
                    "items": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "properties": {
                                "sheet_id": { "type": "string" },
                                "title": { "type": "string" }
                            }
                        }
                    }
                }
            }
            """;

        var data = new System.Text.Json.Nodes.JsonObject
        {
            ["items"] = new System.Text.Json.Nodes.JsonArray
            {
                new System.Text.Json.Nodes.JsonObject
                {
                    ["sheet_id"] = "Sht1",
                    ["title"] = "Sheet1",
                    ["hidden"] = true,
                },
                new System.Text.Json.Nodes.JsonObject
                {
                    ["sheet_id"] = "Sht2",
                    ["title"] = "Sheet2",
                    ["hidden"] = false,
                },
            },
        };

        var projected = Mud.Feishu.AI.FeishuTools.Internal.SchemaProjection.Project(data, outputSchema);

        var items = projected["items"]!.AsArray();
        items.Should().HaveCount(2);
        items[0]!["sheet_id"]!.GetValue<string>().Should().Be("Sht1");
        items[0]!["title"]!.GetValue<string>().Should().Be("Sheet1");
        items[0]!.AsObject().ContainsKey("hidden").Should().BeFalse();
        items[1]!["sheet_id"]!.GetValue<string>().Should().Be("Sht2");
    }

    /// <summary>
    /// SchemaProjection.Project 对空 output_schema 应原样返回（安全默认：不过滤）。
    /// </summary>
    [Fact]
    public void SchemaProjection_ShouldReturnAsIs_WhenOutputSchemaIsEmpty()
    {
        var data = new System.Text.Json.Nodes.JsonObject
        {
            ["id"] = "rec_001",
            ["name"] = "测试记录",
        };

        var projectedNull = Mud.Feishu.AI.FeishuTools.Internal.SchemaProjection.Project(data, null);
        projectedNull["id"]!.GetValue<string>().Should().Be("rec_001");
        projectedNull["name"]!.GetValue<string>().Should().Be("测试记录");

        var projectedEmpty = Mud.Feishu.AI.FeishuTools.Internal.SchemaProjection.Project(data, "");
        projectedEmpty["id"]!.GetValue<string>().Should().Be("rec_001");
    }

    /// <summary>
    /// SchemaProjection.Project 对无 properties 的 schema 应透传（不因 schema 缺失而丢字段）。
    /// </summary>
    [Fact]
    public void SchemaProjection_ShouldPassthrough_WhenSchemaHasNoProperties()
    {
        var outputSchema = """{"type":"object"}""";

        var data = new System.Text.Json.Nodes.JsonObject
        {
            ["id"] = "rec_001",
            ["name"] = "测试记录",
            ["extra"] = "should_be_preserved",
        };

        var projected = Mud.Feishu.AI.FeishuTools.Internal.SchemaProjection.Project(data, outputSchema);

        projected["id"]!.GetValue<string>().Should().Be("rec_001");
        projected["extra"]!.GetValue<string>().Should().Be("should_be_preserved");
    }
}
