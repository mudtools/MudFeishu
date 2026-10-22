// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.CapabilityDemo.Common;

/// <summary>一幕的元数据（CLI 契约 + 能力声明）。</summary>
/// <param name="Id">短标识（<c>a1</c>…<c>a8</c> / <c>verify</c>）。</param>
/// <param name="Alias">可读别名（<c>hello</c>…）。</param>
/// <param name="Title">一句话标题。</param>
/// <param name="BlogSection">对应技术博客章节（审计"演示是否覆盖主张"）。</param>
/// <param name="RequiresModel">是否需要模型 API Key。</param>
/// <param name="RequiresData">是否需要真实飞书租户数据。</param>
/// <param name="IsOffline">是否完全离线（无网络、无凭证）。</param>
internal sealed record ActDescriptor(
    string Id,
    string Alias,
    string Title,
    string BlogSection,
    bool RequiresModel,
    bool RequiresData,
    bool IsOffline);

/// <summary>八幕 + <c>verify</c> 的目录。<b>新增/改名只改这里</b>。</summary>
internal static class ActCatalog
{
    public static IReadOnlyList<ActDescriptor> All { get; } =
    [
        new("a1", "hello", "五分钟上手：两段装配", "五分钟上手 / 底层：用 MAF 当基座", RequiresModel: true, RequiresData: false, IsOffline: false),
        new("a2", "surface", "331 的剖面：工具面与 affordance", "331 不是「少暴露」", RequiresModel: false, RequiresData: false, IsOffline: true),
        new("a3", "meta", "先问元工具，再动手", "另外有 4 个元工具", RequiresModel: true, RequiresData: false, IsOffline: false),
        new("a4", "chain", "七步链与 fail-closed", "每次调用都走同一条链", RequiresModel: false, RequiresData: false, IsOffline: true),
        new("a5", "hitl", "写闭环：审批、续跑、幂等", "写操作默认是关着的", RequiresModel: true, RequiresData: true, IsOffline: false),
        new("a6", "mcpdemo", "进程外：MCP stdio", "走出进程：MCP", RequiresModel: false, RequiresData: false, IsOffline: true),
        new("a7", "skills", "进程外：Skills 产物", "走出进程：Skills", RequiresModel: false, RequiresData: false, IsOffline: true),
        new("a8", "tenants", "多租户隔离", "多租户不是加个字段", RequiresModel: true, RequiresData: true, IsOffline: false),
        new("verify", "verify", "离线自检矩阵（8 组）", "门禁比功能多", RequiresModel: false, RequiresData: false, IsOffline: true),
    ];

    /// <summary>按 id 或别名解析（忽略大小写与首尾空白）。</summary>
    public static ActDescriptor? Resolve(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return All.FirstOrDefault(
            a => string.Equals(a.Id, normalized, StringComparison.OrdinalIgnoreCase)
                 || string.Equals(a.Alias, normalized, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>八幕（不含 verify），用于菜单与自检的"覆盖完整性"断言。</summary>
    public static IEnumerable<ActDescriptor> Acts => All.Where(static a => a.Id != "verify");
}
