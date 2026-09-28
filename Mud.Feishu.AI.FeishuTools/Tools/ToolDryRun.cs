// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.Feishu.AI.FeishuTools.Tools;

/// <summary>
/// 写操作预演（<c>dry_run</c>，AT-F13①）：返回"将下发什么"，但<b>不调用下游</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么在执行器里实现而不是在执行链拦截（R3 评审补全的设计缺口）</b>：
/// <c>method</c>/<c>path</c>/<c>body</c> 只有执行器知道（执行链看到的只是
/// <c>IReadOnlyDictionary</c> 入参与一个已投影的结果）。若在执行链拦截，就必须新增
/// "执行器预先声明请求形状"的契约——那是新机制且会让 3 个写工具各自声明一遍。
/// 执行器内实现只需一次前置判断，且天然与真实请求构造同源（不会出现"预演与实发不一致"）。
/// </para>
/// <para>
/// <b>摘要不回原文（安全约束，有测试断言）</b>：正文类字段（<c>text</c>/<c>fields</c>/<c>form</c>）
/// 只回<b>字段名与长度</b>，不回值。否则 <c>dry_run</c> 会变成一条"绕过出站净化与内容安全的回显通道"
/// ——用户在预演里塞什么，模型就能原样读回什么。
/// </para>
/// <para>
/// <b>路由用 Schema 里的模板字面量</b>（如 <c>/open-apis/im/v1/messages</c>），不代入实际
/// token——模板是 golden 锁定的稳定契约，也不泄露调用现场的标识。
/// </para>
/// </remarks>
internal static class ToolDryRun
{
    /// <summary>构造预演结果文本。</summary>
    /// <param name="toolName">工具名。</param>
    /// <param name="httpMethod">HTTP 方法（来自生成器从 SDK 派生的 <c>x-feishu.source.http</c>）。</param>
    /// <param name="route">路由模板（<c>x-feishu.source.route</c>）。</param>
    /// <param name="extraNote">附加说明行（如幂等键状态回显，T4-1；可空）。</param>
    /// <param name="bodyFields">请求体字段摘要（字段名 + 值长度；<b>不含值本身</b>）。</param>
    public static string Describe(
        string toolName,
        string httpMethod,
        string route,
        string? extraNote = null,
        params (string Name, int Length)[] bodyFields)
    {
        var builder = new StringBuilder();
        builder.Append("[dry_run] ").Append(toolName).Append(" 将下发 ")
            .Append(httpMethod).Append(' ').Append(route);

        if (bodyFields.Length > 0)
        {
            builder.Append("\n请求体字段（仅回字段名与长度，不回原文）：");
            for (var i = 0; i < bodyFields.Length; i++)
            {
                builder.Append(i == 0 ? " " : ", ")
                    .Append(bodyFields[i].Name).Append('=').Append(bodyFields[i].Length.ToString(CultureInfo.InvariantCulture))
                    .Append(" 字符");
            }
        }

        if (!string.IsNullOrEmpty(extraNote))
        {
            builder.Append('\n').AppendLine(extraNote);
        }

        builder.Append("\n未调用下游接口。确认无误后以 dry_run=false 重放同一参数即可真正执行。");
        return builder.ToString();
    }

    /// <summary>
    /// 幂等键的预演回显（T4-1）：<c>dry_run=true</c> 时幂等键<b>不消耗</b>（不下发请求），
    /// 摘要显式回显 provided/omitted，让模型知道"预演不会占坑"。
    /// </summary>
    public static string IdempotencyNote(string? idempotencyKey)
        => idempotencyKey is { Length: > 0 }
            ? $"幂等键：provided（长度 {idempotencyKey.Length.ToString(CultureInfo.InvariantCulture)}，预演不占坑）"
            : "幂等键：omitted（本次调用不保证幂等）";

    /// <summary>读取 <c>dry_run</c> 参数（缺省 false；语义：仅预演、不下发）。</summary>
    /// <param name="arguments">模型入参。</param>
    public static bool IsRequested(IReadOnlyDictionary<string, object?> arguments)
        => ToolArgs.OptionalBool(arguments, "dry_run") ?? false;
}
