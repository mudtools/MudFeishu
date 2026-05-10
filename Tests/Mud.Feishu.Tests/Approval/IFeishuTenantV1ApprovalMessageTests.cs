// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.ApprovalMessage;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuTenantV4ApprovalExternal"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV1ApprovalMessageTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV1ApprovalMessageTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1ApprovalMessage.SendBotMessageAsync(ApprovalBotMessageRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_SendBotMessageAsync_RequestBody()
    {
        string bodyStr = """
{
    "template_id":"1001",
    "user_id":"b85s39b",
    "uuid":"uuid",
    "approval_name":"@i18n@1",
    "title_user_id":"od-8ec33278bc2",
    "title_user_id_type": "user_id",
    "comment":"@i18n@2",
    "content":{
        "user_id":"b85s39b",
        "user_id_type": "user_id",
        "department_id":"od-8ec33278bc2",
        "summaries":[
            {
                "summary":"@i18n@3"
            }
        ]
    },
    "note":"@i18n@4",
    "actions":[
        {
            "action_name":"DETAIL",
            "url":" https://bytedance.com",
            "android_url":"https://bytedance.com",
            "ios_url":"https://bytedance.com",
            "pc_url":"https://bytedance.com"
        }
    ],
    "action_configs": [
        {
            "action_type": "APPROVE",
            "is_need_reason": true,
            "is_reason_required": true,
            "is_need_attachment": true,
            "next_status": "APPROVED"
        },
        {
            "action_type": "REJECT",
            "action_name": "@i18n@5",
            "next_status": "REJECTED"
        }
    ],
    "action_callback": {
        "action_callback_url":"http://feish.cn/approval/openapi/operate",
        "action_callback_token":"sdjkljkx9lsadf110",
        "action_callback_key":"gfdqedvsadfgfsd",
        "action_context":"acasdasd"
    },
    "i18n_resources":[
        {
            "locale":"en-US",
            "is_default":true,
            "texts":{
                "@i18n@1":"Temporary release",
                "@i18n@2":"Disapproval",
                "@i18n@3":"Need to modify",
                "@i18n@4":"From OA,please access through the internal network.",
                "@i18n@5":"Cancel"
            }
        }
    ]
}
""";
        var requestBody = JsonSerializer.Deserialize<ApprovalBotMessageRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);

        // 验证必需字段非空
        Assert.NotNull(requestBody.TemplateId);
        Assert.NotNull(requestBody.UserId);
        Assert.NotNull(requestBody.Uuid);
        Assert.NotNull(requestBody.ApprovalName);
        Assert.NotNull(requestBody.TitleUserId);
        Assert.NotNull(requestBody.TitleUserIdType);
        Assert.NotNull(requestBody.Comment);
        Assert.NotNull(requestBody.Note);
    }

    /// <summary>
    /// 用户测试<see cref="IFeishuTenantV1ApprovalMessage.SendBotMessageAsync(ApprovalBotMessageRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_SendBotMessageAsync_Result()
    {
        string resultStr = """
{
    "code":0,
    "msg":"success",
    "data":{
        "message_id": "6968359519504171036"
    }
}
""";
        var result = JsonSerializer.Deserialize<FeishuApiResult<ApprovalBotMessageResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.MessageId!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1ApprovalMessage.SendBotMessageAsync(CustomApprovalBotMessageRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_SendBotMessageAsyncCustomTemplate_RequestBody()
    {
        string bodyStr = @"
{
    ""template_id"":""1021"",
    ""user_id"":""employeeId1"",
    ""uuid"":""uuid"",
    ""custom_title"":""@i18n@1"",
    ""custom_content"":""@i18n@2"",
    ""note"":""@i18n@3"",
    ""actions"":[
        {
            ""action_name"":""@i18n@4"",
            ""url"":"" https://bytedance.com"",
            ""android_url"":""https://bytedance.com"",
            ""ios_url"":""https://bytedance.com"",
            ""pc_url"":""https://bytedance.com""
        }
    ],
    ""i18n_resources"":[
        {
            ""locale"":""en-US"",
            ""is_default"":true,
            ""texts"":{
                ""@i18n@1"":""Custom template"",
                ""@i18n@2"":""Please help process my approval as soon as possible."",
                ""@i18n@3"":""From OA,please access through the internal network."",
                ""@i18n@4"":""DETAIL""
            }
        }
    ]
}
";
        var requestBody = JsonSerializer.Deserialize<CustomApprovalBotMessageRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);

        // 验证必需字段非空
        Assert.NotNull(requestBody.TemplateId);
        Assert.NotNull(requestBody.CustomTitle);
        Assert.NotNull(requestBody.CustomContent);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1ApprovalMessage.SendBotMessageAsync(CustomApprovalBotMessageRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_SendBotMessageAsyncCustomTemplateAsync_Result()
    {
        string resultStr = @"
{
    ""code"":0,
    ""msg"":""success"",
    ""data"":{
        ""message_id"": ""6968359519504171036""
    }
}
";
        var result = JsonSerializer.Deserialize<FeishuApiResult<ApprovalBotMessageResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.MessageId!);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1ApprovalMessage.UpdateBotMessageAsync(ApprovalBotMessageUpdateRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateBotMessageAsync_RequestBody()
    {
        string bodyStr = @"
{
    ""message_id"":""xxxx"",
    ""status"":""CUSTOM"",
    ""status_name"":""@i18n@1"",
    ""detail_action_name"":""@i18n@2"",
    ""i18n_resources"":[
        {
          ""locale"": ""zh-CN"",
          ""texts"" : {
              ""@i18n@1"": ""已废弃"",
              ""@i18n@2"": ""已废弃按钮"" 
            },
          ""is_default"": true
        }
    ]
}
";
        var requestBody = JsonSerializer.Deserialize<ApprovalBotMessageUpdateRequest>(bodyStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(requestBody);

        // 验证必需字段非空
        Assert.NotNull(requestBody.MessageId);
        Assert.NotNull(requestBody.Status);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV1ApprovalMessage.UpdateBotMessageAsync(ApprovalBotMessageUpdateRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateBotMessageAsync_Result()
    {
        string resultStr = @"
{
    ""code"":0,
    ""msg"":""success"",
    ""data"":{
        ""message_id"": ""xxxx""
    }
}
";
        var result = JsonSerializer.Deserialize<FeishuApiResult<ApprovalBotMessageResult>>(resultStr, _jsonSerializerOptions);

        // 验证顶层对象非空
        Assert.NotNull(result);

        // 验证必需字段非空
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.MessageId!);
    }
}