// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.ChatTabs;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuV1ChatTabs"/>接口的相关函数。
/// </summary>
public class IFeishuV1ChatTabsTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuV1ChatTabsTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatTabs.CreateChatTabsByIdAsync(string, CreateChatTabsRequest, string, bool?, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateChatTabsByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "chat_tabs": [
                {
                  "tab_name": "文档",
                  "tab_type": "doc",
                  "tab_content": {
                    "url": "https://www.feishu.cn",
                    "doc": "https://example.feishu.cn/wiki/wikcnPIcqWjJQwkwDzrB9t40123xz",
                    "meeting_minute": "https://example.feishu.cn/docs/doccnvIXbV22i6hSD3utar4123dx",
                    "task": "https://bytedance.feishu.cn/client/todo/task_list?guid=fa03fb6d-344b-47d9-83e3-049e3b3da931"
                  },
                  "tab_config": {
                    "icon_key": "img_v2_b99741-7628-4abd-aad0-b881e4db83ig",
                    "is_built_in": false
                  }
                }
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CreateChatTabsRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.ChatTabs);
        Assert.NotEmpty(requestBody.ChatTabs[0].TabName!);
        Assert.NotNull(requestBody.ChatTabs[0].TabContent);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatTabs.CreateChatTabsByIdAsync(string, CreateChatTabsRequest, string, bool?, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateChatTabsByIdAsync_Result()
    {
        string resultStr = """
            {
                "code": 0,
                "msg": "ok",
                "data": {
                    "chat_tabs": [
                        {
                           "tab_id": "7101214603622940633",
                           "tab_type": "message"
                        },
                        {
                            "tab_id": "7101214603622940671",
                            "tab_name": "文档",
                            "tab_type": "doc",
                            "tab_content": {
                                "doc": "https://example.feishu.cn/wiki/wikcnPIcqWjJQwkwDzrB9t40123xz"
                            }
                        },
                        {
                            "tab_id": "7158333373373759422",
                            "tab_name": "测试",
                            "tab_type": "url",
                            "tab_content": {
                                "url": "https://www.test.cn"
                            },
                            "tab_config": {
                                "icon_key": "img_v2_b99741-7628-4abd-aad0-b881e4db83ig",
                                "is_built_in": true
                            }
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<ChatTabsCreateResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.ChatTabs!);
        Assert.NotEmpty(result.Data.ChatTabs![1].TabName!);
        Assert.NotNull(result.Data.ChatTabs[1].TabContent);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatTabs.UpdateChatTabsByIdAsync(string, UpdateChatTabsRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateChatTabsByIdAsync_RequestBody()
    {
        string bodyStr = """
                  {
              "chat_tabs": [
                {
                  "tab_id": "7101214603622940671",
                  "tab_name": "文档",
                  "tab_type": "doc",
                  "tab_content": {
                    "url": "https://www.feishu.cn",
                    "doc": "https://example.feishu.cn/wiki/wikcnPIcqWjJQwkwDzrB9t40123xz",
                    "meeting_minute": "https://example.feishu.cn/docs/doccnvIXbV22i6hSD3utar4123dx",
                    "task": "https://bytedance.feishu.cn/client/todo/task_list?guid=fa03fb6d-344b-47d9-83e3-049e3b3da931"
                  },
                  "tab_config": {
                    "icon_key": "img_v2_b99741-7628-4abd-aad0-b881e4db83ig",
                    "is_built_in": false
                  }
                }
              ]
            }      
            """;
        var requestBody = JsonSerializer.Deserialize<UpdateChatTabsRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.ChatTabs);
        Assert.NotEmpty(requestBody.ChatTabs[0].TabName!);
        Assert.NotNull(requestBody.ChatTabs[0].TabContent);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatTabs.UpdateChatTabsByIdAsync(string, UpdateChatTabsRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateChatTabsByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "ok",
                "data": {
                    "chat_tabs": [
                        {
                           "tab_id": "7101214603622940633",
                            "tab_type": "message"
                        },
                        {
                            "tab_id": "7103849256556953620",
                            "tab_name": "update",
                            "tab_type": "doc",
                            "tab_content": {
                                "doc": "https://example.feishu.cn/docx/doxbcjoYDoEtuwC0k0hryQBkSV1"
                            }
                        },
                        {
                            "tab_id": "7103849256561164308",
                            "tab_name": "url-update",
                            "tab_type": "url",
                            "tab_content": {
                                "url": "https://www.feishu.cn/"
                            }
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<ChatTabsUpdateResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.ChatTabs!);
        Assert.NotEmpty(result.Data.ChatTabs![1].TabName!);
        Assert.NotNull(result.Data.ChatTabs[1].TabContent);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatTabs.DeleteChatTabsByIdAsync(string, ChatTabsIdsRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteChatTabsByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "tab_ids": [
                "6936075528890826780"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<ChatTabsIdsRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.TabIds);
        Assert.NotEmpty(requestBody.TabIds[0]);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatTabs.DeleteChatTabsByIdAsync(string, ChatTabsIdsRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteChatTabsByIdAsync_Result()
    {
        string resultStr = """
                        {
            	"code": 0,
            	"msg": "ok",
            	"data": {
            		"chat_tabs": [{
            				"tab_id": "7101214603622940633",
            				"tab_type": "message"
            			},
            			{
            				"tab_id": "7101214603622940671",
            				"tab_name": "文档",
            				"tab_type": "doc",
            				"tab_content": {
            					"doc": "https://example.feishu.cn/wiki/wikcnPIcqWjJQwkwDzrB9t40123xz"
            				}
            			}
            		]
            	}
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<DeleteTabsResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.ChatTabs);
        Assert.NotEmpty(result.Data.ChatTabs[1].TabId);
        Assert.NotNull(result.Data.ChatTabs[1].TabContent);
        Assert.NotEmpty(result.Data.ChatTabs[1].TabContent.Doc);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatTabs.ChatTabsSortByIdAsync(string, ChatTabsIdsRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_ChatTabsSortByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "tab_ids": [
                "6936075528890826780"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<ChatTabsIdsRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.TabIds);
        Assert.NotEmpty(requestBody.TabIds[0]);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatTabs.ChatTabsSortByIdAsync(string, ChatTabsIdsRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_ChatTabsSortByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "ok",
                "data": {
                    "chat_tabs": [
                        {
                            "tab_content": {},
                            "tab_id": "7104164142520467475",
                            "tab_type": "message"
                        },
                        {
                            "tab_content": {},
                            "tab_id": "7104164246245605395",
                            "tab_type": "pin"
                        },
                        {
                            "tab_content": {
                                "url": "https://www.feishu.cn/"
                            },
                            "tab_id": "7104168465417633811",
                            "tab_name": "url2",
                            "tab_type": "url"
                        },
                        {
                            "tab_content": {
                                "doc": "https://example.feishu.cn/docx/doxbcjoYDoEtuwC0k0hryQBkSV1"
                            },
                            "tab_id": "7104168465379885076",
                            "tab_name": "doc2",
                            "tab_type": "doc"
                        },
                        {
                            "tab_content": {
                                "url": "https://www.feishu.cn/"
                            },
                            "tab_id": "7104168141097287699",
                            "tab_name": "url1",
                            "tab_type": "url"
                        },
                        {
                            "tab_content": {
                                "doc": "https://example.feishu.cn/docx/doxbcjoYDoEtuwC0k0hryQBkSV1"
                            },
                            "tab_id": "7104168141063716884",
                            "tab_name": "doc1",
                            "tab_type": "doc"
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<ChatTabsSortResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.ChatTabs);
        Assert.NotNull(result.Data.ChatTabs[2].TabContent);
        Assert.NotEmpty(result.Data.ChatTabs[0].TabType);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatTabs.GetChatTabsListByIdAsync(string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetChatTabsListByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "ok",
                "data": {
                    "chat_tabs": [
                        {
                           "tab_id": "7101214603622940633",
                            "tab_type": "message"
                        },
                        {
                            "tab_id": "7101214603622940671",
                            "tab_name": "文档",
                            "tab_type": "doc",
                            "tab_content": {
                                "doc": "https://example.feishu.cn/wiki/wikcnPIcqWjJQwkwDzrB9t40123xz"
                            }
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetChatTabsResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.ChatTabs!);
        Assert.NotNull(result.Data.ChatTabs![1].TabContent);
        Assert.NotEmpty(result.Data.ChatTabs[1].TabName!);
    }
}
