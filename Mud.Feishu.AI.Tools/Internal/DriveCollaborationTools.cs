// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.Drive;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Drive 协作面工具执行器（评论 4 个 + 权限 6 个，A1 批次）：
/// <c>drive.list_comments</c>/<c>drive.add_comment</c>/<c>drive.reply_comment</c>/<c>drive.resolve_comment</c> +
/// <c>drive.get_permission_public</c>/<c>drive.update_permission_public</c>/<c>drive.grant_permission</c>/
/// <c>drive.update_permission_member</c>/<c>drive.remove_permission</c>/<c>drive.transfer_owner</c>。
/// </summary>
/// <remarks>
/// 执行骨架（catch/回填/截断）由 <see cref="ToolExecutor"/> 承担；本类只保留参数校验与投影语义。
/// 写操作默认空名单不启用，启用前须经宿主授权（<c>IToolExecutionAuthorizer</c>）。
/// </remarks>
internal sealed class DriveCollaborationTools(
    Mud.Feishu.IFeishuTenantV1DriveComments commentsClient,
    Mud.Feishu.IFeishuTenantV1DrivePermissions permissionsClient,
    IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV1DriveComments _commentsClient = commentsClient
        ?? throw new ArgumentNullException(nameof(commentsClient));
    private readonly Mud.Feishu.IFeishuTenantV1DrivePermissions _permissionsClient = permissionsClient
        ?? throw new ArgumentNullException(nameof(permissionsClient));
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;
    private readonly int _maxAutoFetchItems = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxAutoFetchPages;

    // ─────────────────────────── 评论面（4 个） ───────────────────────────

    /// <summary>drive.list_comments：列出云文档评论（分页）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantDriveListCommentsTool))]
    public Task<FeishuToolResult> ListCommentsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DriveListComments, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DriveListCommentsArgs.Unpack(arguments);

            // B1：fetch_all=true 时循环翻页（唯一翻页实现 ToolPagination）。
            if (args.FetchAll == true)
            {
                var maxItems = ToolPagination.ResolveMaxItems(args.MaxItems, _maxAutoFetchItems);
                var aggregated = await ToolPagination.AggregateOutcomesAsync<ApiPageListResult<FileComment>>(
                    async (token, ct) => FeishuApiResultReader.Read(await _commentsClient
                        .GetCommentsPageListAsync(
                            args.FileToken,
                            args.FileType,
                            is_solved: args.IsSolved,
                            page_size: PageSizes.DriveFiles,
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
                    page_size: PageSizes.DriveFiles,
                    page_token: args.PageToken,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectComments);
        });
    }

    /// <summary>drive.add_comment：添加全文评论。</summary>
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
                    executor.ToolName, "POST", $"/open-apis/drive/v1/files/{args.FileToken}/comments",
                    ToolDryRun.IdempotencyNote(null),
                    ("file_token", args.FileToken.Length), ("file_type", args.FileType.Length), ("text", args.Text.Length)));
            }

            var request = new CreateFileCommentRequest
            {
                ReplyList = new CreateFileCommentReplyList
                {
                    Replies =
                    [
                        new CreateFileCommentReply
                        {
                            Content = new ReplyContent
                            {
                                Elements =
                                [
                                    new ReplyElement
                                    {
                                        Type = "text_run",
                                        TextRun = new ReplyElementTextRun { Text = args.Text },
                                    },
                                ],
                            },
                        },
                    ],
                },
            };

            var outcome = FeishuApiResultReader.Read(await _commentsClient
                .CreateFileCommentAsync(
                    args.FileToken,
                    args.FileType,
                    request,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data => new JsonObject
            {
                ["comment_id"] = data.CommentId,
            });
        });
    }

    /// <summary>drive.reply_comment：回复评论。</summary>
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
                    executor.ToolName, "POST", $"/open-apis/drive/v1/files/{args.FileToken}/comments/{args.CommentId}/replies",
                    ToolDryRun.IdempotencyNote(null),
                    ("file_token", args.FileToken.Length), ("comment_id", args.CommentId.Length),
                    ("file_type", args.FileType.Length), ("text", args.Text.Length)));
            }

            var request = new CreateFileCommentReplyRequest
            {
                Content = new ReplyContent
                {
                    Elements =
                    [
                        new ReplyElement
                        {
                            Type = "text_run",
                            TextRun = new ReplyElementTextRun { Text = args.Text },
                        },
                    ],
                },
            };

            var outcome = FeishuApiResultReader.Read(await _commentsClient
                .CreateFileCommentReplyAsync(
                    args.FileToken,
                    args.CommentId,
                    args.FileType,
                    request,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data => new JsonObject
            {
                ["reply_id"] = data.ReplyId,
            });
        });
    }

    /// <summary>drive.resolve_comment：解决/恢复评论。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantDriveResolveCommentTool))]
    public Task<FeishuToolResult> ResolveCommentAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DriveResolveComment, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DriveResolveCommentArgs.Unpack(arguments);

            if (args.IsSolved is null)
            {
                throw new ArgumentException("is_solved 不能为空（true=解决，false=恢复）");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH", $"/open-apis/drive/v1/files/{args.FileToken}/comments/{args.CommentId}",
                    ToolDryRun.IdempotencyNote(null),
                    ("file_token", args.FileToken.Length), ("comment_id", args.CommentId.Length),
                    ("file_type", args.FileType.Length)));
            }

            var request = new PatchFileCommentRequest
            {
                IsSolved = args.IsSolved.Value,
            };

            // PatchFileCommentAsync 返回 FeishuNullDataApiResult（无业务载荷），
            // 走 FeishuApiResultReader.Read<object> 解包：Code != 0 → 失败，否则成功。
            var outcome = FeishuApiResultReader.Read(await _commentsClient
                .PatchFileCommentAsync(
                    args.FileToken,
                    args.CommentId,
                    args.FileType,
                    request,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, _ => new JsonObject
            {
                ["resolved"] = args.IsSolved.Value,
            });
        });
    }

    // ─────────────────────────── 权限面（6 个） ───────────────────────────

    /// <summary>drive.get_permission_public：获取公开权限设置。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantDriveGetPermissionPublicTool))]
    public Task<FeishuToolResult> GetPermissionPublicAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DriveGetPermissionPublic, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DriveGetPermissionPublicArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _permissionsClient
                .GetPermissionPublicAsync(
                    args.Token,
                    args.Type,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectPermissionPublic);
        });
    }

    /// <summary>drive.update_permission_public：更新公开权限设置。</summary>
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
                    executor.ToolName, "PATCH", $"/open-apis/drive/v2/permissions/{args.Token}/public",
                    ToolDryRun.IdempotencyNote(null),
                    ("token", args.Token.Length), ("type", args.Type.Length)));
            }

            var request = new UpdateDrivePermissionsRequest();
            if (args.ExternalAccess is not null) request.ExternalAccessEntity = args.ExternalAccess;
            if (args.SecurityEntity is not null) request.SecurityEntity = args.SecurityEntity;
            if (args.CommentEntity is not null) request.CommentEntity = args.CommentEntity;
            if (args.ShareEntity is not null) request.ShareEntity = args.ShareEntity;
            if (args.LinkShareEntity is not null) request.LinkShareEntity = args.LinkShareEntity;

            var outcome = FeishuApiResultReader.Read(await _permissionsClient
                .UpdatePermissionPublicAsync(
                    args.Token,
                    args.Type,
                    request,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectPermissionPublic);
        });
    }

    /// <summary>drive.grant_permission：添加协作者权限。</summary>
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
                    executor.ToolName, "POST", $"/open-apis/drive/v1/permissions/{args.Token}/members",
                    ToolDryRun.IdempotencyNote(null),
                    ("token", args.Token.Length), ("type", args.Type.Length),
                    ("member_type", args.MemberType.Length), ("member_id", args.MemberId.Length),
                    ("perm", args.Perm.Length)));
            }

            var request = new CreatePermissionMemberRequest
            {
                MemberType = args.MemberType,
                MemberId = args.MemberId,
                Perm = args.Perm,
            };

            var outcome = FeishuApiResultReader.Read(await _permissionsClient
                .CreatePermissionMemberAsync(
                    args.Token,
                    args.Type,
                    request,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["member_id"] = data.Member?.MemberId,
                ["perm"] = data.Member?.Perm,
            });
        });
    }

    /// <summary>drive.update_permission_member：更新协作者权限。</summary>
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
                    executor.ToolName, "PUT", $"/open-apis/drive/v1/permissions/{args.Token}/members/{args.MemberId}",
                    ToolDryRun.IdempotencyNote(null),
                    ("token", args.Token.Length), ("type", args.Type.Length),
                    ("member_id", args.MemberId.Length), ("perm", args.Perm.Length)));
            }

            var request = new UpdatePermissionMemberRequest
            {
                Perm = args.Perm,
            };

            var outcome = FeishuApiResultReader.Read(await _permissionsClient
                .UpdatePermissionMemberAsync(
                    args.Token,
                    args.MemberId,
                    args.Type,
                    request,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["member_id"] = data.Member?.MemberId,
                ["perm"] = data.Member?.Perm,
            });
        });
    }

    /// <summary>drive.remove_permission：删除协作者权限。</summary>
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
                    executor.ToolName, "DELETE", $"/open-apis/drive/v1/permissions/{args.Token}/members/{args.MemberId}",
                    ToolDryRun.IdempotencyNote(null),
                    ("token", args.Token.Length), ("type", args.Type.Length),
                    ("member_id", args.MemberId.Length)));
            }

            var request = new DeletePermissionMemberRequest();

            // DeletePermissionMemberAsync 返回 FeishuNullDataApiResult（无业务载荷）。
            var outcome = FeishuApiResultReader.Read(await _permissionsClient
                .DeletePermissionMemberAsync(
                    args.Token,
                    args.MemberId,
                    args.Type,
                    request,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, _ => new JsonObject
            {
                ["deleted"] = true,
            });
        });
    }

    /// <summary>drive.transfer_owner：转移所有者。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantDriveTransferOwnerTool))]
    public Task<FeishuToolResult> TransferOwnerAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.DriveTransferOwner, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = DriveTransferOwnerArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", $"/open-apis/drive/v1/permissions/{args.Token}/members/transfer_owner",
                    ToolDryRun.IdempotencyNote(null),
                    ("token", args.Token.Length), ("type", args.Type.Length),
                    ("member_id", args.MemberId.Length)));
            }

            var request = new TransferOwnerPermissionMemberRequest
            {
                MemberId = args.MemberId,
            };

            // TransferOwnerPermissionMemberAsync 返回 FeishuNullDataApiResult（无业务载荷）。
            var outcome = FeishuApiResultReader.Read(await _permissionsClient
                .TransferOwnerPermissionMemberAsync(
                    args.Token,
                    args.Type,
                    request,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, _ => new JsonObject
            {
                ["transferred"] = true,
            });
        });
    }

    // ─────────────────────────── 投影 ───────────────────────────

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
                ["reply_count"] = comment.ReplyList?.Replies?.Length ?? 0,
                ["created_at"] = comment.CreateTime,
            });
        }

        return envelope;
    }

    /// <summary>get/update_permission_public 投影：公开权限设置白名单字段。</summary>
    private static JsonObject ProjectPermissionPublic(PermissionPublicResult data)
    {
        var perm = data.PermissionPublic;
        return new JsonObject
        {
            ["external_access_entity"] = perm?.ExternalAccessEntity,
            ["security_entity"] = perm?.SecurityEntity,
            ["comment_entity"] = perm?.CommentEntity,
            ["share_entity"] = perm?.ShareEntity,
            ["manage_collaborator_entity"] = perm?.ManageCollaboratorEntity,
            ["link_share_entity"] = perm?.LinkShareEntity,
            ["copy_entity"] = perm?.CopyEntity,
            ["lock_switch"] = perm?.LockSwitch,
        };
    }

    // ─────────────────────────── 校验 ───────────────────────────

    /// <summary>合法的 member_type 值（白名单校验，防注入）。</summary>
    private static readonly HashSet<string> ValidMemberTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "email", "openid", "openchat", "opendepartmentid", "userid", "unionid", "groupid",
    };

    private static void ValidateMemberType(string memberType)
    {
        if (!ValidMemberTypes.Contains(memberType))
        {
            throw new ArgumentException(
                $"member_type '{memberType}' 不合法。可用值：{string.Join(" / ", ValidMemberTypes.OrderBy(static x => x, StringComparer.Ordinal))}");
        }
    }
}
