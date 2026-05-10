// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.CardMessageStream;
using Mud.Feishu.DataModels.Cards;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuTenantV2AppCardMessageStream"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV2AppCardMessageStreamTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV2AppCardMessageStreamTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV2AppCardMessageStream.CreateCardMessageStreamAsync(CreateAppCardMessageStreamRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateCardMessageStreamAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "app_feed_card": {
                "biz_id": "096e2927-40a6-41a3-9562-314d641d09ae",
                "title": "主标题",
                "avatar_key": "v3_0041_007bca9f-67ba-4199-bf00-4031b12cf226",
                "preview": "预览信息",
                "status_label": {
                  "text": "标签文字",
                  "type": "primary"
                },
                "buttons": {
                  "buttons": [
                    {
                      "multi_url": {
                        "url": "https://www.feishu.cn/",
                        "android_url": "https://www.feishu.cn/",
                        "ios_url": "https://www.feishu.cn/",
                        "pc_url": "https://www.feishu.cn/"
                      },
                      "action_type": "url_page",
                      "text": {
                        "text": "文本"
                      },
                      "button_type": "default",
                      "action_map": {
                        "foo": "bar"
                      }
                    }
                  ]
                },
                "link": {
                  "link": "https://www.feishu.cn/"
                },
                "time_sensitive": false,
                "notify": {
                  "close_notify": true,
                  "custom_sound_text": "您有新的订单",
                  "with_custom_sound": true
                }
              },
              "user_ids": [
                "ou_a0553eda9014c201e6969b478895c230"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CreateAppCardMessageStreamRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.AppMessageCard);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV2AppCardMessageStream.CreateCardMessageStreamAsync(CreateAppCardMessageStreamRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateCardMessageStreamAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "failed_cards": [
                        {
                            "biz_id": "bdf22389-87ec-4890-9eb6-78a7efaeecbb",
                            "user_id": "ou_88553eda9014c201e6969b478895c223",
                            "reason": "NOT_CREATED"
                        }
                    ],
                    "biz_id": "b90ce43a-fca8-4f42-92f4-794bff206ee5"
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CreateAppCardMessageStreamResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.BizId);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV2AppCardMessageStream.UpdateCardMessageStreamAsync(UpdateAppCardMessageStreamRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateCardMessageStreamAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "feed_cards": [
                {
                  "app_feed_card": {
                    "biz_id": "096e2927-40a6-41a3-9562-314d641d09ae",
                    "title": "主标题",
                    "avatar_key": "v3_0041_007bca9f-67ba-4199-bf00-4031b12cf226",
                    "preview": "预览信息",
                    "status_label": {
                      "text": "标签文字",
                      "type": "primary"
                    },
                    "buttons": {
                      "buttons": [
                        {
                          "multi_url": {
                            "url": "https://www.feishu.cn/",
                            "android_url": "https://www.feishu.cn/",
                            "ios_url": "https://www.feishu.cn/",
                            "pc_url": "https://www.feishu.cn/"
                          },
                          "action_type": "url_page",
                          "text": {
                            "text": "文本"
                          },
                          "button_type": "default",
                          "action_map": {
                            "foo": "bar"
                          }
                        }
                      ]
                    },
                    "link": {
                      "link": "https://www.feishu.cn/"
                    },
                    "time_sensitive": false,
                    "notify": {
                      "close_notify": true,
                      "custom_sound_text": "您有新的订单",
                      "with_custom_sound": true
                    }
                  },
                  "user_id": "ou_a0553eda9014c201e6969b478895c230",
                  "update_fields": [
                    "1"
                  ]
                }
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UpdateAppCardMessageStreamRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV2AppCardMessageStream.UpdateCardMessageStreamAsync(UpdateAppCardMessageStreamRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateCardMessageStreamAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "failed_cards": [
                        {
                            "biz_id": "bdf22389-87ec-4890-9eb6-78a7efaeecbb",
                            "user_id": "ou_88553eda9014c201e6969b478895c223",
                            "reason": "NO_PERMISSION"
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<UpdateAppCardMessageStreamResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV2AppCardMessageStream.DeleteCardMessageStreamAsync(DeleteAppCardMessageStreamRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteCardMessageStreamAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "feed_cards": [
                {
                  "biz_id": "ed381d34-49ac-4876-8d9e-23447acb587e",
                  "user_id": "ou_88553eda9014c201e6969b478895c223"
                }
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<DeleteAppCardMessageStreamRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV2AppCardMessageStream.DeleteCardMessageStreamAsync(DeleteAppCardMessageStreamRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteCardMessageStreamAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "failed_cards": [
                        {
                            "biz_id": "bdf22389-87ec-4890-9eb6-78a7efaeecbb",
                            "user_id": "ou_88553eda9014c201e6969b478895c223",
                            "reason": "NO_PERMISSION"
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<DeleteAppCardMessageStreamResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV2AppCardMessageStream.BotTimeSentiveAsync(BotTimeSentiveRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_BotTimeSentiveAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "time_sensitive": true,
              "user_ids": [
                "ou_9d2beb613c85a2412862a49a924558c5"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<BotTimeSentiveRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV2AppCardMessageStream.BotTimeSentiveAsync(BotTimeSentiveRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_BotTimeSentiveAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "failed_user_reasons": [
                        {
                            "error_code": 0,
                            "error_message": "The user is not in the chat",
                            "user_id": "ou_679eaeb583654bff73fefcc6e6371301"
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<BotTimeSentiveResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV2AppCardMessageStream.UpdateCardMessageStreamButtonAsync(UpdateCardMessageStreamButtonRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateCardMessageStreamButtonAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "user_ids": [
                "ou_89553eda9014c201e6969b478895c276"
              ],
              "chat_id": "oc_a0553eda9014c201e6969b478895c230",
              "buttons": {
                "buttons": [
                  {
                    "multi_url": {
                      "url": "https://www.feishu.cn/",
                      "android_url": "https://www.feishu.cn/",
                      "ios_url": "https://www.feishu.cn/",
                      "pc_url": "https://www.feishu.cn/"
                    },
                    "action_type": "url_page",
                    "text": {
                      "text": "文本"
                    },
                    "button_type": "default",
                    "action_map": {
                      "foo": "bar"
                    }
                  }
                ]
              }
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UpdateCardMessageStreamButtonRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV2AppCardMessageStream.UpdateCardMessageStreamButtonAsync(UpdateCardMessageStreamButtonRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateCardMessageStreamButtonAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "failed_user_reasons": [
                        {
                            "error_code": 0,
                            "error_message": "The user is not in the chat",
                            "user_id": "ou_679eaeb583654bff73fefcc6e6371301"
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<BotTimeSentiveResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV2AppCardMessageStream.FeedCardsByFeedCardIdAsync(string, FeedCardsByFeedCardIdRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_FeedCardsByFeedCardIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "time_sensitive": true,
              "user_ids": [
                "ou_9d2beb613c85a2412862a49a924558c5"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<FeedCardsByFeedCardIdRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV2AppCardMessageStream.FeedCardsByFeedCardIdAsync(string, FeedCardsByFeedCardIdRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_FeedCardsByFeedCardIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "failed_user_reasons": [
                        {
                            "error_code": 0,
                            "error_message": "The user is not in the chat",
                            "user_id": "ou_679eaeb583654bff73fefcc6e6371301"
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<BotTimeSentiveResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }
}
