// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.EmployeeType;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuTenantV3EmployeeType"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV3EmployeeTypeTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV3EmployeeTypeTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3EmployeeType.CreateEmployeeTypeAsync(EmployeeTypeEnumRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateEmployeeTypeAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "content": "专家",
              "enum_type": 2,
              "enum_status": 1,
              "i18n_content": [
                {
                  "locale": "zh_cn",
                  "value": "专家（中文）"
                }
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<EmployeeTypeEnumRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Content!);
        Assert.Equal(2, requestBody.EnumType);
        Assert.NotNull(requestBody.I18nContent);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3EmployeeType.CreateEmployeeTypeAsync(EmployeeTypeEnumRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateEmployeeTypeAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "employee_type_enum": {
                        "enum_id": "exGeIjow7zIqWMy+ONkFxA==",
                        "enum_value": "2",
                        "content": "专家",
                        "enum_type": 2,
                        "enum_status": 1,
                        "i18n_content": [
                            {
                                "locale": "zh_cn",
                                "value": "专家（中文）"
                            }
                        ]
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<EmployeeTypeEnumResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.EmployeeTypeEnum);
        Assert.NotEmpty(result.Data.EmployeeTypeEnum.Content!);
        Assert.NotNull(result.Data.EmployeeTypeEnum.I18nContent);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3EmployeeType.UpdateEmployeeTypeAsync(string, EmployeeTypeEnumRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateEmployeeTypeAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "content": "专家",
              "enum_type": 2,
              "enum_status": 1,
              "i18n_content": [
                {
                  "locale": "zh_cn",
                  "value": "专家（中文）"
                }
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<EmployeeTypeEnumRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Content!);
        Assert.Equal(2, requestBody.EnumType);
        Assert.NotNull(requestBody.I18nContent);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3EmployeeType.UpdateEmployeeTypeAsync(string, EmployeeTypeEnumRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateEmployeeTypeAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "employee_type_enum": {
                        "enum_id": "exGeIjow7zIqWMy+ONkFxA==",
                        "enum_value": "2",
                        "content": "专家",
                        "enum_type": 2,
                        "enum_status": 1,
                        "i18n_content": [
                            {
                                "locale": "zh_cn",
                                "value": "专家（中文）"
                            }
                        ]
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<EmployeeTypeEnumResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.EmployeeTypeEnum);
        Assert.NotEmpty(result.Data.EmployeeTypeEnum.Content!);
        Assert.NotNull(result.Data.EmployeeTypeEnum.I18nContent);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3EmployeeType.GetEmployeeTypesAsync(int?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetEmployeeTypesAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "items": [
                        {
                            "enum_id": "exGeIjow7zIqWMy+ONkFxA==",
                            "enum_value": "2",
                            "content": "专家",
                            "enum_type": 2,
                            "enum_status": 1,
                            "i18n_content": [
                                {
                                    "locale": "zh_cn",
                                    "value": "专家（中文）"
                                }
                            ]
                        }
                    ],
                    "has_more": true,
                    "page_token": "3"
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiPageListResult<EmployeeTypeEnum>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.PageToken);
        Assert.NotNull(result.Data.Items);
        Assert.NotNull(result.Data.Items[0].I18nContent);
        Assert.NotEmpty(result.Data.Items[0].Content!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3EmployeeType.DeleteEmployeeTypeByIdAsync(string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteEmployeeTypeByIdAsync_Result()
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
