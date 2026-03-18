// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.TasksCustomFields;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuV2TaskCustomFields"/>接口的相关函数。
/// </summary>
public class IFeishuV2TaskCustomFieldsTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuV2TaskCustomFieldsTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskCustomFields.CreateCustomFieldsAsync(CreateCustomFieldsRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateCustomFieldsAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "resource_type": "tasklist",
              "resource_id": "ec5ed63d-a4a9-44de-a935-7ba243471c0a",
              "name": "优先级",
              "type": "number",
              "number_setting": {
                "format": "normal",
                "custom_symbol": "自定义符号",
                "custom_symbol_position": "left",
                "separator": "thousand",
                "decimal_count": 2
              },
              "member_setting": {
                "multi": true
              },
              "datetime_setting": {
                "format": "yyyy/mm/dd"
              },
              "single_select_setting": {
                "options": [
                  {
                    "name": "高优",
                    "color_index": 1,
                    "is_hidden": false
                  }
                ]
              },
              "multi_select_setting": {
                "options": [
                  {
                    "name": "高优",
                    "color_index": 1,
                    "is_hidden": false
                  }
                ]
              },
              "text_setting": {}
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CreateCustomFieldsRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.MultiSelectSetting);
        Assert.NotNull(requestBody.NumberSetting);
        Assert.NotEmpty(requestBody.ResourceId);
        Assert.NotEmpty(requestBody.ResourceType);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskCustomFields.CreateCustomFieldsAsync(CreateCustomFieldsRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateCustomFieldsAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "custom_field": {
                        "guid": "34d4b29f-3d58-4bc5-b752-6be80fb687c8",
                        "name": "优先级",
                        "type": "number",
                        "number_setting": {
                            "format": "normal",
                            "custom_symbol": "自定义符号",
                            "custom_symbol_position": "left",
                            "separator": "thousand",
                            "decimal_count": 2
                        },
                        "member_setting": {
                            "multi": true
                        },
                        "datetime_setting": {
                            "format": "yyyy/mm/dd"
                        },
                        "single_select_setting": {
                            "options": [
                                {
                                    "guid": "4216f79b-3fda-4dc6-a0c4-a16022e47152",
                                    "name": "高优",
                                    "color_index": 1,
                                    "is_hidden": false
                                }
                            ]
                        },
                        "multi_select_setting": {
                            "options": [
                                {
                                    "guid": "4216f79b-3fda-4dc6-a0c4-a16022e47152",
                                    "name": "高优",
                                    "color_index": 1,
                                    "is_hidden": false
                                }
                            ]
                        },
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "creator"
                        },
                        "created_at": "1688196600000",
                        "updated_at": "1688196600000",
                        "text_setting": {}
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CustomFieldsResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.CustomField);
        Assert.NotEmpty(result.Data.CustomField.Name!);
        Assert.NotEmpty(result.Data.CustomField.Guid!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskCustomFields.UpdateCustomFieldsAsync(string, UpdateCustomFieldsRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateCustomFieldsAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "custom_field": {
                "name": "优先级",
                "number_setting": {
                  "format": "normal",
                  "custom_symbol": "€",
                  "custom_symbol_position": "left",
                  "separator": "thousand",
                  "decimal_count": 2
                },
                "member_setting": {
                  "multi": true
                },
                "datetime_setting": {
                  "format": "yyyy/mm/dd"
                },
                "single_select_setting": {
                  "options": [
                    {
                      "guid": "4216f79b-3fda-4dc6-a0c4-a16022e47152",
                      "name": "高优",
                      "color_index": 1
                    }
                  ]
                },
                "multi_select_setting": {
                  "options": [
                    {
                      "guid": "4216f79b-3fda-4dc6-a0c4-a16022e47152",
                      "name": "高优",
                      "color_index": 1
                    }
                  ]
                },
                "text_setting": {}
              },
              "update_fields": [
                "name"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UpdateCustomFieldsRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.CustomField);
        Assert.NotEmpty(requestBody.CustomField.Name!);
        Assert.NotNull(requestBody.CustomField.NumberSetting!);
        Assert.NotEmpty(requestBody.CustomField.NumberSetting.Format!);
        Assert.NotNull(requestBody.CustomField);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskCustomFields.UpdateCustomFieldsAsync(string, UpdateCustomFieldsRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateCustomFieldsAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "custom_field": {
                        "guid": "34d4b29f-3d58-4bc5-b752-6be80fb687c8",
                        "name": "优先级",
                        "type": "number",
                        "number_setting": {
                            "format": "normal",
                            "custom_symbol": "自定义符号",
                            "custom_symbol_position": "left",
                            "separator": "thousand",
                            "decimal_count": 2
                        },
                        "member_setting": {
                            "multi": true
                        },
                        "datetime_setting": {
                            "format": "yyyy/mm/dd"
                        },
                        "single_select_setting": {
                            "options": [
                                {
                                    "guid": "4216f79b-3fda-4dc6-a0c4-a16022e47152",
                                    "name": "高优",
                                    "color_index": 1,
                                    "is_hidden": false
                                }
                            ]
                        },
                        "multi_select_setting": {
                            "options": [
                                {
                                    "guid": "4216f79b-3fda-4dc6-a0c4-a16022e47152",
                                    "name": "高优",
                                    "color_index": 1,
                                    "is_hidden": false
                                }
                            ]
                        },
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "editor"
                        },
                        "created_at": "1688196600000",
                        "updated_at": "1688196600000",
                        "text_setting": {}
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CustomFieldsResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.CustomField);
        Assert.NotEmpty(result.Data.CustomField.Name!);
        Assert.NotNull(result.Data.CustomField.DatetimeSetting);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskCustomFields.GetCustomFieldsByIdAsync(string, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetCustomFieldsByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "custom_field": {
                        "guid": "34d4b29f-3d58-4bc5-b752-6be80fb687c8",
                        "name": "优先级",
                        "type": "number",
                        "number_setting": {
                            "format": "normal",
                            "custom_symbol": "自定义符号",
                            "custom_symbol_position": "left",
                            "separator": "thousand",
                            "decimal_count": 2
                        },
                        "member_setting": {
                            "multi": true
                        },
                        "datetime_setting": {
                            "format": "yyyy/mm/dd"
                        },
                        "single_select_setting": {
                            "options": [
                                {
                                    "guid": "4216f79b-3fda-4dc6-a0c4-a16022e47152",
                                    "name": "高优",
                                    "color_index": 1,
                                    "is_hidden": false
                                }
                            ]
                        },
                        "multi_select_setting": {
                            "options": [
                                {
                                    "guid": "4216f79b-3fda-4dc6-a0c4-a16022e47152",
                                    "name": "高优",
                                    "color_index": 1,
                                    "is_hidden": false
                                }
                            ]
                        },
                        "creator": {
                            "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                            "type": "user",
                            "role": "creator"
                        },
                        "created_at": "1688196600000",
                        "updated_at": "1688196600000",
                        "text_setting": {}
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CustomFieldsResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.CustomField);
        Assert.NotEmpty(result.Data.CustomField.Name!);
        Assert.NotNull(result.Data.CustomField.DatetimeSetting);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskCustomFields.GetCustomFieldsPageListAsync(string?, string?, int, string?, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetCustomFieldsPageListAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "items": [
                        {
                            "guid": "34d4b29f-3d58-4bc5-b752-6be80fb687c8",
                            "name": "优先级",
                            "type": "number",
                            "number_setting": {
                                "format": "normal",
                                "custom_symbol": "自定义符号",
                                "custom_symbol_position": "left",
                                "separator": "thousand",
                                "decimal_count": 2
                            },
                            "member_setting": {
                                "multi": true
                            },
                            "datetime_setting": {
                                "format": "yyyy/mm/dd"
                            },
                            "single_select_setting": {
                                "options": [
                                    {
                                        "guid": "4216f79b-3fda-4dc6-a0c4-a16022e47152",
                                        "name": "高优",
                                        "color_index": 1,
                                        "is_hidden": false
                                    }
                                ]
                            },
                            "multi_select_setting": {
                                "options": [
                                    {
                                        "guid": "4216f79b-3fda-4dc6-a0c4-a16022e47152",
                                        "name": "高优",
                                        "color_index": 1,
                                        "is_hidden": false
                                    }
                                ]
                            },
                            "creator": {
                                "id": "ou_2cefb2f014f8d0c6c2d2eb7bafb0e54f",
                                "type": "user",
                                "role": "editor"
                            },
                            "created_at": "1688196600000",
                            "updated_at": "1688196600000",
                            "text_setting": {}
                        }
                    ],
                    "page_token": "aWQ9NzEwMjMzMjMxMDE=",
                    "has_more": false
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiPageListResult<CustomFieldInfo>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Items);
        Assert.NotEmpty(result.Data.PageToken!);
        Assert.NotNull(result.Data.Items[0].Guid);
        Assert.NotEmpty(result.Data.Items[0].Name!);
        Assert.NotNull(result.Data.Items[0].DatetimeSetting);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskCustomFields.AddCustomFieldsByIdAsync(string, CustomFieldsToResourceRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_AddCustomFieldsByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "resource_type": "tasklist",
              "resource_id": "0110a4bd-f24b-4a93-8c1a-1732b94f9593"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CustomFieldsToResourceRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.ResourceType);
        Assert.NotEmpty(requestBody.ResourceId);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskCustomFields.AddCustomFieldsByIdAsync(string, CustomFieldsToResourceRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_AddCustomFieldsByIdAsync_Result()
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

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskCustomFields.RemoveCustomFieldsByIdAsync(string, CustomFieldsToResourceRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_RemoveCustomFieldsByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "resource_type": "tasklist",
              "resource_id": "0110a4bd-f24b-4a93-8c1a-1732b94f9593"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CustomFieldsToResourceRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.ResourceType);
        Assert.NotEmpty(requestBody.ResourceId);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskCustomFields.RemoveCustomFieldsByIdAsync(string, CustomFieldsToResourceRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_RemoveCustomFieldsByIdAsync_Result()
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
    /// 用于测试<see cref="IFeishuV2TaskCustomFields.CreateCustomFieldsOptionsAsync(string, CreateCustomFieldsOptionsRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateCustomFieldsOptionsAsync_RequestBody()
    {
        string bodyStr = """                        
            {
              "name": "高优",
              "color_index": 10,
              "insert_before": "2bd905f8-ef38-408b-aa1f-2b2ad33b2913",
              "insert_after": "b13adf3c-cad6-4e02-8929-550c112b5633",
              "is_hidden": false
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CreateCustomFieldsOptionsRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Name!);
        Assert.NotEmpty(requestBody.InsertBefore!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskCustomFields.CreateCustomFieldsOptionsAsync(string, CreateCustomFieldsOptionsRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateCustomFieldsOptionsAsync_Result()
    {
        string resultStr = """
                   {
                "code": 0,
                "msg": "success",
                "data": {
                    "option": {
                        "guid": "4216f79b-3fda-4dc6-a0c4-a16022e47152",
                        "name": "高优",
                        "color_index": 1,
                        "is_hidden": false
                    }
                }
            }     
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CustomFieldsOptionsResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Option);
        Assert.NotEmpty(result.Data.Option.Name);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskCustomFields.UpdateCustomFieldsOptionsAsync(string, string, UpdateCustomFieldsOptionsRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateCustomFieldsOptionsAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "option": {
                "name": "高优",
                "color_index": 10,
                "insert_before": "2bd905f8-ef38-408b-aa1f-2b2ad33b2913",
                "insert_after": "b13adf3c-cad6-4e02-8929-550c112b5633",
                "is_hidden": false
              },
              "update_fields": [
                "name"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UpdateCustomFieldsOptionsRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Option!.Name!);
        Assert.NotEmpty(requestBody.Option.InsertBefore!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV2TaskCustomFields.UpdateCustomFieldsOptionsAsync(string, string, UpdateCustomFieldsOptionsRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateCustomFieldsOptionsAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "option": {
                        "guid": "4216f79b-3fda-4dc6-a0c4-a16022e47152",
                        "name": "高优",
                        "color_index": 1,
                        "is_hidden": false
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CustomFieldsOptionsResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.Option!.Guid!);
    }
}
