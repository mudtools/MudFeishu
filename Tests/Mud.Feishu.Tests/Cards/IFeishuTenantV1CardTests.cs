// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.Cards;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuTenantV1Card"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV1CardTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV1CardTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1Card.CreateCardAsync(CreateCardRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateCardAsync_RequestBody()
    {
        string bodyStr = """
                        {  
              "type": "card_json",
              "data": "{\"schema\":\"2.0\",\"header\":{\"title\":{\"content\":\"项目进度更新提醒\",\"tag\":\"plain_text\"}},\"config\":{\"streaming_mode\":true,\"summary\":{\"content\":\"\"},\"streaming_config\":{\"print_frequency_ms\":{\"default\":70,\"android\":70,\"ios\":70,\"pc\":70},\"print_step\":{\"default\":1,\"android\":1,\"ios\":1,\"pc\":1},\"print_strategy\":\"fast\"}},\"body\":{\"elements\":[{\"tag\":\"markdown\",\"content\":\"截至今日，项目完成度已达80%\",\"element_id\":\"markdown_1\"}]}}"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CreateCardRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.Data);
        Assert.NotEmpty(requestBody.Type!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1Card.CreateCardAsync(CreateCardRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateCardAsync_Result()
    {
        string resultStr = """
                        {
                "code": 0,
                "msg": "success",
                "data": {
                    "card_id": "7355372766134157313"
                }
            }
            """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CreateCardResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.CardId!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1Card.UpdateCardSettingsByIdAsync(string, UpdateCardSettingsRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateCardSettingsByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "settings": "{\"config\":{\"streaming_mode\":true,\"streaming_config\":{\"print_frequency_ms\":{\"default\":70,\"android\":70,\"ios\":70,\"pc\":70},\"print_step\":{\"default\":1,\"android\":1,\"ios\":1,\"pc\":1},\"print_strategy\":\"fast\"}}}",
              "uuid": "a0d69e20-1dd1-458b-k525-dfeca4015204",
              "sequence": 1
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UpdateCardSettingsRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Settings!);
        Assert.NotEmpty(requestBody.Uuid!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1Card.UpdateCardSettingsByIdAsync(string, UpdateCardSettingsRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateCardSettingsByIdAsync_Result()
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
    /// 用于测试<see cref="IFeishuTenantV1Card.PartialUpdateCardByIdAsync(string, PartialUpdateCardRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_PartialUpdateCardByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "uuid": "a0d69e20-1dd1-458b-k525-dfeca4015204",
              "sequence": 1,
              "actions": "[{\"action\":\"partial_update_setting\",\"params\":{\"settings\":{\"config\":{\"streaming_mode\":true}}}},{\"action\":\"add_elements\",\"params\":{\"type\":\"insert_before\",\"target_element_id\":\"markdown_1\",\"elements\":[{\"tag\":\"markdown\",\"element_id\":\"md_1\",\"content\":\"欢迎使用[飞书卡片搭建工具](https://open.feishu.cn/cardkit?from=open_docs)。\"}]}},{\"action\":\"delete_elements\",\"params\":{\"element_ids\":[\"text_1\",\"text_2\"]}},{\"action\":\"partial_update_element\",\"params\":{\"element_id\":\"markdown_2\",\"partial_element\":{\"content\":\"详情参考飞书卡片相关文档。\"}}},{\"action\":\"update_element\",\"params\":{\"element_id\":\"markdown_3\",\"element\":{\"tag\":\"button\",\"text\":{\"tag\":\"plain_text\",\"content\":\"有帮助\"},\"size\":\"medium\",\"icon\":{\"tag\":\"standard_icon\",\"token\":\"emoji_outlined\"}}}}]"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<PartialUpdateCardRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotEmpty(requestBody.Actions!);
        Assert.NotEmpty(requestBody.Uuid!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1Card.PartialUpdateCardByIdAsync(string, PartialUpdateCardRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_PartialUpdateCardByIdAsync_Result()
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
    /// 用于测试<see cref="IFeishuTenantV1Card.UpdateCardByIdAsync(string, UpdateCardRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateCardByIdAsync_RequestBody()
    {
        string bodyStr = """
                      {
              "card": {
                "type": "card_json",
                "data": "{\"schema\":\"2.0\",\"header\":{\"title\":{\"content\":\"项目进度更新提醒\",\"tag\":\"plain_text\"}},\"body\":{\"elements\":[{\"tag\":\"markdown\",\"content\":\"截至今日，项目完成度已达80%\"}]}}"
              },
              "uuid": "a0d69e20-1dd1-458b-k525-dfeca4015204",
              "sequence": 1
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UpdateCardRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.Card);
        Assert.NotEmpty(requestBody.Uuid);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1Card.UpdateCardByIdAsync(string, UpdateCardRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateCardByIdAsync_Result()
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
