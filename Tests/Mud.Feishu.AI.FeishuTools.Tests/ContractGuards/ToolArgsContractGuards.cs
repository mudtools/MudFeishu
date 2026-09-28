// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

/// <summary>
/// 参数解包产物契约守卫（<c>ToolArgsEmitter</c> → <c>FeishuToolArgs/{Tool}Args.g.cs</c>，每类型一文件）：断言每枚契约工具都有
/// 生成的 <c>{Tool}Args</c> 类型、其字段与 Schema 参数<b>逐一对齐</b>，且 <c>Unpack</c> 的必填校验
/// 与可选读取语义正确——这是"执行器不再手写参数名字面量"的前提。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它</b>：迁移执行器首部（<c>ToolArgs.*(arguments, "…")</c> → <c>XxxArgs.Unpack</c>）把
/// 参数名的<b>声明源</b>收敛到接口，但收敛后的产物本身仍可能漏工具/漏字段——守卫把"生成器产物
/// ⇒ Schema 契约"钉成机械约束（防"新增工具忘改生成器"或"生成器映射表与参数形态漂移"）。
/// </para>
/// <para>
/// 测试工程经 <c>InternalsVisibleTo</c> 可见生成的 <c>internal</c> 类型，但<b>不可</b>以符号方式引用
/// <c>Mud.Feishu.AI.Tools</c>（分析器形态 + <c>ReferenceOutputAssembly=false</c>），故
/// <c>Unpack</c>/<c>ToolName</c> 等名字以字面量给出（与 <c>GeneratorDiagnosticsContractGuards</c>
/// 同体例）。
/// </para>
/// </remarks>
public class ToolArgsContractGuards
{
    /// <summary>解包静态方法名（生成器 <c>ToolArgsEmitter.UnpackMethodName</c>）。</summary>
    private const string UnpackMethodName = "Unpack";

    /// <summary>工具名常量名（生成器 <c>ToolArgsEmitter.ToolNameConstant</c>）。</summary>
    private const string ToolNameConstant = "ToolName";

    /// <summary>生成类型名后缀（生成器 <c>ToolArgsEmitter.TypeSuffix</c>）。</summary>
    private const string TypeSuffix = "Args";

    // ────────── 覆盖性 ──────────

    /// <summary>
    /// 契约工具集与生成的 Args 类型集必须<b>双向相等</b>——少一个 = 执行器编译失败，
    /// 多一个 = 僵尸产物（工具已删、产物残留）。
    /// </summary>
    [Fact]
    public void GeneratedArgsTypes_ShouldCoverExactlyTheContractTools()
    {
        var generated = ArgsTypeByToolName();

        generated.Keys.Should().BeEquivalentTo(
            FeishuToolNames.All,
            "每枚 [FeishuTool] 工具都必须有生成的参数解包类型（少则执行器编译失败，多则僵尸产物）");
    }

    /// <summary>每个生成的 Args 类型必须提供 <c>Unpack</c> 静态方法（执行器唯一消费点）。</summary>
    [Fact]
    public void EveryGeneratedArgsType_ShouldExposeUnpack()
    {
        foreach (var (toolName, type) in ArgsTypeByToolName())
        {
            var unpack = type.GetMethod(UnpackMethodName, BindingFlags.Public | BindingFlags.Static);
            unpack.Should().NotBeNull($"{toolName} 的 {type.Name} 必须提供 public static Unpack（执行器唯一消费点）");
            unpack!.ReturnType.Should().Be(type, "Unpack 返回自身类型（解包后的强类型入参）");
            unpack.GetParameters().Should().HaveCount(
                1, "Unpack 接受一个参数字典（IReadOnlyDictionary<string, object?>）");
        }
    }

    // ────────── 字段与 Schema 对齐 ──────────

    /// <summary>
    /// Args 字段（名 + 必填性 + 基础类型）必须与 <c>[ToolParameter]</c> 派生的 Schema 参数逐一对齐。
    /// </summary>
    /// <remarks>
    /// 字段名由生成器按「<c>snake_case</c> → PascalCase」派生，本用例<b>独立复刻</b>该规则做交叉核对
    /// （不 import 生成器内部函数）：规则若被改动而两侧未同步，本用例即红——这正是断言的价值所在。
    /// </remarks>
    [Fact]
    public void ArgsProperties_ShouldMatchSchemaParameters()
    {
        foreach (var (toolName, type) in ArgsTypeByToolName())
        {
            using var document = JsonDocument.Parse(FeishuToolSchemas.SchemaByToolName[toolName]);
            var parameters = document.RootElement.GetProperty("parameters");
            var properties = parameters.GetProperty("properties").EnumerateObject()
                .ToDictionary(static p => p.Name, static p => p.Value, StringComparer.Ordinal);
            var required = parameters.TryGetProperty("required", out var requiredElement)
                ? requiredElement.EnumerateArray().Select(static e => e.GetString()!).ToHashSet(StringComparer.Ordinal)
                : [];

            var declared = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(static p => p.Name, StringComparer.Ordinal);

            declared.Keys.Should().BeEquivalentTo(
                properties.Keys.Select(ToPascalCase),
                $"{toolName} 的 {type.Name} 字段集必须与 Schema 参数集一一对应（含改名）");

            foreach (var (parameterName, schema) in properties)
            {
                var property = declared[ToPascalCase(parameterName)];
                AssertPropertyType(toolName, parameterName, schema, property, required.Contains(parameterName));
            }
        }
    }

    // ────────── 解包行为 ──────────

