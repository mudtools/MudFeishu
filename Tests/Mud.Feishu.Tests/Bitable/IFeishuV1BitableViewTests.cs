// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.Bitable;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests.Bitable;

/// <summary>
/// 用于测试多维表格视图相关接口
/// </summary>
public class IFeishuV1BitableViewTests
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();

    #region 新增视图
    [Fact]
    public void TestCreateViewAsyncRequestBody()
    {
        string bodyStr = """
                        {
              "view_name": "表格视图 1",
              "view_type": "grid"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CreateViewRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestCreateViewAsyncResult()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "view": {
                        "view_id": "vewTpR1urY",
                        "view_name": "表格视图 1",
                        "view_type": "grid"
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CreateViewResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 更新视图
    [Fact]
    public void TestUpdateViewAsyncRequestBody()
    {
        string bodyStr = """
                        {
              "view_name": "grid",
              "property": {
                "filter_info": {
                  "conditions": [
                    {
                      "field_id": "fldpTw2262",
                      "operator": "isGreater",
                      "value": "[\"ExactDate\",\"1642672432000\"]"
                    }
                  ],
                  "conjunction": "and"
                },
                "hidden_fields": null
              }
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UpdateViewRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestUpdateViewAsyncResult()
    {
        string resultStr = """
                        {
              "code": 0,
              "data": {
                "view": {
                  "property": {
                    "filter_info": {
                      "condition_omitted": null,
                      "conditions": [
                        {
                          "condition_id": "conaklRhDC",
                          "field_id": "fldpTw2262",
                          "field_type": 5,
                          "operator": "isGreater",
                          "value": "[\"ExactDate\",1642672432000]"
                        }
                      ],
                      "conjunction": "and"
                    },
                    "hidden_fields": null,
                    "hierarchy_config": null
                  },
                  "view_id": "vewKecDsBf",
                  "view_name": "grid",
                  "view_type": "grid"
                }
              },
              "msg": "success"
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<UpdateViewResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 分页列出视图
    [Fact]
    public void TestGetViewsPageListAsyncResult()
    {
        string resultStr = """
                        {
            	"code": 0,
            	"msg": "success",
            	"data": {
            		"has_more": false,
            		"items": [{
            				"view_id": "vewqtI3f2u",
            				"view_name": "公共表格视图",
            				"view_public_level": "Public",
            				"view_type": "grid"
            			},
            			{
            				"view_id": "vew5Ys1Y1B",
            				"view_name": "个人表格视图",
            				"view_private_owner_id": "ou_fe4e2a0c10f41fb85620eb4b71d12082",
            				"view_public_level": "Private",
            				"view_type": "grid"
            			}
            		],
            		"page_token": "vew5Ys1Y1B",
            		"total": 2
            	}
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiPageListTotalResult<AppViewDetailInfo>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 获取视图
    [Fact]
    public void TestGetViewAsyncResult()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "view": {
                        "view_id": "vewsOleexJ",
                        "view_name": "grid",
                        "view_type": "grid",
                        "property": {
                            "filter_info": {
                                "condition_omitted": null,
                                "conditions": [
                                    {
                                        "condition_id": "conuKMQNNg",
                                        "field_id": "fldVioUai1",
                                        "field_type": 1,
                                        "operator": "is",
                                        "value": "[\"text content\"]"
                                    }
                                ],
                                "conjunction": "and"
                            },
                            "hidden_fields": null
                        }
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetViewResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 删除视图
    [Fact]
    public void TestDeleteViewAsyncResult()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {}
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetViewResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion
}
