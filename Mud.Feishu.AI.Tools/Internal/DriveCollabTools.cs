// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.DataModels.Drive;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Drive 协作面评论工具执行器（<c>drive.list_comments</c> / <c>drive.add_comment</c> /
/// <c>drive.reply_comment</c> / <c>drive.resolve_comment</c>）。
/// </summary>
/// <remarks>执行骨架（catch/回填/截断）由 <see cref="ToolExecutor"/> 承担；本类只保留参数校验与投影语义。</remarks>
internal sealed class DriveCommentTools(
    Mud.Feishu.IFeishuTenantV1DriveComments commentsClient,
    IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV1DriveComments _commentsClient = commentsClient
        ?? throw new ArgumentNullException(nameof(commentsClient));
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;
    private readonly int _maxAutoFetchItems = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxAutoFetchPages;

    /// <summary>drive.list_comments：列出云文档评论（分页；白名单 comment_id/user_id/is_solved/reply_count/created_at）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantDriveListCommentsTool))]
    public Task<FeishuToolResult> ListCommentsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DriveListComments, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DriveListCommentsArgs.Unpack(arguments);

            // B1：fetch_all=true 时由 ToolPagination 循环翻页（唯一翻页实现），false 时等价既有单页行为。
            if (args.FetchAll == true)
            {
                var maxItems = ToolPagination.ResolveMaxItems(args.MaxItems, _maxAutoFetchItems);
                var aggregated = await ToolPagination.AggregateOutcomesAsync<ApiPageListResult<FileComment>>(
                    async (token, ct) => FeishuApiResultReader.Read(await _commentsClient
                        .GetCommentsPageListAsync(
                            args.FileToken,
                            args.FileType,
                            is_solved: args.IsSolved,
                            page_size: PageSizes.DriveComments,
                            page_token: token,
                            cancellationToken: ct)
                        .ConfigureAwait(false)),
                    page => (page.HasMore, page.PageToken),
                    page => ProjectComments(page)["items"]!.AsArray(),
                    maxItems,
                    ToolPagination.ResolveMaxPages(_maxAutoFetchPages),
                    cancellationToken).ConfigureAwait(false);
                return executor.FromPagedResult(aggregated, maxItems);
            }

            var outcome = FeishuApiResultReader.Read(await _commentsClient
                .GetCommentsPageListAsync(
                    args.FileToken,
                    args.FileType,
                    is_solved: args.IsSolved,
                    page_size: PageSizes.DriveComments,
                    page_token: args.PageToken,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectComments);
        });
    }

    /// <summary>drive.add_comment：添加全文评论（<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantDriveAddCommentTool))]
    public Task<FeishuToolResult> AddCommentAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DriveAddComment, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DriveAddCommentArgs.Unpack(arguments);
            if (string.IsNullOrWhiteSpace(args.Text))
            {
                throw new ArgumentException("text 不能为空");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/drive/v1/files/{file_token}/comments",
                    ToolDryRun.IdempotencyNote(null),
                    ("file_type", args.FileType.Length), ("text", args.Text.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _commentsClient
                .CreateFileCommentAsync(
                    args.FileToken,
                    args.FileType,
                    new CreateFileCommentRequest
                    {
                        ReplyList = new CreateFileCommentReplyList
                        {
                            Replies = [new CreateFileCommentReply { Content = BuildReplyContent(args.Text) }],
                        },
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data => new JsonObject { ["comment_id"] = data.CommentId });
        });
    }

    /// <summary>drive.reply_comment：回复指定评论（<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantDriveReplyCommentTool))]
    public Task<FeishuToolResult> ReplyCommentAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DriveReplyComment, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DriveReplyCommentArgs.Unpack(arguments);
            if (string.IsNullOrWhiteSpace(args.Text))
            {
                throw new ArgumentException("text 不能为空");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/drive/v1/files/{file_token}/comments/{comment_id}/replies",
                    ToolDryRun.IdempotencyNote(null),
                    ("file_type", args.FileType.Length), ("text", args.Text.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _commentsClient
                .CreateFileCommentReplyAsync(
                    args.FileToken,
                    args.CommentId,
                    args.FileType,
                    new CreateFileCommentReplyRequest { Content = BuildReplyContent(args.Text) },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data => new JsonObject { ["reply_id"] = data.ReplyId });
        });
    }

    /// <summary>drive.resolve_comment：解决/恢复评论（<c>dry_run=true</c> 时只预演）。</summary>
    /// <remarks>
    /// <c>is_solved</c> 是必填布尔——引擎解包映射表暂不支持必填布尔（MUDFT020/021 约束），
    /// Schema 层未标 required，缺失时在此兜底拒绝（与「Schema 是提示不是安全边界」契约一致）。
    /// </remarks>
    [FeishuToolHandler(typeof(IFeishuTenantDriveResolveCommentTool))]
    public Task<FeishuToolResult> ResolveCommentAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DriveResolveComment, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DriveResolveCommentArgs.Unpack(arguments);
            if (args.IsSolved is null)
            {
                throw new ArgumentException("缺少必填参数 is_solved（true=解决，false=恢复）");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH", "/open-apis/drive/v1/files/{file_token}/comments/{comment_id}",
                    ToolDryRun.IdempotencyNote(null),
                    ("file_type", args.FileType.Length)));
            }

            var nullDataResult = await _commentsClient
                .PatchFileCommentAsync(
                    args.FileToken,
                    args.CommentId,
                    args.FileType,
                    new PatchFileCommentRequest { IsSolved = args.IsSolved.Value },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (RequireNullDataSuccess(executor.ToolName, nullDataResult) is { } failure)
            {
                return failure;
            }

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["resolved"] = true,
                ["comment_id"] = args.CommentId,
                ["is_solved"] = args.IsSolved.Value,
            });
        });
    }

    /// <summary>构造全文评论/回复的正文内容（单文本元素）。</summary>
    private static ReplyContent BuildReplyContent(string text) => new()
    {
        Elements = [new ReplyElement { TextRun = new ReplyElementTextRun { Text = text } }],
    };

    /// <summary>list_comments 投影：items（comment_id/user_id/is_solved/reply_count/created_at）+ 翻页契约。</summary>
    private static JsonObject ProjectComments(ApiPageListResult<FileComment> data)
    {
        var envelope = new JsonObject
        {
            ["items"] = new JsonArray(),
            ["has_more"] = data.HasMore,
        };
        if (!string.IsNullOrEmpty(data.PageToken))
        {
            envelope["page_token"] = data.PageToken;
        }

        foreach (var comment in data.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["comment_id"] = comment.CommentId,
                ["user_id"] = comment.UserId,
                ["is_solved"] = comment.IsSolved,
                ["reply_count"] = comment.ReplyList?.Replies?.Length,
                ["created_at"] = comment.CreateTime,
            });
        }

        return envelope;
    }

    /// <summary>
    /// 解包 <see cref="FeishuNullDataApiResult"/>（不走 <c>FeishuApiResultReader.Read&lt;T&gt;</c>，T 不可推断）。
    /// </summary>
    /// <returns>成功返回 <see langword="null"/>；失败返回结构化错误结果（由调用方直接作为工具结果返回）。</returns>
    private static FeishuToolResult? RequireNullDataSuccess(string toolName, FeishuNullDataApiResult? result)
    {
        if (result is null)
        {
            throw new ArgumentException("飞书接口无响应（result 为空）");
        }

        return result.Code == 0
            ? null
            : FeishuToolResult.FromError(FeishuToolBinding.StructuredError(
                toolName, result.Code,
                $"飞书接口返回错误 code={result.Code.ToString(CultureInfo.InvariantCulture)}, msg={result.Msg ?? "(无错误信息)"}"));
    }
}

