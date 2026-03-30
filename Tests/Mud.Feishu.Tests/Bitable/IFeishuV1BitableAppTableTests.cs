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
/// 用于测试多维表格数据表相关接口
/// </summary>
public class IFeishuV1BitableAppTableTests
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();

    #region 新增一个数据表
    [Fact]
    public void TestCreateAppTableAsyncRequestBody()
    {
        string bodyStr = """
                        {
              "table": {
                "name": "数据表名称",
                "default_view_name": "默认的表格视图",
                "fields": [
                  {
                    "field_name": "索引字段",
                    "type": 1
                  },
                  {
                    "field_name": "单选",
                    "type": 3,
                    "ui_type": "SingleSelect",
                    "property": {
                      "options": [
                        {
                          "name": "Enabled",
                          "color": 0
                        },
                        {
                          "name": "Disabled",
                          "color": 1
                        },
                        {
                          "name": "Draft",
                          "color": 2
                        }
                      ]
                    }
                  }
                ]
              }
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CreateTableRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestCreateAppTableAsyncResult()
    {
        string resultStr = """
                        {
            	"code": 0,
            	"msg": "success",
            	"data": {
            		"table_id": "tblDBTWm6Es84d8c",
            		"default_view_id": "vewUuKOz2R",
            		"field_id_list": [
            			"fldhr2hBEA"
            		]
            	}
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CreateTableResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 新增多个数据表
    [Fact]
    public void TestCreateAppTablesAsyncRequestBody()
    {
        string bodyStr = """
             {
              "tables": [
                {
                  "name": "一个新的数据表"
                }
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CreateTablesRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestCreateAppTablesAsyncResult()
    {
        string resultStr = """
                       {
                "code": 0,
                "msg": "success",
                "data": {
                    "table_ids": [
                        "tblIovTTN2eIW2hn"
                    ]
                }
            } 
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CreateTablesResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 更新数据表
    [Fact]
    public void TestUpdateAppTableAsyncRequestBody()
    {
        string bodyStr = """
                   {
              "name": "新的数据表名称"
            }    
            """;
        var requestBody = JsonSerializer.Deserialize<UpdateAppTableRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestUpdateAppTableAsyncResult()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "name": "新的数据表名称"
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<UpdateAppTableResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 列出数据表
    [Fact]
    public void TestGetAppTablePageListAsyncResult()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "has_more": false,
                    "page_token": "tblKz5D60T4JlfcT",
                    "total": 1,
                    "items": [
                        {
                            "table_id": "tblKz5D60T4JlfcT",
                            "revision": 1,
                            "name": "数据表1"
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiPageListResult<AppTableBaseInfo>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 删除一个数据表
    [Fact]
    public void TestDeleteAppTableAsyncResult()
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
    #endregion

    #region 删除多个数据表
    [Fact]
    public void TestDeleteAppTablesAsyncRequestBody()
    {
        string bodyStr = """
                        {
              "table_ids": [
                "tbl1TkhyTWDkSoZ3"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<BatchDeleteRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestDeleteAppTablesAsyncResult()
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
    #endregion
}
