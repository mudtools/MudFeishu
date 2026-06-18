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
/// 用于测试消息相关接口（租户访问令牌）
/// </summary>
public class IFeishuTenantV1MessageTests
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();

    #region 发送消息
    [Fact]
    public void TestSendMessageAsyncRequestBody()
    {
        string bodyStr = """{"receive_id":"ou_test123","msg_type":"text","content":"{\"text\":\"hello\"}","uuid":"test-uuid-123"}""";
        var requestBody = JsonSerializer.Deserialize<SendMessageRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.Equal("ou_test123", requestBody.ReceiveId);
        Assert.Equal("text", requestBody.MsgType);
        Assert.Equal("test-uuid-123", requestBody.Uuid);
    }

    [Fact]
    public void TestSendMessageAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"message_id":"om_test123"}}""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<MessageDataResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
        Assert.Equal("om_test123", result.Data.MessageId);
    }
    #endregion

    #region 回复消息
    [Fact]
    public void TestReplyMessageAsyncRequestBody()
    {
        string bodyStr = """{"msg_type":"text","content":"{\"text\":\"reply content\"}","uuid":"reply-uuid-123"}""";
        var requestBody = JsonSerializer.Deserialize<ReplyMessageRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.Equal("text", requestBody.MsgType);
    }

    [Fact]
    public void TestReplyMessageAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"message_id":"om_reply123"}}""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<MessageDataResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
        Assert.Equal("om_reply123", result.Data.MessageId);
    }
    #endregion

    #region 编辑消息
    [Fact]
    public void TestEditMessageAsyncRequestBody()
    {
        string bodyStr = """{"msg_type":"text","content":"{\"text\":\"edited content\"}"}""";
        var requestBody = JsonSerializer.Deserialize<EditMessageRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.Equal("text", requestBody.MsgType);
    }

    [Fact]
    public void TestEditMessageAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"message_id":"om_edit123"}}""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<MessageDataResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
        Assert.Equal("om_edit123", result.Data.MessageId);
    }
    #endregion

    #region 转发消息
    [Fact]
    public void TestReceiveMessageAsyncRequestBody()
    {
        string bodyStr = """{"receive_id":"ou_forward123","receive_id_type":"open_id"}""";
        var requestBody = JsonSerializer.Deserialize<ReceiveMessageRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.Equal("ou_forward123", requestBody.ReceiveId);
    }

    [Fact]
    public void TestReceiveMessageAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"message_id":"om_forward123"}}""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<ReceiveMessageResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 合并转发消息
    [Fact]
    public void TestMergeReceiveMessageAsyncRequestBody()
    {
        string bodyStr = """{"receive_id":"ou_merge123","receive_id_type":"open_id","message_id_list":["om_1","om_2"]}""";
        var requestBody = JsonSerializer.Deserialize<MergeReceiveMessageRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.Equal("ou_merge123", requestBody.ReceiveId);
    }

    [Fact]
    public void TestMergeReceiveMessageAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"message_id":"om_merge123"}}""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<MergeReceiveMessageResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 转发话题
    [Fact]
    public void TestReceiveThreadsAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"thread_id":"thread_123"}}""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<ThreadResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 创建消息跟随气泡
    [Fact]
    public void TestCreateMessageFollowUpAsyncRequestBody()
    {
        string bodyStr = """{"follow_ups":[{"content":"{\"text\":\"follow up\"}"}]}""";
        var requestBody = JsonSerializer.Deserialize<MessageFollowUpRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestCreateMessageFollowUpAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":null}""";
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
    }
    #endregion

    #region 查询消息已读用户
    [Fact]
    public void TestGetMessageReadUsesAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"items":[{"user_id":"ou_123"}],"has_more":false,"page_token":""}}""";
        var result = JsonSerializer.Deserialize<FeishuApiPageListResult<ReadMessageUser>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
    }
    #endregion

    #region 获取历史消息
    [Fact]
    public void TestGetHistoryMessageAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"items":[],"has_more":false,"page_token":""}}""";
        var result = JsonSerializer.Deserialize<FeishuApiPageListResult<HistoryMessageData>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
    }
    #endregion

    #region 获取消息文件（小文件）
    [Fact]
    public void TestGetMessageFileResult()
    {
    }
    #endregion

    #region 获取消息文件（大文件）
    [Fact]
    public void TestGetMessageLargeFileResult()
    {
    }
    #endregion

    #region 通过消息ID获取内容
    [Fact]
    public void TestGetContentListByMessageIdAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"items":[]}}""";
        var result = JsonSerializer.Deserialize<FeishuApiListResult<MessageContentData>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
    }
    #endregion

    #region 下载文件（小文件）
    [Fact]
    public void TestDownFileAsyncResult()
    {
    }
    #endregion

    #region 下载文件（大文件）
    [Fact]
    public void TestDownLargeFileAsyncResult()
    {
    }
    #endregion

    #region 下载图片（小文件）
    [Fact]
    public void TestDownImageAsyncResult()
    {
    }
    #endregion

    #region 下载图片（大文件）
    [Fact]
    public void TestDownLargeImageAsyncResult()
    {
    }
    #endregion

    #region 上传文件
    [Fact]
    public void TestUploadFileAsyncRequestBody()
    {
        string bodyStr = """{"file_key":"file_123","file_name":"test.txt","file_type":"stream"}""";
        var requestBody = JsonSerializer.Deserialize<UploadMessageFileRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestUploadFileAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"file_key":"file_123"}}""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<FileUploadResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
        Assert.Equal("file_123", result.Data.FileKey);
    }
    #endregion

    #region 上传图片
    [Fact]
    public void TestUploadImageAsyncRequestBody()
    {
        string bodyStr = """{"image_type":"message","image_key":"img_123"}""";
        var requestBody = JsonSerializer.Deserialize<UploadImageRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestUploadImageAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"image_key":"img_123"}}""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<ImageUpdateResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
        Assert.Equal("img_123", result.Data.ImageKey);
    }
    #endregion

    #region 消息加急（应用内）
    [Fact]
    public void TestMessageUrgentAppAsyncRequestBody()
    {
        string bodyStr = """{"user_id_list":["ou_123","ou_456"],"urgent_type":"app"}""";
        var requestBody = JsonSerializer.Deserialize<MessageUrgentRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestMessageUrgentAppAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"urgent_code":"urgent_123"}}""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<MessageUrgentResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 消息加急（短信）
    [Fact]
    public void TestMessageUrgentSMSAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"urgent_code":"urgent_sms_123"}}""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<MessageUrgentResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 消息加急（电话）
    [Fact]
    public void TestMessageUrgentPhoneAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":{"urgent_code":"urgent_phone_123"}}""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<MessageUrgentResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
    }
    #endregion

    #region 更新URL预览
    [Fact]
    public void TestUpdateUrlPreviewAsyncRequestBody()
    {
        string bodyStr = """{"message_id":"om_123","url":"https://example.com"}""";
        var requestBody = JsonSerializer.Deserialize<UrlPreviewRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    [Fact]
    public void TestUpdateUrlPreviewAsyncResult()
    {
        string resultStr = """{"code":0,"msg":"success","data":null}""";
        var result = JsonSerializer.Deserialize<FeishuNullDataApiResult>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
    }
    #endregion
}
