// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.JobFamilies;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuTenantV3JobFamilies"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV3JobFamiliesTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV3JobFamiliesTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3JobFamilies.CreateJobFamilyAsync(JobFamilyCreateUpdateRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateJobFamilyAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "name": "产品",
              "description": "负责产品策略制定的相关工作",
              "parent_job_family_id": "mga5oa8ayjlpzjq",
              "status": true,
              "i18n_name": [
                {
                  "locale": "zh_cn",
                  "value": "多语言内容"
                }
              ],
              "i18n_description": [
                {
                  "locale": "zh_cn",
                  "value": "多语言内容"
                }
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<JobFamilyCreateUpdateRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Name!);
        Assert.NotEmpty(requestBody.I18nName);
        Assert.Equal(true, requestBody.Status);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3JobFamilies.CreateJobFamilyAsync(JobFamilyCreateUpdateRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateJobFamilyAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "job_family": {
                        "name": "产品",
                        "description": "负责产品策略制定的相关工作",
                        "parent_job_family_id": "mga5oa8ayjlpzjq",
                        "status": true,
                        "i18n_name": [
                            {
                                "locale": "zh_cn",
                                "value": "多语言内容"
                            }
                        ],
                        "i18n_description": [
                            {
                                "locale": "zh_cn",
                                "value": "多语言内容"
                            }
                        ],
                        "job_family_id": "mga5oa8ayjlpkzy"
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<JobFamilyResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.JobFamily!.I18nName);
        Assert.NotEmpty(result.Data.JobFamily.Name!);
        Assert.NotEmpty(result.Data.JobFamily.Description!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3JobFamilies.UpdateJobFamilyAsync(string, JobFamilyCreateUpdateRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateJobFamilyAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "name": "产品",
              "description": "负责产品策略制定的相关工作",
              "parent_job_family_id": "mga5oa8ayjlpzjq",
              "status": true,
              "i18n_name": [
                {
                  "locale": "zh_cn",
                  "value": "多语言内容"
                }
              ],
              "i18n_description": [
                {
                  "locale": "zh_cn",
                  "value": "多语言内容"
                }
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<JobFamilyCreateUpdateRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Name!);
        Assert.NotEmpty(requestBody.I18nName);
        Assert.Equal(true, requestBody.Status);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3JobFamilies.UpdateJobFamilyAsync(string, JobFamilyCreateUpdateRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateJobFamilyAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "job_family": {
                        "name": "产品",
                        "description": "负责产品策略制定的相关工作",
                        "parent_job_family_id": "mga5oa8ayjlpzjq",
                        "status": true,
                        "i18n_name": [
                            {
                                "locale": "zh_cn",
                                "value": "多语言内容"
                            }
                        ],
                        "i18n_description": [
                            {
                                "locale": "zh_cn",
                                "value": "多语言内容"
                            }
                        ],
                        "job_family_id": "mga5oa8ayjlpkzy"
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<JobFamilyResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.JobFamily!.I18nName);
        Assert.NotEmpty(result.Data.JobFamily.Name!);
        Assert.NotEmpty(result.Data.JobFamily.Description!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3JobFamilies.GetJobFamilyByIdAsync(string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetJobFamilyByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "job_family": {
                        "name": "产品",
                        "description": "负责产品策略制定的相关工作",
                        "parent_job_family_id": "mga5oa8ayjlpzjq",
                        "status": true,
                        "i18n_name": [
                            {
                                "locale": "zh_cn",
                                "value": "多语言内容"
                            }
                        ],
                        "i18n_description": [
                            {
                                "locale": "zh_cn",
                                "value": "多语言内容"
                            }
                        ],
                        "job_family_id": "mga5oa8ayjlpkzy"
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<JobFamilyResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.JobFamily!.I18nName);
        Assert.NotEmpty(result.Data.JobFamily.Name!);
        Assert.NotEmpty(result.Data.JobFamily.Description!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3JobFamilies.GetJobFamilesListAsync(string?, int?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetJobFamilesListAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "items": [
                        {
                            "name": "产品",
                            "description": "负责产品策略制定的相关工作",
                            "parent_job_family_id": "mga5oa8ayjlpzjq",
                            "status": true,
                            "i18n_name": [
                                {
                                    "locale": "zh_cn",
                                    "value": "多语言内容"
                                }
                            ],
                            "i18n_description": [
                                {
                                    "locale": "zh_cn",
                                    "value": "多语言内容"
                                }
                            ],
                            "job_family_id": "mga5oa8ayjlpkzy"
                        }
                    ],
                    "page_token": "AQD9/Rn9eij9Pm39ED40/RD/cIFmu77WxpxPB/2oHfQLZ+G8JG6tK7+ZnHiT7COhD2hMSICh/eBl7cpzU6JEC3J7COKNe4jrQ8ExwBCR",
                    "has_more": true
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiPageListResult<JobFamilyInfo>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result!.Data);
        Assert.NotNull(result.Data.Items);
        Assert.NotNull(result.Data.Items[0]!.I18nName);
        Assert.NotEmpty(result.Data.Items[0].Name!);
        Assert.NotEmpty(result.Data.Items[0].Description!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3JobFamilies.DeleteJobFamilyByIdAsync(string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteJobFamilyByIdAsync_Result()
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
