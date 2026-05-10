// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.ChatGroup;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuV1ChatGroup"/>接口的相关函数。
/// </summary>
public class IFeishuV1ChatGroupTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuV1ChatGroupTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroup.UpdateChatGroupByIdAsync(string, UpdateChatRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateChatGroupByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "avatar": "default-avatar_44ae0ca3-e140-494b-956f-78091e348435",
              "name": "群聊",
              "description": "测试群描述",
              "i18n_names": {
                "zh_cn": "群聊",
                "en_us": "group chat",
                "ja_jp": "グループチャット"
              },
              "add_member_permission": "all_members",
              "share_card_permission": "allowed",
              "at_all_permission": "all_members",
              "edit_permission": "all_members",
              "owner_id": "4d7a3c6g",
              "join_message_visibility": "only_owner",
              "leave_message_visibility": "only_owner",
              "membership_approval": "no_approval_required",
              "restricted_mode_setting": {
                "status": false,
                "screenshot_has_permission_setting": "all_members",
                "download_has_permission_setting": "all_members",
                "message_has_permission_setting": "all_members"
              },
              "chat_type": "private",
              "group_message_type": "chat",
              "urgent_setting": "all_members",
              "video_conference_setting": "all_members",
              "hide_member_count_setting": "all_members"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UpdateChatRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroup.UpdateChatGroupByIdAsync(string, UpdateChatRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateChatGroupByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "data": {},
                "msg": "success"
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CreateUpdateChatResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroup.DeleteChatGroupAsync(string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteChatGroupAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {}
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroup.UpdateChatModerationAsync(string, UpdateChatModerationRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateChatModerationAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "moderation_setting": "moderator_list",
              "moderator_added_list": [
                "4d7a3c6g"
              ],
              "moderator_removed_list": [
                "4d7a3c6g"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UpdateChatModerationRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.ModeratorAddedList);
        Assert.NotNull(requestBody.ModerationSetting);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroup.UpdateChatModerationAsync(string, UpdateChatModerationRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateChatModerationAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {}
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroup.GetChatGroupInoByIdAsync(string, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetChatGroupInoByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "avatar": "https://p3-lark-file.byteimg.com/img/lark-avatar-staging/default-avatar_44ae0ca3-e140-494b-956f-78091e348435~100x100.jpg",
                    "name": "测试群名称",
                    "description": "测试群描述",
                    "i18n_names": {
                        "zh_cn": "群聊",
                        "en_us": "group chat",
                        "ja_jp": "グループチャット"
                    },
                    "add_member_permission": "all_members",
                    "share_card_permission": "allowed",
                    "at_all_permission": "all_members",
                    "edit_permission": "all_members",
                    "owner_id_type": "user_id",
                    "owner_id": "4d7a3c6g",
                    "user_manager_id_list": [
                        "ou_9204a37300b3700d61effaa439f34295"
                    ],
                    "bot_manager_id_list": [
                        "cli_a3e157960e7294c"
                    ],
                    "group_message_type": "chat",
                    "chat_mode": "group",
                    "chat_type": "private",
                    "chat_tag": "inner",
                    "join_message_visibility": "only_owner",
                    "leave_message_visibility": "only_owner",
                    "membership_approval": "no_approval_required",
                    "moderation_permission": "all_members",
                    "external": false,
                    "tenant_key": "736588c9260f175e",
                    "user_count": "1",
                    "bot_count": "3",
                    "restricted_mode_setting": {
                        "status": false,
                        "screenshot_has_permission_setting": "all_members",
                        "download_has_permission_setting": "all_members",
                        "message_has_permission_setting": "all_members"
                    },
                    "urgent_setting": "all_members",
                    "video_conference_setting": "all_members",
                    "hide_member_count_setting": "all_members",
                    "chat_status": "normal"
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetChatGroupInfoResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Name);
        Assert.NotNull(result.Data.I18nNames);
        Assert.NotNull(result.Data.RestrictedModeSetting);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroup.PutChatGroupTopNoticeAsync(string, ChatTopNoticeRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_PutChatGroupTopNoticeAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "chat_top_notice": [
                {
                  "action_type": "2",
                  "message_id": "om_dc13264520392913993dd051dba21dcf"
                }
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<ChatTopNoticeRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.ChatTopNotice);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroup.PutChatGroupTopNoticeAsync(string, ChatTopNoticeRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_PutChatGroupTopNoticeAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {}
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroup.DeleteChatGroupTopNoticeAsync(string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteChatGroupTopNoticeAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {}
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroup.GetChatGroupPageListAsync(string, string, int?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetChatGroupPageListAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "items": [
                        {
                            "chat_id": "oc_a0553eda9014c201e6969b478895c230",
                            "avatar": "https://p3-lark-file.byteimg.com/img/lark-avatar-staging/default-avatar_44ae0ca3-e140-494b-956f-78091e348435~100x100.jpg",
                            "name": "测试群名称",
                            "description": "测试群描述",
                            "owner_id": "4d7a3c6g",
                            "owner_id_type": "user_id",
                            "external": false,
                            "tenant_key": "736588c9260f175e",
                            "chat_status": "normal"
                        }
                    ],
                    "page_token": "dmJCRHhpd3JRbGV1VEVNRFFyTitRWDY5ZFkybmYrMEUwMUFYT0VMMWdENEtuYUhsNUxGMDIwemtvdE5ORjBNQQ==",
                    "has_more": false
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiPageListResult<ChatItemInfo>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Items);
        Assert.NotEmpty(result.Data.PageToken!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroup.GetChatGroupPageListByKeywordAsync(string?, string, string, int?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetChatGroupPageListByKeywordAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "items": [
                        {
                            "chat_id": "oc_a0553eda9014c201e6969b478895c230",
                            "avatar": "https://p3-lark-file.byteimg.com/img/lark-avatar-staging/default-avatar_44ae0ca3-e140-494b-956f-78091e348435~100x100.jpg",
                            "name": "测试群名称",
                            "description": "测试群描述",
                            "owner_id": "4d7a3c6g",
                            "owner_id_type": "user_id",
                            "external": false,
                            "tenant_key": "736588c9260f175e",
                            "chat_status": "normal"
                        }
                    ],
                    "page_token": "dmJCRHhpd3JRbGV1VEVNRFFyTitRWDY5ZFkybmYrMEUwMUFYT0VMMWdENEtuYUhsNUxGMDIwemtvdE5ORjBNQQ==",
                    "has_more": true
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiPageListResult<ChatItemInfo>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Items);
        Assert.NotEmpty(result.Data.PageToken!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroup.GetChatGroupModeratorPageListByIdAsync(string, string, int?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetChatGroupModeratorPageListByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "moderation_setting": "moderator_list",
                    "page_token": "dmJCRHhpd3JRbGV1VEVNRFFyTitRWDY5ZFkybmYrMEUwMUFYT0VMMWdENEtuYUhsNUxGMDIwemtvdE5ORjBNQQ==",
                    "has_more": true,
                    "items": [
                        {
                            "user_id_type": "user_id",
                            "user_id": "4d7a3c6g",
                            "tenant_key": "2ca1d211f64f6438"
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<ChatGroupModeratorPageListResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Items);
        Assert.NotEmpty(result.Data.PageToken!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroup.GetChatGroupShareLinkByIdAsync(string, ShareLinkRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_GetChatGroupShareLinkByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "validity_period": "week"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<ShareLinkRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.ValidityPeriod!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroup.GetChatGroupShareLinkByIdAsync(string, ShareLinkRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetChatGroupShareLinkByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "share_link": "https://applink.feishu.cn/client/chat/chatter/add_by_link?link_token=3nf8789-4rfx-427d-a6bf-ed1d2df348aabd",
                    "expire_time": "1609296809",
                    "is_permanent": false
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<ShareLinkDataResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.ShareLink!);
    }
}
