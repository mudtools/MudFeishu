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
/// 用于测试多维表格基础操作相关接口
/// </summary>
public class IFeishuV1BitableTests
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();

    #region 创建多维表格
    [Fact]
    public void TestCreateBitableAppAsyncRequestBody()
    {
        string bodyStr = """
                        {
              "name": "一篇新的多维表格",
              "folder_token": "fldcnqquW1svRIYVT2Np6Iabcef"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CreateBitableAppRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestCreateBitableAppAsyncResult()
    {
        string resultStr = """
                        {
                "code": 0,
                "data": {
                    "app": {
                        "app_token": "S404b*****e9PQsYDWYcNryFn0g",
                        "default_table_id": "tbl********oumSQ",
                        "folder_token": "fldcnqquW1svRIYVT2Np6Iabcef",
                        "name": "一篇新的多维表格",
                        "url": "https://example.feishu.cn/base/S404b*****e9PQsYDWYcNryFn0g"
                    }
                },
                "msg": "success"
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CreateBitableAppResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.App);
    }
    #endregion

    #region 复制多维表格
    [Fact]
    public void TestCopyBitableAppAsyncRequestBody()
    {
        string bodyStr = """
                        {
              "name": "一篇新的多维表格",
              "folder_token": "fldcnqquW1svRIYVT2Np6Iabcef",
              "without_content": false,
              "time_zone": "Asia/Shanghai"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CopyBitableAppRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestCopyBitableAppAsyncResult()
    {
        string resultStr = """
                        {
            	"code": 0,
            	"msg": "success",
            	"data": {
            		"app": {
            			"app_token": "S404b*****e9PQsYDWYcNryFn0g",
            			"name": "一篇新的多维表格",
            			"folder_token": "fldbco*****CIMltVc",
            			"url": "https://example.feishu.cn/base/S404b*****e9PQsYDWYcNryFn0g",
            			"time_zone": ""
            		}
            	}
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CopyBitableResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.App);
    }
    #endregion

    #region 获取多维表格元数据
    [Fact]
    public void TestGetBitableAppInfoAsyncResult()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "app": {
                        "app_token": "appbcbWCzen6D8dezhoCH2RpMAh",
                        "name": "mybase",
                        "revision": 1,
                        "is_advanced": false,
                        "time_zone": "Asia/Beijing",
                        "formula_type": 1,
                        "advance_version": "v1"
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetBitableAppResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.App);
    }
    #endregion

    #region 更新多维表格元数据
    [Fact]
    public void TestUpdateBitableAppAsyncRequestBody()
    {
        string bodyStr = """
                        {
              "name": "新的多维表格名称",
              "is_advanced": true
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UpdateBitableAppRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestUpdateBitableAppAsyncResult()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "app": {
                        "app_token": "appbcbWCzen6D8dezhoCH2RpMAh",
                        "name": "新的多维表格名字",
                        "is_advanced": true
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<UpdateBitableAppResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.App);
    }
    #endregion
}
