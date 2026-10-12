// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.DataModels.Security;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// security「应用 API 调用日志」只读工具执行器（F-1 收官切片：<c>security.list_openapi_logs</c>）。
/// </summary>
/// <remarks>
/// <para>
/// 与 <see cref="SecurityAuditLogTools"/>（<b>人</b>在控制台/客户端的操作审计）互补：本工具查
/// <b>应用</b>对开放接口的调用，是集成排障的第一现场。
/// </para>
/// <para>
/// <b>投影纪律（本工具最要紧的一条）</b>：日志详情里的 <c>payload</c> / <c>response</c> / <c>query_param</c>
/// 是<b>原始业务载荷</b>（可能含个人信息或机密字段）⇒ <b>一律丢弃</b>，只返回
/// <c>path</c> + <c>method</c> + <c>status_code</c> 的最小证据集：足以定位"哪个接口错了、错成什么状态"，
/// 不足以还原数据。要完整载荷请让宿主查自己的日志系统，而不是把它塞进模型上下文。
/// </para>
/// <para>
/// <b>软依赖</b>：<c>IFeishuTenantV1SecurityOpenApiLog</c> 可空可选（与审计日志同口径：缺席返回可执行错误文本，不抛异常）。
/// </para>
/// </remarks>
internal sealed class SecurityOpenApiLogTools(
    IOptions<FeishuAgentOptions> options,
    Mud.Feishu.IFeishuTenantV1SecurityOpenApiLog? openApiLogClient = null)
{
    /// <summary>与行为审计日志同源的本地时间窗上限（秒）：30 天。超过即本地拒绝，不浪费一次网络往返。</summary>
    private const long MaxWindowSeconds = 30L * 24 * 60 * 60;

    private readonly Mud.Feishu.IFeishuTenantV1SecurityOpenApiLog? _openApiLogClient = openApiLogClient;

    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>security.list_openapi_logs：按时间窗查询应用 API 调用日志。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantSecurityListOpenapiLogsTool))]
    public Task<FeishuToolResult> ListOpenApiLogsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SecurityListOpenapiLogs, _maxResultLength);
        if (_openApiLogClient is null)
        {
            return Task.FromResult(FeishuToolResult.FromError(FeishuToolBinding.StructuredError(
                executor.ToolName,
                "未注册 IFeishuTenantV1SecurityOpenApiLog 客户端——应用调用日志能力需宿主启用安全合规 API（软缺席，不影响其它域工具）")));
        }

        return executor.RunAsync(async () =>
        {
            var args = SecurityListOpenapiLogsArgs.Unpack(arguments);

            // 时间窗上限：本地先拦（与行为审计同口径），避免把超大时间窗打到服务端后被拒。
            if (args.Oldest is { } oldest && args.Latest is { } latest && latest - oldest > MaxWindowSeconds)
            {
                throw new ArgumentException($"时间窗不能超过 30 天（当前 oldest={oldest}、latest={latest}）；请缩小范围后重试。");
            }

            var request = new ListOpenApiLogDataRequest
            {
                StartTime = args.Oldest,
                EndTime = args.Latest,
                ApiKeys = string.IsNullOrEmpty(args.ApiKey) ? null : [args.ApiKey!],
                AppId = args.AppId,
                PageToken = args.PageToken,

                // page_size 刻意不下发（保持 SDK 默认）——运维参数不进模型可见面（DP-B1-0）。
            };

            var outcome = FeishuApiResultReader.Read(await _openApiLogClient
                .ListOpenApiLogDataAsync(request, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, Project);
        });
    }

    /// <summary>投影：分页信封（单源构造）+ 最小证据集条目（丢弃 payload / response / query_param）。</summary>
    private static JsonObject Project(ListOpenApiLogDataResult result)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore ?? false, result.PageToken);
        var items = envelope["items"]!.AsArray();

        foreach (var log in result.Items ?? [])
        {
            JsonNode? detail = null;
            if (log.LogDetail is { } d)
            {
                // 只取"足以定位问题"的三要素；payload / response / query_param 一律不带出（见类注释）。
                detail = new JsonObject
                {
                    ["path"] = d.Path,
                    ["method"] = d.Method,
                    ["status_code"] = d.StatusCode,
                };
            }

            items.AddNode(new JsonObject
            {
                ["id"] = log.Id,
                ["api_key"] = log.ApiKey,
                ["event_time"] = log.EventTime,
                ["app_id"] = log.AppId,
                ["ip"] = log.Ip,
                ["log_detail"] = detail,
            });
        }

        if (items.Count == 0)
        {
            envelope["found"] = false;
            envelope["message"] = "该时间窗内没有可返回的调用日志。常见原因：① 时间窗内确无调用；"
                + "② api_key / app_id 过滤过窄（可去掉过滤重试）；③ page_token 已到末页。";
        }

        return envelope;
    }
}
