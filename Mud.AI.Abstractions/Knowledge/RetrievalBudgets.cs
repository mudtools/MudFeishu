// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Abstractions.Knowledge;

/// <summary>
/// 知识检索注入的<b>三级预算闸</b>单一源（N2-e 迁A，原产品 <c>ContextBudgets.Knowledge*</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是常量而非配置键</b>：对齐产品的治理口径——"可配"意味着新配置键，而每个公开配置
/// 属性都必须有真实消费点与守卫登记；当前没有第二个取值需求。超限策略是<b>截断</b>
/// （不是整段丢弃），见各检索/装配实现。
/// </para>
/// <para>
/// <b>三闸各自可达（刻意取值）</b>：条数闸（<see cref="MaxChunks"/>）× 单条闸
/// （<see cref="ChunkPreviewLength"/>）是硬上限，总预算（<see cref="TotalLength"/>）取更小值
/// 使三闸各自可达——短切片撞条数闸、长切片撞总预算、超长单条撞单条闸。任取"永不触发"的值
/// 都会让对应用例变成假绿。
/// </para>
/// <para>
/// <b>中立化说明</b>：预算值是<b>产品中立</b>的通用 RAG 注入策略（与飞书无关）；
/// 产品侧 <c>ContextBudgets</c> 以常量转发保持既有引用面不变（N4 拆分装配器后删除转发）。
/// </para>
/// </remarks>
public static class RetrievalBudgets
{
    /// <summary>知识检索切片：单条预览上限（字符）。</summary>
    public const int ChunkPreviewLength = 500;

    /// <summary>知识检索切片：单轮注入总上限（字符）。</summary>
    public const int TotalLength = 3000;

    /// <summary>知识检索切片：单轮注入条数上限。</summary>
    public const int MaxChunks = 8;
}
