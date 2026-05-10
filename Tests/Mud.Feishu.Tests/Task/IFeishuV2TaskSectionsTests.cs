// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.TasksSections;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuV2TaskSections"/>接口的相关函数。
/// </summary>
public class IFeishuV2TaskSectionsTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuV2TaskSectionsTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskSections.CreateTaskSectionsAsync(CreateTaskSectionsRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateTaskSectionsAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "name": "已经审核过的任务",
              "resource_type": "tasklist",
              "resource_id": "cc371766-6584-cf50-a222-c22cd9055004",
              "insert_before": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2",
              "insert_after": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CreateTaskSectionsRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Name);
        Assert.NotEmpty(requestBody.InsertBefore!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskSections.CreateTaskSectionsAsync(CreateTaskSectionsRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateTaskSectionsAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "section": {
                        "guid": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2",
                        "name": "已经评审过的任务",
                        "resource_type": "tasklist",
                        "is_default": true,
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "creator"
                        },
                        "tasklist": {
                            "guid": "cc371766-6584-cf50-a222-c22cd9055004",
                            "name": "活动分工任务列表"
                        },
                        "created_at": "1675742789470",
                        "updated_at": "1675742789470"
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<TaskSectionsOperationResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Section);
        Assert.NotEmpty(result.Data.Section.Name!);
        Assert.NotNull(result.Data.Section.Tasklist);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskSections.UpdateSectionsAsync(string, UpdateTaskSectionsRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateSectionsAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "section": {
                "name": "已经审核过的任务",
                "insert_before": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2",
                "insert_after": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2"
              },
              "update_fields": [
                "name"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UpdateTaskSectionsRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.Section);
        Assert.NotEmpty(requestBody.Section.Name!);
        Assert.NotNull(requestBody.UpdateFields);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskSections.UpdateSectionsAsync(string, UpdateTaskSectionsRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateSectionsAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "section": {
                        "guid": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2",
                        "name": "已经评审过的任务",
                        "resource_type": "tasklist",
                        "is_default": true,
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "editor",
                            "name": "张明德（明德）"
                        },
                        "tasklist": {
                            "guid": "cc371766-6584-cf50-a222-c22cd9055004",
                            "name": "活动分工任务列表"
                        },
                        "created_at": "1675742789470",
                        "updated_at": "1675742789470"
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<UpdateTaskSectionsResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Section);
        Assert.NotEmpty(result.Data.Section.Name!);
        Assert.NotNull(result.Data.Section.Tasklist);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskSections.GetTaskSectionsByIdAsync(string, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetTaskSectionsByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "section": {
                        "guid": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2",
                        "name": "已经评审过的任务",
                        "resource_type": "tasklist",
                        "is_default": true,
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "creator"
                        },
                        "tasklist": {
                            "guid": "cc371766-6584-cf50-a222-c22cd9055004",
                            "name": "活动分工任务列表"
                        },
                        "created_at": "1675742789470",
                        "updated_at": "1675742789470"
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<TaskSectionsOperationResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Section);
        Assert.NotEmpty(result.Data.Section.Name!);
        Assert.NotNull(result.Data.Section.Tasklist);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskSections.DeleteTaskSectionsByIdAsync(string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteTaskSectionsByIdAsync_Result()
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
    /// 用于测试<see cref="IFeishuV2TaskSections.GetTaskSectionsPageListAsync(string, string?, int, string?, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetTaskSectionsPageListAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "items": [
                        {
                            "guid": "e6e37dcc-f75a-5936-f589-12fb4b5c80c2",
                            "name": "审核过的任务",
                            "is_default": true
                        }
                    ],
                    "page_token": "aWQ9NzEwMjMzMjMxMDE=",
                    "has_more": true
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiPageListResult<SectionSummaryInfo>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Items);
        Assert.NotEmpty(result.Data.Items[0].Guid!);
        Assert.NotEmpty(result.Data.Items[0].Name!);
        Assert.NotEmpty(result.Data.PageToken!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskSections.GetTaskSectionsPageListByIdAsync(string, bool?, string?, string?, int, string?, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetTaskSectionsPageListByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "items": [
                        {
                            "guid": "e297ddff-06ca-4166-b917-4ce57cd3a7a0",
                            "summary": "年终总结",
                            "completed_at": "1675742789470",
                            "start": {
                                "timestamp": "1675454764000",
                                "is_all_day": true
                            },
                            "due": {
                                "timestamp": "1675454764000",
                                "is_all_day": true
                            },
                            "members": [
                                {
                                    "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                    "type": "user",
                                    "role": "editor"
                                }
                            ],
                            "subtask_count": 1
                        }
                    ],
                    "page_token": "aWQ9NzEwMjMzMjMxMDE=",
                    "has_more": true
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiPageListResult<TaskSummary>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Items);
        Assert.NotEmpty(result.Data.Items[0].Guid!);
        Assert.NotEmpty(result.Data.Items[0].Summary!);
        Assert.NotEmpty(result.Data.PageToken!);
    }
}
