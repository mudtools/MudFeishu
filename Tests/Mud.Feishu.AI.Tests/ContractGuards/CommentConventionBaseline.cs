// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.Feishu.AI.Tests.ContractGuards;

/// <summary>手写运行时层的注释扫描结果（文件度量 + 两类违规清单）。</summary>
/// <remarks>
/// 违规清单的指纹是「仓库相对路径|记号」而非行号——行号随编辑漂移，会把冻结基线变成噪声源。
/// 代价是同文件内同记号的第二次出现不会被单独记账，由注释行数冻结（<see cref="FileCommentStats"/>）
/// 兜底：整体只增不删仍会报红。
/// </remarks>
internal sealed class CommentConventionAnalysis
{
    private CommentConventionAnalysis(
        SortedDictionary<string, FileCommentStats> files,
        IReadOnlyList<string> unresolvedAnchors,
        IReadOnlyList<string> unlinkedNarration)
    {
        Files = files;
        UnresolvedAnchors = unresolvedAnchors;
        UnlinkedNarration = unlinkedNarration;
        CommentLines = files.Values.Sum(static stats => stats.Comments);
    }

    /// <summary>文件度量（键为 <c>{工程}/{相对路径}</c>，一律用 <c>/</c> 分隔）。</summary>
    public IReadOnlyDictionary<string, FileCommentStats> Files { get; }

    /// <summary>无法解析为受治理文档的评审编号（指纹 <c>路径|编号</c>）。</summary>
    public IReadOnlyList<string> UnresolvedAnchors { get; }

    /// <summary>叙述过程但未给可解析文档链接的注释块（指纹 <c>路径|关键词</c>）。</summary>
    public IReadOnlyList<string> UnlinkedNarration { get; }

    /// <summary>扫描到的注释行总数（防"扫描面为空"的假绿）。</summary>
    public int CommentLines { get; }

    /// <summary>扫描若干工程的全部 <c>*.cs</c>（跳过 <c>obj</c>/<c>bin</c>），产出度量与违规清单。</summary>
    /// <param name="repositoryRoot">仓库根目录。</param>
    /// <param name="projects">待扫描工程目录名。</param>
    /// <param name="anchorResolves">评审编号是否能在受治理文档里解析。</param>
    /// <param name="linkExists">文档链接是否存在。</param>
    /// <returns>扫描结果。</returns>
    public static CommentConventionAnalysis Run(
        string repositoryRoot,
        IReadOnlyList<string> projects,
        Func<string, bool> anchorResolves,
        Func<string, bool> linkExists)
    {
        var files = new SortedDictionary<string, FileCommentStats>(StringComparer.Ordinal);
        var anchors = new SortedSet<string>(StringComparer.Ordinal);
        var narration = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var project in projects)
        {
            var root = Path.Combine(repositoryRoot, project);
            if (!Directory.Exists(root))
            {
                throw new DirectoryNotFoundException($"{project} 目录不存在（扫描面不得静默为空）：{root}");
            }

            foreach (var path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (IsBuildArtifact(repositoryRoot, path))
                {
                    continue;
                }

                var source = File.ReadAllText(path);
                var key = RelativeKey(repositoryRoot, path);
                files[key] = CommentConventionScanner.Measure(source);

                foreach (var token in CommentConventionScanner.FindUnresolvedAnchors(source, anchorResolves))
                {
                    anchors.Add($"{key}|{token}");
                }

                foreach (var keyword in CommentConventionScanner.FindUnlinkedNarration(source, linkExists))
                {
                    narration.Add($"{key}|{keyword}");
                }
            }
        }

        return new CommentConventionAnalysis(files, anchors.ToList(), narration.ToList());
    }

    private static bool IsBuildArtifact(string repositoryRoot, string path)
    {
        var relative = RelativeKey(repositoryRoot, path);
        return relative.Contains("/obj/", StringComparison.Ordinal)
               || relative.Contains("/bin/", StringComparison.Ordinal);
    }

    private static string RelativeKey(string repositoryRoot, string path)
        => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');
}

/// <summary>冻结基线：逐文件度量 + 两类已登记违规指纹。</summary>
/// <param name="Files">文件度量。</param>
/// <param name="Anchors">已登记的未解析评审编号。</param>
/// <param name="Narration">已登记的未链接过程叙述。</param>
internal sealed record CommentBaseline(
    IReadOnlyDictionary<string, FileCommentStats> Files,
    IReadOnlySet<string> Anchors,
    IReadOnlySet<string> Narration);

