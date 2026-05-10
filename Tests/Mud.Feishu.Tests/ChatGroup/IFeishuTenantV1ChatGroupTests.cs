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
/// 用于测试<see cref="IFeishuTenantV1ChatGroup"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV1ChatGroupTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV1ChatGroupTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1ChatGroup.CreateChatGroupAsync(CreateChatRequest, string, bool?, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateChatGroupAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "avatar": "default-avatar_44ae0ca3-e140-494b-956f-78091e348435",
              "name": "测试群名称",
              "description": "测试群描述",
              "i18n_names": {
                "zh_cn": "群聊",
                "en_us": "group chat",
                "ja_jp": "グループチャット"
              },
              "owner_id": "ou_7d8a6e6df7621556ce0d21922b676706ccs",
              "user_id_list": [
                "ou_7d8a6e6df7621556ce0d21922b676706ccs"
              ],
              "bot_id_list": [
                "cli_a10fbf7e94b8d01d"
              ],
              "group_message_type": "chat",
              "chat_mode": "group",
              "chat_type": "private",
              "join_message_visibility": "all_members",
              "leave_message_visibility": "all_members",
              "membership_approval": "no_approval_required",
              "restricted_mode_setting": {
                "status": false,
                "screenshot_has_permission_setting": "all_members",
                "download_has_permission_setting": "all_members",
                "message_has_permission_setting": "all_members"
              },
              "urgent_setting": "all_members",
              "video_conference_setting": "all_members",
              "edit_permission": "all_members",
              "hide_member_count_setting": "all_members"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CreateChatRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.Name);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1ChatGroup.CreateChatGroupAsync(CreateChatRequest, string, bool?, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateChatGroupAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "chat_id": "oc_a0553eda9014c201e6969b478895c230",
                    "avatar": "https://p3-lark-file.byteimg.com/img/lark-avatar-staging/default-avatar_44ae0ca3-e140-494b-956f-78091e348435~100x100.jpg",
                    "name": "测试群名称",
                    "description": "测试群描述",
                    "i18n_names": {
                        "zh_cn": "群聊",
                        "en_us": "group chat",
                        "ja_jp": "グループチャット"
                    },
                    "owner_id": "ou_7d8a6e6df7621556ce0d21922b676706ccs",
                    "owner_id_type": "open_id",
                    "urgent_setting": "all_members",
                    "video_conference_setting": "all_members",
                    "add_member_permission": "all members",
                    "share_card_permission": "allowed",
                    "at_all_permission": "all members",
                    "edit_permission": "all members",
                    "group_message_type": "chat",
                    "chat_mode": "group",
                    "chat_type": "private",
                    "chat_tag": "inner",
                    "external": false,
                    "tenant_key": "736588c9260f175e",
                    "join_message_visibility": "all_members",
                    "leave_message_visibility": "all_members",
                    "membership_approval": "no_approval_required",
                    "moderation_permission": "all_members",
                    "restricted_mode_setting": {
                        "status": false,
                        "screenshot_has_permission_setting": "all_members",
                        "download_has_permission_setting": "all_members",
                        "message_has_permission_setting": "all_members"
                    },
                    "hide_member_count_setting": "all_members"
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CreateUpdateChatResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.ChatId);
    }
}
