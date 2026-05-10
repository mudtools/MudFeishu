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
public class IFeishuV4ApprovalQueryTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuV4ApprovalQueryTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuV4ApprovalQuery.GetTasksPageListByUserIdAsync(string, string, int, string?, string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetTasksPageListByUserIdAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "tasks": [
                        {
                            "topic": "1",
                            "user_id": "example_user_id",
                            "title": "任务题目示例",
                            "urls": {
                                "helpdesk": "https://blabla",
                                "mobile": "https://blabla",
                                "pc": "https://blabla"
                            },
                            "process_external_id": "example_instance_id",
                            "task_external_id": "example_task_id",
                            "status": "Todo",
                            "process_status": "Running",
                            "definition_code": "000000-00000000000000-0example",
                            "initiators": [
                                "starter"
                            ],
                            "initiator_names": [
                                "发起人姓名"
                            ],
                            "task_id": "1212564555454",
                            "process_id": "1214564545474",
                            "process_code": "123e4567-e89b-12d3-a456-426655440000",
                            "definition_group_id": "1212564555454",
                            "definition_group_name": "流程定义名称",
                            "definition_id": "1212564555454",
                            "definition_name": "流程定义组名称"
                        }
                    ],
                    "page_token": "example_page_token",
                    "has_more": true,
                    "count": {
                        "total": 123,
                        "has_more": false
                    }
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<ApprovalInstancesTaskUserQueryResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Tasks);
    }
}
