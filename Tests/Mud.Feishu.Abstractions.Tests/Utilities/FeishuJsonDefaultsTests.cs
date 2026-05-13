// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Utilities;

namespace Mud.Feishu.Abstractions.Tests.Utilities;

public class FeishuJsonDefaultsTests
{
    [Fact]
    public void DeserializerOptions_ShouldHavePropertyNameCaseInsensitive()
    {
        Assert.True(FeishuJsonDefaults.DeserializerOptions.PropertyNameCaseInsensitive);
    }

    [Fact]
    public void DeserializerOptions_ShouldUseCamelCaseNamingPolicy()
    {
        Assert.Equal(JsonNamingPolicy.CamelCase, FeishuJsonDefaults.DeserializerOptions.PropertyNamingPolicy);
    }

    [Fact]
    public void DeserializerOptions_ShouldNotWriteIndented()
    {
        Assert.False(FeishuJsonDefaults.DeserializerOptions.WriteIndented);
    }

    [Fact]
    public void DeserializerOptions_ShouldIgnoreNullValues()
    {
        Assert.Equal(System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            FeishuJsonDefaults.DeserializerOptions.DefaultIgnoreCondition);
    }

    [Fact]
    public void SerializerOptions_ShouldUseCamelCaseNamingPolicy()
    {
        Assert.Equal(JsonNamingPolicy.CamelCase, FeishuJsonDefaults.SerializerOptions.PropertyNamingPolicy);
    }

    [Fact]
    public void SerializerOptions_ShouldNotWriteIndented()
    {
        Assert.False(FeishuJsonDefaults.SerializerOptions.WriteIndented);
    }

    [Fact]
    public void SerializerOptions_ShouldIgnoreNullValues()
    {
        Assert.Equal(System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            FeishuJsonDefaults.SerializerOptions.DefaultIgnoreCondition);
    }

    [Fact]
    public void DeserializerOptions_ShouldDeserializeCamelCaseJson()
    {
        var json = """{"userName": "test", "userId": 123}""";
        var result = JsonSerializer.Deserialize<TestPayload>(json, FeishuJsonDefaults.DeserializerOptions);

        Assert.NotNull(result);
        Assert.Equal("test", result.UserName);
        Assert.Equal(123, result.UserId);
    }

    [Fact]
    public void SerializerOptions_ShouldSerializeToCamelCase()
    {
        var payload = new TestPayload { UserName = "test", UserId = 123 };
        var json = JsonSerializer.Serialize(payload, FeishuJsonDefaults.SerializerOptions);

        Assert.Contains("userName", json);
        Assert.Contains("userId", json);
    }

    [Fact]
    public void SerializerOptions_ShouldNotSerializeNullValues()
    {
        var payload = new TestPayload { UserName = null, UserId = 123 };
        var json = JsonSerializer.Serialize(payload, FeishuJsonDefaults.SerializerOptions);

        Assert.DoesNotContain("userName", json);
        Assert.Contains("userId", json);
    }

    private class TestPayload
    {
        public string? UserName { get; set; }
        public int UserId { get; set; }
    }
}
