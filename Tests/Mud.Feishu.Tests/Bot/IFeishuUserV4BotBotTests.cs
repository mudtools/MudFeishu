// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.Bot;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuUserV4BotBot"/>接口的相关函数。
/// </summary>
public class IFeishuUserV4BotBotTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuUserV4BotBotTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuUserV4BotBot.SearchBotAsync(Mud.Feishu.DataModels.Bot.SearchBotRequest, int?, string?, string?, System.Threading.CancellationToken)"/>函数的请求体序列化。
    /// </summary>
    [Fact]
    public void Test_SearchBotAsync_RequestBody()
    {
        string bodyStr = """
                        {
                            "query": "会议助手",
                            "filter": {
                                "chat_ids": [
                                    "oc-7890123456abcdef"
                                ],
                                "has_chatter": false
                            }
                        }
                        """;
        var requestBody = JsonSerializer.Deserialize<SearchBotRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.Equal("会议助手", requestBody.Query);
        Assert.NotNull(requestBody.Filter);
        Assert.NotNull(requestBody.Filter.ChatIds);
        Assert.Single(requestBody.Filter.ChatIds);
        Assert.Equal("oc-7890123456abcdef", requestBody.Filter.ChatIds[0]);
        Assert.False(requestBody.Filter.HasChatter);

        var serialized = JsonSerializer.Serialize(requestBody, _jsonSerializerOptions);
        using var document = JsonDocument.Parse(serialized);
        var root = document.RootElement;
        Assert.Equal("会议助手", root.GetProperty("query").GetString());
        Assert.Equal("oc-7890123456abcdef", root.GetProperty("filter").GetProperty("chat_ids")[0].GetString());
        Assert.False(root.GetProperty("filter").GetProperty("has_chatter").GetBoolean());
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuUserV4BotBot.SearchBotAsync(Mud.Feishu.DataModels.Bot.SearchBotRequest, int?, string?, string?, System.Threading.CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_SearchBotAsync_Result()
    {
        string resultStr = """
                        {
                            "code": 0,
                            "msg": "success",
                            "data": {
                                "items": [
                                    {
                                        "id": "7890123456abcdef",
                                        "display_info": "飞书<h>搜索</h>助手",
                                        "meta_data": {
                                            "tenant_id": "7010970696222244883",
                                            "enable_join_group": false,
                                            "chat_id": "oc-7890123456abcdef",
                                            "is_agent": false
                                        }
                                    }
                                ],
                                "has_more": true,
                                "page_token": "eVQrYzJBNDNONlk4VFZBZVlSdzlKdFJ4bVVHVExENDNKVHoxaVdiVnViQT0=",
                                "notice": "The query is too long and has been truncated to the first 50 characters for search."
                            }
                        }
                        """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<SearchBotResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Items);
        Assert.Single(result.Data.Items);
        Assert.Equal("7890123456abcdef", result.Data.Items[0].Id);
        Assert.Equal("飞书<h>搜索</h>助手", result.Data.Items[0].DisplayInfo);
        Assert.NotNull(result.Data.Items[0].MetaData);
        Assert.Equal("7010970696222244883", result.Data.Items[0].MetaData.TenantId);
        Assert.False(result.Data.Items[0].MetaData.EnableJoinGroup);
        Assert.Equal("oc-7890123456abcdef", result.Data.Items[0].MetaData.ChatId);
        Assert.False(result.Data.Items[0].MetaData.IsAgent);
        Assert.True(result.Data.HasMore);
        Assert.Equal("eVQrYzJBNDNONlk4VFZBZVlSdzlKdFJ4bVVHVExENDNKVHoxaVdiVnViQT0=", result.Data.PageToken);
        Assert.Contains("truncated", result.Data.Notice);
    }
}
