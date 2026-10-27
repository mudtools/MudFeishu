// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.Feishu.AI.Tools.Tests.Eval;

/// <summary>
/// 模型可见工具面（评测的打分对象）：从生成器 Schema 取 <c>name / description / parameters</c>
/// （可选再加 <c>x-feishu.affordance</c>），用 TF-IDF 余弦对用户意图做确定性排序。
/// </summary>
/// <remarks>
/// <para>
/// <b>度量对象</b>：这套评测要离线、可复现、零新增包，因此测的是「<b>模型可见的那段文本
/// 能否把意图指向目标工具</b>」——351 工具面上选择负担的根因即在此。它是描述判别度的下界，
/// 不是某个大模型的准确率（后者需在真实 harness 上跑，属另一层）。
/// </para>
/// <para>
/// <b>两个视图</b>：<c>bare</c> = 名字 + 参数名 + 描述（即 <c>tools/list</c> 给模型的内容）；
/// <c>guided</c> = 再加 affordance。差值只说明<b>词汇层面</b>的增益，实测为负——affordance 承载
/// 何时用/勿用/前置条件，属语义面，其收益要由场景层读。
/// </para>
/// <para>
/// <b>权重纪律</b>：工具名片段 ×3、参数名 ×2、描述 ×1、affordance ×1；
/// 描述中出现的<b>其它工具名</b>交叉引用（如「配合 bitable.query_records 使用」）降到 ×0.3，
/// 否则 A 的描述提到 B 会让 A 抢走本该指向 B 的意图。idf 只在 bare 视图上统计，
/// 两个视图共用同一条 idf 轴，差值才可比。
/// </para>
/// </remarks>
internal sealed class EvalToolSurface
{
    private const double NameWeight = 3.0;
    private const double ParameterNameWeight = 2.0;
    private const double DescriptionWeight = 1.0;
    private const double AffordanceWeight = 1.0;
    private const double CrossReferenceWeight = 0.3;

    private static readonly Lazy<EvalToolSurface> Shared = new(static () => new EvalToolSurface());

    private readonly Dictionary<string, Entry> _byName;
    private readonly List<JsonDocument> _keptAlive;
    private readonly Dictionary<string, double> _idf = new(StringComparer.Ordinal);
    private readonly Dictionary<bool, Dictionary<string, Dictionary<string, double>>> _vectors = new();
    private readonly Dictionary<bool, Dictionary<string, double>> _norms = new();

    private EvalToolSurface()
    {
        _byName = new Dictionary<string, Entry>(StringComparer.Ordinal);
        _keptAlive = new List<JsonDocument>();

        foreach (var pair in FeishuToolSchemas.SchemaByToolName)
        {
            var document = JsonDocument.Parse(pair.Value);
            _keptAlive.Add(document);
            _byName[pair.Key] = Parse(pair.Key, document.RootElement);
        }

        _nameFragments = new HashSet<string>(
            _byName.Keys.SelectMany(name => Tokenizer.Tokenize(name)),
            StringComparer.Ordinal);

        var rawBare = RawView(guided: false);
        BuildIdf(rawBare);
        BuildView(rawBare, guided: false);
        BuildView(RawView(guided: true), guided: true);
    }

    /// <summary>单个工具的模型可见面。</summary>
    internal sealed record Entry(
        string Name,
        string Domain,
        string Description,
        IReadOnlyList<string> ParameterNames,
        string Risk,
        bool IsWrite,
        string Identity,
        bool HasAffordance,
        string Affordance,
        JsonElement Parameters);

    internal static EvalToolSurface Instance => Shared.Value;

    internal IReadOnlyList<Entry> Entries => _byName.Values.OrderBy(static e => e.Name, StringComparer.Ordinal).ToArray();

    internal bool Contains(string name) => _byName.ContainsKey(name);

    internal Entry? Find(string name) => _byName.TryGetValue(name, out var entry) ? entry : null;

    /// <summary>该工具 Schema 的 <c>parameters</c> 子树（参数层评测的判据来源）。</summary>
    internal JsonElement ParametersOf(string name) => _byName[name].Parameters;

