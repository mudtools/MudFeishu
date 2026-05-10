// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.ApprovalTask;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuTenantV4ApprovalTask"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV4ApprovalTaskTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV4ApprovalTaskTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.AgreeApprovalAsync(AgreeApprovalTasksRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_AgreeApprovalAsync_RequestBody()
    {
        string bodyStr = """
{
  "approval_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
  "instance_code": "81D31358-93AF-92D6-7425-01A5D67C4E71",
  "user_id": "f7cb567e",
  "comment": "OK",
  "task_id": "12345",
  "form": "[{\"id\":\"111\", \"type\": \"input\", \"value\":\"test\"}]"
}
""";
        var requestBody = JsonSerializer.Deserialize<AgreeApprovalTasksRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.ApprovalCode);
        Assert.NotNull(requestBody.UserId);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.AgreeApprovalAsync(AgreeApprovalTasksRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_AgreeApprovalAsync_Result()
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
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.RejectApprovalAsync(RejectApprovalTaskRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_RejectApprovalAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "approval_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
              "instance_code": "81D31358-93AF-92D6-7425-01A5D67C4E71",
              "user_id": "f7cb567e",
              "comment": "OK",
              "task_id": "12345",
              "form": "[{\"id\":\"user_name\", \"type\": \"input\", \"value\":\"test\"}]"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<RejectApprovalTaskRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.InstanceCode);
        Assert.NotNull(requestBody.UserId);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.RejectApprovalAsync(RejectApprovalTaskRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_RejectApprovalAsync_Result()
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
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.TransferApprovalAsync(TransferApprovalTasksRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_TransferApprovalAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "approval_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
              "instance_code": "81D31358-93AF-92D6-7425-01A5D67C4E71",
              "user_id": "f7cb567e",
              "comment": "OK",
              "transfer_user_id": "f4ip317q",
              "task_id": "12345"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<TransferApprovalTasksRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.InstanceCode);
        Assert.NotNull(requestBody.UserId);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.TransferApprovalAsync(TransferApprovalTasksRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_TransferApprovalAsync_Result()
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
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.RollbackApprovalAsync(RollbackApprovalInstancesRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_RollbackApprovalAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "user_id": "893g4c45",
              "task_id": "7026591166355210260",
              "reason": "申请事项填写不具体，请重新填写",
              "extra": "demo",
              "task_def_key_list": [
                "START",
                "APPROVAL_27997_285502",
                "APPROVAL_462205_2734554"
              ]
            }
            """;
        var requestBody = JsonSerializer.Deserialize<RollbackApprovalInstancesRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.UserId);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.RollbackApprovalAsync(RollbackApprovalInstancesRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_RollbackApprovalAsync_Result()
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
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.InstancesAddSignAsync(InstancesAddSignRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_InstancesAddSignAsync_RequestBody()
    {
        string bodyStr = """
                       {
                "approval_code": "3B68E280-CF10-4198-B4CD-2E3BB97981D8",
                "instance_code": "289330DE-FBF1-4A47-91F9-9EFCCF11BCAE",
                "user_id": "b16g66e3",
                "task_id": "6955096766400167956",
                "comment": "addSignComment",
                "add_sign_user_ids": ["d19b913b","3313g62b"],
                "add_sign_type": 1,
                "approval_method": 1
            }
            """;
        var requestBody = JsonSerializer.Deserialize<InstancesAddSignRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.InstanceCode);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.InstancesAddSignAsync(InstancesAddSignRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_InstancesAddSignAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success"
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.ResubmitApprovalAsync(ResubmitApprovalRequest, string, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_ResubmitApprovalAsync_RequestBody()
    {
        string bodyStr = """
                        {
                "approval_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
                "instance_code": "81D31358-93AF-92D6-7425-01A5D67C4E71",
                "user_id": "f7cb567e",
                "comment": "{\"text\":\"评论\",\"file_codes\":[\"ABCD1232s\",\"ABC12334d22\"]}",
                "task_id": "12345",
                "form": "[{\"id\":\"user_name\", \"type\": \"input\", \"value\":\"test\"}]"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<ResubmitApprovalRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.InstanceCode);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV4ApprovalTask.ResubmitApprovalAsync(ResubmitApprovalRequest, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_ResubmitApprovalAsync_Result()
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
}
