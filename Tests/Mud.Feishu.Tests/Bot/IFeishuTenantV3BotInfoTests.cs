// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.Bot;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuTenantV3BotInfo"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV3BotInfoTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV3BotInfoTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV3BotInfo.GetBotInfoAsync(CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetBotInfoAsync_Result()
    {
        string resultStr = """
                        {
                            "code": 0,
                            "msg": "ok",
                            "bot": {
                                "activate_status": 2,
                                "app_name": "name",
                                "avatar_url": "https://s1-imfile.feishucdn.com/static-resource/v1/da5xxxx14b16113",
                                "ip_white_list": [
                                ],
                                "open_id": "ou_e6e14f667cfe239d7b129b521dce0569"
                            }
                        }
                        """;
        var result = JsonSerializer.Deserialize<GetBotInfoApiResult>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.Equal("ok", result.Msg);
        Assert.NotNull(result.Bot);
        Assert.Equal(2, result.Bot.ActivateStatus);
        Assert.Equal("name", result.Bot.AppName);
        Assert.Equal("ou_e6e14f667cfe239d7b129b521dce0569", result.Bot.OpenId);
        Assert.NotNull(result.Bot.IpWhiteList);
        Assert.Empty(result.Bot.IpWhiteList);
    }
}
