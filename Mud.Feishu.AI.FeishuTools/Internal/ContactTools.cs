// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Users;

namespace Mud.Feishu.AI.FeishuTools.Internal;

/// <summary>
/// 通讯录三工具执行器（<c>contact.resolve_user</c> / <c>contact.get_user</c> / <c>contact.batch_get</c>）。
/// </summary>
/// <remarks>
/// <para>
/// 三者共用同一 SDK 客户端 <c>IFeishuTenantV3User</c>（租户令牌）：通讯录查询是企业内部目录访问，
/// 用租户身份而非用户身份，避免依赖每个终端用户的个人授权。
/// </para>
/// <para>
/// 投影白名单遵循统一纪律：<b>只回填模型继续调用的必要标识与判断依据</b>，不回传原始 DTO
/// （原始 DTO 含大量内部字段与嵌套结构，既浪费上下文又扩大注入面）。
/// </para>
/// </remarks>
internal sealed class ContactTools(
    Mud.Feishu.IFeishuTenantV3User userClient,
    IOptions<FeishuAgentOptions> options)
{
    /// <summary>user_id_type 缺省值（与 SDK 常量 <c>Consts.User_Id_Type</c> 一致）。</summary>
    private const string DefaultUserIdType = "open_id";

    private readonly Mud.Feishu.IFeishuTenantV3User _userClient = userClient
        ?? throw new ArgumentNullException(nameof(userClient));
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>contact.resolve_user：邮箱/手机号 → 用户 ID。</summary>
    public async Task<FeishuToolResult> ResolveUsersAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        try
        {
            var emails = ToolArgs.OptionalStringArray(arguments, "emails");
            var mobiles = ToolArgs.OptionalStringArray(arguments, "mobiles");
            var includeResigned = ToolArgs.OptionalBool(arguments, "include_resigned") ?? false;

            if (emails is null && mobiles is null)
            {
                throw new ArgumentException("emails 与 mobiles 至少需提供一个");
            }

            var total = (emails?.Length ?? 0) + (mobiles?.Length ?? 0);
            if (total > PageSizes.ContactResolve)
            {
                throw new ArgumentException(
                    $"emails 与 mobiles 合计最多 {PageSizes.ContactResolve.ToString(CultureInfo.InvariantCulture)} 项，实际 {total.ToString(CultureInfo.InvariantCulture)} 项");
            }

            var outcome = FeishuApiResultReader.Read(await _userClient
                .GetBatchUsersAsync(
                    new UserQueryRequest
                    {
                        Emails = [.. emails ?? []],
                        Mobiles = [.. mobiles ?? []],
                        IncludeResigned = includeResigned,
                    },
                    user_id_type: DefaultUserIdType,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            if (!outcome.Ok)
            {
                return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.ContactResolveUser, outcome.Code, outcome.ErrorText!));
            }

            var envelope = new JsonObject { ["items"] = new JsonArray() };
            foreach (var user in outcome.Data!.UserList ?? [])
            {
                envelope["items"]!.AsArray().AddNode(new JsonObject
                {
                    ["user_id"] = user.UserId,
                    ["email"] = user.Email,
                    ["mobile"] = user.Mobile,
                    ["status"] = ProjectStatus(user.Status),
                });
            }

            return FeishuToolResult.FromText(ToolResultText.TruncateJson(envelope.ToJsonString(), _maxResultLength));
        }
        catch (ArgumentException ex)
        {
            return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.ContactResolveUser, ex.Message));
        }
    }

    /// <summary>contact.get_user：单个用户详情。</summary>
    public async Task<FeishuToolResult> GetUserAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        try
        {
            var userId = ToolArgs.RequireString(arguments, "user_id");
            var userIdType = ToolArgs.OptionalString(arguments, "user_id_type") ?? DefaultUserIdType;

            var outcome = FeishuApiResultReader.Read(await _userClient
                .GetUserInfoByIdAsync(userId, user_id_type: userIdType, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            if (!outcome.Ok)
            {
                return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.ContactGetUser, outcome.Code, outcome.ErrorText!));
            }

            var envelope = new JsonObject { ["user"] = ProjectUser(outcome.Data) };
            return FeishuToolResult.FromText(ToolResultText.TruncateJson(envelope.ToJsonString(), _maxResultLength));
        }
        catch (ArgumentException ex)
        {
            return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.ContactGetUser, ex.Message));
        }
    }

    /// <summary>contact.batch_get：按 ID 批量取详情。</summary>
    public async Task<FeishuToolResult> BatchGetUsersAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        try
        {
            var userIds = ToolArgs.OptionalStringArray(arguments, "user_ids")
                ?? throw new ArgumentException("缺少必填参数 user_ids");
            var userIdType = ToolArgs.OptionalString(arguments, "user_id_type") ?? DefaultUserIdType;

            if (userIds.Length > PageSizes.ContactResolve)
            {
                throw new ArgumentException(
                    $"user_ids 最多 {PageSizes.ContactResolve.ToString(CultureInfo.InvariantCulture)} 个，实际 {userIds.Length.ToString(CultureInfo.InvariantCulture)} 个");
            }

            var outcome = FeishuApiResultReader.Read(await _userClient
                .GetUserByIdsAsync(userIds, user_id_type: userIdType, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            if (!outcome.Ok)
            {
                return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.ContactBatchGet, outcome.Code, outcome.ErrorText!));
            }

            var envelope = new JsonObject { ["items"] = new JsonArray() };
            foreach (var user in outcome.Data!.Items ?? [])
            {
                envelope["items"]!.AsArray().AddNode(ProjectUser(user));
            }

            return FeishuToolResult.FromText(ToolResultText.TruncateJson(envelope.ToJsonString(), _maxResultLength));
        }
        catch (ArgumentException ex)
        {
            return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.ContactBatchGet, ex.Message));
        }
    }

    /// <summary>用户详情白名单投影（不含 avatar URL 等长文本字段，避免无谓的上下文占用）。</summary>
    private static JsonObject ProjectUser(GetUserInfoResult user) => new()
    {
        ["user_id"] = user.UserId,
        ["open_id"] = user.OpenId,
        ["union_id"] = user.UnionId,
        ["name"] = user.Name,
        ["en_name"] = user.EnName,
        ["nickname"] = user.Nickname,
        ["email"] = user.Email,
        ["mobile"] = user.Mobile,
        ["employee_no"] = user.EmployeeNo,
        ["department_ids"] = ProjectStrings(user.DepartmentIds),
        ["leader_user_id"] = user.LeaderUserId,
        ["work_station"] = user.WorkStation,
        ["status"] = ProjectStatus(user.Status),
    };

    private static JsonNode? ProjectStatus(UserStatus? status) => status is null
        ? null
        : new JsonObject
        {
            ["is_activated"] = status.IsActivated,
            ["is_resigned"] = status.IsResigned,
            ["is_frozen"] = status.IsFrozen,
        };

    private static JsonArray? ProjectStrings(List<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return null;
        }

        var array = new JsonArray();
        foreach (var value in values)
        {
            array.AddNode(JsonValue.Create(value));
        }

        return array;
    }
}