    /// <summary>对意图排序（同分按工具名序数升序，保证可复现）；零分工具不入候选集。</summary>
    internal string[] Rank(string intent, bool guided, int topK)
    {
        var query = TermFrequencies(Tokenizer.Tokenize(intent));
        var queryWeighted = Weight(query, guided);
        var queryNorm = Norm(queryWeighted);
        if (queryNorm <= 0)
        {
            return [];
        }

        var vectors = _vectors[guided];
        var norms = _norms[guided];
        var scored = new List<(string Name, double Score)>(vectors.Count);
        foreach (var document in vectors)
        {
            var norm = norms[document.Key];
            if (norm <= 0)
            {
                continue;
            }

            double dot = 0;
            foreach (var term in queryWeighted)
            {
                if (document.Value.TryGetValue(term.Key, out var value))
                {
                    dot += term.Value * value;
                }
            }

            if (dot > 0)
            {
                scored.Add((document.Key, dot / (queryNorm * norm)));
            }
        }

        return scored
            .OrderByDescending(static s => s.Score)
            .ThenBy(static s => s.Name, StringComparer.Ordinal)
            .Take(topK)
            .Select(static s => s.Name)
            .ToArray();
    }

    /// <summary>目标工具在意图排序中的名次（1 起）；未入候选集返回 -1。</summary>
    internal int RankOf(string intent, string tool, string[] ranked)
    {
        var index = Array.IndexOf(ranked, tool);
        return index < 0 ? -1 : index + 1;
    }

    /// <summary>首位与次位得分之差（判别余量；≤0 即"这份可见面分不出两个工具"）。</summary>
    internal double TopMargin(string intent, string tool, bool guided)
    {
        var ranked = Rank(intent, guided, 20);
        if (ranked.Length < 2 || ranked[0] != tool)
        {
            return 0;
        }

        return Score(intent, ranked[0], guided) - Score(intent, ranked[1], guided);
    }

    private readonly HashSet<string> _nameFragments;

    private static Entry Parse(string name, JsonElement root)
    {
        var extended = root.GetProperty("x-feishu");
        var parameters = root.GetProperty("parameters");
        var propertyNames = parameters.TryGetProperty("properties", out var properties)
            ? properties.EnumerateObject().Select(static p => p.Name).ToArray()
            : [];

        var affordance = new StringBuilder();
        if (extended.TryGetProperty("affordance", out var affordanceNode)
            && affordanceNode.ValueKind == JsonValueKind.Object)
        {
            foreach (var section in affordanceNode.EnumerateObject())
            {
                if (section.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var line in section.Value.EnumerateArray())
                    {
                        affordance.Append(line.ValueKind == JsonValueKind.String ? line.GetString() : line.GetRawText())
                            .Append(' ');
                    }
                }
            }
        }

