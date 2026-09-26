// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.Approval;
using Mud.Feishu.DataModels.ApprovalDistrict;
using Mud.Feishu.DataModels.ApprovalTask;
using Mud.Feishu.DataModels.AttendanceUser;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 与 Go 版 SDK（oapi-sdk-go）对齐审查后新增/修复字段的回归测试（字段以飞书官方文档为准）。
/// </summary>
public class ApprovalAttendanceParityTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public ApprovalAttendanceParityTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    [Fact]
    public void QueryUserSetting_ShouldDeserializeUserSettingsList()
    {
        const string json = """
            {
              "code": 0,
              "msg": "success",
              "data": {
                "user_settings": [
                  { "user_id": "abd754f7", "face_key": "xxxxxb306842b1c189bc5212eefxxxxx", "face_key_update_time": "1625681917" },
                  { "user_id": "abd754f8", "face_key": "yyyyyb306842b1c189bc5212eefyyyyy" }
                ]
              }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<UserSettingsQueryResult>>(json, _jsonSerializerOptions);

        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.UserSettings.Should().HaveCount(2);
        result.Data!.UserSettings[0].UserId.Should().Be("abd754f7");
        result.Data!.UserSettings[0].FaceKey.Should().Be("xxxxxb306842b1c189bc5212eefxxxxx");
        result.Data!.UserSettings[0].FaceKeyUpdateTime.Should().Be("1625681917");
    }

    [Fact]
    public void CreateInstanceRequest_ShouldSerializeI18nResources_AsDictionaryTexts()
    {
        var request = new CreateInstanceRequest
        {
            ApprovalCode = "7C468A54-8745-2245-9675-08B7C63E7A85",
            Form = "[]",
            I18nResources =
            [
                new I18nDictResource
                {
                    Locale = "zh-CN",
                    IsDefault = true,
                    Texts = new Dictionary<string, string> { ["@i18n@1"] = "Permission" }
                }
            ]
        };

        var json = JsonSerializer.Serialize(request, _jsonSerializerOptions);

        // 官方文档示例值为键值对象（i18n_resources.texts 以 Key:Value 赋值）
        json.Should().Contain("\"texts\":{\"@i18n@1\":\"Permission\"}");
    }

    [Fact]
    public void GetApprovalResult_ShouldDeserializeFormWidgetRelation()
    {
        const string json = """
            {
              "code": 0,
              "msg": "success",
              "data": {
                "approval_name": "Payment",
                "status": "ACTIVE",
                "form": "[]",
                "form_widget_relation": "EQ(widget1,widget2)",
                "node_list": [],
                "viewers": []
              }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetApprovalResult>>(json, _jsonSerializerOptions);

        result.Should().NotBeNull();
        result!.Data!.FormWidgetRelation.Should().Be("EQ(widget1,widget2)");
    }

    [Fact]
    public void ListDistrictResult_ShouldDeserializeDistricts()
    {
        const string json = """
            {
              "code": 0,
              "msg": "success",
              "data": {
                "version": "20230308",
                "has_more": false,
                "page_token": "",
                "items": [
                  {
                    "id": "115618457",
                    "name": "Minster",
                    "level": "district",
                    "has_sub_district": false,
                    "parent_districts": [ { "id": "115618456", "name": "Berlin", "level": "city" } ]
                  }
                ]
              }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<ListDistrictResult>>(json, _jsonSerializerOptions);

        result.Should().NotBeNull();
        result!.Data!.Version.Should().Be("20230308");
        result.Data!.HasMore.Should().BeFalse();
        result.Data!.Items.Should().HaveCount(1);
        var district = result.Data!.Items[0];
        district.Id.Should().Be("115618457");
        district.Level.Should().Be("district");
        district.ParentDistricts.Should().HaveCount(1);
        district.ParentDistricts![0].Name.Should().Be("Berlin");
    }

    [Fact]
    public void SearchDistrictRequest_ShouldSerializeOfficialShape()
    {
        var request = new SearchDistrictRequest { DistrictIds = ["156182582"], Keyword = "Hangzhou" };
        var json = JsonSerializer.Serialize(request, _jsonSerializerOptions);

        json.Should().Contain("\"district_ids\":[\"156182582\"]");
        json.Should().Contain("\"keyword\":\"Hangzhou\"");
    }

    [Fact]
    public void InstanceDetailResult_ShouldDeserializeUatShape()
    {
        const string json = """
            {
              "code": 0,
              "msg": "success",
              "data": {
                "definition_name": "Leave",
                "start_time": "1564590532967",
                "end_time": "0",
                "user_id": "f3ta757q",
                "serial_number": "202102060002",
                "status": "PENDING",
                "instance_code": "6A123516-FB88-470D-A428-9AF58B71B3C0",
                "tasks": [
                  { "id": "6992353208872969234", "user_id": "f7cb567e", "status": "PENDING", "node_id": "n1", "node_name": "L1", "type": "AND", "start_time": "1564590532967", "end_time": "0" }
                ],
                "comments": [
                  { "id": "7081516627711606803", "user_id": "f7cb567e", "comment": "ok", "create_time": "1564590532967" }
                ],
                "operation_records": [
                  { "type": "PASS", "create_time": "1564590532967", "user_id": "f7cb567e", "cc_user_ids": ["u1"], "task_id": "6992353208872969234", "comment": "ok", "node_id": "n1" }
                ],
                "current_nodes": [
                  { "node_id": "n1", "node_name": "L1", "type": "AND", "approvers": [ { "task_id": "6992353208872969234", "user_id": "f7cb567e" } ] }
                ]
              }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<InstanceDetailResult>>(json, _jsonSerializerOptions);

        result.Should().NotBeNull();
        var detail = result!.Data!;
        detail.SerialNumber.Should().Be("202102060002");
        detail.Tasks.Should().HaveCount(1);
        detail.Tasks![0].NodeId.Should().Be("n1");
        detail.Comments.Should().HaveCount(1);
        detail.OperationRecords![0].CcUserIds.Should().Contain("u1");
        detail.CurrentNodes![0].Approvers![0].TaskId.Should().Be("6992353208872969234");
    }

    [Fact]
    public void AddSignTaskRequest_ShouldSerializeOfficialShape()
    {
        var request = new AddSignTaskRequest
        {
            InstanceCode = "6A123516-FB88-470D-A428-9AF58B71B3C0",
            TaskId = "6992353208872969234",
            Comment = "sign",
            AddSignUserIds = ["f7cb567e"],
            AddSignType = 1,
            ApprovalMethod = 2
        };
        var json = JsonSerializer.Serialize(request, _jsonSerializerOptions);

        json.Should().Contain("\"instance_code\"");
        json.Should().Contain("\"add_sign_user_ids\":[\"f7cb567e\"]");
        json.Should().Contain("\"add_sign_type\":1");
        json.Should().Contain("\"approval_method\":2");
    }

    [Fact]
    public void GetApprovalTaskPageListResult_ShouldDeserializeTasks()
    {
        const string json = """
            {
              "code": 0,
              "msg": "success",
              "data": {
                "has_more": false,
                "page_token": "",
                "count": 1,
                "tasks": [
                  {
                    "topic": "1",
                    "user_id": "f7cb567e",
                    "title": "Leave",
                    "status": "PENDING",
                    "instance_status": "PENDING",
                    "definition_code": "7C468A54-8745-2245-9675-08B7C63E7A85",
                    "task_id": "6992353208872969234",
                    "instance_code": "6A123516-FB88-470D-A428-9AF58B71B3C0",
                    "support_api_operate": true,
                    "summaries": [ { "key": "k", "value": "v" } ]
                  }
                ]
              }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetApprovalTaskPageListResult>>(json, _jsonSerializerOptions);

        result.Should().NotBeNull();
        result!.Data!.Count.Should().Be(1);
        var task = result.Data!.Tasks.Single();
        task.Topic.Should().Be("1");
        task.SupportApiOperate.Should().BeTrue();
        task.Summaries![0].Key.Should().Be("k");
    }
}
