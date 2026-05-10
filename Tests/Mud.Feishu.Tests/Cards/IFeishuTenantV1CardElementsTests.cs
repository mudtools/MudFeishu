// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.CardElements;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuTenantV1CardElements"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV1CardElementsTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV1CardElementsTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1CardElements.CreateCardElementAsync(string, CreateCardElementRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateCardElementAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "type": "insert_after",
              "target_element_id": "markdown_1",
              "uuid": "a0d69e20-1dd1-458b-k525-dfeca4015204",
              "sequence": 1,
              "elements": "[{\"tag\":\"button\",\"element_id\":\"button_1\",\"text\":{\"tag\":\"plain_text\",\"content\":\"查看更多\"},\"type\":\"default\",\"width\":\"default\",\"size\":\"medium\",\"behaviors\":[{\"type\":\"open_url\",\"default_url\":\"https://open.feishu.cn/?lang=zh-CN\",\"pc_url\":\"\",\"ios_url\":\"\",\"android_url\":\"\"}]}]"
            }
            """;
        var requestBody = JsonSerializer.Deserialize<CreateCardElementRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.Elements);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1CardElements.CreateCardElementAsync(string, CreateCardElementRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CreateCardElementAsync_Result()
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
    /// 用于测试<see cref="IFeishuTenantV1CardElements.UpdateCardElementByIdAsync(string, string, UpdateCardElementRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateCardElementByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "uuid": "a0d69e20-1dd1-458b-k525-dfeca4015204",
              "element": "{\"tag\":\"markdown\",\"element_id\":\"md_1\",\"content\":\"这是一段更新后的文本\"}",
              "sequence": 1
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UpdateCardElementRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.Element);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1CardElements.UpdateCardElementByIdAsync(string, string, UpdateCardElementRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateCardElementByIdAsync_Result()
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
    /// 用于测试<see cref="IFeishuTenantV1CardElements.UpdateCardElementAttributeByIdAsync(string, string, UpdateCardElementAttributeRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateCardElementAttributeByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "partial_element": "{\"content\":\"更新后的组件文本\"}",
              "uuid": "a0d69e20-1dd1-458b-k525-dfeca4015204",
              "sequence": 1
            }
            """;
        var requestBody = JsonSerializer.Deserialize<UpdateCardElementAttributeRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1CardElements.UpdateCardElementAttributeByIdAsync(string, string, UpdateCardElementAttributeRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateCardElementAttributeByIdAsync_Result()
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
    /// 用于测试<see cref="IFeishuTenantV1CardElements.StreamUpdateCardTextByIdAsync(string, string, StreamUpdateTextRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_StreamUpdateCardTextByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "uuid": "a0d69e20-1dd1-458b-k525-dfeca4015204",
              "content": "这是更新后的文本内容。将以打字机式的效果输出",
              "sequence": 1
            }
            """;
        var requestBody = JsonSerializer.Deserialize<StreamUpdateTextRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1CardElements.StreamUpdateCardTextByIdAsync(string, string, StreamUpdateTextRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_StreamUpdateCardTextByIdAsync_Result()
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
    /// 用于测试<see cref="IFeishuTenantV1CardElements.DeleteCardElementByIdAsync(string, string, DeleteCardElementRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteCardElementByIdAsync_RequestBody()
    {
        string bodyStr = """
                        {
              "uuid": "a0d69e20-1dd1-458b-k525-dfeca4015204",
              "sequence": 1
            }
            """;
        var requestBody = JsonSerializer.Deserialize<DeleteCardElementRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1CardElements.DeleteCardElementByIdAsync(string, string, DeleteCardElementRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_DeleteCardElementByIdAsync_Result()
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
