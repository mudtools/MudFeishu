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
/// 用于测试<see cref="IFeishuTenantV6ApplicationAppVersion"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV6ApplicationAppVersionTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV6ApplicationAppVersionTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV6ApplicationAppVersion.GetAppVersionAsync(string, string, string, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetAppVersionAsync_Result()
    {
        string resultStr = """
                        {
                            "code": 0,
                            "msg": "success",
                            "data": {
                                "app_version": {
                                    "app_id": "cli_9f3ca975326b501b",
                                    "version": "1.0.0",
                                    "version_id": "oav_d317f090b7258ad0372aa53963cda70d",
                                    "app_name": "应用名称",
                                    "avatar_url": "https://www.example.com",
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
                                    "common_categories": ["效率"],
                                    "events": ["contact.user.created_v3"],
                                    "status": 2,
                                    "create_time": "1700000000000",
                                    "publish_time": "1700001000000",
                                    "ability": {
                                        "gadget": {
                                            "enable_pc_mode": 1,
                                            "pc_use_mobile_pkg": false,
                                            "mobile_version": "1.0.0"
                                        },
                                        "web_app": {
                                            "pc_url": "https://www.example.com",
                                            "mobile_url": "https://www.example.com"
                                        },
                                        "bot": {
                                            "card_request_url": "https://www.example.com/callback",
                                            "bot_menu_enable": true,
                                            "bot_menus": [
                                                {
                                                    "menu_id": "menu_1",
                                                    "sort": 1,
                                                    "default_name": "帮助",
                                                    "i18n_name": {
                                                        "zh_cn": "帮助"
                                                    },
                                                    "redirect_link": {
                                                        "pc_url": "https://www.example.com",
                                                        "mobile_url": "https://www.example.com"
                                                    },
                                                    "event_key": "help"
                                                }
                                            ],
                                            "bot_menu_display_strategy": 1
                                        },
                                        "workplace_widgets": [
                                            {
                                                "min_lark_version": "5.0.0"
                                            }
                                        ],
                                        "navigate": {
                                            "pc": {
                                                "version": "1",
                                                "image_url": "https://www.example.com/pc.png",
                                                "hover_image_url": "https://www.example.com/pc_hover.png"
                                            }
                                        }
                                    },
                                    "remark": {
                                        "remark": "发布说明",
                                        "update_remark": "更新说明",
                                        "visibility": {
                                            "is_all": false,
                                            "visible_list": {
                                                "open_ids": ["ou_1ce003a0f23484806bcab85d2a55a000"],
                                                "department_ids": ["od-ddee42c0f8a948a5e650341e2153243b"],
                                                "group_ids": ["b6d1g5dd6fd26186"]
                                            }
                                        }
                                    },
                                    "event_infos": [
                                        {
                                            "event_type": "contact.user.created_v3",
                                            "event_name": "新增用户",
                                            "event_description": "事件描述"
                                        }
                                    ]
                                }
                            }
                        }
                        """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetAppVersionResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.AppVersion);
        var version = result.Data.AppVersion;
        Assert.Equal("cli_9f3ca975326b501b", version.AppId);
        Assert.Equal("1.0.0", version.Version);
        Assert.Equal("oav_d317f090b7258ad0372aa53963cda70d", version.VersionId);
        Assert.Equal(2, version.Status);
        Assert.NotNull(version.Scopes);
        Assert.Single(version.Scopes);
        Assert.NotNull(version.I18n);
        Assert.Single(version.I18n);
        Assert.NotNull(version.Ability);
        Assert.NotNull(version.Ability.Gadget);
        Assert.Equal(1, version.Ability.Gadget.EnablePcMode);
        Assert.NotNull(version.Ability.WebApp);
        Assert.Equal("https://www.example.com", version.Ability.WebApp.PcUrl);
        Assert.NotNull(version.Ability.Bot);
        Assert.True(version.Ability.Bot.BotMenuEnable);
        Assert.NotNull(version.Ability.Bot.BotMenus);
        Assert.Single(version.Ability.Bot.BotMenus);
        Assert.Equal("帮助", version.Ability.Bot.BotMenus[0].DefaultName);
        Assert.NotNull(version.Ability.Bot.BotMenus[0].I18nName);
        Assert.Equal("帮助", version.Ability.Bot.BotMenus[0].I18nName!["zh_cn"]);
        Assert.Equal("help", version.Ability.Bot.BotMenus[0].EventKey);
        Assert.Equal(1, version.Ability.Bot.BotMenuDisplayStrategy);
        Assert.NotNull(version.Ability.WorkplaceWidgets);
        Assert.Single(version.Ability.WorkplaceWidgets);
        Assert.NotNull(version.Ability.Navigate);
        Assert.NotNull(version.Ability.Navigate.Pc);
        Assert.Equal("https://www.example.com/pc.png", version.Ability.Navigate.Pc.ImageUrl);
        Assert.NotNull(version.Remark);
        Assert.Equal("发布说明", version.Remark.Remark);
        Assert.NotNull(version.Remark.Visibility);
        Assert.False(version.Remark.Visibility.IsAll);
        Assert.NotNull(version.Remark.Visibility.VisibleList);
        Assert.Contains("ou_1ce003a0f23484806bcab85d2a55a000", version.Remark.Visibility.VisibleList.OpenIds!);
        Assert.Contains("od-ddee42c0f8a948a5e650341e2153243b", version.Remark.Visibility.VisibleList.DepartmentIds!);
        Assert.Contains("b6d1g5dd6fd26186", version.Remark.Visibility.VisibleList.GroupIds!);
        Assert.NotNull(version.EventInfos);
        Assert.Single(version.EventInfos);
        Assert.Equal("contact.user.created_v3", version.EventInfos[0].EventType);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV6ApplicationAppVersion.GetAppVersionsPageListAsync(string, string, int?, string?, int?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetAppVersionsPageListAsync_Result()
    {
        string resultStr = """
                        {
                            "code": 0,
                            "msg": "success",
                            "data": {
                                "items": [
                                    {
                                        "app_id": "cli_9f3ca975326b501b",
                                        "version": "1.0.0",
                                        "version_id": "oav_d317f090b7258ad0372aa53963cda70d",
                                        "app_name": "应用名称",
                                        "status": 2
                                    }
                                ],
                                "has_more": false,
                                "page_token": "new-e3c5a0627cdf0c2e057da7257b90376a"
                            }
                        }
                        """;
        var result = JsonSerializer.Deserialize<FeishuApiPageListResult<AppVersionInfo>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Items);
        Assert.Single(result.Data.Items);
        Assert.Equal("1.0.0", result.Data.Items[0].Version);
        Assert.False(result.Data.HasMore);
        Assert.Equal("new-e3c5a0627cdf0c2e057da7257b90376a", result.Data.PageToken);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV6ApplicationAppVersion.GetContactsRangeSuggestAsync(string, string, string?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetContactsRangeSuggestAsync_Result()
    {
        string resultStr = """
                        {
                            "code": 0,
                            "msg": "success",
                            "data": {
                                "contacts_range": {
                                    "contacts_scope_type": "some",
                                    "visible_list": {
                                        "open_ids": [
                                            "ou_1ce003a0f23484806bcab85d2a55a000"
                                        ],
                                        "department_ids": [
                                            "od-ddee42c0f8a948a5e650341e2153243b"
                                        ],
                                        "group_ids": [
                                            "b6d1g5dd6fd26186"
                                        ]
                                    }
                                }
                            }
                        }
                        """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetContactsRangeSuggestResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.ContactsRange);
        Assert.Equal("some", result.Data.ContactsRange.ContactsScopeType);
        Assert.NotNull(result.Data.ContactsRange.VisibleList);
        Assert.Single(result.Data.ContactsRange.VisibleList.OpenIds!);
        Assert.Single(result.Data.ContactsRange.VisibleList.DepartmentIds!);
        Assert.Single(result.Data.ContactsRange.VisibleList.GroupIds!);
    }
}
