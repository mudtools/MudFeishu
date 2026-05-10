// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.RoleMembers;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuTenantV3RoleMember"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV3RoleMemberTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV3RoleMemberTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3RoleMember.BatchAddMemberAsync(string, RoleMembersRequest, string?, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_BatchAddMemberAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "members": [
                "ou-12832197382"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<RoleMembersRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Members);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3RoleMember.BatchAddMemberAsync(string, RoleMembersRequest, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_BatchAddMemberAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "results": [
                        {
                            "user_id": "od-4e6ac4d14bcd5071a37a39de902c7141",
                            "reason": 1
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<RoleAssignmentResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.Results);
        Assert.NotEmpty(result.Data.Results[0].UserId);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3RoleMember.BatchAddMembersSopesAsync(string, RoleMembersScopeRequest, string?, string?, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_BatchAddMembersSopesAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "members": [
                "ou-12832197382"
              ],
              "departments": [
                "ou-12343455"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<RoleMembersScopeRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Departments);
        Assert.NotEmpty(requestBody.Members);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3RoleMember.BatchAddMembersSopesAsync(string, RoleMembersScopeRequest, string?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_BatchAddMembersSopesAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "results": [
                        {
                            "user_id": "od-4e6ac4d14bcd5071a37a39de902c7141",
                            "reason": 1
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<RoleAssignmentResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.Results);
        Assert.NotEmpty(result.Data.Results[0].UserId);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3RoleMember.GetMembersSopesAsync(string, string, string?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetMembersSopesAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "member": {
                        "user_id": "od-4e6ac4d14bcd5071a37a39de902c7141",
                        "scope_type": "All",
                        "department_ids": [
                            "od-4e6ac4d14bcd5071a37a39de902c7141"
                        ]
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<RoleMemberScopeResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Member);
        Assert.NotEmpty(result.Data.Member.UserId);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3RoleMember.GetMembersAsync(string, int?, string?, string?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetMembersAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "members": [
                        {
                            "user_id": "od-4e6ac4d14bcd5071a37a39de902c7141",
                            "scope_type": "All",
                            "department_ids": [
                                "od-4e6ac4d14bcd5071a37a39de902c7141"
                            ]
                        }
                    ],
                    "page_token": "2132323",
                    "has_more": false
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<RoleMemberScopeListResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Members);
        Assert.NotEmpty(result.Data.Members[0].UserId);
        Assert.NotEmpty(result.Data.PageToken!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3RoleMember.DeleteMembersByRoleIdAsync(string, RoleMembersRequest, string?, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteMembersByRoleIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "members": [
                "ou-12832197382"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<RoleMembersRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Members);
        Assert.NotEmpty(requestBody.Members[0]);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3RoleMember.DeleteMembersByRoleIdAsync(string, RoleMembersRequest, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteMembersByRoleIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "result": [
                        {
                            "user_id": "od-4e6ac4d14bcd5071a37a39de902c7141",
                            "reason": 1
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<RoleAssignmentResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
}