/// <summary>
/// 基线的读写。写入必须**确定性**（键排序、无时间戳），否则每次重新固化都产生假 diff；
/// 手写 JSON 而不是走序列化器，是为了让 diff 直接可读（这份文件本身就是清理台账）。
/// </summary>
internal static class CommentBaselineJson
{
    private const string Note =
        "注释预算冻结基线（手写运行时层）。只允许下压：清理后重新固化，新违规必须解释而不是重新固化。"
        + "重新固化：pwsh ./scripts/comment-audit.ps1 -UpdateBaseline";

    /// <summary>把扫描结果写成确定性 JSON。</summary>
    /// <param name="analysis">扫描结果。</param>
    /// <param name="projects">作用域工程（写入文件头，防作用域漂移）。</param>
    /// <returns>JSON 文本。</returns>
    public static string Write(CommentConventionAnalysis analysis, IReadOnlyList<string> projects)
    {
        var json = new StringBuilder();
        json.AppendLine("{");
        json.Append("  \"note\": ").AppendLine(Quote(Note) + ",");
        json.Append("  \"scope\": [")
            .Append(string.Join(", ", projects.Select(Quote)))
            .AppendLine("],");
        json.AppendLine("  \"files\": {");

        var entries = analysis.Files.ToList();
        for (var i = 0; i < entries.Count; i++)
        {
            var stats = entries[i].Value;
            json.Append("    ").Append(Quote(entries[i].Key)).Append(": {")
                .Append("\"lines\": ").Append(stats.Lines).Append(", ")
                .Append("\"comments\": ").Append(stats.Comments).Append(", ")
                .Append("\"maxBlock\": ").Append(stats.MaxBlock)
                .Append(i == entries.Count - 1 ? "}" : "},").AppendLine();
        }

        json.AppendLine("  },");
        AppendArray(json, "anchors", analysis.UnresolvedAnchors);
        json.AppendLine(",");
        AppendArray(json, "narration", analysis.UnlinkedNarration);
        json.AppendLine();
        json.AppendLine("}");
        return json.ToString();
    }

    /// <summary>解析基线 JSON。</summary>
    /// <param name="text">JSON 文本。</param>
    /// <returns>基线。</returns>
    public static CommentBaseline Read(string text)
    {
        using var document = JsonDocument.Parse(text);
        var root = document.RootElement;

        var files = new Dictionary<string, FileCommentStats>(StringComparer.Ordinal);
        foreach (var property in root.GetProperty("files").EnumerateObject())
        {
            files[property.Name] = new FileCommentStats(
                property.Value.GetProperty("lines").GetInt32(),
                property.Value.GetProperty("comments").GetInt32(),
                property.Value.GetProperty("maxBlock").GetInt32());
        }

        return new CommentBaseline(files, ReadArray(root, "anchors"), ReadArray(root, "narration"));
    }

    /// <summary>读取基线的作用域声明。</summary>
    /// <param name="text">JSON 文本。</param>
    /// <returns>作用域工程名。</returns>
    public static IReadOnlyList<string> ReadScope(string text)
    {
        using var document = JsonDocument.Parse(text);
        return document.RootElement.GetProperty("scope")
            .EnumerateArray()
            .Select(static item => item.GetString() ?? string.Empty)
            .ToList();
    }

    private static void AppendArray(StringBuilder json, string name, IReadOnlyList<string> values)
    {
        json.Append("  ").Append(Quote(name)).Append(": [");
        if (values.Count == 0)
        {
            json.Append(']');
            return;
        }

        json.AppendLine();
        for (var i = 0; i < values.Count; i++)
        {
            json.Append("    ").Append(Quote(values[i])).Append(i == values.Count - 1 ? string.Empty : ",").AppendLine();
        }

        json.Append("  ]");
    }

    private static HashSet<string> ReadArray(JsonElement root, string name)
    {
        var values = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in root.GetProperty(name).EnumerateArray())
        {
            values.Add(item.GetString() ?? string.Empty);
        }

        return values;
    }

    private static string Quote(string value)
        => "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
}
