// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.ApprovalFile;
using System.Text.Json;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// 用于测试<see cref="IFeishuTenantV2ApprovalFile"/>接口的相关函数。
/// </summary>
public class IFeishuTenantV2ApprovalFileTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public IFeishuTenantV2ApprovalFileTests()
    {
        _jsonSerializerOptions = HttpClientExtensions.GetDefaultJsonSerializerOptions();
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV2ApprovalFile.UploadFileAsync(UploadApprovalRequest, CancellationToken)"/>函数的请求体反序列化。
    /// </summary>
    [Fact]
    public void Test_UploadFileAsync_RequestBody()
    {
        string bodyStr = @"
{
	""name"":""123.doc"",
	""type"":""attachment""
}
";
        var requestBody = JsonSerializer.Deserialize<UploadApprovalRequest>(bodyStr, _jsonSerializerOptions);

        Assert.NotNull(requestBody);
    }

    /// <summary>
    /// 用于测试<see cref="IFeishuTenantV2ApprovalFile.UploadFileAsync(UploadApprovalRequest, CancellationToken)"/>函数的返回结果反序列化。
    /// </summary>
    [Fact]
    public void Test_UploadFileAsync_Result()
    {
        string resultStr = @"
{
    ""code"":0,
    ""msg"":""success"",
    ""data"": {
        ""code"": ""D93653C3-2609-4EE0-8041-61DC1D84F0B5"",
        ""url"": ""https://example.com/lark-approval-attachment/image/20210819/a8c1a1f1-47ae-4147-9deb-a8bf2c1234.jpg~image.image?x-expires=1634941234&x-signature=1234Tfv50ryUesNwKTUTnBlJivY%3D#.jpg""
    }
}";
        var result = JsonSerializer.Deserialize<FeishuApiResult<FileUploadResult>>(resultStr, _jsonSerializerOptions);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.Code!);
        Assert.NotEmpty(result.Data.Url!);
    }
}
