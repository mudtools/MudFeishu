// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.UserGroup;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuTenantV3UserGroup"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV3UserGroupTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV3UserGroupTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3UserGroup.CreateUserGroupAsync(UserGroupInfoRequest, string?, string?, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateUserGroupAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "name": "IT 外包组",
              "description": "IT服务人员的集合",
              "type": 1,
              "group_id": "g122817"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UserGroupInfoRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Name!);
        Assert.NotEmpty(requestBody.Description!);
        Assert.NotEmpty(requestBody.GroupId!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3UserGroup.CreateUserGroupAsync(UserGroupInfoRequest, string?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateUserGroupAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "group_id": "g122817"
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<UserGroupCreateResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.GroupId!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3UserGroup.UpdateUserGroupAsync(string, UserGroupUpdateRequest, string?, string?, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateUserGroupAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "name": "外包 IT 用户组",
              "description": "IT 外包用户组，需要进行细粒度权限管控"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UserGroupUpdateRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Name!);
        Assert.NotEmpty(requestBody.Description!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3UserGroup.UpdateUserGroupAsync(string, UserGroupUpdateRequest, string?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateUserGroupAsync_Result()
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
    /// 用于测试<see cref="IFeishuTenantV3UserGroup.GetUserGroupInfoByIdAsync(string, string?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetUserGroupInfoByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "group": {
                        "id": "g193821",
                        "name": "IT 外包组",
                        "description": "IT 外包组，需要对该组人群进行细颗粒度权限管控。",
                        "member_user_count": 2,
                        "member_department_count": 0,
                        "type": 1
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<UserGroupQueryResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Group);
        Assert.NotEmpty(result.Data.Group.Name);
        Assert.NotEmpty(result.Data.Group.Description);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3UserGroup.GetUserGroupsAsync(int?, string?, int?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetUserGroupsAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "grouplist": [
                        {
                            "id": "g193821",
                            "name": "IT 外包组",
                            "description": "IT 外包组，需要对该组人群进行细颗粒度权限管控。",
                            "member_user_count": 2,
                            "member_department_count": 0,
                            "type": 1,
                            "department_scope_list": [
                                "od-4e6ac4d14bcd5071a37a39de902c7141"
                            ],
                            "group_id": "4ba51ab38648f9cd"
                        }
                    ],
                    "page_token": "AQD9/Rn9556539ED40/dk53s4Ebp882DYfFaPFbz00L4CMZJrqGdzNyc8BcZtDbwVUvRmQTvyMYicnGWrde9X56TgdBuS+JDTJJDDPw=",
                    "has_more": true
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<UserGroupListResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.PageToken);
        Assert.NotEmpty(result.Data.GroupList);
        Assert.NotEmpty(result.Data.GroupList[0].Name);
        Assert.NotEmpty(result.Data.GroupList[0].Description);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3UserGroup.GetUserBelongGroupsAsync(string, string?, int?, int?, string?, int?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetUserBelongGroupsAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "group_list": [
                        "og-1455998e138698e1386"
                    ],
                    "page_token": "AQD9/Rn9eij9Pm39ED40/dk53s4Ebp882DYfFaPFbz00L4CMZJrqGdzNyc8BcZtDbwVUvRmQTvyMYicnGWrde9X56TgdBuS+JKiSIkdexPw=",
                    "has_more": false
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<UserBelongGroupListResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.PageToken);
        Assert.NotEmpty(result.Data.GroupList);
        Assert.NotEmpty(result.Data.GroupList[0]);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3UserGroup.DeleteUserGroupByIdAsync(string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteUserGroupByIdAsync_Result()
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
}
