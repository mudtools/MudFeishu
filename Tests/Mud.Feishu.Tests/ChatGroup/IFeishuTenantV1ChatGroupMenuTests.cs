// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.ChatGroupMenu;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuTenantV1ChatGroupMenu"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV1ChatGroupMenuTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV1ChatGroupMenuTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1ChatGroupMenu.AddMenuByIdAsync(string, AddChatGroupMenuRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_AddMenuByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "menu_tree": {
                "chat_menu_top_levels": [
                  {
                    "chat_menu_item": {
                      "action_type": "NONE",
                      "redirect_link": {
                        "common_url": "https://open.feishu.cn/",
                        "ios_url": "https://open.feishu.cn/",
                        "android_url": "https://open.feishu.cn/",
                        "pc_url": "https://open.feishu.cn/",
                        "web_url": "https://open.feishu.cn/"
                      },
                      "image_key": "img_v2_b0fbe905-7988-4282-b882-82edd010336j",
                      "name": "群聊",
                      "i18n_names": {
                        "zh_cn": "评审报名",
                        "en_us": "Sign up",
                        "ja_jp": "サインアップ"
                      }
                    },
                    "children": [
                      {
                        "chat_menu_item": {
                          "action_type": "NONE",
                          "redirect_link": {
                            "common_url": "https://open.feishu.cn/",
                            "ios_url": "https://open.feishu.cn/",
                            "android_url": "https://open.feishu.cn/",
                            "pc_url": "https://open.feishu.cn/",
                            "web_url": "https://open.feishu.cn/"
                          },
                          "image_key": "img_v2_b0fbe905-7988-4282-b882-82edd010336j",
                          "name": "群聊",
                          "i18n_names": {
                            "zh_cn": "评审报名",
                            "en_us": "Sign up",
                            "ja_jp": "サインアップ"
                          }
                        }
                      }
                    ]
                  }
                ]
              }
            }
            """;
        var requestBody = JsonSerializer.Deserialize<AddChatGroupMenuRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1ChatGroupMenu.AddMenuByIdAsync(string, AddChatGroupMenuRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_AddMenuByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "menu_tree": {
                        "chat_menu_top_levels": [
                            {
                                "chat_menu_top_level_id": "7117116451961487361",
                                "chat_menu_item": {
                                    "action_type": "NONE",
                                    "redirect_link": {
                                        "common_url": "https://open.feishu.cn/",
                                        "ios_url": "https://open.feishu.cn/",
                                        "android_url": "https://open.feishu.cn/",
                                        "pc_url": "https://open.feishu.cn/",
                                        "web_url": "https://open.feishu.cn/"
                                    },
                                    "name": "菜单",
                                    "i18n_names": {
                                        "zh_cn": "菜单",
                                        "en_us": "Menu",
                                        "ja_jp": "メニュー"
                                    }
                                },
                                "children": [
                                    {
                                        "chat_menu_second_level_id": "7039638308221468675",
                                        "chat_menu_item": {
                                            "action_type": "REDIRECT_LINK",
                                            "redirect_link": {
                                                "common_url": "https://open.feishu.cn/",
                                                "ios_url": "https://open.feishu.cn/",
                                                "android_url": "https://open.feishu.cn/",
                                                "pc_url": "https://open.feishu.cn/",
                                                "web_url": "https://open.feishu.cn/"
                                            },
                                            "image_key": "img_v2_b0fbe905-7988-4282-b882-82edd010336j",
                                            "name": "报名",
                                            "i18n_names": {
                                                "zh_cn": "报名",
                                                "en_us": "Sign up",
                                                "ja_jp": "サインアップ"
                                            }
                                        }
                                    }
                                ]
                            }
                        ]
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<ChatGroupMenuResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1ChatGroupMenu.UpdateMenuByIdAsync(string, string, UpdateChatMenuItemRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateMenuByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "update_fields": [
                "ICON"
              ],
              "chat_menu_item": {
                "action_type": "NONE",
                "redirect_link": {
                  "common_url": "https://open.feishu.cn/",
                  "ios_url": "https://open.feishu.cn/",
                  "android_url": "https://open.feishu.cn/",
                  "pc_url": "https://open.feishu.cn/",
                  "web_url": "https://open.feishu.cn/"
                },
                "image_key": "img_v2_b0fbe905-7988-4282-b882-82edd010336j",
                "name": "群聊",
                "i18n_names": {
                  "zh_cn": "评审报名",
                  "en_us": "Sign up",
                  "ja_jp": "サインアップ"
                }
              }
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UpdateChatMenuItemRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1ChatGroupMenu.UpdateMenuByIdAsync(string, string, UpdateChatMenuItemRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateMenuByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "chat_menu_item": {
                        "action_type": "NONE",
                        "redirect_link": {
                            "common_url": "https://open.feishu.cn/",
                            "ios_url": "https://open.feishu.cn/",
                            "android_url": "https://open.feishu.cn/",
                            "pc_url": "https://open.feishu.cn/",
                            "web_url": "https://open.feishu.cn/"
                        },
                        "image_key": "img_v2_b0fbe905-7988-4282-b882-82edd010336j",
                        "name": "报名",
                        "i18n_names": {
                            "zh_cn": "报名",
                            "en_us": "Sign up",
                            "ja_jp": "サインアップ"
                        }
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<UpdateChatMenuItemResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1ChatGroupMenu.DeleteMenuByIdAsync(string, ChartMenuIdsRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteMenuByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "chat_menu_top_level_ids": [
                "6936075528890826780"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<ChartMenuIdsRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.ChatMenuTopLevelIds);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1ChatGroupMenu.DeleteMenuByIdAsync(string, ChartMenuIdsRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteMenuByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "menu_tree": {
                        "chat_menu_top_levels": [
                            {
                                "chat_menu_top_level_id": "7117116451961487361",
                                "chat_menu_item": {
                                    "action_type": "NONE",
                                    "redirect_link": {
                                        "common_url": "https://open.feishu.cn/",
                                        "ios_url": "https://open.feishu.cn/",
                                        "android_url": "https://open.feishu.cn/",
                                        "pc_url": "https://open.feishu.cn/",
                                        "web_url": "https://open.feishu.cn/"
                                    },
                                    "image_key": "img_v2_b0fbe905-7988-4282-b882-82edd010336j",
                                    "name": "菜单",
                                    "i18n_names": {
                                        "zh_cn": "菜单",
                                        "en_us": "Menu",
                                        "ja_jp": "メニュー"
                                    }
                                },
                                "children": [
                                    {
                                        "chat_menu_second_level_id": "7039638308221468675",
                                        "chat_menu_item": {
                                            "action_type": "REDIRECT_LINK",
                                            "redirect_link": {
                                                "common_url": "https://open.feishu.cn/",
                                                "ios_url": "https://open.feishu.cn/",
                                                "android_url": "https://open.feishu.cn/",
                                                "pc_url": "https://open.feishu.cn/",
                                                "web_url": "https://open.feishu.cn/"
                                            },
                                            "image_key": "img_v2_b0fbe905-7988-4282-b882-82edd010336j",
                                            "name": "报名",
                                            "i18n_names": {
                                                "zh_cn": "报名",
                                                "en_us": "Sign up",
                                                "ja_jp": "サインアップ"
                                            }
                                        }
                                    }
                                ]
                            }
                        ]
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<ChatGroupMenuResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.MenuTree);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1ChatGroupMenu.SortMenuByIdAsync(string, ChartMenuIdsRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_SortMenuByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "chat_menu_top_level_ids": [
                "6936075528890826780"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<ChartMenuIdsRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.ChatMenuTopLevelIds);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1ChatGroupMenu.SortMenuByIdAsync(string, ChartMenuIdsRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_SortMenuByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "menu_tree": {
                        "chat_menu_top_levels": [
                            {
                                "chat_menu_top_level_id": "7117116451961487361",
                                "chat_menu_item": {
                                    "action_type": "NONE",
                                    "redirect_link": {
                                        "common_url": "https://open.feishu.cn/",
                                        "ios_url": "https://open.feishu.cn/",
                                        "android_url": "https://open.feishu.cn/",
                                        "pc_url": "https://open.feishu.cn/",
                                        "web_url": "https://open.feishu.cn/"
                                    },
                                    "image_key": "img_v2_b0fbe905-7988-4282-b882-82edd010336j",
                                    "name": "菜单",
                                    "i18n_names": {
                                        "zh_cn": "菜单",
                                        "en_us": "Menu",
                                        "ja_jp": "メニュー"
                                    }
                                },
                                "children": [
                                    {
                                        "chat_menu_second_level_id": "7039638308221468675",
                                        "chat_menu_item": {
                                            "action_type": "REDIRECT_LINK",
                                            "redirect_link": {
                                                "common_url": "https://open.feishu.cn/",
                                                "ios_url": "https://open.feishu.cn/",
                                                "android_url": "https://open.feishu.cn/",
                                                "pc_url": "https://open.feishu.cn/",
                                                "web_url": "https://open.feishu.cn/"
                                            },
                                            "image_key": "img_v2_b0fbe905-7988-4282-b882-82edd010336j",
                                            "name": "报名",
                                            "i18n_names": {
                                                "zh_cn": "报名",
                                                "en_us": "Sign up",
                                                "ja_jp": "サインアップ"
                                            }
                                        }
                                    }
                                ]
                            }
                        ]
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<ChatGroupMenuResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.MenuTree);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1ChatGroupMenu.GetMenuByIdAsync(string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetMenuByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "menu_tree": {
                        "chat_menu_top_levels": [
                            {
                                "chat_menu_top_level_id": "7117116451961487361",
                                "chat_menu_item": {
                                    "action_type": "NONE",
                                    "redirect_link": {
                                        "common_url": "https://open.feishu.cn/",
                                        "ios_url": "https://open.feishu.cn/",
                                        "android_url": "https://open.feishu.cn/",
                                        "pc_url": "https://open.feishu.cn/",
                                        "web_url": "https://open.feishu.cn/"
                                    },
                                    "image_key": "img_v2_b0fbe905-7988-4282-b882-82edd010336j",
                                    "name": "菜单",
                                    "i18n_names": {
                                        "zh_cn": "菜单",
                                        "en_us": "Menu",
                                        "ja_jp": "メニュー"
                                    }
                                },
                                "children": [
                                    {
                                        "chat_menu_second_level_id": "7039638308221468675",
                                        "chat_menu_item": {
                                            "action_type": "REDIRECT_LINK",
                                            "redirect_link": {
                                                "common_url": "https://open.feishu.cn/",
                                                "ios_url": "https://open.feishu.cn/",
                                                "android_url": "https://open.feishu.cn/",
                                                "pc_url": "https://open.feishu.cn/",
                                                "web_url": "https://open.feishu.cn/"
                                            },
                                            "image_key": "img_v2_b0fbe905-7988-4282-b882-82edd010336j",
                                            "name": "报名",
                                            "i18n_names": {
                                                "zh_cn": "报名",
                                                "en_us": "Sign up",
                                                "ja_jp": "サインアップ"
                                            }
                                        }
                                    }
                                ]
                            }
                        ]
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<ChatGroupMenuResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.MenuTree);
    }
}
