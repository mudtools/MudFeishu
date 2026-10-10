// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;
using Mud.Feishu.AI.Tools.Generated;
using Mud.Feishu.AI.Tools.Tools;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// 运行时 schema 自省执行器（<c>feishu.schema_read</c>，R6 / S3）。
/// </summary>
/// <remarks>
/// <para>
/// 数据源是编译期 <see cref="FeishuToolMethodCatalog"/>（1228 个 SDK 方法的结构化事实），
/// 无下游调用、无网络、无租户依赖。支持三种查询模式：
/// <list type="bullet">
/// <item>精确匹配：给定方法限定名（如 <c>IFeishuTenantV1OkrPeriod.ListPeriodsAsync</c>），返回单条目；</item>
/// <item>关键字搜索：按方法名/接口名/模块大小写不敏感搜索，返回匹配条目；</item>
/// <item>模块过滤：给定模块名（如 <c>Okr</c>），返回该模块下全部方法。</item>
/// </list>
/// </para>
/// <para>
/// <b>与 <c>feishu.capability_lookup</c> 的分工</b>：后者回答<b>分组级</b>存在性（"有没有"），
/// 本工具回答<b>方法级</b>签名事实（"怎么调"）——HTTP 方法、路由模板、参数位置与类型、令牌身份、风险分级。
/// </para>
/// </remarks>
internal sealed class SchemaReadTools(IOptions<FeishuAgentOptions> options)
{
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>feishu.schema_read：查询 SDK 方法签名事实。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantSchemaReadTool))]
    public Task<FeishuToolResult> SchemaReadAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.FeishuSchemaRead, _maxResultLength);
        return executor.RunAsync(() =>
        {
            var method = ToolArgs.OptionalString(arguments, "method");
            var keyword = ToolArgs.OptionalString(arguments, "keyword");
            var module = ToolArgs.OptionalString(arguments, "module");
            var limit = ToolArgs.OptionalInt(arguments, "limit") ?? 20;

            // 三个判据至少要有一个：目录是 Dictionary（无稳定顺序），
            // "什么都不给就返回前 20 条"会产出一个**任意**子集——看起来像答案，实际无意义。
            if (string.IsNullOrEmpty(method) && string.IsNullOrEmpty(keyword) && string.IsNullOrEmpty(module))
            {
                throw new ArgumentException(
                    "至少提供 method / keyword / module 之一（目录共 "
                    + FeishuToolMethodCatalog.SdkMethodCount.ToString(CultureInfo.InvariantCulture)
                    + " 个方法，不限定条件无法给出有意义的子集）。");
            }

            if (limit < 1)
            {
                limit = 20;
            }
            else if (limit > 100)
            {
                limit = 100;
            }

            var results = new List<FeishuToolMethodCatalog.Entry>();

            if (!string.IsNullOrEmpty(method))
            {
                if (FeishuToolMethodCatalog.ByQualifiedName.TryGetValue(method, out var entry))
                {
                    results.Add(entry);
                }
            }
            else
            {
                foreach (var pair in FeishuToolMethodCatalog.ByQualifiedName)
                {
                    if (results.Count >= limit)
                    {
                        break;
                    }

                    if (!string.IsNullOrEmpty(module)
                        && !string.Equals(pair.Value.Module, module, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(keyword))
                    {
                        var qualifiedName = pair.Key;
                        if (qualifiedName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) < 0
                            && pair.Value.Module.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }
                    }

                    results.Add(pair.Value);
                }
            }

            var array = new JsonArray();
            foreach (var entry in results)
            {
                array.AddNode(EntryToJson(entry));
            }

            var envelope = new JsonObject
            {
                ["sdk_method_count"] = FeishuToolMethodCatalog.SdkMethodCount,
                ["matched_count"] = results.Count,
                ["methods"] = array,
                ["note"] = "每条方法含：interface/method/http/route/token_kind/module/path_params/query_params/body_type/risk/curated/curated_tool。"
                    + "curated=true 表示已有策展工具可直接调用（curated_tool 给出工具名）；"
                    + "curated=false 表示该方法未策展为工具，**本工具集没有调用它的通道**——"
                    + "请如实告知用户该能力暂不可用（可建议宿主策展），不要臆造调用。",
            };

            // R-1：出站唯一出口（B-1 一类——此前 TruncateJson 的截断对模型不可见）。
            return Task.FromResult(ToolResultPipeline.OkJson(envelope, _maxResultLength));
        });
    }

    private static JsonObject EntryToJson(FeishuToolMethodCatalog.Entry entry)
    {
        var pathParams = new JsonArray();
        foreach (var p in entry.PathParams)
        {
            pathParams.AddNode(JsonValue.Create(p));
        }

        var queryParams = new JsonArray();
        foreach (var q in entry.QueryParams)
        {
            queryParams.AddNode(JsonValue.Create(q));
        }

        return new JsonObject
        {
            ["interface"] = entry.InterfaceName,
            ["method"] = entry.Method,
            ["qualified_name"] = entry.InterfaceName + "." + entry.Method,
            ["http"] = entry.Http,
            ["route"] = entry.Route,
            ["token_kind"] = entry.TokenKind,
            ["module"] = entry.Module,
            ["path_params"] = pathParams,
            ["query_params"] = queryParams,
            ["body_type"] = entry.BodyType,
            // R-5：风险词表单源（此前本类私有一份 RiskLabel，与 GenericApiTools 的另一份逐字重复，
            // 并与 FeishuToolRiskNames.ToLiteral 构成三份同源映射）。
            ["risk"] = FeishuToolRiskNames.ToLiteral(entry.Risk),
            ["curated"] = entry.Curated,
            ["curated_tool"] = entry.CuratedTool,
        };
    }
}