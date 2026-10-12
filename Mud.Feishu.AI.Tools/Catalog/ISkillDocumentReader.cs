// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Catalog;

/// <summary>
/// R-3 / F-8：<b>Skills 产物的读取接缝</b>——宿主 Agent 按域读取 <c>SKILL.md</c> / <c>references/*.md</c>，
/// 保证"读取即所得"（内容与 <see cref="IToolSchemaExporter.Export(ToolSchemaDialect.Skills)"/> 同源）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它（F-8 的根因）</b>：<c>skills/**</c> 已随 NuGet 包分发，也有导出接口，
/// 但此前<b>仓内零消费者</b>——"包内产物 == 读取内容"没有任何路径验证过。本接缝把"读"变成
/// 与"导出"同一份编译期单源的<b>一等能力</b>，宿主不必自己拼 <c>skills/feishu-{domain}/...</c> 路径、
/// 也不必做文件 IO（库仍然不做 IO：返回值直接来自编译期常量）。
/// </para>
/// <para>
/// <b>为什么不是文件路径</b>：包内布局（<c>skills/feishu-{domain}/SKILL.md</c>）是实现细节；
/// 把它暴露成"你去读这个路径"会把宿主的读取方式与包的目录布局绑死，且宿主在非文件环境
/// （如单文件/AOT 发布、内存宿主）下无法使用。按域读取则是稳定语义。
/// </para>
/// <para>
/// <b>与 <see cref="IToolSchemaExporter"/> 的关系</b>：导出面向"搬运整套产物"，
/// 读取面向"按需取一篇"。二者数据源同一（编译期 <c>FeishuToolGuidance</c> + 工具面常量），
/// 一致性由 <c>SkillsVersionConsistencyContractGuards</c> 逐条断言。
/// </para>
/// </remarks>
public interface ISkillDocumentReader
{
    /// <summary>全部产物的相对路径（<c>Ordinal</c> 升序；与导出清单的 <c>files[].path</c> 同源）。</summary>
    IReadOnlyList<string> Paths { get; }

    /// <summary>产物所属包版本（frontmatter 的 <c>package_version</c>，取自程序集版本）。</summary>
    string PackageVersion { get; }

    /// <summary>按路径读取产物（不存在 ⇒ <see langword="null"/>）。</summary>
    /// <param name="path">产物相对路径（如 <c>skills/feishu-im/SKILL.md</c>）。</param>
    /// <returns>内容；不存在为 <see langword="null"/>。</returns>
    string? Read(string path);

    /// <summary>按域读取该域的 <c>SKILL.md</c>（不存在 ⇒ <see langword="null"/>）。</summary>
    /// <param name="domain">域前缀（如 <c>im</c>、<c>bitable</c>）。</param>
    /// <returns>SKILL.md 全文；该域无产物为 <see langword="null"/>。</returns>
    string? ReadSkill(string domain);

    /// <summary>按域 + 主题读取 references 细则（不存在 ⇒ <see langword="null"/>）。</summary>
    /// <param name="domain">域前缀。</param>
    /// <param name="topic">主题（如 <c>filter-sort-syntax</c>）。</param>
    /// <returns>细则全文；不存在为 <see langword="null"/>。</returns>
    string? ReadReference(string domain, string topic);
}

/// <summary>
/// <see cref="ISkillDocumentReader"/> 的实现：数据源 = 编译期 <c>FeishuSkillDocumentBuilder.Build()</c>。
/// </summary>
/// <remarks>
/// <b>零文件 IO、零反射</b>：产物在编译期已固化进程序集（<c>Guidance.g.cs</c> 等），本类型只做字典查找。
/// 因此"包里发的 skills"与"宿主读到的 skills"在结构上不可能是两份东西——
/// 这正是 F-8 要消除的"零消费方"风险。
/// </remarks>
internal sealed class FeishuSkillDocumentReader : ISkillDocumentReader
{
    private readonly Dictionary<string, string> _byPath;

    public FeishuSkillDocumentReader()
    {
        _byPath = FeishuSkillDocumentBuilder.Build()
            .ToDictionary(static d => d.Path, static d => d.Content, StringComparer.Ordinal);

        Paths = [.. _byPath.Keys.OrderBy(static p => p, StringComparer.Ordinal)];
    }

    /// <inheritdoc />
    public IReadOnlyList<string> Paths { get; }

    /// <inheritdoc />
    public string PackageVersion => FeishuSkillDocumentBuilder.PackageVersion;

    /// <inheritdoc />
    public string? Read(string path)
        => path is not null && _byPath.TryGetValue(path, out var content) ? content : null;

    /// <inheritdoc />
    public string? ReadSkill(string domain)
        => Read(SkillPath(domain));

    /// <inheritdoc />
    public string? ReadReference(string domain, string topic)
        => Read($"{FeishuSkillDocumentBuilder.RootDirectory}/feishu-{domain}/{FeishuSkillDocumentBuilder.ReferencesDirectory}/{topic}.md");

    private static string SkillPath(string domain)
        => $"{FeishuSkillDocumentBuilder.RootDirectory}/feishu-{domain}/{FeishuSkillDocumentBuilder.SkillFileName}";
}
