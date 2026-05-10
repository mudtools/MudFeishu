// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.TasksComments;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuV2TaskComments"/>接口的相关函数。
/// </summary>
public class IFeishuV2TaskCommentsTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuV2TaskCommentsTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskComments.CreateCommentAsync(CreateCommentRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateCommentAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "content": "这是一条评论。",
              "reply_to_comment_id": "6937231762296684564",
              "resource_type": "task",
              "resource_id": "ccb55625-95d2-2e80-655f-0e40bf67953f"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CreateCommentRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Content);
        Assert.NotEmpty(requestBody.ReplyToCommentId!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskComments.CreateCommentAsync(CreateCommentRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateCommentAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "comment": {
                        "id": "7197020628442939411",
                        "content": "这是一条评论",
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "creator"
                        },
                        "reply_to_comment_id": "7166825117308174356",
                        "created_at": "1675742789470",
                        "updated_at": "1675742789470",
                        "resource_type": "task",
                        "resource_id": "ccb55625-95d2-2e80-655f-0e40bf67953f"
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CommentOpreationResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Comment);
        Assert.NotEmpty(result.Data.Comment.ReplyToCommentId!);
        Assert.NotEmpty(result.Data.Comment.ResourceId!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskComments.GetCommentByIdAsync(string, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetCommentByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "comment": {
                        "id": "7197020628442939411",
                        "content": "这是一条评论",
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "creator"
                        },
                        "reply_to_comment_id": "7166825117308174356",
                        "created_at": "1675742789470",
                        "updated_at": "1675742789470",
                        "resource_type": "task",
                        "resource_id": "ccb55625-95d2-2e80-655f-0e40bf67953f"
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CommentOpreationResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Comment);
        Assert.NotEmpty(result.Data.Comment.Content!);
        Assert.NotEmpty(result.Data.Comment.ResourceType!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskComments.UpdateCommentByIdAsync(string, UpdateCommentRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateCommentByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "comment": {
                "content": "举杯邀明月，对影成三人"
              },
              "update_fields": [
                "content"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UpdateCommentRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.Comment);
        Assert.NotNull(requestBody.UpdateFields);
        Assert.NotEmpty(requestBody.Comment.Content!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskComments.UpdateCommentByIdAsync(string, UpdateCommentRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateCommentByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "comment": {
                        "id": "7197020628442939411",
                        "content": "这是一条评论",
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "creator"
                        },
                        "reply_to_comment_id": "7166825117308174356",
                        "created_at": "1675742789470",
                        "updated_at": "1675742789470",
                        "resource_type": "task",
                        "resource_id": "ccb55625-95d2-2e80-655f-0e40bf67953f"
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CommentOpreationResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Comment);
        Assert.NotEmpty(result.Data.Comment.Id!);
        Assert.NotEmpty(result.Data.Comment.ReplyToCommentId!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskComments.DeleteCommentByIdAsync(string, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteCommentByIdAsync_Result()
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
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskComments.GetCommentPageListAsync(string?, string?, string?, int, string?, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetCommentPageListAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "items": [
                        {
                            "id": "7197020628442939411",
                            "content": "这是一条评论",
                            "creator": {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "user",
                                "role": "creator"
                            },
                            "reply_to_comment_id": "7166825117308174356",
                            "created_at": "1675742789470",
                            "updated_at": "1675742789470",
                            "resource_type": "task",
                            "resource_id": "ccb55625-95d2-2e80-655f-0e40bf67953f"
                        }
                    ],
                    "page_token": "aWQ9NzEwMjMzMjMxMDE=",
                    "has_more": true
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiPageListResult<TaskCommentInfo>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Items);
        Assert.NotEmpty(result.Data.Items[0].Content!);
        Assert.NotEmpty(result.Data.Items[0].ResourceType!);
        Assert.NotEmpty(result.Data.PageToken!);
    }
}
