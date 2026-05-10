// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
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
/// 用于测试批量消息相关接口
/// </summary>
public class IFeishuTenantV1BatchMessageTests
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();

    #region 批量发送文本消息
    [Fact]
    public void TestBatchSendTextMessageAsyncRequestBody()
    {
        string bodyStr = """{"msg_type":"text","content":{"text":"hello"},"receive_id_list":["ou_123"]}""";
        var requestBody = JsonSerializer.Deserialize<BatchSenderTextMessageRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.MsgType);
    }

    [Fact]
    public void TestBatchSendTextMessageAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"message_id":"om_batch_123","invalid_receiver_ids":[]}}""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<BatchMessageResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.MessageId!);
    }
    #endregion

    #region 批量发送富文本消息
    [Fact]
    public void TestBatchSendRichTextMessageAsyncRequestBody()
    {
        string bodyStr = """{"msg_type":"post","content":{"post":{"zh_cn":{"title":"test"}}},"receive_id_list":["ou_123"]}""";
        var requestBody = JsonSerializer.Deserialize<BatchSenderRichTextMessageRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.MsgType);
    }

    [Fact]
    public void TestBatchSendRichTextMessageAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"message_id":"om_batch_456","invalid_receiver_ids":[]}}""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<BatchMessageResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.MessageId!);
    }
    #endregion

    #region 批量发送图片消息
    [Fact]
    public void TestBatchSendImageMessageAsyncRequestBody()
    {
        string bodyStr = """{"msg_type":"image","content":{"image_key":"img_123"},"receive_id_list":["ou_123"]}""";
        var requestBody = JsonSerializer.Deserialize<BatchSenderMessageImageRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.MsgType);
    }

    [Fact]
    public void TestBatchSendImageMessageAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"message_id":"om_batch_789","invalid_receiver_ids":[]}}""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<BatchMessageResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.MessageId!);
    }
    #endregion

    #region 批量发送群分享消息
    [Fact]
    public void TestBatchSendGroupShareMessageAsyncRequestBody()
    {
        string bodyStr = """{"msg_type":"share_chat","content":{"share_chat_id":"oc_123"},"receive_id_list":["ou_123"]}""";
        var requestBody = JsonSerializer.Deserialize<BatchSenderMessageGroupShareRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.MsgType);
    }

    [Fact]
    public void TestBatchSendGroupShareMessageAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"message_id":"om_batch_abc","invalid_receiver_ids":[]}}""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<BatchMessageResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.MessageId!);
    }
    #endregion

    #region 撤回批量消息
    [Fact]
    public void TestRevokeMessageAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":null}""";
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
    }
    #endregion

    #region 查询批量消息已读状态
    [Fact]
    public void TestGetUserReadMessageInfosAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"read_user_count":10,"read_user_list":[]}}""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<BatchMessageReadStatusResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 查询批量消息进度
    [Fact]
    public void TestGetBatchMessageProgressAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"status":"success","total_count":100,"sent_count":100}}""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<BatchMessageProgressResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }
    #endregion
}
