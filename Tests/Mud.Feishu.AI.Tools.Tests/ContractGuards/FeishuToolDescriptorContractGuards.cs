// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Mud.Feishu.AI.Tools.Generated;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// 工具描述符契约守卫（§4.6.5.5）：验证生成器产出的描述符质量。
/// </summary>
/// <remarks>
/// 守卫项：
/// <list type="bullet">
/// <item>每个描述符的 Output 非空（或显式豁免）。</item>
/// <item>FeishuToolSchemas.SchemaByToolName 的键集与描述符表键集一致。</item>
/// <item>risk == high-risk-write 时 input schema 含 confirm。</item>
/// <item>每个工具的 name 与 FeishuToolNames 契约名一致。</item>
/// <item>每个工具的 description 非空。</item>
/// <item>每个工具的 parameters 含 type=object 且 properties 非空。</item>
/// </list>
/// </remarks>
public class FeishuToolDescriptorContractGuards
{
    /// <summary>
    /// SchemaByToolName 的键集必须与 FeishuToolNames.All 完全一致
    /// （防增删/改名漂移——生成器产出与契约表任何不一致都必须可见）。
    /// </summary>
    [Fact]
    public void SchemaByToolName_Keys_ShouldMatchFeishuToolNamesAll()
    {
        SchemaByToolName.Keys.Should().BeEquivalentTo(FeishuToolNames.All,
            "FeishuToolSchemas.SchemaByToolName 的键集必须与 FeishuToolNames.All 完全一致" +
            "（生成器产出与契约表的漂移必须可见）");
    }

    // 原 AllTools_ShouldHaveNonEmptyDescription 已删除（测试体系治理 B2，见
    // 《单元测试体系过度测试分析与解决方案》§三.5）：它是 **MUDFT005 的运行时等价断言**
    // （方法 XML 注释自认），MUDFT005 为 Warning 恒 0 集，由 CI / verify-build -DenyToolWarnings
    // 在构建期断言——运行期副本不提供增量信息。

    /// <summary>
    /// 每个工具的 parameters 必须含 type=object 且 properties 非空
    /// （防生成器漏产参数表导致模型无法调用）。
    /// </summary>
    [Fact]
    public void AllTools_ShouldHaveValidParametersSchema()
    {
        foreach (var (toolName, schemaJson) in SchemaByToolName)
        {
            using var document = JsonDocument.Parse(schemaJson);
            var root = document.RootElement;

            root.TryGetProperty("parameters", out var paramsElement).Should().BeTrue(
                $"工具 {toolName} 必须有 parameters 字段");

            paramsElement.GetProperty("type").GetString().Should().Be("object",
                $"工具 {toolName} 的 parameters.type 必须为 object");

            paramsElement.TryGetProperty("properties", out var propsElement).Should().BeTrue(
                $"工具 {toolName} 必须有 parameters.properties");

            // 零参数的唯一合法理由是：**平台侧本来就没有入参**（不是"为了绕门禁"）。判据机械化：
            //   · 非 SDK 工具（无 Source）：KnowledgeSearch 走检索引擎语义匹配，无业务入参；
            //   · SDK 工具：其源方法除 CancellationToken 外**没有任何参数**（反射判定，见下方 helper）。
            // 这条规则取代了原先"逐个登记工具名"的清单——清单会随新域增长变成垃圾桶，
            // 而反射判据对新域自动生效、且骗不过去（源方法有参数就必须把参数给模型）。
            if (toolName != FeishuToolNames.KnowledgeSearch && !SourceMethodTakesNoParameters(schemaJson))
            {
                propsElement.EnumerateObject().Count().Should().BeGreaterThan(0,
                    $"工具 {toolName} 的 parameters.properties 不得为空——"
                    + "除非其 SDK 源方法本身无入参（Source 声明里能看出来），或它是 KnowledgeSearch 这类非 SDK 工具");
            }
        }
    }

    /// <summary>
    /// 工具的 <c>x-feishu.source.sdk</c>（形如 <c>Iface.Method</c>）所指方法是否<b>除 CancellationToken 外无参数</b>。
    /// </summary>
    /// <remarks>
    /// <b>失败方向是刻意选的</b>：解析失败 / 无 Source / 反射找不到 ⇒ 一律返回 <c>false</c>
    /// （"无法证明无入参"就按有入参要求）。这样反射或格式一旦写坏，会立刻有一批工具报红，
    /// 而不是静默放行零参数工具——守卫宁可吵，不可哑。
    /// </remarks>
    internal static bool SourceMethodTakesNoParameters(string schemaJson)
    {
        using var document = JsonDocument.Parse(schemaJson);
        if (!document.RootElement.TryGetProperty("x-feishu", out var extension)
            || !extension.TryGetProperty("source", out var source)
            || !source.TryGetProperty("sdk", out var sdk)
            || sdk.GetString() is not { Length: > 0 } reference)
        {
            return false;
        }

        var separator = reference.LastIndexOf('.');
        if (separator <= 0 || separator == reference.Length - 1)
        {
            return false;
        }

        var interfaceName = reference[..separator];
        var methodName = reference[(separator + 1)..];

        var interfaceType = typeof(Mud.Feishu.IFeishuTenantV1HelpDeskFaq).Assembly
            .GetTypes()
            .FirstOrDefault(type => type.IsInterface && string.Equals(type.Name, interfaceName, StringComparison.Ordinal));

        if (interfaceType is null)
        {
            return false;
        }

        // 方法可能声明在**基接口**上（如 IFeishuV1LingoRepo 上的 GetRepoListAsync），故把继承链一起扫。
        var method = interfaceType.GetInterfaces()
            .Prepend(interfaceType)
            .SelectMany(static type => type.GetMethods())
            .FirstOrDefault(candidate => string.Equals(candidate.Name, methodName, StringComparison.Ordinal));

        return method is not null
            && method.GetParameters().All(static parameter => parameter.ParameterType == typeof(CancellationToken));
    }

