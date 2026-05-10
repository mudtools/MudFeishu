// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.TasksAttachments;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuV2TaskAttachments"/>接口的相关函数。
/// </summary>
public class IFeishuV2TaskAttachmentsTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuV2TaskAttachmentsTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }


    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskAttachments.UploadAttachmentAsync(UploadTaskAttachmentsRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UploadAttachmentAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "items": [
                        {
                            "guid": "f860de3e-6881-4ddd-9321-070f36d1af0b",
                            "file_token": "boxcnTDqPaRA6JbYnzQsZ2doB2b",
                            "name": "foo.jpg",
                            "size": 62232,
                            "resource": {
                                "type": "task",
                                "id": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                            },
                            "uploader": {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "user",
                                "role": "creator"
                            },
                            "is_cover": false,
                            "uploaded_at": "1675742789470"
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<TaskAttachmentsUploadResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Items);
        Assert.NotEmpty(result.Data.Items[0].Guid!);
        Assert.NotEmpty(result.Data.Items[0].Name!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskAttachments.GetAttachmentPageListAsync(string?, string?, int, string?, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetAttachmentPageListAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "items": [
                        {
                            "guid": "f860de3e-6881-4ddd-9321-070f36d1af0b",
                            "file_token": "boxcnTDqPaRA6JbYnzQsZ2doB2b",
                            "name": "foo.jpg",
                            "size": 62232,
                            "resource": {
                                "type": "task",
                                "id": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                            },
                            "uploader": {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "user",
                                "role": "editor"
                            },
                            "is_cover": false,
                            "uploaded_at": "1675742789470",
                            "url": "https://example.com/download/authcode/?code=OWMzNDlmMjJmZThkYzZkZGJlMjYwZTI0OTUxZTE2MDJfMDZmZmMwOWVj"
                        }
                    ],
                    "page_token": "aWQ9NzEwMjMzMjMxMDE=",
                    "has_more": true
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiPageListResult<AttachmentResultInfo>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Items);
        Assert.NotEmpty(result.Data.PageToken!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskAttachments.GetAttachmentByIdAsync(string, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetAttachmentByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "attachment": {
                        "guid": "f860de3e-6881-4ddd-9321-070f36d1af0b",
                        "file_token": "boxcnTDqPaRA6JbYnzQsZ2doB2b",
                        "name": "foo.jpg",
                        "size": 62232,
                        "resource": {
                            "type": "task",
                            "id": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
                        },
                        "uploader": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "editor"
                        },
                        "is_cover": false,
                        "uploaded_at": "1675742789470",
                        "url": "https://example.com/download/authcode/?code=OWMzNDlmMjJmZThkYzZkZGJlMjYwZTI0OTUxZTE2MDJfMDZmZmMwOWVj"
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetAttachmentsInfoResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Attachment);
        Assert.NotEmpty(result.Data.Attachment.Name!);
        Assert.NotNull(result.Data.Attachment.Uploader);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskAttachments.DeleteAttachmentByIdAsync(string, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteAttachmentByIdAsync_Result()
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
}