/// <summary>
/// Drive 协作面权限工具执行器（<c>drive.get_permission_public</c> / <c>drive.update_permission_public</c> /
/// <c>drive.grant_permission</c> / <c>drive.update_permission_member</c> / <c>drive.remove_permission</c> /
/// <c>drive.transfer_owner</c>）。
/// </summary>
/// <remarks>
/// <para>
/// 权限域按引擎风险分级一律 <c>high-risk-write</c>（方法名触发危险词表 <c>permission</c>；
/// 含底层为 GET 的 get_permission_public——见 Curation/FeishuDriveToolInterfaces.cs 的 MUDFT017 修复记录），
/// 全部走写面授权门禁；写工具默认空名单不启用，启用前须经宿主授权。
/// </para>
/// <para>
/// dry_run 路由用 Schema 模板字面量（不代入实际 token，见 <see cref="ToolDryRun"/> 的安全约束）。
/// </para>
/// </remarks>
internal sealed class DrivePermissionTools(
    Mud.Feishu.IFeishuTenantV1DrivePermissions permissionsClient,
    IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV1DrivePermissions _permissionsClient = permissionsClient
        ?? throw new ArgumentNullException(nameof(permissionsClient));
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>drive.get_permission_public：获取公开权限设置（底层 GET 只读；风险分级见类注释）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantDriveGetPermissionPublicTool))]
    public Task<FeishuToolResult> GetPermissionPublicAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DriveGetPermissionPublic, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DriveGetPermissionPublicArgs.Unpack(arguments);

            // 底层虽是 GET 只读，但本工具被引擎风险分级为写面（MUDFT017）——dry_run 契约对
            // 全部写工具一致生效：预演只回 method/path，不调用下游。
            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "GET", "/open-apis/drive/v2/permissions/{token}/public",
                    ToolDryRun.IdempotencyNote(null),
                    ("type", args.Type.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _permissionsClient
                .GetPermissionPublicAsync(args.Token, args.Type, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectPermissionPublic);
        });
    }

    /// <summary>drive.update_permission_public：更新公开权限设置（<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantDriveUpdatePermissionPublicTool))]
    public Task<FeishuToolResult> UpdatePermissionPublicAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DriveUpdatePermissionPublic, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DriveUpdatePermissionPublicArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH", "/open-apis/drive/v2/permissions/{token}/public",
                    ToolDryRun.IdempotencyNote(null),
                    ("external_access", args.ExternalAccess?.Length ?? 0),
                    ("security_entity", args.SecurityEntity?.Length ?? 0),
                    ("comment_entity", args.CommentEntity?.Length ?? 0),
                    ("share_entity", args.ShareEntity?.Length ?? 0),
                    ("link_share_entity", args.LinkShareEntity?.Length ?? 0)));
            }

            var outcome = FeishuApiResultReader.Read(await _permissionsClient
                .UpdatePermissionPublicAsync(
                    args.Token,
                    args.Type,
                    new UpdateDrivePermissionsRequest
                    {
                        ExternalAccessEntity = args.ExternalAccess,
                        SecurityEntity = args.SecurityEntity,
                        CommentEntity = args.CommentEntity,
                        ShareEntity = args.ShareEntity,
                        LinkShareEntity = args.LinkShareEntity,
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectPermissionPublic);
        });
    }

    /// <summary>drive.grant_permission：添加协作者权限（<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantDriveGrantPermissionTool))]
    public Task<FeishuToolResult> GrantPermissionAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DriveGrantPermission, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DriveGrantPermissionArgs.Unpack(arguments);

            ValidateMemberType(args.MemberType);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/drive/v1/permissions/{token}/members",
                    ToolDryRun.IdempotencyNote(null),
                    ("type", args.Type.Length),
                    ("member_type", args.MemberType.Length),
                    ("member_id", args.MemberId.Length),
                    ("perm", args.Perm.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _permissionsClient
                .CreatePermissionMemberAsync(
                    args.Token,
                    args.Type,
                    new CreatePermissionMemberRequest
                    {
                        MemberType = args.MemberType,
                        MemberId = args.MemberId,
                        Perm = args.Perm,
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectPermissionMember);
        });
    }

    /// <summary>drive.update_permission_member：更新协作者权限档位（<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantDriveUpdatePermissionMemberTool))]
    public Task<FeishuToolResult> UpdatePermissionMemberAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DriveUpdatePermissionMember, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DriveUpdatePermissionMemberArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH", "/open-apis/drive/v1/permissions/{token}/members/{member_id}",
                    ToolDryRun.IdempotencyNote(null),
                    ("type", args.Type.Length),
                    ("perm", args.Perm.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _permissionsClient
                .UpdatePermissionMemberAsync(
                    args.Token,
                    args.MemberId,
                    args.Type,
                    new UpdatePermissionMemberRequest { Perm = args.Perm },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectPermissionMember);
        });
    }

    /// <summary>drive.remove_permission：删除协作者权限（<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantDriveRemovePermissionTool))]
    public Task<FeishuToolResult> RemovePermissionAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DriveRemovePermission, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DriveRemovePermissionArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE", "/open-apis/drive/v1/permissions/{token}/members/{member_id}",
                    ToolDryRun.IdempotencyNote(null),
                    ("type", args.Type.Length)));
            }

            var nullDataResult = await _permissionsClient
                .DeletePermissionMemberAsync(
                    args.Token,
                    args.MemberId,
                    args.Type,
                    new DeletePermissionMemberRequest(),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (RequireNullDataSuccess(executor.ToolName, nullDataResult) is { } failure)
            {
                return failure;
            }

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["removed"] = true,
                ["member_id"] = args.MemberId,
            });
        });
    }

    /// <summary>drive.transfer_owner：转移云文档所有者（<c>dry_run=true</c> 时只预演）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantDriveTransferOwnerTool))]
    public Task<FeishuToolResult> TransferOwnerAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DriveTransferOwner, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DriveTransferOwnerArgs.Unpack(arguments);

            ValidateMemberType(args.MemberType);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/drive/v1/permissions/{token}/members/transfer_owner",
                    ToolDryRun.IdempotencyNote(null),
                    ("type", args.Type.Length),
                    ("member_type", args.MemberType.Length),
                    ("member_id", args.MemberId.Length)));
            }

            var nullDataResult = await _permissionsClient
                .TransferOwnerPermissionMemberAsync(
                    args.Token,
                    args.Type,
                    new TransferOwnerPermissionMemberRequest
                    {
                        MemberType = args.MemberType,
                        MemberId = args.MemberId,
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (RequireNullDataSuccess(executor.ToolName, nullDataResult) is { } failure)
            {
                return failure;
            }

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["transferred"] = true,
                ["member_id"] = args.MemberId,
            });
        });
    }

    /// <summary>合法的 member_type 值（取值闭集白名单，防"拼错的类型静默落到下游"）。</summary>
    private static readonly HashSet<string> ValidMemberTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "email", "openid", "openchat", "opendepartmentid", "userid", "unionid", "groupid",
    };

    /// <summary>校验 member_type 落在闭集内（非法值 → 结构化 invalid_args，附全部合法值）。</summary>
    private static void ValidateMemberType(string memberType)
    {
        if (!ValidMemberTypes.Contains(memberType))
        {
            throw new ArgumentException(
                $"member_type '{memberType}' 不合法。可用值：{string.Join(" / ", ValidMemberTypes.OrderBy(static x => x, StringComparer.Ordinal))}");
        }
    }

    /// <summary>公开权限设置投影：link_share_entity/external_access 等白名单字段。</summary>
    private static JsonObject ProjectPermissionPublic(PermissionPublicResult data)
    {
        var detail = data.PermissionPublic;
        return new JsonObject
        {
            ["external_access_entity"] = detail?.ExternalAccessEntity,
            ["security_entity"] = detail?.SecurityEntity,
            ["comment_entity"] = detail?.CommentEntity,
            ["share_entity"] = detail?.ShareEntity,
            ["link_share_entity"] = detail?.LinkShareEntity,
            ["lock_switch"] = detail?.LockSwitch,
        };
    }

    /// <summary>协作者投影：member_id/perm（member_type 为请求回显，不在响应白名单内）。</summary>
    private static JsonObject ProjectPermissionMember(PermissionMemberOopsResult data) => new()
    {
        ["member_id"] = data.Member?.MemberId,
        ["perm"] = data.Member?.Perm,
    };

    /// <summary>
    /// 解包 <see cref="FeishuNullDataApiResult"/>（不走 <c>FeishuApiResultReader.Read&lt;T&gt;</c>，T 不可推断）。
    /// </summary>
    /// <returns>成功返回 <see langword="null"/>；失败返回结构化错误结果（由调用方直接作为工具结果返回）。</returns>
    private static FeishuToolResult? RequireNullDataSuccess(string toolName, FeishuNullDataApiResult? result)
    {
        if (result is null)
        {
            throw new ArgumentException("飞书接口无响应（result 为空）");
        }

        return result.Code == 0
            ? null
            : FeishuToolResult.FromError(FeishuToolBinding.StructuredError(
                toolName, result.Code,
                $"飞书接口返回错误 code={result.Code.ToString(CultureInfo.InvariantCulture)}, msg={result.Msg ?? "(无错误信息)"}"));
    }
}
