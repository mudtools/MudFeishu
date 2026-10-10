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
/// Security「行为审计日志」只读工具执行器（F-1 P0 批：<c>security.query_audit_logs</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>软依赖</b>：<c>IFeishuTenantV1SecurityAuditLog</c> 可空可选——宿主未启用审计 API 时本域软缺席
/// （工具不入注册表；即使被白名单点名也返回"能力不可用"的可执行错误，而不是抛异常逃出执行链）。
/// </para>
/// <para>
/// <b>参数前置校验（F-8）</b>：平台对审计查询有两条硬约束——起止相差 ≤ 30 天、page_size ∈ [1,200]，
/// 另有"operator_value 必须与 operator_type 成对"的隐式要求。三条都在<b>本地</b>拒绝并给出可自我纠正的
/// 错误（<c>invalid_args</c>），既不消耗下游调用，也不让模型等一个必然失败的请求。
/// </para>
/// <para>
/// <b>PII 最小披露</b>：回填排查所需事实（事件/操作者/对象/接收者/时间/IP/城市/设备型号/OS），
/// <b>有意丢弃</b> Mac 地址与企业编号（见 Curation 的投影纪律说明）。
/// </para>
/// </remarks>
internal sealed class SecurityAuditLogTools(
    IOptions<FeishuAgentOptions> options,
    Mud.Feishu.IFeishuTenantV1SecurityAuditLog? auditLogClient = null)
{
    /// <summary>平台约束：查询时间起止相差不得超过 30 天（秒）。</summary>
    private const int MaxRangeSeconds = 30 * 24 * 60 * 60;

    private readonly Mud.Feishu.IFeishuTenantV1SecurityAuditLog? _auditLogClient = auditLogClient;

    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>security.query_audit_logs：查询成员行为审计日志。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantSecurityQueryAuditLogsTool))]
    public Task<FeishuToolResult> QueryAuditLogsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SecurityQueryAuditLogs, _maxResultLength);
        var client = _auditLogClient;
        if (client is null)
        {
            // 软缺席（与 BoardTools / CountryRegionTools 同口径）：可执行的错误文本，不是异常。
            return Task.FromResult(FeishuToolResult.FromError(FeishuToolBinding.StructuredError(
                executor.ToolName,
                "未注册 IFeishuTenantV1SecurityAuditLog 客户端——行为审计日志能力需宿主启用该 API（软缺席，不影响其它域工具）")));
        }

        return executor.RunAsync(async () =>
        {
            var args = SecurityQueryAuditLogsArgs.Unpack(arguments);
            var query = BuildQuery(args);

            var outcome = FeishuApiResultReader.Read(await client
                .GetAuditLogDataAsync(query, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, Project);
        });
    }

    /// <summary>扁平入参 → 查询对象，并施加平台的本地可判定约束（越界即 <c>invalid_args</c>）。</summary>
    private static GetAuditLogDataQuery BuildQuery(SecurityQueryAuditLogsArgs args)
    {
        if (args.Latest is { } latest && args.Oldest is { } oldest)
        {
            if (latest <= oldest)
            {
                throw new ArgumentException("latest 必须晚于 oldest（两者都是秒级时间戳）");
            }

            if (latest - oldest > MaxRangeSeconds)
            {
                throw new ArgumentException("查询时间范围起止相差不能超过 30 天，请缩小 oldest/latest 区间");
            }
        }

        if (!string.IsNullOrEmpty(args.OperatorValue) && string.IsNullOrEmpty(args.OperatorType))
        {
            throw new ArgumentException("填写 operator_value 时必须同时给出 operator_type（user 或 bot）");
        }

        return new GetAuditLogDataQuery
        {
            Oldest = args.Oldest,
            Latest = args.Latest,
            EventName = args.EventName,
            EventModule = args.EventModule,
            OperatorType = args.OperatorType,
            OperatorValue = args.OperatorValue,
            UserIdType = args.UserIdType,
            // 页大小**不下发**（保持 null = 平台默认 20）：运维参数不进模型可见面（DP-B1-0：页不进、预算进）。
            PageToken = args.PageToken,
        };
    }

    /// <summary>
    /// 白名单投影（PII 最小披露）：事件 / 操作者 / 对象 / 接收者 / 时间 / IP / 城市 / 设备型号 / OS。
    /// 有意丢弃 <c>mc</c>（Mac 地址，最强设备指纹）与 <c>operator_tenant</c>（对企业编号无增量）。
    /// </summary>
    private static JsonObject Project(GetAuditLogDataResult result)
    {
        // 分页信封**必须**经 ToolResultJsons 单源构造（PaginationContractGuards 机械拦截）：
        // 手写 items/has_more/page_token 会让"空 token 不写入"这类语义静默漂移
        // （模型会照抄空 token 续页 → 重复拉第一页且无法自查）。
        // 条目**直接**写进信封的 items 数组（不能先建临时数组再搬——JsonNode 单亲约束会抛"已有父节点"）。
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        var items = envelope["items"]!.AsArray();
        foreach (var item in result.Items ?? [])
        {
            var objects = new JsonArray();
            foreach (var entity in item.Objects ?? [])
            {
                objects.AddNode(new JsonObject
                {
                    ["object_type"] = entity.ObjectType,
                    ["object_value"] = entity.ObjectValue,
                    ["object_name"] = entity.ObjectName,
                    ["object_owner"] = entity.ObjectOwner,
                });
            }

            var recipients = new JsonArray();
            foreach (var recipient in item.Recipients ?? [])
            {
                recipients.AddNode(new JsonObject
                {
                    ["recipient_type"] = recipient.RecipientType,
                    ["recipient_value"] = recipient.RecipientValue,
                });
            }

            // AddNode（而非 Add）：见 ToolResultText.AddNode——泛型 Add<T>(T) 的裁剪/AOT 注解会红。
            items.AddNode(new JsonObject
            {
                ["event_id"] = item.EventId,
                ["unique_id"] = item.UniqueId,
                ["event_name"] = item.EventName,
                ["event_module"] = item.EventModule,
                ["operator_type"] = item.OperatorType,
                ["operator_value"] = item.OperatorValue,
                ["event_time"] = item.EventTime,
                ["ip"] = item.Ip,
                ["department_ids"] = ProjectDepartmentIds(item.DepartmentIds),
                ["objects"] = objects,
                ["recipients"] = recipients,
                ["audit_detail"] = item.AuditDetail is null
                    ? null
                    : new JsonObject
                    {
                        ["city"] = item.AuditDetail.City,
                        ["device_model"] = item.AuditDetail.DeviceModel,
                        ["os"] = item.AuditDetail.Os,
                    },
            });
        }

        if (items.Count == 0)
        {
            // 空结果哨兵（F-5）：显式"没查到"，并给出最可能的原因（时间窗/筛选条件），而不是让模型猜。
            envelope["found"] = false;
            envelope["message"] = "该时间窗与筛选条件下未查询到审计日志。请确认：① 起止时间是否为秒级时间戳且相差 ≤ 30 天；"
                + "② event_name / event_module 是否为平台支持的取值；③ 操作人/操作对象筛选条件是否过窄。";
        }

        return envelope;
    }

    /// <summary>部门 ID 数组 → <see cref="JsonArray"/>（<c>AddNode</c> 而非泛型 <c>Add&lt;T&gt;</c>：AOT 裁剪注解开销）。</summary>
    private static JsonArray? ProjectDepartmentIds(string[]? departmentIds)
    {
        if (departmentIds is null || departmentIds.Length == 0)
        {
            return null;
        }

        var array = new JsonArray();
        foreach (var id in departmentIds)
        {
            array.AddNode(JsonValue.Create(id));
        }

        return array;
    }
}
