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
/// 用于测试<see cref="IFeishuTenantV6Application"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV6ApplicationTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV6ApplicationTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV6Application.UpdateApplicationOwnerAsync(string, UpdateApplicationOwnerRequest, string?, CancellationToken)"/>函数的请求体序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateApplicationOwnerAsync_RequestBody()
    {
        string bodyStr = """
                        {
                            "owner_id": "ou_84aad35d084aa403a838cf73ee184670"
                        }
                        """;
        var requestBody = JsonSerializer.Deserialize<UpdateApplicationOwnerRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.Equal("ou_84aad35d084aa403a838cf73ee184670", requestBody.OwnerId);

        var serialized = JsonSerializer.Serialize(requestBody, _jsonSerializerOptions);
        using var document = JsonDocument.Parse(serialized);
        Assert.Equal("ou_84aad35d084aa403a838cf73ee184670", document.RootElement.GetProperty("owner_id").GetString());
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV6Application.UpdateApplicationCollaboratorsAsync(string, UpdateApplicationCollaboratorsRequest, string?, CancellationToken)"/>函数的请求体序列化。
    /// </summary>
    [Fact]
    public void Test_UpdateApplicationCollaboratorsAsync_RequestBody()
    {
        string bodyStr = """
                        {
                            "adds": [
                                {
                                    "type": "administrator",
                                    "user_id": "ou_d317f090b7258ad0372aa53963cda70d"
                                }
                            ],
                            "removes": [
                                "ou_84aad35d084aa403a838cf73ee18467"
                            ]
                        }
                        """;
        var requestBody = JsonSerializer.Deserialize<UpdateApplicationCollaboratorsRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
        Assert.NotNull(requestBody.Adds);
        Assert.Single(requestBody.Adds);
        Assert.Equal("administrator", requestBody.Adds[0].Type);
        Assert.Equal("ou_d317f090b7258ad0372aa53963cda70d", requestBody.Adds[0].UserId);
        Assert.NotNull(requestBody.Removes);
        Assert.Single(requestBody.Removes);

        var serialized = JsonSerializer.Serialize(requestBody, _jsonSerializerOptions);
        using var document = JsonDocument.Parse(serialized);
        Assert.Equal("administrator", document.RootElement.GetProperty("adds")[0].GetProperty("type").GetString());
        Assert.Equal("ou_84aad35d084aa403a838cf73ee18467", document.RootElement.GetProperty("removes")[0].GetString());
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV6Application.GetApplicationCollaboratorsAsync(string, string?, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_GetApplicationCollaboratorsAsync_Result()
    {
        string resultStr = """
                        {
                            "code": 0,
                            "msg": "success",
                            "data": {
                                "collaborators": [
                                    {
                                        "type": "administrator",
                                        "user_id": "ou_d317f090b7258ad0372aa53963cda70d"
                                    }
                                ]
                            }
                        }
                        """;
        var result = JsonSerializer.Deserialize<FeishuApiResult<GetApplicationCollaboratorsResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.Equal(0, result.Code);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Collaborators);
        Assert.Single(result.Data.Collaborators);
        Assert.Equal("administrator", result.Data.Collaborators[0].Type);
        Assert.Equal("ou_d317f090b7258ad0372aa53963cda70d", result.Data.Collaborators[0].UserId);
    }
}
