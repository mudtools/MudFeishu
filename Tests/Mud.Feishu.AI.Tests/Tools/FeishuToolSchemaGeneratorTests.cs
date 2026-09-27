// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Generated;

namespace Mud.Feishu.AI.Tests.Tools;

/// <summary>
/// [FeishuTool] 源生成器产物契约（T1-3）：编译期 Schema 含 name/description/parameters/required
/// 与 required_scopes 权限元数据（已决策⑥）；可空参数不进 required。
/// </summary>
public class FeishuToolSchemaGeneratorTests
{
    [Fact]
    public void SchemaByToolName_ShouldContainAnnotatedTool()
    {
        FeishuToolSchemas.SchemaByToolName.Should().ContainKey("bitable.query_records",
            "标注 [FeishuTool] 的接口必须在编译期产出 Schema 常量");
    }

    [Fact]
    public void GeneratedSchema_ShouldCarryRequiredParameters_AndScopes()
    {
        var schema = FeishuToolSchemas.SchemaByToolName["bitable.query_records"];
        using var document = JsonDocument.Parse(schema);
        var root = document.RootElement;

        root.GetProperty("name").GetString().Should().Be("bitable.query_records");
        root.GetProperty("description").GetString().Should().Contain("多维表格");

        var properties = root.GetProperty("parameters").GetProperty("properties");
        properties.GetProperty("app_token").GetProperty("type").GetString().Should().Be("string");
        properties.GetProperty("app_token").GetProperty("description").GetString().Should().Contain("AppToken");
        properties.GetProperty("table_id").GetProperty("type").GetString().Should().Be("string");

        var required = root.GetProperty("parameters").GetProperty("required")
            .EnumerateArray().Select(e => e.GetString()!).ToArray();
        required.Should().BeEquivalentTo(["app_token", "table_id"],
            "可空参数（filter/view_id/page_token）不得进入 required");

        var scopes = root.GetProperty("x-feishu").GetProperty("required_scopes")
            .EnumerateArray().Select(e => e.GetString()!).ToArray();
        scopes.Should().Contain("bitable:app:readonly", "权限元数据随 Schema 输出（已决策⑥）");
        root.GetProperty("x-feishu").GetProperty("is_write").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void GeneratedSchema_ShouldNotBeReGeneratedFromReflection()
    {
        // AOT 约束：Schema 是编译期字符串常量（生成器产物），不是运行期反射序列化结果。
        var schema = FeishuToolSchemas.SchemaByToolName["bitable.query_records"];
        schema.Should().NotBeNullOrWhiteSpace();
        schema.Should().StartWith("{\"name\":");
    }
}
