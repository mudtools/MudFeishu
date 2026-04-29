// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.Messages;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试消息相关接口（基础接口）
/// </summary>
public class IFeishuV1MessageTests
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();

    #region 撤回消息
    [Fact]
    public void TestRevokeMessageAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":null}""";
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
    }
    #endregion

    #region 添加表情回复
    [Fact]
    public void TestAddMessageReactionsAsyncRequestBody()
    {
        string bodyStr = """{"reaction_type":{"emoji_type":"LAUGH"},"message_id":"om_123"}""";
        var requestBody = JsonSerializer.Deserialize<EmojiReactionRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestAddMessageReactionsAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"reaction_id":"reaction_123"}}""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<EmojiReactionResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 获取表情回复列表
    [Fact]
    public void TestGetMessageReactionsPageListAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"items":[]}}""";
        var result = JsonSerializer.Deserialize<FeishuApiListResult<EmojiReactionResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
    }
    #endregion

    #region 删除表情回复
    [Fact]
    public void TestDeleteMessageReactionsAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"reaction_id":"reaction_123"}}""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<EmojiReactionResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }
    #endregion

    #region Pin消息
    [Fact]
    public void TestPinMessageAsyncRequestBody()
    {
        string bodyStr = """{"message_id":"om_123"}""";
        var requestBody = JsonSerializer.Deserialize<MessageRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestPinMessageAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"pin_id":"pin_123"}}""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<PinDataResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 移除Pin消息
    [Fact]
    public void TestDeletePinMessageAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":null}""";
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
    }
    #endregion

    #region 获取Pin消息列表
    [Fact]
    public void TestGetPinMessagePageListAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"items":[],"has_more":false,"page_token":""}}""";
        var result = JsonSerializer.Deserialize<FeishuApiPageListResult<PinInfo>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
    }
    #endregion
}
