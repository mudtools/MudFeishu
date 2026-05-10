// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.TasksActivitySubscriptions;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuV2TaskActivitySubscriptions"/>接口的相关函数。
/// </summary>
public class IFeishuV2TaskActivitySubscriptionsTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuV2TaskActivitySubscriptionsTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskActivitySubscriptions.CreateActivitySubscriptionsAsync(string, CreateActivitySubscriptionsRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateActivitySubscriptionsAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "name": "我的订阅",
              "subscribers": [
                {
                  "id": "oc_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                  "type": "chat"
                }
              ],
              "include_keys": [
                100
              ],
              "disabled": false
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CreateActivitySubscriptionsRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.Subscribers);
        Assert.NotNull(requestBody.IncludeKeys);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskActivitySubscriptions.CreateActivitySubscriptionsAsync(string, CreateActivitySubscriptionsRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateActivitySubscriptionsAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "activity_subscription": {
                        "guid": "d19e3a2a-edc0-4e4e-b7cc-950e162b53ae",
                        "name": "Roadmap订阅",
                        "subscribers": [
                            {
                                "id": "oc_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "user",
                                "role": "editor"
                            }
                        ],
                        "include_keys": [
                            101
                        ],
                        "disabled": false
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<TasklistActivitySubscriptionResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskActivitySubscriptions.GetActivitySubscriptionsByIdAsync(string, string, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetActivitySubscriptionsByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "activity_subscription": {
                        "guid": "d19e3a2a-edc0-4e4e-b7cc-950e162b53ae",
                        "name": "Roadmap订阅",
                        "subscribers": [
                            {
                                "id": "oc_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "chat",
                                "role": "editor"
                            }
                        ],
                        "include_keys": [
                            101
                        ],
                        "disabled": false
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<TasklistActivitySubscriptionResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.ActivitySubscription);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskActivitySubscriptions.GetActivitySubscriptionsListByIdAsync(string, int, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetActivitySubscriptionsListByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "items": [
                        {
                            "guid": "d19e3a2a-edc0-4e4e-b7cc-950e162b53ae",
                            "name": "Roadmap订阅",
                            "subscribers": [
                                {
                                    "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                    "type": "user",
                                    "role": "editor"
                                }
                            ],
                            "include_keys": [
                                101
                            ],
                            "disabled": false
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiListResult<TasklistActivitySubscriptionInfo>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Items);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskActivitySubscriptions.UpdateActivitySubscriptionsByIdAsync(string, string, UpdateActivitySubscriptionsRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateActivitySubscriptionsByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "activity_subscription": {
                "name": "Roadmap订阅",
                "subscribers": [
                  {
                    "id": "oc_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                    "type": "chat"
                  }
                ],
                "include_keys": [
                  101
                ],
                "disabled": false
              },
              "update_fields": [
                "name"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UpdateActivitySubscriptionsRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.UpdateFields);
        Assert.NotNull(requestBody.ActivitySubscription);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskActivitySubscriptions.UpdateActivitySubscriptionsByIdAsync(string, string, UpdateActivitySubscriptionsRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateActivitySubscriptionsByIdAsync_Result()
    {
        string resultStr = """
            {
                "code": 0,
                "msg": "success",
                "data": {
                    "activity_subscription": {
                        "guid": "d19e3a2a-edc0-4e4e-b7cc-950e162b53ae",
                        "name": "Roadmap订阅",
                        "subscribers": [
                            {
                                "id": "oc_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "chat",
                                "role": "editor"
                            }
                        ],
                        "include_keys": [
                            101
                        ],
                        "disabled": false
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<TasklistActivitySubscriptionResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.ActivitySubscription);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskActivitySubscriptions.DeleteActivitySubscriptionsByIdAsync(string, string, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteActivitySubscriptionsByIdAsync_Result()
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