    /// <summary>
    /// 每个工具的 name 必须与 FeishuToolNames 契约名逐一匹配
    /// （防生成器产出名与契约名不一致）。
    /// </summary>
    [Fact]
    public void AllToolNamesInSchema_ShouldMatchContractNames()
    {
        foreach (var (toolName, schemaJson) in SchemaByToolName)
        {
            using var document = JsonDocument.Parse(schemaJson);
            var root = document.RootElement;

            root.GetProperty("name").GetString().Should().Be(toolName,
                "Schema 中的 name 字段必须与 SchemaByToolName 的键一致");

            FeishuToolNames.All.Should().Contain(toolName,
                $"工具名 {toolName} 必须在 FeishuToolNames.All 契约表中注册");
        }
    }

    /// <summary>
    /// 写类工具的 x-feishu.is_write 必须为 true，
    /// 只读工具必须为 false（防读写标记倒挂绕过授权门禁）。
    /// </summary>
    [Fact]
    public void IsWriteFlag_ShouldMatchReadWriteClassification()
    {
        foreach (var (toolName, schemaJson) in SchemaByToolName)
        {
            using var document = JsonDocument.Parse(schemaJson);
            var root = document.RootElement;

            var isWrite = root.GetProperty("x-feishu").GetProperty("is_write").GetBoolean();
            var expectedWrite = FeishuToolNames.IsWriteTool(toolName);

            isWrite.Should().Be(expectedWrite,
                $"工具 {toolName} 的 is_write 标记必须与契约读写分类一致" +
                $"（读写标记倒挂会绕过授权门禁）");
        }
    }

    /// <summary>
    /// 每个工具必须声明非空的 required_scopes（§4.6.5.5：scope 随 Schema 供授权钩子与审计消费）。
    /// </summary>
    /// <remarks>
    /// <b>豁免（AT-F12 收窄，R3 评审）</b>：唯一豁免是<b>不映射任何飞书 API</b>的元工具
    /// （<c>feishu.capability_lookup</c> 只读编译期目录常量）。给它挂一个凭空的平台 scope 是
    /// 对宿主的误导——审计会以为需要开权限，而实际它一个飞书请求都不发。
    /// 豁免以<b>显式白名单</b>表达（而非"无 source 就放行"），因为 <c>knowledge.search</c> 同样无
    /// <c>Source</c>（绑定本地 <c>IRetriever</c> 门面）却真实需要 <c>aily:knowledge:readonly</c>。
    /// </remarks>
    [Fact]
    public void AllTools_ShouldHaveNonEmptyRequiredScopes()
    {
        // 不映射飞书 API 的元工具白名单（每增一项都要说明"为什么它不需要任何平台权限"）。
        var scopeExemptTools = new[] { FeishuToolNames.FeishuCapabilityLookup };

        foreach (var (toolName, schemaJson) in SchemaByToolName)
        {
            using var document = JsonDocument.Parse(schemaJson);
            var root = document.RootElement;

            var scopes = root.GetProperty("x-feishu").GetProperty("required_scopes")
                .EnumerateArray().Select(e => e.GetString()!).ToArray();

            scopes.Should().NotContainNulls(
                $"工具 {toolName} 的 required_scopes 中不得有 null 元素");

            if (scopeExemptTools.Contains(toolName, StringComparer.Ordinal))
            {
                scopes.Should().BeEmpty(
                    $"工具 {toolName} 不映射飞书 API，其 required_scopes 必须为空——"
                    + "声明占位 scope 会让宿主审计误以为需要开权限");
                continue;
            }

            scopes.Should().NotBeEmpty(
                $"工具 {toolName} 必须声明非空 required_scopes（授权钩子与审计消费）");
        }
    }

    /// <summary>
    /// 生成器产出非空（工具数 > 0）——先断言产出，再断言诊断
    /// （AGENTS.md 已记录过"只断言诊断计数而构建失败即假绿"的教训）。
    /// </summary>
    [Fact]
    public void SchemaByToolName_ShouldNotBeEmpty()
    {
        SchemaByToolName.Should().NotBeEmpty(
            "生成器产出为空意味着源生成器未扫描到任何 [AgentTool] 接口——" +
            "先断言产出非空，再断言诊断（防假绿）");

        SchemaByToolName.Count.Should().BeGreaterThan(0,
            "工具数必须 > 0（生成器至少应产出 FeishuToolNames.All 中的工具）");
    }

    private static IReadOnlyDictionary<string, string> SchemaByToolName => FeishuToolSchemas.SchemaByToolName;
}
