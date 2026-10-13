// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Departments;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// contact 部门面「查询向」只读工具执行器（F-1 P0：<c>contact.get_department_parents</c> / <c>contact.search_departments</c>）。
/// </summary>
/// <remarks>
/// <para>
/// 放在独立执行器而非并入 <see cref="ContactDepartmentTools"/>：后者已承担"下钻 + 取成员"两条带
/// <c>fetch_all</c> 聚合的路径；本类两条是<b>反向/搜索</b>路径（单页 + page_token），混在一起会让
/// 两个不同形态的分页契约挤在同一个类里，维护性更差。
/// </para>
/// <para>
/// <b>非 PII</b>：部门是组织架构，投影沿用 <see cref="ContactDepartmentTools"/> 的白名单口径
/// （ID / 名称 / 父 ID / 成员数），<b>不带出 leader/HRBP 等人员字段</b>——
/// "这个部门归谁管"要经 <c>contact.*</c> 的用户侧授权面，不由本工具顺手给出。
/// </para>
/// <para>
/// <b>软依赖</b>：<c>IFeishuTenantV3Departments</c> 可空可选，缺席时返回可执行错误文本。
/// </para>
/// </remarks>
internal sealed class ContactDepartmentQueryTools(
    IOptions<FeishuAgentOptions> options,
    Mud.Feishu.IFeishuTenantV3Departments? departmentsClient = null)
{
    private readonly Mud.Feishu.IFeishuTenantV3Departments? _departmentsClient = departmentsClient;

    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>P-0 补齐（统一分页语义）：<c>fetch_all</c> 的条目/页数预算（来自宿主配置）。</summary>
    private readonly int _maxAutoFetchItems = ToolExecutor.Require(options).MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = ToolExecutor.Require(options).MaxAutoFetchPages;

    /// <summary>contact.get_department_parents：查询部门的上级链（自下而上）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantContactGetDepartmentParentsTool))]
    public Task<FeishuToolResult> GetDepartmentParentsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ContactGetDepartmentParents, _maxResultLength);
        if (_departmentsClient is null)
        {
            return Task.FromResult(FeishuToolResult.FromError(FeishuToolBinding.StructuredError(
                executor.ToolName,
                "未注册 IFeishuTenantV3Departments 客户端——部门查询能力需宿主启用通讯录部门 API（软缺席）")));
        }

        return executor.RunAsync(async () =>
        {
            var args = ContactGetDepartmentParentsArgs.Unpack(arguments);

            // P-0 补齐：统一分页出口——聚合路径与单页路径共用同一次调用定义与同一份投影。
            async Task<FeishuApiOutcome<ApiPageListResult<GetDepartmentInfo>>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _departmentsClient
                    .GetParentDepartmentsByIdAsync(
                        args.DepartmentId,
                        page_token: token,
                        cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync, result => Project(result, topLevelHint: true), args.FetchAll, args.MaxItems, args.PageToken,
                _maxAutoFetchItems, _maxAutoFetchPages, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>contact.search_departments：按名称搜索部门。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantContactSearchDepartmentsTool))]
    public Task<FeishuToolResult> SearchDepartmentsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ContactSearchDepartments, _maxResultLength);
        if (_departmentsClient is null)
        {
            return Task.FromResult(FeishuToolResult.FromError(FeishuToolBinding.StructuredError(
                executor.ToolName,
                "未注册 IFeishuTenantV3Departments 客户端——部门查询能力需宿主启用通讯录部门 API（软缺席）")));
        }

        return executor.RunAsync(async () =>
        {
            var args = ContactSearchDepartmentsArgs.Unpack(arguments);

            // P-0 补齐：统一分页出口——聚合路径与单页路径共用同一次调用定义与同一份投影。
            async Task<FeishuApiOutcome<ApiPageListResult<GetDepartmentInfo>>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _departmentsClient
                    .SearchDepartmentsAsync(
                        new SearchDepartmentRequest { Query = args.Query },
                        page_token: token,
                        cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync, result => Project(result, topLevelHint: false), args.FetchAll, args.MaxItems, args.PageToken,
                _maxAutoFetchItems, _maxAutoFetchPages, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// 部门条目投影（与 ContactDepartmentTools 同白名单口径）+ 信封单源构造。
    /// </summary>
    /// <param name="topLevelHint">为空结果补充"已是顶层 / 换个关键字"的差异化提示。</param>
    private static JsonObject Project(ApiPageListResult<GetDepartmentInfo> result, bool topLevelHint)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        var items = envelope["items"]!.AsArray();

        foreach (var dept in result.Items ?? [])
        {
            items.AddNode(new JsonObject
            {
                ["department_id"] = dept.DepartmentId,
                ["open_department_id"] = dept.OpenDepartmentId,
                ["name"] = dept.Name,
                ["parent_department_id"] = dept.ParentDepartmentId,
                ["member_count"] = dept.MemberCount,
            });
        }

        if (items.Count == 0)
        {
            envelope["found"] = false;
            envelope["message"] = topLevelHint
                ? "未查询到该部门的上级——通常说明它已是顶层（根）部门；也可核对 department_id 是否正确。"
                : "没有匹配该关键字的部门。可换更短的关键字（如「研发」而非「研发中心一部」）重试，"
                  + "或先用 contact.list_departments 从根部门下钻确认层级。";
        }

        return envelope;
    }
}
