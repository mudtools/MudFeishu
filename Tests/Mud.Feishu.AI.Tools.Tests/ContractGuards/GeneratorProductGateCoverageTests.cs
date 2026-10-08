// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using Mud.Feishu.AI.FeishuTools.SdkProfile;
using Mud.HttpUtils.Attributes;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

/// <summary>
/// <b>R5 / B-9</b>：<b>生成器产物门禁覆盖守卫</b> —— 断言每一个发射产物要么被 golden 直接覆盖，
/// 要么有明确登记的"传递性门禁依据"。
/// </summary>
/// <remarks>
/// <para>
/// <b>R-1+2c 迁移后的口径变更（重要）</b>：原实现扫描本仓生成器源码的 <c>AddSource(...)</c> 调用
/// 反推产物集合；引擎上游化后该源码已不在本仓（<c>Mud.HttpUtils.Generator</c> 是 analyzer 包，
/// 只有 IL 没有源码）⇒ 改为<b>双锚点</b>：
/// </para>
/// <list type="number">
/// <item><b>产物清单</b>= 剖面前缀（<c>ProductPrefix</c>/<c>ProductPluralPrefix</c>）驱动的
/// 引擎出口模板（见 <see cref="BuildProductGates"/> 的 8 条模板，镜像组件侧设计文档 §5.1 的出口表）。
/// 模板与剖面前缀任一变动，登记表立即不匹配 ⇒ 必须显式评审"新产物归谁门禁"。</item>
/// <item><b>正方向证据</b>= 反射断言这些产物<b>真的存在于程序集里</b>
/// （<see cref="EmittedProducts_ShouldExistInTheOwningAssembly"/>）——这是本仓仍能机械检测的
/// "引擎停止发射/改名"方向，比"新增产物"方向更常发生（引擎升级会改名，见能力目录产物名实测）。</item>
/// </list>
/// <para>
/// <b>为什么原判据仍然成立</b>：仓库不提交任何生成产物（<c>*.g.cs</c> 无一入库），
/// 所以"产物漂移"不可能通过提交引入 ⇒ 不需要"提交态 diff"式门禁；
/// 真正的风险是"新增了一个产物却没人给它想门禁"，该风险由模板清单 + 登记表一致性承接。
/// </para>
/// </remarks>
public class GeneratorProductGateCoverageTests
{
    /// <summary>
    /// 产物 → 门禁依据的<b>登记表</b>（按剖面前缀实例化；本守卫的单一真相源）。
    /// </summary>
    /// <remarks>
    /// <b>三类依据</b>：
    /// <list type="bullet">
    /// <item><b>golden</b>：<c>FeishuToolSchemas.golden.txt</c> 直接比对产物文本；</item>
    /// <item><b>传递性</b>：产物是已门禁输入的纯函数，自身不必再快照（再快照就是与既有门禁功能重复）；</item>
    /// <item><b>不可漂移</b>：产物内容由仓库内文件直接嵌入，输入本身即真相源。</item>
    /// </list>
    /// </remarks>
    private static Dictionary<string, string> ProductGates { get; } = BuildProductGates();

    /// <summary>
    /// 核心断言：登记表必须<b>恰好等于</b>板块出口模板集合（既不缺项，也无陈旧项）。
    /// </summary>
    [Fact]
    public void ProductGates_ShouldCoverExactlyTheEngineOutletTemplates()
    {
        var expected = OutletTemplates()
            .Select(template => template.Key)
            .OrderBy(static key => key, StringComparer.Ordinal)
            .ToArray();

        ProductGates.Keys.OrderBy(static key => key, StringComparer.Ordinal).Should().BeEquivalentTo(
            expected,
            "产物登记表与引擎出口模板不一致——新增出口若不登记，其漂移将<b>静默无门禁</b>；"
            + "已不再发射的出口留在表里会掩盖真实缺口");
    }

    /// <summary>登记表的每条依据<b>不得为空</b>（防止用空字符串占位骗过守卫）。</summary>
    [Fact]
    public void EveryRegisteredGateBasis_ShouldBeNonEmptyAndExplainWhy()
    {
        var empty = ProductGates
            .Where(static kv => string.IsNullOrWhiteSpace(kv.Value))
            .Select(static kv => kv.Key)
            .ToArray();

        empty.Should().BeEmpty("以下产物登记了空依据——等于没门禁：{0}", string.Join(" | ", empty));
    }

