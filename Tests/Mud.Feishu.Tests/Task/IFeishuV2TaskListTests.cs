// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.TasksList;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuV2TaskList"/>接口的相关函数。
/// </summary>
public class IFeishuV2TaskListTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuV2TaskListTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskList.CreateTaskListAsync(CreateTaskListRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateTaskListAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "name": "年会工作任务清单",
              "members": [
                {
                  "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                  "type": "user",
                  "role": "editor"
                }
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CreateTaskListRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Name);
        Assert.NotNull(requestBody.Members);
        Assert.NotEmpty(requestBody.Members[0].Id);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskList.CreateTaskListAsync(CreateTaskListRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateTaskListAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "tasklist": {
                        "guid": "cc371766-6584-cf50-a222-c22cd9055004",
                        "name": "年会总结工作任务清单",
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "editor"
                        },
                        "owner": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "owner"
                        },
                        "members": [
                            {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "user",
                                "role": "editor"
                            }
                        ],
                        "url": "https://applink.feishu.cn/client/todo/task_list?guid=b45b360f-1961-4058-b338-7f50c96e1b52",
                        "created_at": "1675742789470",
                        "updated_at": "1675742789470"
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<TaskListOperationResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Tasklist);
        Assert.NotEmpty(result.Data.Tasklist.Url!);
        Assert.NotEmpty(result.Data.Tasklist.Name!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskList.GetTaskListByIdAsync(string, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetTaskListByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "tasklist": {
                        "guid": "cc371766-6584-cf50-a222-c22cd9055004",
                        "name": "年会总结工作任务清单",
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "creator"
                        },
                        "owner": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "owner"
                        },
                        "members": [
                            {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "user",
                                "role": "editor"
                            }
                        ],
                        "url": "https://applink.feishu.cn/client/todo/task_list?guid=b45b360f-1961-4058-b338-7f50c96e1b52",
                        "created_at": "1675742789470",
                        "updated_at": "1675742789470"
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<TaskListOperationResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Tasklist);
        Assert.NotEmpty(result.Data.Tasklist.Url!);
        Assert.NotEmpty(result.Data.Tasklist.Name!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskList.UpdateTaskListByIdAsync(string, UpdateTaskListRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateTaskListByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "tasklist": {
                "name": "年会工作任务清单",
                "owner": {
                  "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                  "type": "user",
                  "role": "owner"
                }
              },
              "update_fields": [
                "owner"
              ],
              "origin_owner_to_role": "editor"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UpdateTaskListRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.UpdateFields);
        Assert.NotEmpty(requestBody.Tasklist.Name!);
        Assert.NotEmpty(requestBody.OriginOwnerToRole!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskList.UpdateTaskListByIdAsync(string, UpdateTaskListRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateTaskListByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "tasklist": {
                        "guid": "cc371766-6584-cf50-a222-c22cd9055004",
                        "name": "年会总结工作任务清单",
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "creator"
                        },
                        "owner": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "owner",
                            "role": "editor"
                        },
                        "members": [
                            {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "user",
                                "role": "editor"
                            }
                        ],
                        "url": "https://applink.feishu.cn/client/todo/task_list?guid=b45b360f-1961-4058-b338-7f50c96e1b52",
                        "created_at": "1675742789470",
                        "updated_at": "1675742789470"
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<TaskListOperationResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Tasklist);
        Assert.NotEmpty(result.Data.Tasklist.Name!);
        Assert.NotEmpty(result.Data.Tasklist.Url!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskList.DeleteTaskListByIdAsync(string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteTaskListByIdAsync_Result()
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
    /// 用于测试<see cref="IFeishuV2TaskList.AddTaskListMemberByIdAsync(string, AddTaskListMemberRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_AddTaskListMemberByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "members": [
                {
                  "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                  "type": "user",
                  "role": "editor"
                }
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<AddTaskListMemberRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Members[0].Id);
        Assert.NotEmpty(requestBody.Members[0].Role!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskList.AddTaskListMemberByIdAsync(string, AddTaskListMemberRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_AddTaskListMemberByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "tasklist": {
                        "guid": "cc371766-6584-cf50-a222-c22cd9055004",
                        "name": "年会总结工作任务清单",
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "creator"
                        },
                        "owner": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "owner"
                        },
                        "members": [
                            {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "user",
                                "role": "editor"
                            }
                        ],
                        "url": "https://applink.feishu.cn/client/todo/task_list?guid=b45b360f-1961-4058-b338-7f50c96e1b52",
                        "created_at": "1675742789470",
                        "updated_at": "1675742789470"
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<TaskListOperationResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Tasklist);
        Assert.NotEmpty(result.Data.Tasklist.Url!);
        Assert.NotNull(result.Data.Tasklist.Members);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskList.RemoveTaskListMemberByIdAsync(string, RemoveTaskListMemberRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_RemoveTaskListMemberByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "members": [
                {
                  "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                  "type": "user",
                  "role": "editor"
                }
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<RemoveTaskListMemberRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.Members);
        Assert.NotEmpty(requestBody.Members[0].Id);
        Assert.NotEmpty(requestBody.Members[0].Role!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskList.RemoveTaskListMemberByIdAsync(string, RemoveTaskListMemberRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_RemoveTaskListMemberByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "tasklist": {
                        "guid": "cc371766-6584-cf50-a222-c22cd9055004",
                        "name": "年会总结工作任务清单",
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "creator"
                        },
                        "owner": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "owner"
                        },
                        "members": [
                            {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "user",
                                "role": "editor"
                            }
                        ],
                        "url": "https://applink.feishu.cn/client/todo/task_list?guid=b45b360f-1961-4058-b338-7f50c96e1b52",
                        "created_at": "1675742789470",
                        "updated_at": "1675742789470"
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<TaskListOperationResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Tasklist);
        Assert.NotEmpty(result.Data.Tasklist.Guid!);
        Assert.NotEmpty(result.Data.Tasklist.Name!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskList.GetTaskListPageListByIdAsync(string, int, string?, bool?, string?, string?, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetTaskListPageListByIdAsync_Result()
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
                                    "role": "assignee",
                                    "name": "张明德（明德）"
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
        Assert.NotNull(result.Data.Items[0].Start);
        Assert.NotEmpty(result.Data.PageToken!);
    }
}
