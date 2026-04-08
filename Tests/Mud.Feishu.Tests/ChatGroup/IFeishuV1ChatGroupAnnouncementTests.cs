// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.ChatGroupNotice;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuV1ChatGroupAnnouncement"/>接口的相关函数。
/// </summary>
public class IFeishuV1ChatGroupAnnouncementTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuV1ChatGroupAnnouncementTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupAnnouncement.GetNoticeInfoByIdAsync(string, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetNoticeInfoByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "revision_id": 1,
                    "create_time": 1609296809,
                    "update_time": 1609296809,
                    "owner_id": "ou_7d8a6e6df7621556ce0d21922b676706ccs",
                    "owner_id_type": "user_id",
                    "modifier_id": "ou_7d8a6e6df7621556ce0d21922b676706ccs",
                    "modifier_id_type": "user_id",
                    "announcement_type": "docx",
                    "create_time_v2": "1609296809",
                    "update_time_v2": "1609296809"
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetAnnouncementResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupAnnouncement.GetNoticeBlocksListByIdAsync(string, int?, string?, int?, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetNoticeBlocksListByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "data": {
                    "has_more": false,
                    "items": [
                        {
                            "block_id": "oc_5ad11d72b830411d72b836c20",
                            "block_type": 1,
                            "children": [
                                "EJ8pdBT6RoA2BtxKXfuc1TD0n8f",
                                "WoF4dolv0ohvegxctR9c1t0sn4b",
                                "UPD7dhkrLo8ZCfxoAYLcuEbDnzd"
                            ],
                            "page": {
                                "elements": [
                                    {
                                        "text_run": {
                                            "content": "",
                                            "text_element_style": {
                                                "bold": false,
                                                "inline_code": false,
                                                "italic": false,
                                                "strikethrough": false,
                                                "underline": false
                                            }
                                        }
                                    }
                                ],
                                "style": {
                                    "align": 1
                                }
                            },
                            "parent_id": ""
                        },
                        {
                            "block_id": "EJ8pdBT6RoA2BtxKXfuc1TD0n8f",
                            "block_type": 3,
                            "heading1": {
                                "elements": [
                                    {
                                        "text_run": {
                                            "content": "新版群公告",
                                            "text_element_style": {
                                                "bold": false,
                                                "inline_code": false,
                                                "italic": false,
                                                "strikethrough": false,
                                                "underline": false
                                            }
                                        }
                                    }
                                ],
                                "style": {
                                    "align": 1,
                                    "folded": false
                                }
                            },
                            "parent_id": "oc_5ad11d72b830411d72b836c20"
                        },
                        {
                            "block_id": "WoF4dolv0ohvegxctR9c1t0sn4b",
                            "block_type": 2,
                            "parent_id": "oc_5ad11d72b830411d72b836c20",
                            "text": {
                                "elements": [
                                    {
                                        "text_run": {
                                            "content": "当你需要向群内所有成员传达",
                                            "text_element_style": {
                                                "bold": false,
                                                "inline_code": false,
                                                "italic": false,
                                                "strikethrough": false,
                                                "underline": false
                                            }
                                        }
                                    },
                                    {
                                        "text_run": {
                                            "content": "重要信息",
                                            "text_element_style": {
                                                "bold": true,
                                                "inline_code": false,
                                                "italic": false,
                                                "strikethrough": false,
                                                "underline": false
                                            }
                                        }
                                    },
                                    {
                                        "text_run": {
                                            "content": "时，可以使用群公告功能。后续入群的成员也可看到当前群公告。",
                                            "text_element_style": {
                                                "bold": false,
                                                "inline_code": false,
                                                "italic": false,
                                                "strikethrough": false,
                                                "underline": false
                                            }
                                        }
                                    }
                                ],
                                "style": {
                                    "align": 1,
                                    "folded": false
                                }
                            }
                        },
                        {
                            "block_id": "UPD7dhkrLo8ZCfxoAYLcuEbDnzd",
                            "block_type": 2,
                            "parent_id": "oc_5ad11d72b830411d72b836c20",
                            "text": {
                                "elements": [
                                    {
                                        "text_run": {
                                            "content": "群公告编辑",
                                            "text_element_style": {
                                                "bold": false,
                                                "inline_code": false,
                                                "italic": false,
                                                "strikethrough": false,
                                                "underline": false
                                            }
                                        }
                                    },
                                    {
                                        "text_run": {
                                            "content": "支持实时保存及多人协作",
                                            "text_element_style": {
                                                "background_color": 4,
                                                "bold": false,
                                                "inline_code": false,
                                                "italic": false,
                                                "strikethrough": false,
                                                "underline": false
                                            }
                                        }
                                    },
                                    {
                                        "text_run": {
                                            "content": "，群成员",
                                            "text_element_style": {
                                                "bold": false,
                                                "inline_code": false,
                                                "italic": false,
                                                "strikethrough": false,
                                                "underline": false
                                            }
                                        }
                                    },
                                    {
                                        "text_run": {
                                            "content": "可实时查看最新群公告内容。",
                                            "text_element_style": {
                                                "background_color": 4,
                                                "bold": false,
                                                "inline_code": false,
                                                "italic": false,
                                                "strikethrough": false,
                                                "underline": false
                                            }
                                        }
                                    }
                                ],
                                "style": {
                                    "align": 1,
                                    "folded": false
                                }
                            }
                        }
                    ]
                },
                "msg": "success"
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiPageListResult<AnnouncementBlock>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Items);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupAnnouncement.CreateNoticeBlockAsync(string, string, CreateBlockRequest, int, string?, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateNoticeBlockAsync_RequestBody()
    {
        string bodyStr = """
                        {
                "index": 0,
                "children": [
                    {
                        "block_type": 2,
                        "text": {
                            "elements": [
                                {
                                    "text_run": {
                                        "content": "当你需要向群内所有成员传达重要信息时，可以使用群公告功能。后续入群的成员也可看到当前群公告。",
                                        "text_element_style": {
                                            "background_color": 14,
                                            "text_color": 5
                                        }
                                    }
                                },
                                {
                                    "text_run": {
                                        "content": "群公告编辑支持实时保存及多人协作，群成员可实时查看最新群公告内容。",
                                        "text_element_style": {
                                            "background_color": 14,
                                            "bold": true,
                                            "text_color": 5
                                        }
                                    }
                                }
                            ],
                            "style": {}
                        }
                    }
                ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CreateBlockRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Childrens!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupAnnouncement.CreateNoticeBlockAsync(string, string, CreateBlockRequest, int, string?, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateNoticeBlockAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "data": {
                    "children": [
                        {
                            "block_id": "doxcnXgNGAtaAraIRVeCfmbx4Eo",
                            "block_type": 2,
                            "parent_id": "oc_5ad11d72b830411d72b836c20",
                            "text": {
                                "elements": [
                                    {
                                        "text_run": {
                                            "content": "当你需要向群内所有成员传达重要信息时，可以使用群公告功能。后续入群的成员也可看到当前群公告。",
                                            "text_element_style": {
                                                "background_color": 14,
                                                "text_color": 5
                                            }
                                        }
                                    },
                                    {
                                        "text_run": {
                                            "content": "群公告编辑支持实时保存及多人协作，群成员可实时查看最新群公告内容。",
                                            "text_element_style": {
                                                "background_color": 14,
                                                "bold": true,
                                                "text_color": 5
                                            }
                                        }
                                    }
                                ],
                                "style": {}
                            }
                        }
                    ],
                    "client_token": "ea403093-3af1-4e9d-8f5d-53c5a4e4c36e",
                    "document_revision_id": 2
                },
                "msg": ""
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CreateBlockResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Childrens);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupAnnouncement.UpdateNoticeBlockAsync(string, BlocksBatchUpdateRequest, int, string?, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateNoticeBlockAsync_RequestBody()
    {
        string bodyStr = """{"requests":[],"update_text_elements":[]}""";
        var requestBody = JsonSerializer.Deserialize<BlocksBatchUpdateRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupAnnouncement.UpdateNoticeBlockAsync(string, BlocksBatchUpdateRequest, int, string?, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateNoticeBlockAsync_Result()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"revision_id":1}}""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<BatchUpdateResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupAnnouncement.GetBlockContentByIdAsync(string, string, int?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetBlockContentByIdAsync_Result()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"items":[]}}""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetBlockContentListResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupAnnouncement.GetBlockContentPageListByIdAsync(string, string, int?, string?, int?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetBlockContentPageListByIdAsync_Result()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"items":[],"has_more":false,"page_token":""}}""";
        var result = JsonSerializer.Deserialize<FeishuApiPageListResult<AnnouncementBlock>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Items);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupAnnouncement.DeleteBlockByIdAsync(string, string, DeleteAnnouncementBlockRequest, string?, string?, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteBlockByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "start_index": 0,
              "end_index": 1
            }
            """;
        var requestBody = JsonSerializer.Deserialize<DeleteAnnouncementBlockRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV1ChatGroupAnnouncement.DeleteBlockByIdAsync(string, string, DeleteAnnouncementBlockRequest, string?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteBlockByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "revision_id": 1,
                    "client_token": "fe599b60-450f-46ff-b2ef-9f6675625b97"
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<DeleteAnnouncementBlockResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.RevisionId);
        Assert.NotEmpty(result.Data.ClientToken!);
    }
}
