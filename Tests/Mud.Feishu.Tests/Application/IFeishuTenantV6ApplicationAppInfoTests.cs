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
/// 用于测试<see cref="IFeishuTenantV6ApplicationAppInfo"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV6ApplicationAppInfoTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV6ApplicationAppInfoTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV6ApplicationAppInfo.GetApplicationAsync(string, string, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetApplicationAsync_Result()
    {
        string resultStr = """
                        {
                            "code": 0,
                            "msg": "success",
                            "data": {
                                "app": {
                                    "app_id": "cli_9b445f5258795107",
                                    "creator_id": "ou_d317f090b7258ad0372aa53963cda70d",
                                    "status": 1,
                                    "scene_type": 1,
                                    "payment_type": 0,
                                    "create_source": "base",
                                    "redirect_urls": [
                                        "https://www.example.com"
                                    ],
                                    "online_version_id": "oav_d317f090b7258ad0372aa53963cda70d",
                                    "unaudit_version_id": "oav_d317f090b7258ad0372aa53963cda70d",
                                    "app_name": "应用名称",
                                    "avatar_url": "https://sf1-ttcdn-tos.pstatp.com/img/avatar/d279000ca4d3f7f6aaff~72x72.jpg",
                                    "description": "应用描述",
                                    "scopes": [
                                        {
                                            "scope": "contact:user.base",
                                            "description": "获取应用信息",
                                            "level": 1,
                                            "token_types": [
                                                "user"
                                            ]
                                        }
                                    ],
                                    "back_home_url": "https://www.example.com",
                                    "i18n": [
                                        {
                                            "i18n_key": "zh_cn",
                                            "name": "应用名称",
                                            "description": "应用描述",
                                            "help_use": "使用场景"
                                        }
                                    ],
                                    "primary_language": "zh_cn",
                                    "common_categories": ["效率"],
                                    "owner": {
                                        "type": 1,
                                        "owner_id": "ou_d317f090b7258ad0372aa53963cda70d",
                                        "name": "张三"
                                    },
                                    "mobile_default_ability": "bot",
                                    "pc_default_ability": "bot",
                                    "allow_refresh_token": true,
                                    "callback_info": {
                                        "callback_type": "websocket",
                                        "subscribed_callbacks": ["card.action.trigger"]
                                    }
                                }
                            }
                        }
                        """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetApplicationResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.App);
        var app = result.Data.App;
        Assert.Equal("cli_9b445f5258795107", app.AppId);
        Assert.Equal(1, app.Status);
        Assert.Equal(1, app.SceneType);
        Assert.Equal(0, app.PaymentType);
        Assert.Equal("base", app.CreateSource);
        Assert.NotNull(app.RedirectUrls);
        Assert.Single(app.RedirectUrls);
        Assert.Equal("oav_d317f090b7258ad0372aa53963cda70d", app.OnlineVersionId);
        Assert.NotNull(app.Scopes);
        Assert.Single(app.Scopes);
        Assert.Equal("contact:user.base", app.Scopes[0].Scope);
        Assert.Equal(1, app.Scopes[0].Level);
        Assert.NotNull(app.Scopes[0].TokenTypes);
        Assert.Contains("user", app.Scopes[0].TokenTypes!);
        Assert.NotNull(app.I18n);
        Assert.Single(app.I18n);
        Assert.Equal("zh_cn", app.I18n[0].I18nKey);
        Assert.Equal("应用名称", app.I18n[0].Name);
        Assert.Equal("zh_cn", app.PrimaryLanguage);
        Assert.NotNull(app.Owner);
        Assert.Equal(1, app.Owner.Type);
        Assert.Equal("ou_d317f090b7258ad0372aa53963cda70d", app.Owner.OwnerId);
        Assert.Equal("bot", app.MobileDefaultAbility);
        Assert.True(app.AllowRefreshToken);
        Assert.NotNull(app.CallbackInfo);
        Assert.Equal("websocket", app.CallbackInfo.CallbackType);
    }
}
