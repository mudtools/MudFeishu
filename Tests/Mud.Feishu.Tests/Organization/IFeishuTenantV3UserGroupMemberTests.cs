// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.UserGroupMember;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuTenantV3UserGroupMember"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV3UserGroupMemberTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV3UserGroupMemberTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3UserGroupMember.AddMemberAsync(string, UserGroupMemberRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_AddMemberAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "member_type": "user",
              "member_id_type": "open_id",
              "member_id": "ou_7dab8a3d3cdcc9da365777c7ad535d62"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UserGroupMemberRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.MemberId!);
        Assert.NotEmpty(requestBody.MemberType);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3UserGroupMember.AddMemberAsync(string, UserGroupMemberRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_AddMemberAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "data": {},
                "msg": "success"
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3UserGroupMember.BatchAddMemberAsync(string, BatchMembersRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_BatchAddMemberAsync_RequestBody()
    {
        string bodyStr = """
            {
              "members": [
                {
                  "member_id": "u287xj12",
                  "member_type": "user",
                  "member_id_type": "user_id"
                }
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<BatchMembersRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Members);
        Assert.NotEmpty(requestBody.Members[0].MemberId!);
        Assert.NotEmpty(requestBody.Members[0].MemberType);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3UserGroupMember.BatchAddMemberAsync(string, BatchMembersRequest, CancellationToken)"/>函数的返回结果反序列化。
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
                            "member_id": "u287xj12",
                            "code": 0
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<BatchAddMemberResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.Results);
        Assert.NotEmpty(result.Data.Results[0].MemberId);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3UserGroupMember.GetMemberListByGroupIdAsync(string, int?, string?, string?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetMemberListByGroupIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "memberlist": [
                        {
                            "member_id": "u287xj12",
                            "member_type": "user",
                            "member_id_type": "user_id"
                        }
                    ],
                    "page_token": "TDRRV9/Rn9eij9Pm39ED40/dk53s4Ebp882DYfFaPFbz00L4CMZJrqGdzNyc8BcZtDbwVUvRmQTvyMYicnGWrde9X56TgdBuS+JKiJDGexPw=",
                    "has_more": true
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<MemberListRequest>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.PageToken);
        Assert.NotEmpty(result.Data.MemberList);
        Assert.NotEmpty(result.Data.MemberList[0].MemberType);
        Assert.NotEmpty(result.Data.MemberList[0].MemberId);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3UserGroupMember.RemoveMemberAsync(string, UserGroupMemberRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_RemoveMemberAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "member_type": "user",
              "member_id": "xj82871k",
              "member_id_type": "open_id"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UserGroupMemberRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.MemberId!);
        Assert.NotEmpty(requestBody.MemberType);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3UserGroupMember.RemoveMemberAsync(string, UserGroupMemberRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_RemoveMemberAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "data": {},
                "msg": "success"
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3UserGroupMember.BatchRemoveMemberAsync(string, BatchMembersRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_BatchRemoveMemberAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "members": [
                {
                  "member_id": "u287xj12",
                  "member_type": "user",
                  "member_id_type": "user_id"
                }
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<BatchMembersRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Members);
        Assert.NotEmpty(requestBody.Members[0].MemberType);
        Assert.NotEmpty(requestBody.Members[0].MemberId!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3UserGroupMember.BatchRemoveMemberAsync(string, BatchMembersRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_BatchRemoveMemberAsync_Result()
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