        return new Entry(
            name,
            name.Split('.')[0],
            root.TryGetProperty("description", out var description) ? description.GetString() ?? string.Empty : string.Empty,
            propertyNames,
            extended.TryGetProperty("risk", out var risk) ? risk.GetString() ?? "read" : "read",
            extended.TryGetProperty("is_write", out var isWrite) && isWrite.GetBoolean(),
            extended.TryGetProperty("identity", out var identity) ? identity.GetString() ?? "tenant" : "tenant",
            extended.TryGetProperty("affordance", out _),
            affordance.ToString(),
            parameters);
    }

    private Dictionary<string, Dictionary<string, double>> RawView(bool guided)
        => _byName.Values.ToDictionary(
            static entry => entry.Name,
            entry => RawTermWeights(entry, guided),
            StringComparer.Ordinal);

    private void BuildView(Dictionary<string, Dictionary<string, double>> raw, bool guided)
    {
        var vectors = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal);
        var norms = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var pair in raw)
        {
            var weighted = Weight(pair.Value, guided);
            vectors[pair.Key] = weighted;
            norms[pair.Key] = Norm(weighted);
        }

        _vectors[guided] = vectors;
        _norms[guided] = norms;
    }

    private void BuildIdf(Dictionary<string, Dictionary<string, double>> rawBare)
    {
        var documentFrequency = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var vector in rawBare.Values)
        {
            foreach (var term in vector.Keys)
            {
                documentFrequency[term] = documentFrequency.GetValueOrDefault(term) + 1;
            }
        }

        var total = rawBare.Count;
        foreach (var pair in documentFrequency)
        {
            _idf[pair.Key] = Math.Log(1.0 + (double)total / pair.Value);
        }
    }

    private Dictionary<string, double> RawTermWeights(Entry entry, bool guided)
    {
        var weights = new Dictionary<string, double>(StringComparer.Ordinal);
        var ownNameTokens = new HashSet<string>(Tokenizer.Tokenize(entry.Name), StringComparer.Ordinal);

        void Add(string text, double weight)
        {
            foreach (var token in Tokenizer.Tokenize(text))
            {
                var effective = !ownNameTokens.Contains(token) && IsCrossReference(token)
                    ? CrossReferenceWeight
                    : weight;
                weights[token] = weights.GetValueOrDefault(token) + effective;
            }
        }

        Add(entry.Name, NameWeight);
        foreach (var parameter in entry.ParameterNames)
        {
            Add(parameter, ParameterNameWeight);
        }

        Add(entry.Description, DescriptionWeight);
        if (guided && entry.HasAffordance)
        {
            Add(entry.Affordance, AffordanceWeight);
        }

        return weights;
    }

    private bool IsCrossReference(string token)
        => token.All(static c => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9'))
        && _nameFragments.Contains(token);

    private Dictionary<string, double> Weight(Dictionary<string, double> termFrequencies, bool guided)
    {
        // idf 轴只来自 bare 视图（见类型注释）。affordance 独有的词在 bare 语料里没有文档命中，
        // 按「全集外」取最高 idf——它必然是判别词，而不是「文档/记录」这类满屏通用词。
        var unseenIdf = Math.Log(1.0 + _byName.Count);
        var result = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var pair in termFrequencies)
        {
            var tf = 1.0 + Math.Log(pair.Value);
            var idf = _idf.TryGetValue(pair.Key, out var value) ? value : (guided ? unseenIdf : 0);
            if (idf > 0)
            {
                result[pair.Key] = tf * idf;
            }
        }

        return result;
    }

    private static double Norm(Dictionary<string, double> vector)
        => Math.Sqrt(vector.Values.Sum(static v => v * v));

    private double Score(string intent, string tool, bool guided)
    {
        var query = Weight(TermFrequencies(Tokenizer.Tokenize(intent)), guided);
        var queryNorm = Norm(query);
        var document = _vectors[guided][tool];
        var norm = _norms[guided][tool];
        if (queryNorm <= 0 || norm <= 0)
        {
            return 0;
        }

        return query.Where(pair => document.TryGetValue(pair.Key, out _))
            .Sum(pair => pair.Value * document[pair.Key]) / (queryNorm * norm);
    }

    private static Dictionary<string, double> TermFrequencies(IReadOnlyList<string> tokens)
    {
        var counts = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var token in tokens)
        {
            counts[token] = counts.GetValueOrDefault(token) + 1.0;
        }

        return counts;
    }
}

/// <summary>
/// 混合分词器：拉丁/数字串成词（<c>_</c> 与 <c>.</c> 为分隔符），CJK 连续串同时产出
/// 单字与相邻二字组（中文没有空格，二字组是"多维表格 / 新增记录"这类术语的最小可辨单元）。
/// </summary>
internal static class Tokenizer
{
    internal static IReadOnlyList<string> Tokenize(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var tokens = new List<string>();
        var latin = new StringBuilder();
        var cjk = new List<char>();

        void FlushLatin()
        {
            if (latin.Length > 0)
            {
                tokens.Add(latin.ToString().ToLowerInvariant());
                latin.Clear();
            }
        }

        void FlushCjk()
        {
            for (var i = 0; i < cjk.Count; i++)
            {
                tokens.Add(cjk[i].ToString());
                if (i + 1 < cjk.Count)
                {
                    tokens.Add(string.Concat(cjk[i], cjk[i + 1]));
                }
            }

            cjk.Clear();
        }

        foreach (var ch in text)
        {
            if (ch >= 0x4E00 && ch <= 0x9FFF)
            {
                FlushLatin();
                cjk.Add(ch);
            }
            else if (char.IsLetterOrDigit(ch))
            {
                FlushCjk();
                latin.Append(ch);
            }
            else
            {
                FlushLatin();
                FlushCjk();
            }
        }

        FlushLatin();
        FlushCjk();
        return tokens;
    }
}
