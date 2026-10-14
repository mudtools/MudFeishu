// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Abstractions.Context;

/// <summary>
/// 上下文装配器的<b>通用预算与标记</b>（N2-e 迁A，原产品 <c>ContextBudgets</c> 的通用子集）。
/// </summary>
/// <remarks>
/// <para>
/// 收录标准：与产品语义无关、任何生态的装配器都要用的两个值——
/// ① <see cref="MaxFactsPerAssembler"/>（防"事件载荷字段爆炸"把预算吃光）；
/// ② <see cref="TruncationMarker"/>（超预算时追加，让模型知道还有内容未注入）。
/// </para>
/// <para>
/// <b>不收录</b>：各产品事件族的字段预算（审批 / 多维表格 / 日程 / 群历史）与
/// <b>untrusted 标注文案</b>——后者含产品文案（"来自飞书事件载荷"），按「共享结构、
/// 不共享语义」留在产品侧；中立装配器（N4 的 <c>FactListContextAssembler</c>）经构造参数
/// 接收宿主给的标注文案。
/// </para>
/// <para>
/// <b>中立化说明</b>：产品侧 <c>ContextBudgets</c> 以常量转发保持既有引用面不变
/// （N4 拆分装配器后删除转发）。
/// </para>
/// </remarks>
public static class AssemblerBudgets
{
    /// <summary>
    /// 单次装配最多注入的事实条数（防止"事件载荷字段爆炸"把预算吃光）。
    /// </summary>
    public const int MaxFactsPerAssembler = 16;

    /// <summary>截断标记（超预算时追加，让模型知道还有内容未注入）。</summary>
    public const string TruncationMarker = "…（内容超预算已截断）";
}
