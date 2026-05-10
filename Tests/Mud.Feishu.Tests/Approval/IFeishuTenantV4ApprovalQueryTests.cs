// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.ApprovalQuery;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuV4ApprovalQuery"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV4ApprovalQueryTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV4ApprovalQueryTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalQuery.GetInstancesPageListAsync(ApprovalInstancesQueryRequest, int, string?, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_GetInstancesPageListAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "user_id": "lwiu098wj",
              "approval_code": "EB828003-9FFE-4B3F-AA50-2E199E2ED942",
              "instance_code": "EB828003-9FFE-4B3F-AA50-2E199E2ED943",
              "instance_external_id": "EB828003-9FFE-4B3F-AA50-2E199E2ED976",
              "group_external_id": "1234567",
              "instance_title": "test",
              "instance_status": "PENDING",
              "instance_start_time_from": "1547654251506",
              "instance_start_time_to": "1547654251506",
              "locale": "zh-CN"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<ApprovalInstancesQueryRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalQuery.GetInstancesPageListAsync(ApprovalInstancesQueryRequest, int, string?, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetInstancesPageListAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "count": 10,
                    "instance_list": [
                        {
                            "approval": {
                                "code": "EB828003-9FFE-4B3F-AA50-2E199E2ED943",
                                "name": "approval",
                                "is_external": true,
                                "external": {
                                    "batch_cc_read": false
                                },
                                "approval_id": "7090754740375519252",
                                "icon": "https://lf3-ea.bytetos.com/obj/goofy/ee/approval/approval-admin/image/iconLib/v3/person.png"
                            },
                            "group": {
                                "external_id": "0004",
                                "name": "groupA"
                            },
                            "instance": {
                                "code": "EB828003-9FFE-4B3F-AA50-2E199E2ED943",
                                "external_id": "0004_3ED52DC1-AA6C",
                                "user_id": "lwiu098wj",
                                "start_time": "1547654251506",
                                "end_time": "1547654251506",
                                "status": "pending",
                                "title": "test",
                                "extra": "{}",
                                "serial_id": "201902020001",
                                "link": {
                                    "pc_link": "https://www.example.com/",
                                    "mobile_link": "https://www.example.com/"
                                }
                            }
                        }
                    ],
                    "page_token": "nF1ZXJ5VGhlbkZldGNoCgAAAAAA6PZwFmUzSldvTC1yU",
                    "has_more": false
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<ApprovalInstancesQueryResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Count);
        Assert.NotNull(result.Data.InstanceLists);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalQuery.GetCarbonCopyPageListAsync(ApprovalInstancesCcQueryRequest, int, string?, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_GetCarbonCopyPageListAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "user_id": "lwiu098wj",
              "approval_code": "EB828003-9FFE-4B3F-AA50-2E199E2ED942",
              "instance_code": "EB828003-9FFE-4B3F-AA50-2E199E2ED943",
              "instance_external_id": "EB828003-9FFE-4B3F-AA50-2E199E2ED976",
              "group_external_id": "1234567",
              "cc_title": "test",
              "read_status": "read",
              "cc_create_time_from": "1547654251506",
              "cc_create_time_to": "1547654251506",
              "locale": "zh-CN"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<ApprovalInstancesCcQueryRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.InstanceCode!);
        Assert.NotEmpty(requestBody.ApprovalCode!);
        Assert.NotEmpty(requestBody.UserId!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalQuery.GetCarbonCopyPageListAsync(ApprovalInstancesCcQueryRequest, int, string?, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetCarbonCopyPageListAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "count": 10,
                    "cc_list": [
                        {
                            "approval": {
                                "code": "EB828003-9FFE-4B3F-AA50-2E199E2ED943",
                                "name": "approval",
                                "is_external": true,
                                "external": {
                                    "batch_cc_read": false
                                },
                                "approval_id": "7090754740375519252",
                                "icon": "https://lf3-ea.bytetos.com/obj/goofy/ee/approval/approval-admin/image/iconLib/v3/person.png"
                            },
                            "group": {
                                "external_id": "0004",
                                "name": "groupA"
                            },
                            "instance": {
                                "code": "EB828003-9FFE-4B3F-AA50-2E199E2ED943",
                                "external_id": "0004_3ED52DC1-AA6C",
                                "user_id": "lwiu098wj",
                                "start_time": "1547654251506",
                                "end_time": "1547654251506",
                                "status": "pending",
                                "title": "test",
                                "extra": "{}",
                                "serial_id": "201902020001",
                                "link": {
                                    "pc_link": "https://www.baidu.com/",
                                    "mobile_link": "https://www.baidu.com/"
                                }
                            },
                            "cc": {
                                "user_id": "lwiu098wj",
                                "create_time": "1547654251506",
                                "read_status": "read",
                                "title": "test",
                                "extra": "{}",
                                "link": {
                                    "pc_link": "https://www.baidu.com/",
                                    "mobile_link": "https://www.baidu.com/"
                                }
                            }
                        }
                    ],
                    "page_token": "nF1ZXJ5VGhlbkZldGNoCgAAAAAA6PZwFmUzSldvTC1yU",
                    "has_more": false
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<ApprovalInstancesCcQueryResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.CcLists);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalQuery.GetTasksPageListAsync(ApprovalInstancesTaskQueryRequest, int, string?, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_GetTasksPageListAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "user_id": "lwiu098wj",
              "approval_code": "EB828003-9FFE-4B3F-AA50-2E199E2ED942",
              "instance_code": "EB828003-9FFE-4B3F-AA50-2E199E2ED943",
              "instance_external_id": "EB828003-9FFE-4B3F-AA50-2E199E2ED976",
              "group_external_id": "1234567",
              "task_title": "test",
              "task_status": "PENDING",
              "task_start_time_from": "1547654251506",
              "task_start_time_to": "1547654251506",
              "locale": "zh-CN",
              "task_status_list": [
                "PENDING"
              ],
              "order": 2
            }
            """;
        var requestBody = JsonSerializer.Deserialize<ApprovalInstancesTaskQueryRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.InstanceCode!);
        Assert.NotEmpty(requestBody.ApprovalCode!);
        Assert.NotEmpty(requestBody.UserId!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalQuery.GetTasksPageListAsync(ApprovalInstancesTaskQueryRequest, int, string?, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetTasksPageListAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "count": 10,
                    "task_list": [
                        {
                            "approval": {
                                "code": "EB828003-9FFE-4B3F-AA50-2E199E2ED943",
                                "name": "approval",
                                "is_external": true,
                                "external": {
                                    "batch_cc_read": false
                                }
                            },
                            "group": {
                                "external_id": "0004",
                                "name": "groupA"
                            },
                            "instance": {
                                "code": "EB828003-9FFE-4B3F-AA50-2E199E2ED943",
                                "external_id": "0004_3ED52DC1-AA6C",
                                "user_id": "lwiu098wj",
                                "start_time": "1547654251506",
                                "end_time": "1547654251506",
                                "status": "pending",
                                "title": "test",
                                "extra": "{}",
                                "serial_id": "201902020001",
                                "link": {
                                    "pc_link": "https://www.baidu.com/",
                                    "mobile_link": "https://www.baidu.com/"
                                }
                            },
                            "task": {
                                "user_id": "lwiu098wj",
                                "start_time": "1547654251506",
                                "end_time": "1547654251506",
                                "status": "pending",
                                "title": "test",
                                "extra": "{}",
                                "link": {
                                    "pc_link": "https://www.baidu.com/",
                                    "mobile_link": "https://www.baidu.com/"
                                },
                                "task_id": "7110153401253494803",
                                "update_time": "1547654251506"
                            }
                        }
                    ],
                    "page_token": "nF1ZXJ5VGhlbkZldGNoCgAAAAAA6PZwFmUzSldvTC1yU",
                    "has_more": false
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<ApprovalInstancesTaskQueryResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.TaskLists);
    }
}
