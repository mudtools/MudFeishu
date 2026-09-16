// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

#if NET8_0_OR_GREATER
using System.Text.Json;
using FluentAssertions;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels;

namespace Mud.Feishu.Abstractions.Tests.Utilities;

/// <summary>
/// FeishuApiResultJsonContext 单元测试。
/// 验证 P0-1 修复：FeishuApiResult<T> 系列泛型响应包装的 JSON 源生成上下文。
/// </summary>
public class FeishuApiResultJsonContextTests
{
    [Fact]
    public void FeishuApiResult_BaseType_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """{"code":10001,"msg":"invalid app_id"}""";

        // Act
        var result = JsonSerializer.Deserialize(json, FeishuApiResultJsonContext.Default.FeishuApiResult);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be(10001);
        result.Msg.Should().Be("invalid app_id");
    }

    [Fact]
    public void FeishuApiResult_GetUserDataResult_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """{"code":0,"msg":"success","data":{"name":"test_user","open_id":"ou_xxx","union_id":"on_xxx","tenant_key":"tk_xxx"}}""";

        // Act
        var result = JsonSerializer.Deserialize(
            json, FeishuApiResultJsonContext.Default.FeishuApiResultGetUserDataResult);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be(0);
        result.Msg.Should().Be("success");
        result.Data.Should().NotBeNull();
        result.Data!.OpenId.Should().Be("ou_xxx");
        result.Data.UnionId.Should().Be("on_xxx");
        result.Data.TenantKey.Should().Be("tk_xxx");
    }

    [Fact]
    public void FeishuApiResult_WithMissingData_ShouldDeserializeCorrectly()
    {
        // Arrange - 不带 data 字段的响应
        var json = """{"code":0,"msg":"success"}""";

        // Act
        var result = JsonSerializer.Deserialize(
            json, FeishuApiResultJsonContext.Default.FeishuApiResultGetUserDataResult);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be(0);
        result.Data.Should().BeNull();
    }

    [Fact]
    public void FeishuApiResult_NullData_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """{"code":0,"msg":"success","data":null}""";

        // Act
        var result = JsonSerializer.Deserialize(
            json, FeishuApiResultJsonContext.Default.FeishuApiResultGetUserDataResult);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be(0);
        result.Data.Should().BeNull();
    }

    [Fact]
    public void FeishuApiResult_Serialize_ShouldProduceCorrectJson()
    {
        // Arrange
        var apiResult = new FeishuApiResult<GetUserDataResult>
        {
            Code = 0,
            Msg = "success",
            Data = new GetUserDataResult { Name = "test_user", OpenId = "ou_test", UnionId = "on_test" }
        };

        // Act
        var json = JsonSerializer.Serialize(apiResult, FeishuApiResultJsonContext.Default.FeishuApiResultGetUserDataResult);

        // Assert
        json.Should().Contain("\"code\":0");
        json.Should().Contain("\"msg\":\"success\"");
        json.Should().Contain("\"open_id\":\"ou_test\"");
        json.Should().Contain("\"union_id\":\"on_test\"");
    }
}
#endif
