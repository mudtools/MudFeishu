// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.Application;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuTenantV6ApplicationAppStore"/>、<see cref="IFeishuTenantV6ApplicationAppUsage"/>、<see cref="IFeishuTenantV6ApplicationFeedback"/>与<see cref="IFeishuTenantV6ApplicationAppBadge"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV6ApplicationAppStoreTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV6ApplicationAppStoreTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV6ApplicationAppStore.CheckUserPaidScopeAsync(string?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CheckUserPaidScopeAsync_Result()
    {
        string resultStr = """
                        {
                            "code": 0,
                            "msg": "success",
                            "data": {
                                "status": "valid",
                                "price_plan_id": "price_9daf66c96968c003",
                                "is_trial": false,
                                "service_stop_time": "1572280518"
                            }
                        }
                        """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CheckUserPaidScopeResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
        Assert.Equal("valid", result.Data.Status);
        Assert.Equal("price_9daf66c96968c003", result.Data.PricePlanId);
        Assert.False(result.Data.IsTrial);
        Assert.Equal("1572280518", result.Data.ServiceStopTime);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV6ApplicationAppStore.GetPaidOrdersPageAsync(int, string?, string?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetPaidOrdersPageAsync_Result()
    {
        string resultStr = """
                        {
                            "code": 0,
                            "msg": "success",
                            "data": {
                                "total": 100,
                                "has_more": true,
                                "page_token": "10",
                                "order_list": [
                                    {
                                        "order_id": "6708978506916697671",
                                        "price_plan_id": "price_9daf66c96968c003",
                                        "price_plan_type": "per_seat_per_year",
                                        "seats": 2,
                                        "buy_count": 1,
                                        "create_time": "1565003215",
                                        "pay_time": "1565003216",
                                        "status": "normal",
                                        "buy_type": "buy",
                                        "src_order_id": "6718900620264210445",
                                        "dst_order_id": "6717597079260102659",
                                        "order_pay_price": 100,
                                        "tenant_key": "2e5c3a3ae38f175f"
                                    }
                                ]
                            }
                        }
                        """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetPaidOrdersResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
        Assert.Equal(100, result.Data.Total);
        Assert.True(result.Data.HasMore);
        Assert.Equal("10", result.Data.PageToken);
        Assert.NotNull(result.Data.OrderList);
        Assert.Single(result.Data.OrderList);
        var order = result.Data.OrderList[0];
        Assert.Equal("6708978506916697671", order.OrderId);
        Assert.Equal("per_seat_per_year", order.PricePlanType);
        Assert.Equal(2, order.Seats);
        Assert.Equal("normal", order.Status);
        Assert.Equal("buy", order.BuyType);
        Assert.Equal(100, order.OrderPayPrice);
        Assert.Equal("2e5c3a3ae38f175f", order.TenantKey);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV6ApplicationAppStore.GetOrderAsync(string, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetOrderAsync_Result()
    {
        string resultStr = """
                        {
                            "code": 0,
                            "msg": "success",
                            "data": {
                                "order": {
                                    "order_id": "6708978506916697671",
                                    "price_plan_id": "price_9daf66c96968c003",
                                    "price_plan_type": "per_seat_per_year",
                                    "status": "normal"
                                }
                            }
                        }
                        """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetOrderResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Order);
        Assert.Equal("6708978506916697671", result.Data.Order.OrderId);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV6ApplicationAppUsage.GetAppUsageOverviewAsync(string, AppUsageOverviewRequest, string?, CancellationToken)"/>等函数的请求体序列化与返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_AppUsage_RequestBody_And_Result()
    {
        string bodyStr = """
                        {
                            "date": "2022-08-01",
                            "cycle_type": 1,
                            "department_id": "od-a140b4eeb892b90a0ab3e616fc2054d6",
                            "ability": "app"
                        }
                        """;
        var requestBody = JsonSerializer.Deserialize<AppUsageOverviewRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.Equal("2022-08-01", requestBody.Date);
        Assert.Equal(1, requestBody.CycleType);
        Assert.Equal("app", requestBody.Ability);

        var serialized = JsonSerializer.Serialize(requestBody, _jsonSerializerOptions);
        using var document = JsonDocument.Parse(serialized);
        Assert.Equal("2022-08-01", document.RootElement.GetProperty("date").GetString());
        Assert.Equal(1, document.RootElement.GetProperty("cycle_type").GetInt32());

        string resultStr = """
                        {
                            "code": 0,
                            "msg": "success",
                            "data": {
                                "items": [
                                    {
                                        "metric_name": "app_active_user_cnt",
                                        "metric_value": 100
                                    }
                                ]
                            }
                        }
                        """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetAppUsageOverviewResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Items);
        Assert.Single(result.Data.Items);
        Assert.Equal("app_active_user_cnt", result.Data.Items[0].MetricName);
        Assert.Equal(100, result.Data.Items[0].MetricValue);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV6ApplicationAppUsage.GetDepartmentUsageOverviewAsync(string, DepartmentUsageOverviewRequest, string?, CancellationToken)"/>函数的请求体序列化与返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DepartmentUsageOverview_RequestBody_And_Result()
    {
        string bodyStr = """
                        {
                            "date": "2022-08-01",
                            "cycle_type": 1,
                            "recursion": 1,
                            "page_size": 10
                        }
                        """;
        var requestBody = JsonSerializer.Deserialize<DepartmentUsageOverviewRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.Equal(1, requestBody.Recursion);
        Assert.Equal(10, requestBody.PageSize);

        string resultStr = """
                        {
                            "code": 0,
                            "msg": "success",
                            "data": {
                                "has_more": false,
                                "page_token": "1",
                                "items": [
                                    {
                                        "department_id": "od-a140b4eeb892b90a0ab3e616fc2054d6",
                                        "app": [
                                            {
                                                "metric_name": "app_active_user_cnt",
                                                "metric_value": 100
                                            }
                                        ],
                                        "bot": [
                                            {
                                                "metric_name": "bot_msg_push_cnt",
                                                "metric_value": 200
                                            }
                                        ]
                                    }
                                ]
                            }
                        }
                        """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetDepartmentUsageOverviewResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.False(result.Data.HasMore);
        Assert.NotNull(result.Data.Items);
        Assert.Single(result.Data.Items);
        Assert.Equal("od-a140b4eeb892b90a0ab3e616fc2054d6", result.Data.Items[0].DepartmentId);
        Assert.NotNull(result.Data.Items[0].App);
        Assert.Single(result.Data.Items[0].App);
        Assert.NotNull(result.Data.Items[0].Bot);
        Assert.Equal(200, result.Data.Items[0].Bot![0].MetricValue);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV6ApplicationFeedback.GetFeedbacksPageAsync(string, string?, string?, int?, int?, int?, string?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetFeedbacksPageAsync_Result()
    {
        string resultStr = """
                        {
                            "code": 0,
                            "msg": "success",
                            "data": {
                                "feedback_list": [
                                    {
                                        "feedback_id": "7057888018203574291",
                                        "app_id": "cli_9b445f5258795107",
                                        "feedback_time": "2022-01-30 11:30:12",
                                        "tenant_name": "bytedance",
                                        "feedback_type": 1,
                                        "status": 0,
                                        "fault_type": [1, 2, 3],
                                        "fault_time": "2022-01-30 11:30:12",
                                        "source": 1,
                                        "contact": "wang@bytedance.com",
                                        "update_time": "2022-01-30 11:30:12",
                                        "description": "反馈描述",
                                        "user_id": "ou_9565b69967831233761cc2f11b4c089f",
                                        "images": ["https://www.example.com/1.png"]
                                    }
                                ],
                                "has_more": false
                            }
                        }
                        """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetFeedbacksResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.FeedbackList);
        Assert.Single(result.Data.FeedbackList);
        var feedback = result.Data.FeedbackList[0];
        Assert.Equal("7057888018203574291", feedback.FeedbackId);
        Assert.Equal("bytedance", feedback.TenantName);
        Assert.Equal(1, feedback.FeedbackType);
        Assert.Equal(0, feedback.Status);
        Assert.NotNull(feedback.FaultType);
        Assert.Equal(3, feedback.FaultType!.Count);
        Assert.Equal("反馈描述", feedback.Description);
        Assert.NotNull(feedback.Images);
        Assert.Single(feedback.Images);
        Assert.False(result.Data.HasMore);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV6ApplicationAppBadge.SetAppBadgeAsync(SetAppBadgeRequest, string?, CancellationToken)"/>函数的请求体序列化。
    /// </summary>
    [Fact]
    public void Test_SetAppBadgeAsync_RequestBody()
    {
        string bodyStr = """
                        {
                            "user_id": "ou_d317f090b7258ad0372aa53963cda70d",
                            "version": "1664360599355",
                            "extra": "{}",
                            "pc": {
                                "web_app": 1,
                                "gadget": 0
                            },
                            "mobile": {
                                "gadget": 3
                            }
                        }
                        """;
        var requestBody = JsonSerializer.Deserialize<SetAppBadgeRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.Equal("ou_d317f090b7258ad0372aa53963cda70d", requestBody.UserId);
        Assert.Equal("1664360599355", requestBody.Version);
        Assert.NotNull(requestBody.Pc);
        Assert.Equal(1, requestBody.Pc.WebApp);
        Assert.Equal(0, requestBody.Pc.Gadget);
        Assert.NotNull(requestBody.Mobile);
        Assert.Equal(3, requestBody.Mobile.Gadget);
        Assert.Null(requestBody.Mobile.WebApp);

        var serialized = JsonSerializer.Serialize(requestBody, _jsonSerializerOptions);
        using var document = JsonDocument.Parse(serialized);
        Assert.Equal(3, document.RootElement.GetProperty("mobile").GetProperty("gadget").GetInt32());
    }
}
