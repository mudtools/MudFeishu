// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.Users;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuTenantV3User"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV3UserTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV3UserTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3User.CreateUserAsync(CreateUserRequest, string?, string?, string?, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateUserAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "user_id": "3e3cf96b",
              "name": "张三",
              "en_name": "San Zhang",
              "nickname": "Alex Zhang",
              "email": "zhangsan@gmail.com",
              "mobile": "13011111111",
              "mobile_visible": false,
              "gender": 1,
              "avatar_key": "2500c7a9-5fff-4d9a-a2de-3d59614ae28g",
              "department_ids": [
                "od-4e6ac4d14bcd5071a37a39de902c714111111"
              ],
              "leader_user_id": "ou_7dab8a3d3cdcc9da365777c7ad535d62",
              "city": "杭州",
              "country": "CN",
              "work_station": "北楼-H34",
              "join_time": 2147483647,
              "employee_no": "1",
              "employee_type": 1,
              "orders": [
                {
                  "department_id": "od-4e6ac4d14bcd5071a37a39de902c7141",
                  "user_order": 100,
                  "department_order": 100,
                  "is_primary_dept": true
                }
              ],
              "custom_attrs": [
                {
                  "type": "TEXT",
                  "id": "DemoId",
                  "value": {
                    "text": "DemoText",
                    "url": "http://www.fs.cn",
                    "pc_url": "http://www.fs.cn",
                    "option_id": "edcvfrtg",
                    "generic_user": {
                      "id": "9b2fabg5",
                      "type": 1
                    }
                  }
                }
              ],
              "enterprise_email": "demo@mail.com",
              "job_title": "xxxxx",
              "geo": "cn",
              "job_level_id": "mga5oa8ayjlp9rb",
              "job_family_id": "mga5oa8ayjlp9rb",
              "subscription_ids": [
                "23213213213123123"
              ],
              "dotted_line_leader_user_ids": [
                "ou_7dab8a3d3cdcc9da365777c7ad535d62"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CreateUserRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Name!);
        Assert.NotEmpty(requestBody.Country!);
        Assert.NotEmpty(requestBody.DepartmentIds);
        Assert.NotEmpty(requestBody.JobFamilyId!);
        Assert.NotEmpty(requestBody.DottedLineLeaderUserIds!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3User.CreateUserAsync(CreateUserRequest, string?, string?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateUserAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "user": {
                        "union_id": "on_94a1ee5551019f18cd73d9f111898cf2",
                        "user_id": "3e3cf96b",
                        "open_id": "ou_7dab8a3d3cdcc9da365777c7ad535d62",
                        "name": "张三",
                        "en_name": "San Zhang",
                        "nickname": "Alex Zhang",
                        "email": "zhangsan@gmail.com",
                        "mobile": "13011111111",
                        "mobile_visible": false,
                        "gender": 1,
                        "avatar_key": "2500c7a9-5fff-4d9a-a2de-3d59614ae28g",
                        "avatar": {
                            "avatar_72": "https://foo.icon.com/xxxx",
                            "avatar_240": "https://foo.icon.com/xxxx",
                            "avatar_640": "https://foo.icon.com/xxxx",
                            "avatar_origin": "https://foo.icon.com/xxxx"
                        },
                        "status": {
                            "is_frozen": false,
                            "is_resigned": false,
                            "is_activated": true,
                            "is_exited": false,
                            "is_unjoin": false
                        },
                        "department_ids": [
                            "od-4e6ac4d14bcd5071a37a39de902c7141"
                        ],
                        "leader_user_id": "ou_7dab8a3d3cdcc9da365777c7ad535d62",
                        "city": "杭州",
                        "country": "CN",
                        "work_station": "北楼-H34",
                        "join_time": 2147483647,
                        "is_tenant_manager": false,
                        "employee_no": "1",
                        "employee_type": 1,
                        "orders": [
                            {
                                "department_id": "od-4e6ac4d14bcd5071a37a39de902c7141",
                                "user_order": 100,
                                "department_order": 100,
                                "is_primary_dept": true
                            }
                        ],
                        "custom_attrs": [
                            {
                                "type": "TEXT",
                                "id": "DemoId",
                                "value": {
                                    "text": "DemoText",
                                    "url": "http://www.fs.cn",
                                    "pc_url": "http://www.fs.cn",
                                    "option_id": "edcvfrtg",
                                    "option_value": "option",
                                    "name": "name",
                                    "picture_url": "https://xxxxxxxxxxxxxxxxxx",
                                    "generic_user": {
                                        "id": "9b2fabg5",
                                        "type": 1
                                    }
                                }
                            }
                        ],
                        "enterprise_email": "demo@mail.com",
                        "job_title": "xxxxx",
                        "is_frozen": false,
                        "geo": "cn",
                        "job_level_id": "mga5oa8ayjlp9rb",
                        "job_family_id": "mga5oa8ayjlp9rb",
                        "dotted_line_leader_user_ids": [
                            "ou_7dab8a3d3cdcc9da365777c7ad535d62"
                        ]
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CreateOrUpdateUserResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.User);
        Assert.NotEmpty(result.Data.User.Name!);
        Assert.NotEmpty(result.Data.User.DottedLineLeaderUserIds);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3User.UpdateUserIdAsync(string, UpdateUserIdRequest, string?, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateUserIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "new_user_id": "3e3cf96b"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UpdateUserIdRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.NewUserId!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3User.UpdateUserIdAsync(string, UpdateUserIdRequest, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateUserIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {}
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3User.GetBatchUsersAsync(UserQueryRequest, string?, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_GetBatchUsersAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "emails": [
                "zhangsan@z.com",
                "lisi@a.com"
              ],
              "mobiles": [
                "13011111111",
                "13022222222"
              ],
              "include_resigned": true
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UserQueryRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Emails);
        Assert.NotEmpty(requestBody.Emails[0]);
        Assert.NotEmpty(requestBody.Mobiles);
        Assert.NotEmpty(requestBody.Mobiles[0]);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3User.GetBatchUsersAsync(UserQueryRequest, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetBatchUsersAsync_Result()
    {
        string resultStr = """
                        {
            	"code": 0,
            	"msg": "success",
            	"data": {
            		"user_list": [{
            				"user_id": "ou_979112345678741d29069abcdef01234",
            				"email": "zhanxxxxx@a.com",
            				"status": {
            					"is_frozen": false,
            					"is_resigned": false,
            					"is_activated": true,
            					"is_exited": false,
            					"is_unjoin": false
            				}
            			}, {
            				"user_id": "ou_919112245678741d29069abcdef02345",
            				"email": "lisixxxx@a.com",
            				"status": {
            					"is_frozen": false,
            					"is_resigned": false,
            					"is_activated": true,
            					"is_exited": false,
            					"is_unjoin": false
            				}
            			},
            			{
            				"user_id": "ou_46a087654321a1dc920ffab8fedc3456",
            				"mobile": "130xxxx1111",
            				"status": {
            					"is_frozen": false,
            					"is_resigned": false,
            					"is_activated": true,
            					"is_exited": false,
            					"is_unjoin": false
            				}
            			}, {
            				"user_id": "ou_01b081675121a1dc920ffab97cdc4567",
            				"mobile": "130xxxx2222",
            				"status": {
            					"is_frozen": false,
            					"is_resigned": false,
            					"is_activated": true,
            					"is_exited": false,
            					"is_unjoin": false
            				}
            			}
            		]
            	}
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<UserQueryListResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.UserList);
        Assert.NotEmpty(result.Data.UserList[0].Email!);
        Assert.NotEmpty(result.Data.UserList[0].UserId!);
        Assert.NotNull(result.Data.UserList[0].Status);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3User.GetUsersByKeywordAsync(string, int, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetUsersByKeywordAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "ok",
                "data": {
                    "has_more": true,
                    "page_token": "20",
                    "users": [
                        {
                            "avatar": {
                                "avatar_72": "https://example.com/static-resource/v1/d1ca00148ad2c2cf1111~72x72.png",
                                "avatar_240": "https://example.com/static-resource/v1/d1ca00148ad2c2cf1111~240x240.png",
                                "avatar_640": "https://example.com/static-resource/v1/d1ca00148ad2c2cf1111~640x640.png",
                                "avatar_origin": "https://example.com/static-resource/v1/d1ca00148ad2c2cf1111~noop.png"
                            },
                            "department_ids": [
                                "od-c02cc3b682a71cdb3a4f14fc4cdb1234"
                            ],
                            "name": "zhangsan",
                            "open_id": "ou_7d8a6e6df7621556ce0d21922b674321",
                            "user_id": "02a11111"
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<UserSearchListResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.PageToken!);
        Assert.NotEmpty(result.Data.Users);
        Assert.NotEmpty(result.Data.Users[0].Name!);
        Assert.NotEmpty(result.Data.Users[0].DepartmentIds);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3User.DeleteUserByIdAsync(string, DeleteSettingsRequest, string?, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteUserByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "department_chat_acceptor_user_id": "ou_7dab8a3d3cdcc9da365777c7ad535d62",
              "external_chat_acceptor_user_id": "ou_7dab8a3d3cdcc9da365777c7ad535d62",
              "docs_acceptor_user_id": "ou_7dab8a3d3cdcc9da365777c7ad535d62",
              "calendar_acceptor_user_id": "ou_7dab8a3d3cdcc9da365777c7ad535d62",
              "application_acceptor_user_id": "ou_7dab8a3d3cdcc9da365777c7ad535d62",
              "email_acceptor": {
                "processing_type": "1",
                "acceptor_user_id": "ou_7dab8a3d3cdcc9da365777c7ad535d62"
              }
            }
            """;
        var requestBody = JsonSerializer.Deserialize<DeleteSettingsRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.DepartmentChatAcceptorUserId!);
        Assert.NotEmpty(requestBody.CalendarAcceptorUserId!);
        Assert.NotEmpty(requestBody.ApplicationAcceptorUserId!);
        Assert.NotEmpty(requestBody.DocsAcceptorUserId!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3User.DeleteUserByIdAsync(string, DeleteSettingsRequest, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteUserByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {}
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3User.ResurrectUserByIdAsync(string, ResurrectUserRequest, string?, string?, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_ResurrectUserByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "departments": [
                {
                  "department_id": "od-4e6ac4d14bcd5071a37a39de902c7141",
                  "user_order": 0,
                  "department_order": 0
                }
              ],
              "subscription_ids": [
                "23213213213123123"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<ResurrectUserRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Departments);
        Assert.NotEmpty(requestBody.SubscriptionIds);
        Assert.NotEmpty(requestBody.Departments[0].DepartmentId!);
        Assert.NotEmpty(requestBody.SubscriptionIds[0]);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3User.ResurrectUserByIdAsync(string, ResurrectUserRequest, string?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_ResurrectUserByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {}
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3User.LogoutAsync(LogoutRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_LogoutAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "idp_credential_id": "user@xxx.xx",
              "logout_type": 1,
              "terminal_type": [
                1
              ],
              "user_id": "ou_7dab8a3d3cdcc9da365777c7ad535d62",
              "logout_reason": 34,
              "sid": "AAAAAAAAAANll6nQoIAAFA=="
            }
            """;
        var requestBody = JsonSerializer.Deserialize<LogoutRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.TerminalType);
        Assert.NotEmpty(requestBody.IdpCredentialId!);
        Assert.NotEmpty(requestBody.UserId!);
        Assert.NotEmpty(requestBody.Sid!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3User.LogoutAsync(LogoutRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_LogoutAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {}
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3User.GetJsTicketAsync(CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetJsTicketAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "ok",
                "data": {
                    "expire_in": 7200,
                    "ticket": "0560604568baf296731aa37f0c8ebe3e049c19d7"
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<TicketData>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.Equal(7200, result.Data.ExpireIn);
        Assert.NotEmpty(result.Data.Ticket!);
    }
}
