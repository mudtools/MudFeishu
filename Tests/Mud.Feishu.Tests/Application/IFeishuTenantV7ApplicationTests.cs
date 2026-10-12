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
/// 用于测试<see cref="IFeishuTenantV7Application"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV7ApplicationTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV7ApplicationTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV7Application.UpdateApplicationBaseAsync(string, UpdateApplicationBaseRequest, CancellationToken)"/>函数的请求体序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateApplicationBaseAsync_RequestBody()
    {
        string bodyStr = """
                        {
                            "i18ns": [
                                {
                                    "i18n_key": "zh_cn",
                                    "name": "应用名称",
                                    "description": "应用描述",
                                    "help_use": "使用场景"
                                }
                            ],
                            "avatar_url": "https://s3-imfile.feishucdn.com/static-resource/v1/v2_953a8fc1-50bd-4b2e-87e2-b09e47dba23g",
                            "homepage_url": "https://open.feishu.cn/"
                        }
                        """;
        var requestBody = JsonSerializer.Deserialize<UpdateApplicationBaseRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.I18ns);
        Assert.Single(requestBody.I18ns);
        Assert.Equal("zh_cn", requestBody.I18ns[0].I18nKey);
        Assert.Equal("应用名称", requestBody.I18ns[0].Name);
        Assert.Equal("https://open.feishu.cn/", requestBody.HomepageUrl);

        var serialized = JsonSerializer.Serialize(requestBody, _jsonSerializerOptions);
        using var document = JsonDocument.Parse(serialized);
        Assert.Equal("应用名称", document.RootElement.GetProperty("i18ns")[0].GetProperty("name").GetString());
        Assert.Equal("https://open.feishu.cn/", document.RootElement.GetProperty("homepage_url").GetString());
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV7Application.UpdateApplicationAbilityAsync(string, UpdateApplicationAbilityRequest, CancellationToken)"/>函数的请求体序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateApplicationAbilityAsync_RequestBody()
    {
        string bodyStr = """
                        {
                            "web_app": {
                                "enable": true,
                                "pc_url": "https://www.example.com",
                                "pc_new_page_open_mode": "new_tab",
                                "mobile_url": "https://www.example.com"
                            },
                            "bot": {
                                "enable": true,
                                "i18ns": [
                                    {
                                        "i18n_key": "zh_cn",
                                        "get_started_desc": "如何使用机器人"
                                    }
                                ],
                                "bot_menu_enable": true,
                                "bot_menus": [
                                    {
                                        "sort": 1,
                                        "default_name": "帮助",
                                        "event_key": "help"
                                    }
                                ]
                            }
                        }
                        """;
        var requestBody = JsonSerializer.Deserialize<UpdateApplicationAbilityRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.WebApp);
        Assert.True(requestBody.WebApp.Enable);
        Assert.Equal("new_tab", requestBody.WebApp.PcNewPageOpenMode);
        Assert.NotNull(requestBody.Bot);
        Assert.True(requestBody.Bot.Enable);
        Assert.NotNull(requestBody.Bot.I18ns);
        Assert.Single(requestBody.Bot.I18ns);
        Assert.Equal("如何使用机器人", requestBody.Bot.I18ns[0].GetStartedDesc);
        Assert.NotNull(requestBody.Bot.BotMenus);
        Assert.Single(requestBody.Bot.BotMenus);
        Assert.Equal("help", requestBody.Bot.BotMenus[0].EventKey);

        var serialized = JsonSerializer.Serialize(requestBody, _jsonSerializerOptions);
        using var document = JsonDocument.Parse(serialized);
        Assert.True(document.RootElement.GetProperty("web_app").GetProperty("enable").GetBoolean());
        Assert.True(document.RootElement.GetProperty("bot").GetProperty("enable").GetBoolean());
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV7Application.UpdateApplicationConfigAsync(string, UpdateApplicationConfigRequest, string?, string?, CancellationToken)"/>函数的请求体序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateApplicationConfigAsync_RequestBody()
    {
        string bodyStr = """
                        {
                            "scope": {
                                "add_scopes": [
                                    {
                                        "scope_name": "contact:user.base:readonly",
                                        "token_type": "tenant"
                                    }
                                ]
                            },
                            "event": {
                                "subscription_type": "websocket",
                                "add_events": ["contact.user.created_v3"]
                            },
                            "security": {
                                "add": {
                                    "redirect_urls": ["https://www.example.com"],
                                    "allowed_ips": ["10.0.0.1"]
                                },
                                "allow_refresh_token": true
                            },
                            "visibility": {
                                "is_visible_to_all": false,
                                "visible_list": {
                                    "user_ids": ["ou_d317f090b7258ad0372aa53963cda70d"],
                                    "department_ids": ["od-ddee42c0f8a948a5e650341e2153243b"]
                                }
                            },
                            "contacts": {
                                "contacts_range_type": "some",
                                "visible_list": {
                                    "department_ids": ["od-ddee42c0f8a948a5e650341e2153243b"]
                                }
                            },
                            "event_and_callback_encrypt_strategy": {
                                "encryption_key": "key",
                                "verification_token": "token"
                            },
                            "callback": {
                                "callback_type": "webhook",
                                "request_url": "https://www.example.com/callback",
                                "add_callbacks": ["url.preview.get"]
                            }
                        }
                        """;
        var requestBody = JsonSerializer.Deserialize<UpdateApplicationConfigRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.Scope);
        Assert.Single(requestBody.Scope.AddScopes!);
        Assert.Equal("contact:user.base:readonly", requestBody.Scope.AddScopes[0].ScopeName);
        Assert.Equal("tenant", requestBody.Scope.AddScopes[0].TokenType);
        Assert.NotNull(requestBody.Event);
        Assert.Equal("websocket", requestBody.Event.SubscriptionType);
        Assert.Contains("contact.user.created_v3", requestBody.Event.AddEvents!);
        Assert.NotNull(requestBody.Security);
        Assert.NotNull(requestBody.Security.Add);
        Assert.Contains("10.0.0.1", requestBody.Security.Add.AllowedIps!);
        Assert.True(requestBody.Security.AllowRefreshToken);
        Assert.NotNull(requestBody.Visibility);
        Assert.False(requestBody.Visibility.IsVisibleToAll);
        Assert.NotNull(requestBody.Visibility.VisibleList);
        Assert.Contains("ou_d317f090b7258ad0372aa53963cda70d", requestBody.Visibility.VisibleList.UserIds!);
        Assert.NotNull(requestBody.Contacts);
        Assert.Equal("some", requestBody.Contacts.ContactsRangeType);
        Assert.NotNull(requestBody.EventAndCallbackEncryptStrategy);
        Assert.Equal("key", requestBody.EventAndCallbackEncryptStrategy.EncryptionKey);
        Assert.NotNull(requestBody.Callback);
        Assert.Equal("webhook", requestBody.Callback.CallbackType);
        Assert.Contains("url.preview.get", requestBody.Callback.AddCallbacks!);

        var serialized = JsonSerializer.Serialize(requestBody, _jsonSerializerOptions);
        using var document = JsonDocument.Parse(serialized);
        Assert.Equal("websocket", document.RootElement.GetProperty("event").GetProperty("subscription_type").GetString());
        Assert.False(document.RootElement.GetProperty("visibility").GetProperty("is_visible_to_all").GetBoolean());
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV7Application.SubmitApplicationPublishAsync(string, SubmitApplicationPublishRequest, CancellationToken)"/>函数的请求体与返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_SubmitApplicationPublishAsync()
    {
        string bodyStr = """
                        {
                            "changelog": "更新了小程序的头像",
                            "mobile_default_ability": "gadget",
                            "pc_default_ability": "gadget",
                            "remark": "更新了移动端默认应用能力",
                            "version": "1.1.1"
                        }
                        """;
        var requestBody = JsonSerializer.Deserialize<SubmitApplicationPublishRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.Equal("更新了小程序的头像", requestBody.Changelog);
        Assert.Equal("gadget", requestBody.MobileDefaultAbility);
        Assert.Equal("gadget", requestBody.PcDefaultAbility);
        Assert.Equal("更新了移动端默认应用能力", requestBody.Remark);
        Assert.Equal("1.1.1", requestBody.Version);

        string resultStr = """
                        {
                            "code": 0,
                            "msg": "success",
                            "data": {
                                "version_id": "oav_d317f090b7258ad0372aa53963cda70d",
                                "version": "1.1.1"
                            }
                        }
                        """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<SubmitApplicationPublishResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
        Assert.Equal("oav_d317f090b7258ad0372aa53963cda70d", result.Data.VersionId);
        Assert.Equal("1.1.1", result.Data.Version);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV7Application.UploadAppAvatarAsync(UploadAppAvatarRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UploadAppAvatarAsync_Result()
    {
        string resultStr = """
                        {
                            "code": 0,
                            "msg": "success",
                            "data": {
                                "url": "https://s3-imfile.feishucdn.com/static-resource/v1/v3_006n_24c0a858-0b0d-490b-a8d4-b1e2ef421c8g"
                            }
                        }
                        """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<UploadAppAvatarResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
        Assert.Contains("static-resource", result.Data.Url);
    }
}
