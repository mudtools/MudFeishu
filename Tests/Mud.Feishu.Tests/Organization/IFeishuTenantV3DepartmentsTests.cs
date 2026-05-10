// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.Departments;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuTenantV3Departments"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV3DepartmentsTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV3DepartmentsTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3Departments.CreateDepartmentAsync(DepartmentCreateRequest, string?, string?, string?, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateDepartmentAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "name": "DemoName",
              "i18n_name": {
                "zh_cn": "Demo名称",
                "ja_jp": "デモ名",
                "en_us": "Demo Name"
              },
              "parent_department_id": "D067",
              "department_id": "D096",
              "leader_user_id": "ou_7dab8a3d3cdcc9da365777c7ad535d62",
              "order": "100",
              "unit_ids": [
                "custom_unit_id"
              ],
              "create_group_chat": false,
              "leaders": [
                {
                  "leaderType": 1,
                  "leaderID": "ou_7dab8a3d3cdcc9da365777c7ad535d62"
                }
              ],
              "group_chat_employee_types": [
                1
              ],
              "department_hrbps": [
                "ou_7dab8a3d3cdcc9da365777c7ad535d62"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<DepartmentCreateRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);

        // 验证必需字段非空
        Assert.NotEmpty(requestBody.Name);
        Assert.NotNull(requestBody.DepartmentHrbps);
        Assert.NotNull(requestBody.Leaders);
        Assert.NotNull(requestBody.I18nName);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3Departments.CreateDepartmentAsync(DepartmentCreateRequest, string?, string?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateDepartmentAsync_Result()
    {
        string resultStr = """
                        {
                "code":0,
                "msg":"success",
                "data":{
                    "department":{
                        "name":"DemoName",
                        "i18n_name":{
                            "zh_cn":"Demo名称",
                            "ja_jp":"デモ名",
                            "en_us":"Demo Name"
                        },
                        "parent_department_id":"D067",
                        "department_id":"D096",
                        "open_department_id":"od-4e6ac4d14bcd5071a37a39de902c7141",
                        "leader_user_id":"ou_7dab8a3d3cdcc9da365777c7ad535d62",
                        "chat_id":"oc_5ad11d72b830411d72b836c20",
                        "order":"100",
                        "unit_ids":[
                            "custom_unit_id"
                        ],
                        "member_count":100,
                        "status":{
                            "is_deleted":false
                        },
                        "leaders":[
                            {
                                "leaderID":"ou_357368f98775f04bea02afc6b1d33478",
                                "leaderType":1
                            }
                        ],
                        "department_hrbps":[
                            "ou_7dab8a3d3cdcc9da365777c7ad535d62",
                            "ou_7dab8a3d3cdcc9da365777c7ad535d63"
                        ]
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<DepartmentCreateUpdateResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Department);
        Assert.NotEmpty(result.Data.Department.Name);
        Assert.NotNull(result.Data.Department.I18nName);
        Assert.NotNull(result.Data.Department.DepartmentHrbps);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3Departments.UpdatePartDepartmentAsync(string, DepartmentPartUpdateRequest, string?, string?, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdatePartDepartmentAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "name": "DemoName",
              "i18n_name": {
                "zh_cn": "Demo名称",
                "ja_jp": "デモ名",
                "en_us": "Demo Name"
              },
              "parent_department_id": "D067",
              "leader_user_id": "ou_7dab8a3d3cdcc9da365777c7ad535d62",
              "order": "100",
              "create_group_chat": false,
              "leaders": [
                {
                  "leaderType": 1,
                  "leaderID": "ou_7dab8a3d3cdcc9da365777c7ad535d62"
                }
              ],
              "group_chat_employee_types": [
                1
              ],
              "department_hrbps": [
                "ou_7dab8a3d3cdcc9da365777c7ad535d62"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<DepartmentPartUpdateRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Name);
        Assert.NotNull(requestBody.I18nName);
        Assert.NotNull(requestBody.Leaders);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3Departments.UpdatePartDepartmentAsync(string, DepartmentPartUpdateRequest, string?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdatePartDepartmentAsync_Result()
    {
        string resultStr = """
                        {
                "code":0,
                "msg":"success",
                "data":{
                    "department":{
                        "name":"DemoName",
                        "i18n_name":{
                            "zh_cn":"Demo名称",
                            "ja_jp":"デモ名",
                            "en_us":"Demo Name"
                        },
                        "parent_department_id":"D067",
                        "department_id":"D096",
                        "open_department_id":"od-4e6ac4d14bcd5071a37a39de902c7141",
                        "leader_user_id":"ou_7dab8a3d3cdcc9da365777c7ad535d62",
                        "chat_id":"oc_5ad11d72b830411d72b836c20",
                        "order":"100",
                        "unit_ids":[
                            "custom_unit_id"
                        ],
                        "member_count":100,
                        "status":{
                            "is_deleted":false
                        },
                        "leaders":[
                            {
                                "leaderID":"ou_357368f98775f04bea02afc6b1d33478",
                                "leaderType":1
                            }
                        ],
                        "department_hrbps":[
                            "ou_7dab8a3d3cdcc9da365777c7ad535d62",
                            "ou_7dab8a3d3cdcc9da365777c7ad535d63"
                        ]
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<DepartmentCreateUpdateResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Department);
        Assert.NotEmpty(result.Data.Department.Name);
        Assert.NotNull(result.Data.Department.I18nName);
        Assert.NotNull(result.Data.Department.DepartmentHrbps);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3Departments.UpdateDepartmentAsync(string, DepartmentUpdateRequest, string?, string?, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateDepartmentAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "name": "DemoName",
              "i18n_name": {
                "zh_cn": "Demo名称",
                "ja_jp": "デモ名",
                "en_us": "Demo Name"
              },
              "parent_department_id": "D067",
              "leader_user_id": "ou_7dab8a3d3cdcc9da365777c7ad535d62",
              "order": "100",
              "create_group_chat": false,
              "leaders": [
                {
                  "leaderType": 1,
                  "leaderID": "ou_7dab8a3d3cdcc9da365777c7ad535d62"
                }
              ],
              "group_chat_employee_types": [
                1
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<DepartmentUpdateRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Name);
        Assert.NotNull(requestBody.I18nName);
        Assert.NotNull(requestBody.Leaders);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3Departments.UpdateDepartmentAsync(string, DepartmentUpdateRequest, string?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateDepartmentAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "department": {
                        "name": "DemoName",
                        "i18n_name": {
                            "zh_cn": "Demo名称",
                            "ja_jp": "デモ名",
                            "en_us": "Demo Name"
                        },
                        "parent_department_id": "D067",
                        "department_id": "D096",
                        "open_department_id": "od-4e6ac4d14bcd5071a37a39de902c7141",
                        "leader_user_id": "ou_7dab8a3d3cdcc9da365777c7ad535d62",
                        "chat_id": "oc_5ad11d72b830411d72b836c20",
                        "order": "100",
                        "unit_ids": [
                            "custom_unit_id"
                        ],
                        "member_count": 100,
                        "status": {
                            "is_deleted": false
                        },
                        "leaders": [
                            {
                                "leaderID": "ou_357368f98775f04bea02afc6b1d33478",
                                "leaderType": 1
                            }
                        ]
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<DepartmentUpdateResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Department);
        Assert.NotEmpty(result.Data.Department.Name);
        Assert.NotNull(result.Data.Department.I18nName);
        Assert.NotNull(result.Data.Department.Leaders);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3Departments.UpdateDepartmentIdAsync(string, DepartMentUpdateIdRequest, string?, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateDepartmentIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "new_department_id": "NewDevDepartID"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<DepartMentUpdateIdRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.NewDepartmentId!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3Departments.UpdateDepartmentIdAsync(string, DepartMentUpdateIdRequest, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateDepartmentIdAsync_Result()
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
    /// 用于测试<see cref="IFeishuTenantV3Departments.UnbindDepartmentChatAsync(DepartmentRequest, string?, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UnbindDepartmentChatAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "department_id": "D096"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<DepartmentRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.DepartmentId);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3Departments.UnbindDepartmentChatAsync(DepartmentRequest, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UnbindDepartmentChatAsync_Result()
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
    /// 用于测试<see cref="IFeishuTenantV3Departments.DeleteDepartmentByIdAsync(string, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteDepartmentByIdAsync_Result()
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
        Assert.NotNull(result.Data);
    }
}
