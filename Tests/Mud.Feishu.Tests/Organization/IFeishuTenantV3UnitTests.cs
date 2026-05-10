// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.Units;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuTenantV3Unit"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV3UnitTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV3UnitTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3Unit.CreateUnitAsync(UnitInfoRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateUnitAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "unit_id": "BU121",
              "name": "消费者事业部",
              "unit_type": "子公司"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UnitInfoRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.UnitType!);
        Assert.NotEmpty(requestBody.UnitId!);
        Assert.NotEmpty(requestBody.Name!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3Unit.CreateUnitAsync(UnitInfoRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateUnitAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "unit_id": "BU121"
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<UnitCreateResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.UnitId!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3Unit.UpdateUnitAsync(string, UnitNameUpdateRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateUnitAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "name": "消费者事业部"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UnitNameUpdateRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Name);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3Unit.UpdateUnitAsync(string, UnitNameUpdateRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateUnitAsync_Result()
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
    /// 用于测试<see cref="IFeishuTenantV3Unit.BindDepartmentAsync(UnitBindDepartmentRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_BindDepartmentAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "unit_id": "BU121",
              "department_id": "od-4e6ac4d14bcd5071a37a39de902c7141",
              "department_id_type": "open_department_id"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UnitBindDepartmentRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.UnitId!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3Unit.BindDepartmentAsync(UnitBindDepartmentRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_BindDepartmentAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {}
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<UnitCreateResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3Unit.UnBindDepartmentAsync(UnitBindDepartmentRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UnBindDepartmentAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "unit_id": "BU121",
              "department_id": "od-4e6ac4d14bcd5071a37a39de902c7141",
              "department_id_type": "open_department_id"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UnitBindDepartmentRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.DepartmentId!);
        Assert.NotEmpty(requestBody.DepartmentIdType!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3Unit.UnBindDepartmentAsync(UnitBindDepartmentRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UnBindDepartmentAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {}
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<UnitCreateResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3Unit.GetDepartmentListAsync(string, int?, string?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetDepartmentListAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "departmentlist": [
                        {
                            "unit_id": "BU121",
                            "department_id": "od-4e6ac4d14bcd5071a37a39de902c7141"
                        }
                    ],
                    "has_more": true,
                    "page_token": "AQD9/Rn9eij9Pm39ED40/dk53s4Ebp882DYfFaPFbz00L4CMZJrqGdzNyc8BcZtDbwVUvRmQTvyMYicnGWrde9X56TgdBuS+JdtW="
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<UnitDepartmentListResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.PageToken);
        Assert.NotEmpty(result.Data.DepartmentList);
        Assert.NotEmpty(result.Data.DepartmentList[0].UnitId);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3Unit.GetUnitInfoAsync(string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetUnitInfoAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "unit": {
                        "unit_id": "BU121",
                        "name": "消费者事业部",
                        "unit_type": "事业部"
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<UnitInfoResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Unit);
        Assert.NotEmpty(result.Data.Unit.Name);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3Unit.GetUnitListAsync(int?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetUnitListAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "unitlist": [
                        {
                            "unit_id": "BU121",
                            "name": "消费者事业部",
                            "unit_type": "事业部"
                        }
                    ],
                    "has_more": true,
                    "page_token": "AQD9/Rn9eij9Pm39ED40/dk53s4Ebp882DYfFaPFbz00L4CMZJrqGdzNyc8BcZtDbwVUvRmQTvyMYicnGWrde9X56TgdBudfdagatagdd="
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<UnitListDataResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.UnitList);
        Assert.NotNull(result.Data.PageToken);
        Assert.NotEmpty(result.Data.UnitList[0].Name);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3Unit.DeleteUnitByIdAsync(string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteUnitByIdAsync_Result()
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
}
