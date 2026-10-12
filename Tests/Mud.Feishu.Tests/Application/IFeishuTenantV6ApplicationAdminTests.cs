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
/// 用于测试<see cref="IFeishuTenantV6ApplicationAdmin"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV6ApplicationAdminTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV6ApplicationAdminTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV6ApplicationAdmin.GetAppAdminUsersAsync(CancellationToken)"/>等应用管理员函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_AppAdmin_Results()
    {
        string adminUsersResultStr = """
                        {
                            "code": 0,
                            "msg": "ok",
                            "data": {
                                "user_list": [
                                    {
                                        "open_id": "ou_a3b5bea08516cc528cd5546030344a",
                                        "union_id": "on_8ed6aa67826108097d9ee143819da2",
                                        "user_id": "e33ggbsd"
                                    }
                                ]
                            }
                        }
                        """;
        var adminUsersResult = JsonSerializer.Deserialize<FeishuApiResult<GetAppAdminUsersResult>>(adminUsersResultStr, _jsonSerializerOptions);

        Assert.NotNull(adminUsersResult);
        Assert.Equal(0, adminUsersResult.Code);
        Assert.NotNull(adminUsersResult.Data);
        Assert.NotNull(adminUsersResult.Data.UserList);
        Assert.Single(adminUsersResult.Data.UserList);
        Assert.Equal("ou_a3b5bea08516cc528cd5546030344a", adminUsersResult.Data.UserList[0].OpenId);
        Assert.Equal("on_8ed6aa67826108097d9ee143819da2", adminUsersResult.Data.UserList[0].UnionId);
        Assert.Equal("e33ggbsd", adminUsersResult.Data.UserList[0].UserId);

        string adminScopeResultStr = """
                        {
                            "code": 0,
                            "msg": "success",
                            "data": {
                                "is_all": false,
                                "departments_list": [
                                    "od-a140b4eeb892b90a0ab3e616fc2054d6",
                                    "od-a140b4eeb892b90a0ab3e616fc205477"
                                ]
                            }
                        }
                        """;
        var adminScopeResult = JsonSerializer.Deserialize<FeishuApiResult<GetAppAdminScopeResult>>(adminScopeResultStr, _jsonSerializerOptions);

        Assert.NotNull(adminScopeResult);
        Assert.NotNull(adminScopeResult.Data);
        Assert.False(adminScopeResult.Data.IsAll);
        Assert.NotNull(adminScopeResult.Data.DepartmentsList);
        Assert.Equal(2, adminScopeResult.Data.DepartmentsList.Count);
        Assert.Contains("od-a140b4eeb892b90a0ab3e616fc2054d6", adminScopeResult.Data.DepartmentsList);

        string verifyAdminResultStr = """
                        {
                            "code": 0,
                            "msg": "ok",
                            "data": {
                                "is_app_admin": false
                            }
                        }
                        """;
        var verifyAdminResult = JsonSerializer.Deserialize<FeishuApiResult<VerifyAppAdminResult>>(verifyAdminResultStr, _jsonSerializerOptions);

        Assert.NotNull(verifyAdminResult);
        Assert.NotNull(verifyAdminResult.Data);
        Assert.False(verifyAdminResult.Data.IsAppAdmin);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV6ApplicationAdmin.GetApplicationsPageListAsync(string, int?, string?, int?, int?, int?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetApplicationsPageListAsync_Result()
    {
        string resultStr = """
                        {
                            "code": 0,
                            "msg": "success",
                            "data": {
                                "app_list": [
                                    {
                                        "app_id": "cli_9b445f5258795107",
                                        "creator_id": "ou_d317f090b7258ad0372aa53963cda70d",
                                        "status": 1,
                                        "scene_type": 0,
                                        "payment_type": 0,
                                        "create_source": "developer_console",
                                        "app_name": "应用名称",
                                        "description": "应用描述"
                                    }
                                ],
                                "page_token": "AQD9/Rn9eij9Pm39ED40",
                                "has_more": true,
                                "total_count": 34
                            }
                        }
                        """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetApplicationsResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.AppList);
        Assert.Single(result.Data.AppList);
        Assert.Equal("cli_9b445f5258795107", result.Data.AppList[0].AppId);
        Assert.Equal("developer_console", result.Data.AppList[0].CreateSource);
        Assert.True(result.Data.HasMore);
        Assert.Equal(34, result.Data.TotalCount);
        Assert.Equal("AQD9/Rn9eij9Pm39ED40", result.Data.PageToken);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV6ApplicationAdmin.GetUserVisibleAppsPageAsync(string?, string?, int?, string?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetUserVisibleAppsPageAsync_Result()
    {
        string resultStr = """
                        {
                            "code": 0,
                            "msg": "success",
                            "data": {
                                "app_list": [
                                    {
                                        "app_id": "cli_9cb844403dbb9108",
                                        "app_name": "审批",
                                        "app_scene_type": 1,
                                        "avatar_url": "https://s3-imfile.feishucdn.com/static-resource/v1/e37af67a-b012-4ee0-80ea-a4d28c94b4eg",
                                        "description": "简单高效的审批工具",
                                        "mobile_default_ability": 1,
                                        "pc_default_ability": 1,
                                        "primary_language": "zh_cn",
                                        "status": 1,
                                        "create_source": "developer_console"
                                    }
                                ],
                                "has_more": 1,
                                "lang": "zh_cn",
                                "page_size": 3,
                                "page_token": "3",
                                "total_count": 34
                            }
                        }
                        """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetUserVisibleAppsResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.AppList);
        Assert.Single(result.Data.AppList);
        var app = result.Data.AppList[0];
        Assert.Equal("cli_9cb844403dbb9108", app.AppId);
        Assert.Equal("审批", app.AppName);
        Assert.Equal(1, app.AppSceneType);
        Assert.Equal(1, app.MobileDefaultAbility);
        Assert.Equal(1, app.PcDefaultAbility);
        Assert.Equal("zh_cn", app.PrimaryLanguage);
        Assert.Equal(1, app.Status);
        Assert.Equal("developer_console", app.CreateSource);
        Assert.Equal(1, result.Data.HasMore);
        Assert.Equal(34, result.Data.TotalCount);
        Assert.Equal("zh_cn", result.Data.Lang);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV6ApplicationAdmin.GetAppVisibilityAsync(string, int?, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetAppVisibilityAsync_Result()
    {
        string resultStr = """
                        {
                            "code": 0,
                            "msg": "success",
                            "data": {
                                "departments": [
                                    {
                                        "id": "od-aa2c50a04769feefededb7a05b7525a8"
                                    }
                                ],
                                "invisible_departments": [
                                    {
                                        "id": "od-d3079cfb6ed783b9bdc2e9eac04e85b4"
                                    }
                                ],
                                "users": [
                                    {
                                        "open_id": "ou_d317f090b7258ad0372aa53963cda70d",
                                        "user_id": "79affdge"
                                    }
                                ],
                                "invisible_users": [
                                    {
                                        "open_id": "ou_42ebd86208405e7d4ba6fd36cece410a"
                                    }
                                ],
                                "groups": [
                                    {
                                        "id": "96815a9cd9beg8g4"
                                    }
                                ],
                                "invisible_groups": [
                                    {
                                        "id": "8abfd94b552g2g9c"
                                    }
                                ],
                                "is_visible_to_all": 0,
                                "has_more_users": 0,
                                "user_page_token": "ou_42ebd86208405e7d4ba6fd36cece410a"
                            }
                        }
                        """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetAppVisibilityResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Departments);
        Assert.Single(result.Data.Departments);
        Assert.Equal("od-aa2c50a04769feefededb7a05b7525a8", result.Data.Departments[0].Id);
        Assert.NotNull(result.Data.InvisibleDepartments);
        Assert.Single(result.Data.InvisibleDepartments);
        Assert.NotNull(result.Data.Users);
        Assert.Single(result.Data.Users);
        Assert.Equal("ou_d317f090b7258ad0372aa53963cda70d", result.Data.Users[0].OpenId);
        Assert.Equal("79affdge", result.Data.Users[0].UserId);
        Assert.NotNull(result.Data.Users[0]);
        Assert.Equal(0, result.Data.IsVisibleToAll);
        Assert.Equal(0, result.Data.HasMoreUsers);
        Assert.Equal("ou_42ebd86208405e7d4ba6fd36cece410a", result.Data.UserPageToken);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV6ApplicationAdmin.CheckVisibilityWhiteBlackListAsync(string, CheckVisibilityWhiteBlackListRequest, string?, string?, CancellationToken)"/>函数的请求体与返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_CheckVisibilityWhiteBlackListAsync()
    {
        string bodyStr = """
                        {
                            "user_ids": ["ou_d317f090b7258ad0372aa53963cda70d"],
                            "department_ids": ["od-aa2c50a04769feefededb7a05b7525a8"],
                            "group_ids": ["96815a9cd9beg8g4"]
                        }
                        """;
        var requestBody = JsonSerializer.Deserialize<CheckVisibilityWhiteBlackListRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.Single(requestBody.UserIds!);
        Assert.Single(requestBody.DepartmentIds!);
        Assert.Single(requestBody.GroupIds!);

        string resultStr = """
                        {
                            "code": 0,
                            "msg": "success",
                            "data": {
                                "user_visibility_list": [
                                    {
                                        "user_id": "ou_d317f090b7258ad0372aa53963cda70d",
                                        "in_white_list": false,
                                        "in_black_list": false,
                                        "in_paid_list": false
                                    }
                                ],
                                "department_visibility_list": [
                                    {
                                        "department_id": "od-aa2c50a04769feefededb7a05b7525a8",
                                        "in_white_list": false,
                                        "in_black_list": false
                                    }
                                ],
                                "group_visibility_list": [
                                    {
                                        "group_id": "96815a9cd9beg8g4",
                                        "in_white_list": true,
                                        "in_black_list": false
                                    }
                                ]
                            }
                        }
                        """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<CheckVisibilityWhiteBlackListResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.UserVisibilityList!);
        Assert.False(result.Data.UserVisibilityList![0].InWhiteList);
        Assert.False(result.Data.UserVisibilityList[0].InBlackList);
        Assert.False(result.Data.UserVisibilityList[0].InPaidList);
        Assert.Single(result.Data.DepartmentVisibilityList!);
        Assert.Equal("od-aa2c50a04769feefededb7a05b7525a8", result.Data.DepartmentVisibilityList![0].DepartmentId);
        Assert.Single(result.Data.GroupVisibilityList!);
        Assert.True(result.Data.GroupVisibilityList![0].InWhiteList);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV6ApplicationAdmin.UpdateApplicationVisibilityAsync(string, UpdateApplicationVisibilityRequest, string?, string?, CancellationToken)"/>函数的请求体序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateApplicationVisibilityAsync_RequestBody()
    {
        string bodyStr = """
                        {
                            "add_visible_list": {
                                "user_ids": ["ou_d317f090b7258ad0372aa53963cda70d"],
                                "department_ids": ["od-aa2c50a04769feefededb7a05b7525a8"],
                                "group_ids": ["96815a9cd9beg8g4"]
                            },
                            "del_invisible_list": {
                                "user_ids": ["ou_42ebd86208405e7d4ba6fd36cece410a"]
                            },
                            "is_visible_to_all": false
                        }
                        """;
        var requestBody = JsonSerializer.Deserialize<UpdateApplicationVisibilityRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.AddVisibleList);
        Assert.Single(requestBody.AddVisibleList.UserIds!);
        Assert.Single(requestBody.AddVisibleList.DepartmentIds!);
        Assert.Single(requestBody.AddVisibleList.GroupIds!);
        Assert.NotNull(requestBody.DelInvisibleList);
        Assert.Single(requestBody.DelInvisibleList.UserIds!);
        Assert.False(requestBody.IsVisibleToAll);

        var serialized = JsonSerializer.Serialize(requestBody, _jsonSerializerOptions);
        using var document = JsonDocument.Parse(serialized);
        Assert.False(document.RootElement.GetProperty("is_visible_to_all").GetBoolean());
        Assert.Equal("ou_d317f090b7258ad0372aa53963cda70d", document.RootElement.GetProperty("add_visible_list").GetProperty("user_ids")[0].GetString());
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV6ApplicationAdmin.UpdateApplicationContactsRangeAsync(string, UpdateApplicationContactsRangeRequest, string?, string?, CancellationToken)"/>函数的请求体序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateApplicationContactsRangeAsync_RequestBody()
    {
        string bodyStr = """
                        {
                            "contacts_range_type": "some",
                            "add_visible_list": {
                                "department_ids": ["od-ddee42c0f8a948a5e650341e2153243b"]
                            }
                        }
                        """;
        var requestBody = JsonSerializer.Deserialize<UpdateApplicationContactsRangeRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.Equal("some", requestBody.ContactsRangeType);
        Assert.NotNull(requestBody.AddVisibleList);
        Assert.Single(requestBody.AddVisibleList.DepartmentIds!);
        Assert.Null(requestBody.DelVisibleList);
    }
}
