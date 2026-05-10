// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.Approval;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuTenantV4Approval"/>接口的单元测试类。
/// </summary>
public class IFeishuTenantV4ApprovalTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV4ApprovalTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4Approval.CreateApprovalAsync(CreateApprovalRequest, string, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateApprovalAsync_RequestBody()
    {
        string bodyStr = """
                        {
                "approval_name": "@i18n@approval_name",
                "approval_code": "813718CE-F38D-45CA-A5C1-ACF4F564B526",
                "viewers":[
                    {
                        "viewer_type":"TENANT",
                        "viewer_user_id":""
                    }
                ],
                "form": {
                    "form_content": "[{\"id\":\"111\",\"name\":\"@i18n@event_name\",\"required\":true,\"type\":\"input\"},{\"id\":\"222\",\"name\":\"@i18n@time_interval\",\"required\":true,\"type\":\"dateInterval\",\"value\":{\"format\":\"YYYY-MM-DD hh:mm\",\"intervalAllowModify\":false}},{\"id\":\"333\",\"name\":\"@i18n@event_type\",\"type\":\"radioV2\",\"value\":[{\"key\":\"1\",\"text\":\"@i18n@recurrence_event\"},{\"key\":\"2\",\"text\":\"@i18n@single_event\"}]},{\"id\":\"444\",\"name\":\"@i18n@attende_count\",\"required\":true,\"type\":\"number\"},{\"id\":\"555\",\"name\":\"@i18n@apply_reason\",\"required\":true,\"type\":\"textarea\"}]"
                    },

                "node_list": [{
                      "id": "START",
                      "privilege_field":{ 
            				 "writable": ["111","222"],
            				 "readable": ["111","222"]
            		  }
                    },{
                      "id": "7106864726566",
                      "privilege_field":{ 
            				 "writable": ["111","222"],
            				 "readable": ["111","222"]
            		  },
                      "name": "@i18n@node_name",
                      "node_type": "AND",
                      "approver": [
                        {
                          "type": "Personal",
                          "user_id": "59a92c4a"
                        }
                      ],
                      "ccer": [
                        {
                          "type": "Supervisor",
                          "level": "2"
                        }
                      ]
                    },{
                      "id": "END"
                    }],
                "settings" : {
                      "revert_interval":0
                    },
                "config" : {
                      "can_update_viewer": false,
                      "can_update_form": true,
                      "can_update_process": true,
                      "can_update_revert": true,
                      "help_url":"https://www.baidu.com"
                    },
                "icon": 1,
                "i18n_resources" : [{
                      "locale": "zh-CN",
                      "texts" : [
                          {"key":"@i18n@approval_name","value":"审批名称"},
                          {"key":"@i18n@event_name","value":"日程名称"},
                          {"key":"@i18n@node_name","value":"审批"},
                          {"key":"@i18n@time_interval","value":"日程名称"},
                          {"key":"@i18n@event_type","value":"日程类型"},
                          {"key":"@i18n@recurrence_event","value":"重复性日程"},
                          {"key":"@i18n@single_event","value":"单次日程"},
                          {"key":"@i18n@attende_count","value":"参与人数量"},
                          {"key":"@i18n@apply_reason","value":"申请原因"}
                        ],
                      "is_default": true
                    }],
                "process_manager_ids": ["1c5ea995"]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CreateApprovalRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.ApprovalCode);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4Approval.CreateApprovalAsync(CreateApprovalRequest, string, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateApprovalAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "approval_code": "81D31358-93AF-92D6-7425-01A5D67C4E71",
                    "approval_id": "7090754740375519252"
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CreateApprovalResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.ApprovalCode!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4Approval.GetApprovalByCodeAsync(string, string, string?, bool?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetApprovalByCodeAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "approval_name": "Payment",
                    "status": "ACTIVE",
                    "form": "[{\"id\": \"widget1\", \"custom_id\": \"user_name\",\"name\": \"Item application\",\"type\": \"textarea\",\"printable\": true,\"required\": true}\"]",
                    "node_list": [
                        {
                            "name": "Approval",
                            "need_approver": true,
                            "node_id": "46e6d96cfa756980907209209ec03b64",
                            "custom_node_id": "46e6d96cfa756980907209209ec03b64",
                            "node_type": "AND",
                            "approver_chosen_multi": true,
                            "approver_chosen_range": [
                                {
                                    "approver_range_type": 2,
                                    "approver_range_ids": [
                                        "ou_e03053f0541cecc3269d7a9dc34a0b21"
                                    ]
                                }
                            ],
                            "require_signature": false
                        }
                    ],
                    "viewers": [
                        {
                            "type": "TENANT",
                            "id": "ou_e03053f0541cecc3269d7a9dc34a0b21",
                            "user_id": "f7cb567e"
                        }
                    ],
                    "approval_admin_ids": [
                        "ou_3cda9c969f737aaa05e6915dce306cb9"
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetApprovalResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.ApprovalName);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4Approval.CreateInstanceAsync(CreateInstanceRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateInstanceAsync_RequestBody()
    {
        string bodyStr = """
                        {
                "approval_code":"4202AD96-9EC1-4284-9C48-B923CDC4F30B",
                "user_id":"59a92c4a",
                "open_id":"ou_806a18fb5bdf525e38ba219733bdbd73",
                "form":"[{\"id\":\"111\",\"type\":\"input\",\"value\":\"11111\"},{\"id\":\"222\",\"required\":true,\"type\":\"dateInterval\",\"value\":{\"start\":\"2019-10-01T08:12:01+08:00\",\"end\":\"2019-10-02T08:12:01+08:00\",\"interval\": 2.0}},{\"id\":\"333\",\"type\":\"radioV2\",\"value\":\"1\"},{\"id\":\"444\",\"type\":\"number\", \"value\":\"4\"},{\"id\":\"555\",\"type\":\"textarea\",\"value\":\"fsafs\"}]",
                "node_approver_user_id_list":[
                    {"key": "46e6d96cfa756980907209209ec03b64","value":["59a92c4a"]},
                    {"key": "manager_node_id","value":["59a92c4a"]}
                ],
                "node_approver_open_id_list":[
                    {"key": "46e6d96cfa756980907209209ec03b64","value":["ou_806a18fb5bdf525e38ba219733bdbd73"]},
                    {"key": "manager_node_id","value":["ou_806a18fb5bdf525e38ba219733bdbd73"]}
                ],
                "node_cc_user_id_list":[
                    {"key": "46e6d96cfa756980907209209ec03b64","value":["59a92c4a"]},
                    {"key": "manager_node_id","value":["59a92c4a"]}
                ],
                "node_cc_open_id_list":[
                    {"key": "46e6d96cfa756980907209209ec03b64","value":["ou_806a18fb5bdf525e38ba219733bdbd73"]},
                    {"key": "manager_node_id","value":["ou_806a18fb5bdf525e38ba219733bdbd73"]}
                ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CreateInstanceRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.ApprovalCode);
        Assert.NotEmpty(requestBody.UserId!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4Approval.CreateInstanceAsync(CreateInstanceRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateInstanceAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "instance_code": "81D31358-93AF-92D6-7425-01A5D67C4E71"
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CreateInstancesResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.InstanceCode!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4Approval.CancelInstanceAsync(CancelInstancesRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CancelInstanceAsync_RequestBody()
    {
        string bodyStr = """
                        {
                "approval_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
                "instance_code": "81D31358-93AF-92D6-7425-01A5D67C4E71",
                "user_id": "f7cb567e"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CancelInstancesRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.ApprovalCode);
        Assert.NotEmpty(requestBody.UserId!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4Approval.CancelInstanceAsync(CancelInstancesRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CancelInstanceAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {}
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4Approval.CarbonCopyInstanceAsync(CarbonCopyInstanceRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CarbonCopyInstanceAsync_RequestBody()
    {
        string bodyStr = """
                        {
                "approval_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
                "instance_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
                "user_id": "f7cb567e",
                "cc_user_ids": [
                    "f7cb567e"
                ],
                "comment": "ok"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CarbonCopyInstanceRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.ApprovalCode!);
        Assert.NotEmpty(requestBody.UserId!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4Approval.CarbonCopyInstanceAsync(CarbonCopyInstanceRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CarbonCopyInstanceAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {}
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4Approval.PreviewInstanceAsync(PreviewInstanceRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_PreviewInstanceAsync_RequestBody()
    {
        string bodyStr = """
                        {
                "approval_code":"C2CAAA90-70D9-3214-906B-B6FFF947F00D",
                "user_id":"f7cb567e",
                "department_id":"",
                "form":"[{\"id\":\"widget16256287451710001\", \"type\": \"number\", \"value\":\"43\"}]"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<PreviewInstanceRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.ApprovalCode!);
        Assert.NotEmpty(requestBody.UserId!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4Approval.PreviewInstanceAsync(PreviewInstanceRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_PreviewInstanceAfterAsync_RequestBody()
    {
        string bodyStr = """
                       {
                "instance_code":"12345CA6-97AC-32BB-8231-47C33FFFCCFD",
                "user_id":"f7cb567e",
                "task_id": "6982332863116876308"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<PreviewInstanceAfterRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.InstanceCode!);
        Assert.NotEmpty(requestBody.UserId!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4Approval.PreviewInstanceAsync(PreviewInstanceRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_PreviewInstanceAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "preview_nodes": [
                        {
                            "user_id_list": [
                                "ffffffff"
                            ],
                            "end_cc_id_list": [],
                            "node_id": "b078ffd28db767c502ac367053f6e0ac",
                            "node_name": "发起",
                            "node_type": "",
                            "comments": [],
                            "custom_node_id": ""
                        },
                        {
                            "user_id_list": [
                                "ffffffff"
                            ],
                            "end_cc_id_list": [],
                            "node_id": "e6ce10282a3cc3bf4a408feffd678dcf",
                            "node_name": "审批",
                            "node_type": "AND",
                            "comments": [],
                            "custom_node_id": "",
                            "is_empty_logic": false,
                            "is_approver_type_free": false,
                            "has_cc_type_free": false
                        },
                        {
                            "user_id_list": [],
                            "end_cc_id_list": [],
                            "node_id": "b1a326c06d88bf042f73d70f50197905",
                            "node_name": "结束",
                            "node_type": "",
                            "comments": [],
                            "custom_node_id": ""
                        }
                    ]
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<PreviewNodeResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4Approval.GetInstanceByIdAsync(string, string?, string?, bool?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetInstanceByIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "approval_name": "Payment",
                    "start_time": "1564590532967",
                    "end_time": "1564590532967",
                    "user_id": "f3ta757q",
                    "open_id": "ou_3cda9c969f737aaa05e6915dce306cb9",
                    "serial_number": "202102060002",
                    "department_id": "od-8ec33ffec336c3a39a278bc25e931676",
                    "status": "PENDING",
                    "uuid": "1234567",
                    "form": "[{\"id\": \"widget1\",\"custom_id\": \"user_info\",\"name\": \"Item application\",\"type\": \"textarea\"},\"value\":\"aaaa\"]",
                    "task_list": [
                        {
                            "id": "1234",
                            "user_id": "f7cb567e",
                            "open_id": "ou_123457",
                            "status": "PENDING",
                            "node_id": "46e6d96cfa756980907209209ec03b64",
                            "node_name": "开始",
                            "custom_node_id": "manager",
                            "type": "AND",
                            "start_time": "1564590532967",
                            "end_time": "0"
                        }
                    ],
                    "comment_list": [
                        {
                            "id": "1234",
                            "user_id": "f7cb567e",
                            "open_id": "ou_123456",
                            "comment": "ok",
                            "create_time": "评论时间",
                            "files": [
                                {
                                    "url": "https://p3-approval-sign.byteimg.com/lark-approval-attachment/image/20220714/1/332f3596-0845-4746-a4bc-818d54ad435b.png~tplv-ottatrvjsm-image.image?x-expires=1659033558&x-signature=6edF3k%2BaHeAuvfcBRGOkbckoUl4%3D#.png",
                                    "file_size": 186823,
                                    "title": "e018906140ed9388234bd03b0.png",
                                    "type": "image"
                                }
                            ]
                        }
                    ],
                    "timeline": [
                        {
                            "type": "PASS",
                            "create_time": "1564590532967",
                            "user_id": "f7cb567e",
                            "open_id": "ou_123456",
                            "user_id_list": [
                                "eea5gefe"
                            ],
                            "open_id_list": [
                                "ou_123456"
                            ],
                            "task_id": "1234",
                            "comment": "ok",
                            "cc_user_list": [
                                {
                                    "user_id": "eea5gefe",
                                    "cc_id": "123445",
                                    "open_id": "ou_12345"
                                }
                            ],
                            "ext": "{\"user_id\":\"62d4a44c\",\"open_id\":\"ou_123456\"}",
                            "node_key": "APPROVAL_240330_4058663",
                            "files": [
                                {
                                    "url": "https://p3-approval-sign.byteimg.com/lark-approval-attachment/image/20220714/1/332f3596-0845-4746-a4bc-818d54ad435b.png~tplv-ottatrvjsm-image.image?x-expires=1659033558&x-signature=6edF3k%2BaHeAuvfcBRGOkbckoUl4%3D#.png",
                                    "file_size": 186823,
                                    "title": "e018906140ed9388234bd03b0.png",
                                    "type": "image"
                                }
                            ]
                        }
                    ],
                    "modified_instance_code": "81D31358-93AF-92D6-7425-01A5D67C4E71",
                    "reverted_instance_code": "81D31358-93AF-92D6-7425-01A5D67C4E71",
                    "approval_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
                    "reverted": false,
                    "instance_code": "81D31358-93AF-92D6-7425-01A5D67C4E71"
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetApprovalInstanceResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.InstanceCode);
    }
}
