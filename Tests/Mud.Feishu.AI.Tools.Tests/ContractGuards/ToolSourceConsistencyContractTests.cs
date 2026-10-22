// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// <b>R5 / S-20</b>：<c>[AgentTool(Source = …)]</c> 的<b>语义正确性</b>守卫 ——
/// 声明的 SDK 源接口必须是处理该工具的执行器<b>实际注入</b>的接口。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它</b>：<c>Source</c> 是<b>人工填写</b>的字符串，而现有校验只覆盖：
/// <list type="bullet">
/// <item><c>MUDFT019</c>：类型与方法<b>能否解析</b>；</item>
/// <item><c>MUDFT002</c>：接口名是否符合 SDK 范式；</item>
/// <item><c>MUDFT016</c>：接口是否带令牌前缀（Tenant/User）；</item>
/// <item><c>MUDFT017</c>：<b>读写分类</b>是否与源方法的 HTTP 动词矛盾。</item>
/// </list>
/// 于是"填一个<b>存在、命名合规、动词兼容</b>但语义无关的方法"可以全数通过 ——
/// 而 <c>Source</c> 正是 <c>x-feishu</c> 里 <b>HTTP 路由 / 风险分级 / 输出形状</b>的推导依据，
/// 一旦指错，模型看到的 <c>route</c> 就是<b>假事实</b>（S-17 已实测发生过一次）。
/// </para>
/// <para>
/// <b>本条不变量把"人工声明"与"代码事实"钉在一起</b>：执行器若真的通过某 SDK 接口调用平台，
/// 该接口必然出现在它的构造参数里。反过来，声明了却未注入 ⇒ 声明与实现不可能同时为真。
/// </para>
/// <para>
/// <b>刻意不做的事</b>：不校验"Source 方法名 == 执行器调用的方法名"。执行器可能（且常常）
/// 组合多个 SDK 调用（如 <c>docx.replace_document</c> 是三步组合），方法级的强绑定会逼出
/// 大量无意义的例外。接口级绑定已足以拦住"指到别的域"这类真实错误。
/// </para>
/// </remarks>
public class ToolSourceConsistencyContractTests
{
    /// <summary>
    /// <b>核心断言</b>：凡声明了 <c>Source</c> 的工具，其执行器必须注入 <c>Source</c> 的接口类型。
    /// </summary>
    [Fact]
    public void DeclaredSourceInterface_ShouldBeInjectedByTheBoundExecutor()
    {
        var assembly = typeof(Mud.Feishu.AI.Tools.Internal.DocxWriteTools).Assembly;

        var executorByInterface = MapExecutorByToolInterface(assembly);
        var violations = new List<string>();
        var checkedCount = 0;

        foreach (var toolInterface in assembly.GetTypes().Where(static t => t.IsInterface))
        {
            var attribute = toolInterface.GetCustomAttribute<AgentToolAttribute>();
            if (attribute?.Source is not { Length: > 0 } source)
            {
                continue; // 未声明 Source：由 MUDFT019 口径处理（无 Source 即无脱钩风险）。
            }

            var separator = source.LastIndexOf('.');
            if (separator <= 0)
            {
                violations.Add($"工具 '{attribute.Name}'：Source '{source}' 不是 '接口.方法' 形态");
                continue;
            }

            var sourceTypeFull = source.Substring(0, separator);
            var sourceTypeSimple = sourceTypeFull.Substring(sourceTypeFull.LastIndexOf('.') + 1);

            if (!executorByInterface.TryGetValue(toolInterface, out var executor))
            {
                violations.Add(
                    $"工具 '{attribute.Name}'（{toolInterface.Name}）：声明 Source={sourceTypeSimple}，"
                    + "但全程序集找不到标注 [ToolHandler] 的执行器");
                continue;
            }

            var injected = executor
                .GetConstructors()
                .SelectMany(static c => c.GetParameters())
                .Select(static p => p.ParameterType.Name)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static n => n, StringComparer.Ordinal)
                .ToArray();

            checkedCount++;

            if (!injected.Contains(sourceTypeSimple, StringComparer.Ordinal))
            {
                violations.Add(
                    $"'{attribute.Name}'：执行器 {executor.Name} 声明 Source={sourceTypeSimple}，"
                    + $"但它并未注入该接口（实际注入：{(injected.Length == 0 ? "无" : string.Join(", ", injected))}）");
            }
        }

