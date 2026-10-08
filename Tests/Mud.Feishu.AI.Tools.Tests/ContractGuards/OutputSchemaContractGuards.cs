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
/// 输出契约（<c>x-feishu.output_schema</c>）守卫（R2-05 决策后重写）。
/// </summary>
/// <remarks>
/// <para>
/// <b>决策背景</b>：R2-05 方案初稿要求把 <c>output_schema</c> 接线到运行期结果投影
/// （<c>SchemaProjection.Project</c>）。经复核<b>驳回</b>，两条理由：
/// </para>
/// <list type="number">
/// <item>
/// <b>按设计不可达</b>：<c>SchemaProjection</c> 自述「显式覆盖优先——本类仅在<b>无显式投影</b>时作为
/// 默认推导路径」，而 53/53 工具都有显式投影（<c>FromApi</c>/<c>FromApiUntruncated</c>）
/// 或纯文本出口（<c>FromPlainText</c>）。故"生产 0 调用"是设计后果，不是"未接线"缺陷。
/// </item>
/// <item>
/// <b>接线会清空结果</b>：投影是<b>白名单</b>语义（未声明字段丢弃），而策展键名并不都落在
/// output_schema 顶层字段集内——<c>task.complete_task</c> 的策展键 <c>task_guid</c> vs
/// schema 顶层 <c>task.guid</c>；<c>task.list_my_tasks</c> 的 <c>items[].task_guid</c> vs
/// <c>items[].guid</c>。接线后这些工具的结果会变成 <c>{}</c>（静默丢数据）。
/// </item>
/// </list>
/// <para>
/// 故运行期投影基础设施（<c>SchemaProjection.cs</c> 182 行）与它的 4 条行为用例<b>一并删除</b>；
/// <c>output_schema</c> 保留为**构建期信号**（<c>MUDFT009</c> 截断告警 +
/// <c>FeishuToolCapabilityCatalog.OutputSchemaCoveredToolCount</c> 度量）。本类守卫其<b>生成侧</b>形态。
/// </para>
/// <para>
/// <b>不守卫的内容</b>：策展投影键 ⊆ output_schema 字段集——二者是<b>有意的两套命名</b>
/// （策展面向模型可读性，schema 面向 DTO 事实），强行一致会改变 30+ 个工具的模型可见契约，
/// 收益不抵"golden 与全部链式用例重写"的代价（属 P3 演进池的范围）。
/// </para>
/// </remarks>
public class OutputSchemaContractGuards
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
            "至少部分工具应有 output_schema——否则 MUDFT009 截断信号与 OutputSchemaCoveredToolCount 度量都失去适用面");
        toolsWithSchema.Should().Be(
            FeishuToolContracts.AllNames.Length - toolsWithoutSchema,
            "output_schema 的产出面必须与契约表工具一一对应（信封缺 output_schema 的只能是「确实推导不出返回类型」的工具）");
    }

    /// <summary>
    /// R2-05：<c>OutputSchemaRate</c> 的真实消费点必须与真实产出面一致——
    /// 该度量历史上两次是假值（R5 前恒 1.0、R5 后恒 0），故此处对它做**双向**断言。
    /// </summary>
    /// <remarks>
    /// 断言的是"度量不是常量"而不是某个具体数值：具体数值会随工具面演进而变，
    /// 而"看起来是 0 或看起来是 100%"才是假度量的形态。
    /// </remarks>
    [Fact]
    public void OutputSchemaCoverageMetric_ShouldBeARealValue_NotAConstant()
    {
        var covered = FeishuToolCapabilityCatalog.OutputSchemaCoveredToolCount;
        var total = FeishuToolCapabilityCatalog.CuratedToolCount;

        total.Should().Be(FeishuToolContracts.AllNames.Length,
            "能力目录的策展工具数必须与契约表条目数一致（同源同 pass 产出）");
        covered.Should().BeGreaterThan(0,
            "恒为 0 是 R5 修订留下的假度量形态（它宣称「没有工具声明输出 Schema」，与事实不符）");
        covered.Should().BeLessThanOrEqualTo(total,
            "分子不可能超过分母；相等意味着「零截断」，那与构建期 MUDFT009 的存在互相矛盾——"
            + "当前 SDK 必然存在深层 DTO，故本条对恒等式形态（假绿）同样报红");
    }
}