    /// <summary>必填参数缺失（含必填数组为空）必须抛 <see cref="ArgumentException"/>（执行链转结构化错误）。</summary>
    [Fact]
    public void Unpack_ShouldThrow_WhenRequiredParameterMissing()
    {
        // bitable.get_records_by_ids：必填 app_token/table_id（string）+ 必填 record_ids（string[]）。
        var unpack = UnpackOf(FeishuToolNames.BitableGetRecordsByIds);

        var missingScalar = () => unpack.Invoke(null, [Args(("table_id", "tbl1"), ("record_ids", new[] { "rec1" }))]);
        missingScalar.Should().Throw<TargetInvocationException>()
            .WithInnerException<ArgumentException>()
            .WithMessage("*app_token*", "缺失必填标量参数必须回填可读的 ArgumentException");

        // 空数组视同缺失（OptionalStringArray 对空数组返回 null）——飞书批量接口对空列表无有意义语义。
        var emptyArray = () => unpack.Invoke(null, [Args(("app_token", "bas1"), ("table_id", "tbl1"), ("record_ids", Array.Empty<string>()))]);
        emptyArray.Should().Throw<TargetInvocationException>()
            .WithInnerException<ArgumentException>()
            .WithMessage("*record_ids*");
    }

    /// <summary>可选参数缺省为 <see langword="null"/>；标量数组与整数按声明类型解包。</summary>
    [Fact]
    public void Unpack_ShouldReadOptionalParameters()
    {
        var records = UnpackOf(FeishuToolNames.BitableGetRecordsByIds)
            .Invoke(null, [Args(("app_token", "bas1"), ("table_id", "tbl1"), ("record_ids", new[] { "rec1", "rec2" }))])!;

        ReadProperty(records, "AppToken").Should().Be("bas1");
        ReadProperty(records, "RecordIds").Should().BeEquivalentTo(new[] { "rec1", "rec2" });

        // docx.get_raw_content：lang 为唯一整型参数（OptionalInt），缺省 null（执行器回落 0=中文）。
        var docx = UnpackOf(FeishuToolNames.DocxGetRawContent);
        ReadProperty(docx.Invoke(null, [Args(("document_id", "doxcn1"))])!, "Lang").Should().BeNull(
            "lang 缺省为 null（执行器回落 0）");
        ReadProperty(docx.Invoke(null, [Args(("document_id", "doxcn1"), ("lang", 1))])!, "Lang").Should().Be(1);

        // 整型参数存在但无法解析 → 抛（不静默降级为 0：0 与「未提供」是两种语义）。
        var badLang = () => docx.Invoke(null, [Args(("document_id", "doxcn1"), ("lang", "abc"))]);
        badLang.Should().Throw<TargetInvocationException>()
            .WithInnerException<ArgumentException>()
            .WithMessage("*lang*");
    }

    // ────────── 断言辅助 ──────────

    private static void AssertPropertyType(
        string toolName,
        string parameterName,
        JsonElement schema,
        PropertyInfo property,
        bool isRequired)
    {
        var jsonType = schema.GetProperty("type").GetString();
        var expected = jsonType switch
        {
            "string" => typeof(string),
            "array" => typeof(string[]),
            "integer" => typeof(int),
            "boolean" => typeof(bool),
            _ => null,
        };

        expected.Should().NotBeNull(
            $"{toolName}.{parameterName} 的 Schema 类型 {jsonType} 未在本守卫的期望表内——"
            + "新增参数形态时须同批扩展 ToolArgs 映射与守卫");

        if (expected!.IsValueType)
        {
            // 值类型：可空性在 CLR 层可见（int? → Nullable<int>），必须与 Schema 的 required 一致。
            property.PropertyType.Should().Be(
                isRequired ? expected : typeof(Nullable<>).MakeGenericType(expected),
                $"{toolName}.{parameterName} 的 required 语义必须与字段可空性一致");
            return;
        }

        // 引用类型：可空性由 #nullable 注解决定（反射不可见），此处只锁基础类型。
        property.PropertyType.Should().Be(expected, $"{toolName}.{parameterName} 的基础类型必须与 Schema 一致");
    }

    private static object? ReadProperty(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)!.GetValue(instance);

    private static MethodInfo UnpackOf(string toolName)
        => ArgsTypeByToolName()[toolName].GetMethod(UnpackMethodName, BindingFlags.Public | BindingFlags.Static)!;

    private static Dictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(static p => p.Key, static p => p.Value, StringComparer.Ordinal);

    /// <summary>按 <c>ToolName</c> 常量索引全部生成产物（常量由生成器写入，故无需在测试里复刻工具名 → 类型名规则）。</summary>
    private static IReadOnlyDictionary<string, Type> ArgsTypeByToolName()
    {
        var map = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var type in typeof(FeishuToolNames).Assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract || !type.Name.EndsWith(TypeSuffix, StringComparison.Ordinal))
            {
                continue;
            }

            var toolName = type
                .GetField(ToolNameConstant, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                ?.GetRawConstantValue() as string;

            if (toolName is not null)
            {
                map[toolName] = type;
            }
        }

        return map;
    }

    /// <summary>复刻生成器的 <c>snake_case</c> → PascalCase 规则（按 <c>.</c>/<c>_</c>/<c>-</c> 分段首字母大写）。</summary>
    private static string ToPascalCase(string name)
    {
        var builder = new System.Text.StringBuilder(name.Length);
        var upperNext = true;
        foreach (var ch in name)
        {
            if (ch is '.' or '_' or '-')
            {
                upperNext = true;
                continue;
            }

            builder.Append(upperNext ? char.ToUpperInvariant(ch) : ch);
            upperNext = false;
        }

        return builder.ToString();
    }
}