    /// <summary>
    /// 正方向证据：固定名产物<b>必须真实存在于宿主程序集</b>（引擎停止发射或改名即红）。
    /// </summary>
    /// <remarks>
    /// 这一条是本仓在"引擎源码不可见"约束下仍能机械检测的回归方向。实测已因此捕获过一处改名：
    /// 组件侧能力目录产物名由 <c>ProductPrefix</c> 派生（<c>FeishuToolCapabilityCatalog</c>），
    /// 与本仓迁移前的 <c>FeishuCapabilityCatalog</c> 不同 —— 若没有本条，该差异只会在
    /// 消费方 <c>CS0103</c> 上暴露一次，之后无人守护。
    /// </remarks>
    [Fact]
    public void EmittedProducts_ShouldExistInTheOwningAssembly()
    {
        var assembly = typeof(FeishuToolNames).Assembly;
        var prefix = ReadProfileSlot("ProductPrefix");
        var plural = ReadProfileSlot("ProductPluralPrefix");

        var expectedTypes = new[]
        {
            prefix + "Schemas",
            prefix + "Names",
            prefix + "Contract",
            prefix + "Contracts",
            prefix + "Guidance",
            prefix + "CapabilityCatalog",
            plural + "ServiceCollectionCoreExtensions",
        };

        var missing = expectedTypes
            .Where(name => FindProductType(assembly, name) is null)
            .ToArray();

        missing.Should().BeEmpty(
            "以下产物类型在程序集里找不到——引擎未发射（剖面写坏 / owner 门槛未命中）或已改名：{0}",
            string.Join(", ", missing));

        // 反向自证：探测必须真的看得见产物（否则"全找不到"也会让上一条在空集合上通过）。
        var seen = expectedTypes.Count(name => FindProductType(assembly, name) is not null);
        seen.Should().Be(
            expectedTypes.Length,
            "探测机制失效（假绿）：预期 {0} 个产物类型，实际只看见 {1} 个——请先修守卫",
            expectedTypes.Length,
            seen);
    }

    // ────────── 采集实现 ──────────

    /// <summary>
    /// 引擎出口模板（<c>{Prefix}</c> = 剖面前缀，<c>{TypeName}</c>/<c>{RegistrarName}</c> = 动态段）。
    /// </summary>
    /// <remarks>
    /// 与组件侧设计文档 §5.1 的出口表一一对应（<c>{P}Schemas</c> / <c>{P}Names</c> /
    /// <c>{P}Contracts</c> / <c>{P}Args/*</c> / <c>{P}DomainRegistrars/*</c> /
    /// <c>{Plural}ServiceCollectionCoreExtensions</c> / <c>{P}Guidance</c> / <c>{P}CapabilityCatalog</c>）。
    /// 引擎升级新增出口时，本表与 <see cref="ProductGates"/> 必须同批更新。
    /// </remarks>
    private static Dictionary<string, string> OutletTemplates()
    {
        var prefix = ReadProfileSlot("ProductPrefix");
        var plural = ReadProfileSlot("ProductPluralPrefix");

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [prefix + "Schemas.g.cs"] = "golden：与 FeishuToolSchemas.golden.txt 逐字节比对，构建期 MUDFT014 断言",
            [prefix + "Names.g.cs"] =
                "传递性：工具名表是工具面的纯函数；工具面变动已被 golden 捕获（golden 内含每个工具的 name 字段）",
            [prefix + "Contracts.g.cs"] =
                "传递性：类型化契约表由同一批 CapabilityEntry 派生，输入与 {P}Schemas 完全同源",
            [prefix + "CapabilityCatalog.g.cs"] =
                "传递性：能力目录是 CapabilityEntry 的聚合视图（同批同 pass），无独立输入",
            [prefix + "Guidance.g.cs"] =
                "不可漂移：guidance 正文来自 AdditionalFiles（Guidance/{domain}.md，仓库内文件），直接嵌入产物；"
                + "域清单由 GuidanceAssetContractGuards 锁定",
            [prefix + "Args/{TypeName}.g.cs"] =
                "传递性：参数解包器由绑定派生；绑定由 MUDFT022~025（未绑定/绑定不成立/签名不符/构造参数不可解析）"
                + "零容忍门禁覆盖",
            [prefix + "DomainRegistrars/{RegistrarName}.g.cs"] =
                "传递性：注册器由 ToolHandlerBinding 派生；同上由 MUDFT022~025 覆盖"
                + "（另：注册器内的 {P}Names 常量引用受 ToolExecutorSkeletonGuards 的「每方法至多 1 次」约束）",
            [plural + "ServiceCollectionCoreExtensions.g.cs"] =
                "传递性：核心扩展方法由同一批绑定派生，同 MUDFT022~025 覆盖",
        };
    }

    /// <summary>按剖面前缀实例化登记表（依据文本里的 <c>{P}</c> 占位符同时被替换，便于阅读）。</summary>
    private static Dictionary<string, string> BuildProductGates()
    {
        var prefix = ReadProfileSlot("ProductPrefix");
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (key, basis) in OutletTemplates())
        {
            map[key] = basis.Replace("{P}", prefix, StringComparison.Ordinal);
        }

        return map;
    }

    /// <summary>产物类型的两个合法命名空间（Schemas/Contracts/Guidance/Catalog 在 Generated；Names/DI 在宿主命名空间）。</summary>
    private static Type? FindProductType(Assembly assembly, string typeName)
        => assembly.GetType("Mud.Feishu.AI.Tools.Generated." + typeName, throwOnError: false)
            ?? assembly.GetType("Mud.Feishu.AI.FeishuTools." + typeName, throwOnError: false);

    private static string ReadProfileSlot(string slotName)
    {
        var attribute = typeof(FeishuToolProfile).GetCustomAttribute<SdkToolProfileAttribute>();
        attribute.Should().NotBeNull("剖面必须标注 [SdkToolProfile]（否则工具面不会产出）");

        var property = typeof(SdkToolProfileAttribute).GetProperty(slotName, BindingFlags.Public | BindingFlags.Instance);
        property.Should().NotBeNull($"SdkToolProfileAttribute 必须仍有 {slotName} 槽");

        return property!.GetValue(attribute!)?.ToString() ?? string.Empty;
    }
}
