// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.ApprovalComments;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuTenantV4ApprovalComments"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV4ApprovalCommentsTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV4ApprovalCommentsTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalComments.CreateCommentAsync(string, string, CreateCommentRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateCommentAsync_RequestBody()
    {
        string bodyStr = """
{
  "content": "{\"text\":\"@username艾特展示\",\"files\":[{\"url\":\"xxx\",\"fileSize\":155149,\"title\":\"9a9fedc5cfb01a4a20c715098.png\",\"type\":\"image\",\"extra\":\"\"}]}",
  "at_info_list": [
    {
      "user_id": "579fd9c4",
      "name": "张敏",
      "offset": "0"
    }
  ],
  "parent_comment_id": "7081516627711524883",
  "comment_id": "7081516627711524883",
  "disable_bot": false,
  "extra": "{\"a\":\"a\"}"
}
""";
        var requestBody = JsonSerializer.Deserialize<CreateCommentRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.Content);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalComments.CreateCommentAsync(string, string, CreateCommentRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateCommentAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "comment_id": "7081516627711606803"
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CommentOperationResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.CommentId);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalComments.DeleteCommentByIdAsync(string, string, string, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteCommentByIdAsync_Result()
    {
        string resultStr = @"
{
    ""code"": 0,
    ""msg"": ""success"",
    ""data"": {
        ""comment_id"": ""7081516627711606803""
    }
}
";
        var result = JsonSerializer.Deserialize<FeishuApiResult<CommentOperationResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.CommentId);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalComments.RemoveCommentsAsync(string, string, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_RemoveCommentsAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "instance_id": "6A123516-FB88-470D-A428-9AF58B71B3C0",
                    "external_id": "6A123516-FB88-470D-A428-9AF58B71B3C0"
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CommentsRemoveResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalComments.GetCommentsPageListByIdAsync(string, string, int, string?, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetCommentsPageListByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "comments": [
                        {
                            "id": "7081516627711524883",
                            "content": "{\"text\":\"x@某某来自小程序的评论，这是一条回复\"}",
                            "create_time": "1648801211000",
                            "update_time": "1648801211000",
                            "is_delete": 1,
                            "replies": [
                                {
                                    "id": "7081516611634741268",
                                    "content": "{\"text\":\"x@某某来自小程序的评论，这是一条回复\"}",
                                    "create_time": "1648803677000",
                                    "update_time": "1648803677000",
                                    "is_delete": 0,
                                    "at_info_list": [
                                        {
                                            "user_id": "579fd9c4",
                                            "name": "张某",
                                            "offset": "1"
                                        }
                                    ],
                                    "commentator": "893g4c45",
                                    "extra": "{\"a\":\"a\"}"
                                }
                            ],
                            "at_info_list": [
                                {
                                    "user_id": "579fd9c4",
                                    "name": "张某",
                                    "offset": "1"
                                }
                            ],
                            "commentator": "893g4c45",
                            "extra": "{\"a\":\"a\"}"
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CommentsPageListResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Comments);
    }
}
