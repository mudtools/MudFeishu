// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Results;

/// <summary>
/// 飞书侧入参读取适配器（v2.3 P1-D）：<b>依赖生成物 <c>ToolArgs</c></b> 的三个字典读取器——
/// 骨架 <c>ToolPagination</c> / <c>ToolDryRun</c> 中立化上移后不能反向依赖 owner 程序集内
/// 由 <c>ToolArgsEmitter</c> 生成的 <c>ToolArgs</c> 门面，故把「原始参数字典 → 标量」的
/// 飞书侧读取点留本程序集。
/// </summary>
/// <remarks>
/// 纪律不变：取值仍经唯一门面 <c>ToolArgs.OptionalBool</c>/<c>ToolArgs.OptionalInt</c>
/// （与生成的 <c>*Args.Unpack</c> 同源；<c>ToolArgumentShapeContractGuards</c> 机械锁定不得自写形态 switch）。
/// </remarks>
internal static class ToolArgsReaders
{
    /// <summary>从参数字典读取 <c>fetch_all</c> 参数（默认 false）。</summary>
    public static bool ReadFetchAll(IReadOnlyDictionary<string, object?> arguments)
        => ToolArgs.OptionalBool(arguments, "fetch_all") ?? false;

    /// <summary>
    /// 从参数字典读取 <c>max_items</c> 参数（默认 null → 由 Options 填充）。
    /// </summary>
    /// <remarks>
    /// 语义与生成的 <c>*Args.Unpack</c> 对齐：参数存在但无法解析为整数时<b>抛</b>
    /// （由执行链转结构化错误），不静默降级为"未提供"。
    /// </remarks>
    public static int? ReadMaxItems(IReadOnlyDictionary<string, object?> arguments)
        => ToolArgs.OptionalInt(arguments, "max_items");

    /// <summary>
    /// 从<b>原始参数字典</b>读取 <c>dry_run</c>（执行链消费点 B3：dry_run 调用不参与重试，
    /// 而执行链看到的只有参数字典——此处与生成器解包语义保持一致，缺省 false）。
    /// </summary>
    public static bool IsDryRunRequested(IReadOnlyDictionary<string, object?> arguments)
        => arguments is not null && (ToolArgs.OptionalBool(arguments, "dry_run") ?? false);
}
