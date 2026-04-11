// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.ChatGroupMember;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuV1ChatGroupMember"/>接口的相关函数。
/// </summary>
public class IFeishuV1ChatGroupMemberTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuV1ChatGroupMemberTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupMember.AddManagersAsync(string, GroupManagerRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_AddManagersAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "manager_ids": [
                "ou_9204a37300b3700d61effaa439f34295"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<GroupManagerRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.ManagerIds);
        Assert.NotEmpty(requestBody.ManagerIds[0]);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupMember.AddManagersAsync(string, GroupManagerRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_AddManagersAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "chat_managers": [
                        "ou_9204a37300b3700d61effaa439f34295"
                    ],
                    "chat_bot_managers": [
                        "cli_a10fbf7e94b8d01d"
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GroupManagerResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.ChatBotManagers!);
        Assert.NotEmpty(result.Data.ChatBotManagers![0]!);

        Assert.NotEmpty(result.Data.ChatBotManagers);
        Assert.NotEmpty(result.Data.ChatBotManagers[0]);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupMember.DeleteManagersAsync(string, GroupManagerRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteManagersAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "manager_ids": [
                "ou_9204a37300b3700d61effaa439f34295"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<GroupManagerRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.ManagerIds);
        Assert.NotEmpty(requestBody.ManagerIds[0]);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupMember.DeleteManagersAsync(string, GroupManagerRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteManagersAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "chat_managers": [
                        "ou_9204a37300b3700d61effaa439f34295"
                    ],
                    "chat_bot_managers": [
                        "cli_a10fbf7e94b8d01d"
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GroupManagerResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.ChatBotManagers!);
        Assert.NotEmpty(result.Data.ChatBotManagers![0]!);

        Assert.NotEmpty(result.Data.ChatBotManagers);
        Assert.NotEmpty(result.Data.ChatBotManagers[0]);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupMember.AddMemberAsync(string, MembersRequest, string, int, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_AddMemberAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "id_list": [
                "4d7a3c6g"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<MembersRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.IdList);
        Assert.NotEmpty(requestBody.IdList[0]);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupMember.AddMemberAsync(string, MembersRequest, string, int, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_AddMemberAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "invalid_id_list": [
                        "4d7a3c6g"
                    ],
                    "not_existed_id_list": [
                        "4d7a3c6g"
                    ],
                    "pending_approval_id_list": [
                        "4d7a3c6g"
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<AddMemberResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.InvalidIdList!);
        Assert.NotEmpty(result.Data.InvalidIdList![0]!);
        Assert.NotEmpty(result.Data.NotExistedIdList!);
        Assert.NotEmpty(result.Data.NotExistedIdList![0]!);
        Assert.NotEmpty(result.Data.PendingApprovalIdList!);
        Assert.NotEmpty(result.Data.PendingApprovalIdList![0]!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupMember.MeJoinChatGroupAsync(string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_MeJoinChatGroupAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "data": {},
                "msg": "ok"
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupMember.RemoveMemberAsync(string, MembersRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_RemoveMemberAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "id_list": [
                "4d7a3c6g"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<MembersRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.IdList);
        Assert.NotEmpty(requestBody.IdList[0]);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupMember.RemoveMemberAsync(string, MembersRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_RemoveMemberAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "invalid_id_list": [
                        "4d7a3c6g"
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<RemoveMemberResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.InvalidIdList!);
        Assert.NotEmpty(result.Data.InvalidIdList![0]!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupMember.GetMemberPageListByIdAsync(string, string, int?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetMemberPageListByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "items": [
                        {
                            "member_id_type": "open_id",
                            "member_id": "ou_9204a37300b3700d61effaa439f34295",
                            "name": "张三",
                            "tenant_key": "736588c9260f175d"
                        }
                    ],
                    "page_token": "dmJCRHhpd3JRbGV1VEVNRFFyTitRWDY5ZFkybmYrMEUwMUFYT0VMMWdENEtuYUhsNUxGMDIwemtvdE5ORjBNQQ==",
                    "has_more": true,
                    "member_total": 2
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetMemberPageListResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.PageToken!);
        Assert.NotEmpty(result.Data.Items!);
        Assert.NotEmpty(result.Data.Items![0].MemberId!);
        Assert.NotEmpty(result.Data.Items[0].MemberIdType!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupMember.GetMemberInChatByIdAsync(string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetMemberInChatByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "is_in_chat": false
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetMemberIsInChatResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Equal(false, result.Data.IsInChat);
    }
}
