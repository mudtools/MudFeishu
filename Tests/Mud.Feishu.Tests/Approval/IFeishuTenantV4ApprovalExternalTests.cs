// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.ApprovalExternal;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuTenantV4ApprovalExternal"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV4ApprovalExternalTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV4ApprovalExternalTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalExternal.CreateApprovalAsync(CreateApprovalExternalRequest, string, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateApprovalAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "approval_name": "@i18n@d937443c-686f-11f0-aa8c-b6e035aec42e",
              "approval_code": "F46EB460-9476-4789-9524-ECD564291234",
              "group_code": "work_group",
              "group_name": "@i18n@d937444f-686f-11f0-aa8c-b6e035aec42e",
              "external": {
                "create_link_pc": "https://applink.feishu.cn/client/mini_program/open?mode=appCenter&appId=cli_9c90fc38e07a9101&path=pc/pages/create-form/index?id=9999",
                "create_link_mobile": "https://applink.feishu.cn/client/mini_program/open?appId=cli_9c90fc38e07a9101&path=pages/approval-form/index?id=9999",
                "support_pc": true,
                "support_mobile": true,
                "support_batch_read": false,
                "action_callback_url": "http://feishu.cn/approval/openapi/operate",
                "action_callback_token": "sdjkljkx9lxxxxx",
                "action_callback_key": "gfdqedvsadxxxxx",
                "enable_mark_readed": false
              },
              "i18n_resources": [
                {
                  "locale": "zh-CN",
                  "is_default": true,
                  "texts": [
                    {
                      "key": "@i18n@d937443c-686f-11f0-aa8c-b6e035aec42e",
                      "value": "people"
                    },
                    {
                      "key": "@i18n@d937444f-686f-11f0-aa8c-b6e035aec42e",
                      "value": "hr"
                    }
                  ]
                }
              ],
              "viewers": [
                {
                  "viewer_type": "TENANT"
                }
              ],
              "managers": [
                "96449fb3"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CreateApprovalExternalRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.GroupName);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalExternal.CreateApprovalAsync(CreateApprovalExternalRequest, string, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateApprovalAsync_Result()
    {
        string resultStr = @"
{
    ""code"": 0,
    ""msg"": ""success"",
    ""data"": {
        ""approval_code"": ""C30381C8-7A5F-4717-A9CF-C233BF0202D4""
    }
}
";
        var result = JsonSerializer.Deserialize<FeishuApiResult<CreateApprovalExternalResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.ApprovalCode!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalExternal.GetApprovalByCodeAsync(string, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetApprovalByCodeAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "approval_name": "@i18n@1",
                    "approval_code": "permission_test",
                    "group_code": "work_group",
                    "group_name": "@i18n@2",
                    "description": "@i18n@2",
                    "external": {
                        "biz_name": "@i18n@3",
                        "biz_type": "permission",
                        "create_link_mobile": "https://applink.feishu.cn/client/mini_program/open?appId=cli_9c90fc38e07a9101&path=pages/approval-form/index?id=9999",
                        "create_link_pc": "https://applink.feishu.cn/client/mini_program/open?mode=appCenter&appId=cli_9c90fc38e07a9101&path=pc/pages/create-form/index?id=9999",
                        "support_pc": true,
                        "support_mobile": true,
                        "support_batch_read": true,
                        "enable_mark_readed": true,
                        "enable_quick_operate": true,
                        "action_callback_url": "http://www.feishu.cn/approval/openapi/instanceOperate",
                        "action_callback_token": "sdjkljkx9lsadf110",
                        "action_callback_key": "gfdqedvsadfgfsd",
                        "allow_batch_operate": true,
                        "exclude_efficiency_statistics": true
                    },
                    "viewers": [
                        {
                            "viewer_type": "USER",
                            "viewer_user_id": "19a294c2",
                            "viewer_department_id": "od-ac9d697abfa990b715dcc33d58a62a9d"
                        }
                    ],
                    "i18n_resources": [
                        {
                            "locale": "zh-CN",
                            "texts": [
                                {
                                    "key": "@i18n@1",
                                    "value": "审批定义"
                                }
                            ],
                            "is_default": true
                        }
                    ],
                    "managers": [
                        "19a294c2"
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetApprovalExternalResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.ApprovalCode);
        Assert.NotNull(result.Data.ApprovalName);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalExternal.SyncInstancesAsync(SyncApprovalInstancesRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_SyncInstancesAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "approval_code": "81D31358-93AF-92D6-7425-01A5D67C4E71",
              "status": "PENDING",
              "extra": "{\"xxx\":\"xxx\",\"business_key\":\"xxx\"}",
              "instance_id": "24492654",
              "links": {
                "pc_link": "https://applink.feishu.cn/client/mini_program/open?mode=appCenter&appId=cli_9c90fc38e07a9101&path=pc/pages/detail?id=1234",
                "mobile_link": "https://applink.feishu.cn/client/mini_program/open?appId=cli_9c90fc38e07a9101&path=pages/detail?id=1234"
              },
              "title": "@i18n@1",
              "form": [
                {
                  "name": "@i18n@2",
                  "value": "@i18n@3"
                }
              ],
              "user_id": "a987sf9s",
              "user_name": "@i18n@9",
              "open_id": "ou_be73cbc0ee35eb6ca54e9e7cc14998c1",
              "department_id": "od-8ec33278bc2",
              "department_name": "@i18n@10",
              "start_time": "1556468012678",
              "end_time": "1556468012678",
              "update_time": "1556468012678",
              "display_method": "BROWSER",
              "update_mode": "UPDATE",
              "task_list": [
                {
                  "task_id": "112534",
                  "user_id": "a987sf9s",
                  "open_id": "ou_be73cbc0ee35eb6ca54e9e7cc14998c1",
                  "title": "@i18n@4",
                  "links": {
                    "pc_link": "https://applink.feishu.cn/client/mini_program/open?mode=appCenter&appId=cli_9c90fc38e07a9101&path=pc/pages/detail?id=1234",
                    "mobile_link": "https://applink.feishu.cn/client/mini_program/open?appId=cli_9c90fc38e07a9101&path=pages/detail?id=1234"
                  },
                  "status": "PENDING",
                  "extra": "{\"xxx\":\"xxx\",\"complete_reason\":\"approved\"}",
                  "create_time": "1556468012678",
                  "end_time": "1556468012678",
                  "update_time": "1556468012678",
                  "action_context": "123456",
                  "action_configs": [
                    {
                      "action_type": "APPROVE",
                      "action_name": "@i18n@5",
                      "is_need_reason": false,
                      "is_reason_required": false,
                      "is_need_attachment": false
                    }
                  ],
                  "display_method": "BROWSER",
                  "exclude_statistics": false,
                  "node_id": "node",
                  "node_name": "i18n@name",
                  "generate_type": "EXTERNAL_CONSIGN"
                }
              ],
              "cc_list": [
                {
                  "cc_id": "123456",
                  "user_id": "12345",
                  "open_id": "ou_be73cbc0ee35eb6ca54e9e7cc14998c1",
                  "links": {
                    "pc_link": "https://applink.feishu.cn/client/mini_program/open?mode=appCenter&appId=cli_9c90fc38e07a9101&path=pc/pages/detail?id=1234",
                    "mobile_link": "https://applink.feishu.cn/client/mini_program/open?appId=cli_9c90fc38e07a9101&path=pages/detail?id=1234"
                  },
                  "read_status": "READ",
                  "extra": "{\"xxx\":\"xxx\"}",
                  "title": "xxx",
                  "create_time": "1556468012678",
                  "update_time": "1556468012678",
                  "display_method": "BROWSER"
                }
              ],
              "i18n_resources": [
                {
                  "locale": "zh-CN",
                  "texts": [
                    {
                      "key": "@i18n@1",
                      "value": "people"
                    }
                  ],
                  "is_default": true
                }
              ],
              "trusteeship_url_token": "788981c886b1c28ac29d1e68efd60683d6d90dfce80938ee9453e2a5f3e9e306",
              "trusteeship_user_id_type": "user_id",
              "trusteeship_urls": {
                "form_detail_url": "https://#{your_domain}/api/form_detail",
                "action_definition_url": "https://#{your_domain}/api/action_definition",
                "approval_node_url": "https://#{your_domain}/api/approval_node",
                "action_callback_url": "https://#{your_domain}/api/action_callback",
                "pull_business_data_url": "https://#{your_domain}/api/pull_business_data"
              },
              "trusteeship_cache_config": {
                "form_policy": "DISABLE",
                "form_vary_with_locale": false,
                "form_version": "1"
              },
              "resource_region": "cn"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<SyncApprovalInstancesRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.ApprovalCode);
        Assert.NotEmpty(requestBody.InstanceId);

        Assert.NotEmpty(requestBody.CcLists!);
        Assert.NotEmpty(requestBody.TaskLists!);
        Assert.NotNull(requestBody.Links);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalExternal.SyncInstancesAsync(SyncApprovalInstancesRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_SyncInstancesAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "data": {
                        "approval_code": "81D31358-93AF-92D6-7425-01A5D67C4E71",
                        "status": "PENDING",
                        "extra": "{\"xxx\":\"xxx\",\"business_key\":\"xxx\"}",
                        "instance_id": "24492654",
                        "links": {
                            "pc_link": "https://applink.feishu.cn/client/mini_program/open?mode=appCenter&appId=cli_9c90fc38e07a9101&path=pc/pages/detail?id=1234",
                            "mobile_link": "https://applink.feishu.cn/client/mini_program/open?appId=cli_9c90fc38e07a9101&path=pages/detail?id=1234"
                        },
                        "title": "@i18n@1",
                        "form": [
                            {
                                "name": "@i18n@2",
                                "value": "@i18n@3"
                            }
                        ],
                        "user_id": "a987sf9s",
                        "user_name": "@i18n@9",
                        "open_id": "ou_be73cbc0ee35eb6ca54e9e7cc14998c1",
                        "department_id": "od-8ec33278bc2",
                        "department_name": "@i18n@10",
                        "start_time": "1556468012678",
                        "end_time": "1556468012678",
                        "update_time": "1556468012678",
                        "display_method": "BROWSER",
                        "update_mode": "UPDATE",
                        "task_list": [
                            {
                                "task_id": "112534",
                                "user_id": "a987sf9s",
                                "open_id": "ou_be73cbc0ee35eb6ca54e9e7cc14998c1",
                                "title": "i18n1",
                                "links": {
                                    "pc_link": "https://applink.feishu.cn/client/mini_program/open?mode=appCenter&appId=cli_9c90fc38e07a9101&path=pc/pages/detail?id=1234",
                                    "mobile_link": "https://applink.feishu.cn/client/mini_program/open?appId=cli_9c90fc38e07a9101&path=pages/detail?id=1234"
                                },
                                "status": "PENDING",
                                "extra": "{\"xxx\":\"xxx\",\"complete_reason\":\"approved\"}",
                                "create_time": "1556468012678",
                                "end_time": "1556468012678",
                                "update_time": "1556468012678",
                                "action_context": "123456",
                                "action_configs": [
                                    {
                                        "action_type": "APPROVE",
                                        "action_name": "@i18n@5",
                                        "is_need_reason": false,
                                        "is_reason_required": false,
                                        "is_need_attachment": false
                                    }
                                ],
                                "display_method": "BROWSER",
                                "exclude_statistics": false,
                                "node_id": "node",
                                "node_name": "i18n@name",
                                "generate_type": "EXTERNAL_CONSIGN"
                            }
                        ],
                        "cc_list": [
                            {
                                "cc_id": "123456",
                                "user_id": "12345",
                                "open_id": "ou_be73cbc0ee35eb6ca54e9e7cc14998c1",
                                "links": {
                                    "pc_link": "https://applink.feishu.cn/client/mini_program/open?mode=appCenter&appId=cli_9c90fc38e07a9101&path=pc/pages/detail?id=1234",
                                    "mobile_link": "https://applink.feishu.cn/client/mini_program/open?appId=cli_9c90fc38e07a9101&path=pages/detail?id=1234"
                                },
                                "read_status": "READ",
                                "extra": "{\"xxx\":\"xxx\"}",
                                "title": "xxx",
                                "create_time": "1556468012678",
                                "update_time": "1556468012678",
                                "display_method": "BROWSER"
                            }
                        ],
                        "i18n_resources": [
                            {
                                "locale": "zh-CN",
                                "texts": [
                                    {
                                        "key": "@i18n@1",
                                        "value": "people"
                                    }
                                ],
                                "is_default": true
                            }
                        ],
                        "trusteeship_url_token": "788981c886b1c28ac29d1e68efd60683d6d90dfce80938ee9453e2a5f3e9e306",
                        "trusteeship_user_id_type": "user_id",
                        "trusteeship_urls": {
                            "form_detail_url": "https://#{your_domain}/api/form_detail",
                            "action_definition_url": "https://#{your_domain}/api/action_definition",
                            "approval_node_url": "https://#{your_domain}/api/approval_node",
                            "action_callback_url": "https://#{your_domain}/api/action_callback",
                            "pull_business_data_url": "https://#{your_domain}/api/pull_business_data"
                        },
                        "trusteeship_cache_config": {
                            "form_policy": "DISABLE",
                            "form_vary_with_locale": false,
                            "form_version": "1"
                        },
                        "resource_region": "cn"
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<SyncExternalInstancesResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Data);
        Assert.NotEmpty(result.Data.Data.ApprovalCode);
        Assert.NotEmpty(result.Data.Data.InstanceId);

        Assert.NotEmpty(result.Data.Data.CcLists!);
        Assert.NotEmpty(result.Data.Data.TaskLists!);
        Assert.NotNull(result.Data.Data.Links);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalExternal.CheckInstancesAsync(CheckExternalInstancesRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CheckInstancesAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "instances": [
                {
                  "instance_id": "1234234234242423",
                  "update_time": "1591603040000",
                  "tasks": [
                    {
                      "task_id": "112253",
                      "update_time": "1591603040000"
                    }
                  ]
                }
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CheckExternalInstancesRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.Instances);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalExternal.CheckInstancesAsync(CheckExternalInstancesRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CheckInstancesAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "diff_instances": [
                        {
                            "instance_id": "1234234234242423",
                            "update_time": "1591603040000",
                            "tasks": [
                                {
                                    "task_id": "112253",
                                    "update_time": "1591603040000"
                                }
                            ]
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CheckExternalInstancesResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalExternal.GetInstancesStatePageListAsync(GetExternalInstancesStateRequest, int, string?, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_GetInstancesStatePageListAsync_RequestBody()
    {
        string bodyStr = """
                      {
                "approval_codes": [
                    "B7B65FFE-C2GC-452F-9F0F-9AA8352363D6"
                ],
                "instance_ids": [
                    "oa_159160304"
                ],
                "user_ids": [
                    "112321"
                ],
                "status": "PENDING"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<GetExternalInstancesStateRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.InstanceIds!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalExternal.GetInstancesStatePageListAsync(GetExternalInstancesStateRequest, int, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetInstancesStatePageListAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "data": [
                        {
                            "instance_id": "29075",
                            "approval_id": "fwwweffff33111133xxx",
                            "approval_code": "B7B65FFE-C2GC-452F-9F0F-9AA8352363D6",
                            "status": "PENDING",
                            "update_time": "1621863215000",
                            "tasks": [
                                {
                                    "id": "310",
                                    "status": "PENDING",
                                    "update_time": "1621863215000"
                                }
                            ]
                        }
                    ],
                    "page_token": "nF1ZXJ5VGhlbkZldGNoCgAAAAAA6PZwFmUzSldvTC1yU",
                    "has_more": false
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetInstancesStateResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }
}