        // 覆盖数用**精确基线**而非下限：一条"永远为绿"的守卫与没有守卫等价，却会让人以为已守住。
        // 基线取值来自 golden 的权威口径（`source.sdk` 非空者 119 / 125，与声明面 Source 计数一致）。
        executorByInterface.Count.Should().Be(
            351,
            "363 个工具必须都能反查到执行器（MUDFT022/023 已在构建期保证一一绑定）；此处骤降说明反查机制失效。"
            + "R-12：原 163 包含 feishu.api_call，该工具已整条删除；F-1：mdm + security + org + contact + hire 共 20 工具（162 → 182）；"
            + "专题一 B1 五域 + B2 补全：lingo 6 只读 + 5 写 + application 7 只读 + 2 写 + helpdesk 6 只读 + 4 写"
            + " + chatgroup 4 只读 + 12 写 + trustparty 8 只读 + 3 写 + aily 3 只读（182 → 242）；"
            + "T-1 读取闭环：drive.download_file / drive.export_document / calendar.agenda / calendar.rsvp（242 → 246）；"
            + "T-2 表格加深：bitable 19（7 读 + 12 写）+ sheets 21（6 读 + 15 写）（246 → 286）；"
            + "T-3 成环：im 4 + mail 17（草稿/规则/模板/签名/附件）+ task 12（清单/搜索/提醒/重开）（286 → 319）；"
            + "T-4 补齐：wiki 6（3 读 + 3 写）+ approval 2（撤回/抄送）+ vc 4（1 读 + 3 机器人写）（319 → 331）；"
            + "S-6 首刀：im 表情 4（2 读 + 2 写）+ minutes 5（1 读 + 4 写）+ okr 指标 3（2 读 + 1 写）+ wiki.space_create（331 → 344）；"
            + "幻灯片整域 12（7 读 + 5 写，含截图/取图两个字节落盘出口）（344 → 356）；"
            + "Markdown 生命周期 5（2 读 + 3 写）（356 → 361）；"
            + "S-4 复合工作流 2（只读编排，无 Source）（361 → 363）；"
            + "S-6 余项：docx.history_list（Drive 版本列表）+ docx.media_insert（Drive 媒体上传 → Docx 图片块）（363 → 365）；"
            + "T-4 工具面收敛：邮件 6→3 + Drive 协作者 2→1 + 表格单条写去重（365 → 351）");

        checkedCount.Should().Be(
            344,
            "344 个工具声明了 Source（其余 5 个无 Source，由 MUDFT019 口径覆盖）。"
            + "新增/删除 Source 时需同步本基线——但不许把它改成'大于某个下限'，那会让覆盖缩水静默通过");

        violations.Should().BeEmpty(
            "Source 是 x-feishu 中 route/risk/输出形状的推导依据，指错即产出假事实：{0}",
            string.Join(" | ", violations));
    }

    /// <summary>
    /// <b>守卫自证</b>：证明这套检查<b>确实会失败</b>，而不是恒真。
    /// </summary>
    /// <remarks>
    /// 一条永远为绿的守卫与没有守卫是等价的，却会让人误以为"已守住"。本用例用同一套判据
    /// 喂入一对<b>真实但错配</b>的组合（<c>DocxWriteTools</c> 从不注入 IM 客户端），
    /// 断言它<b>被判定为违约</b>。
    /// </remarks>
    [Fact]
    public void Guard_ShouldDetectMismatch_WhenSourceInterfaceIsNotInjected()
    {
        var docxWriteTools = typeof(Mud.Feishu.AI.Tools.Internal.DocxWriteTools);

        var injected = docxWriteTools
            .GetConstructors()
            .SelectMany(static c => c.GetParameters())
            .Select(static p => p.ParameterType.Name)
            .ToArray();

        // 正向：真实注入的接口应被判为"一致"。
        injected.Should().Contain("IFeishuTenantV1DocxBlocks", "docx 写工具确实注入该客户端");

        // 反向：IM 客户端不可能被 docx 写工具注入 ⇒ 若某工具把 Source 填成 IM 的接口，
        //       判据必须报违约（这正是 S-20 要拦的那类假事实）。
        injected.Should().NotContain(
            "IFeishuTenantV1Message",
            "该判据的检出能力正建立在「未注入即违约」上——此处必须为真，否则判据恒真、守卫失效");
    }

    /// <summary>工具接口 → 处理它的执行器类型（由 <c>[ToolHandler(typeof(接口))]</c> 反查）。</summary>
    private static Dictionary<Type, Type> MapExecutorByToolInterface(Assembly assembly)
    {
        var map = new Dictionary<Type, Type>();

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsInterface || type.IsAbstract)
            {
                continue;
            }

            foreach (var method in type.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var handler = method.GetCustomAttribute<ToolHandlerAttribute>();
                if (handler is not null)
                {
                    map[handler.ToolInterface] = method.DeclaringType!;
                }
            }
        }

        return map;
    }
}
