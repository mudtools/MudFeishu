// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.DataModels.WsEndpoint;
using ProtoBuf;
using System.Text;
using System.Text.Json;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// FrameBuilder 单元测试 - 验证 ProtoBuf 帧构建和控制帧解析
/// </summary>
public class FrameBuilderTests
{
    #region BuildPingFrame

    [Fact]
    public void BuildPingFrame_ShouldReturnValidProtoBufData()
    {
        // Act
        var data = FrameBuilder.BuildPingFrame(1001);

        // Assert
        data.Should().NotBeNull();
        data.Should().NotBeEmpty();
    }

    [Fact]
    public void BuildPingFrame_ShouldProduceCorrectFrameStructure()
    {
        // Act
        var data = FrameBuilder.BuildPingFrame(1001);

        // Deserialize back
        var frame = Serializer.Deserialize<EventProtoData>(new MemoryStream(data));

        // Assert - 对照 Java SDK newPingFrame
        frame.Service.Should().Be(1001);
        frame.Method.Should().Be(FrameBuilder.MethodControl); // CONTROL = 0
        frame.SeqID.Should().Be(0);
        frame.LogID.Should().Be(0);
        frame.Headers.Should().NotBeNull();
        frame.Headers!.Length.Should().Be(1);
        frame.Headers[0].Key.Should().Be("type");
        frame.Headers[0].Value.Should().Be("ping");
        frame.Payload.Should().BeNull();
    }

    [Fact]
    public void BuildPingFrame_ShouldHandleZeroServiceId()
    {
        // Act
        var data = FrameBuilder.BuildPingFrame(0);
        var frame = Serializer.Deserialize<EventProtoData>(new MemoryStream(data));

        // Assert
        frame.Service.Should().Be(0);
        frame.Method.Should().Be(FrameBuilder.MethodControl);
    }

    #endregion

    #region IsControlFrame / IsDataFrame

    [Fact]
    public void IsControlFrame_ShouldReturnTrue_WhenMethodIsZero()
    {
        var frame = new EventProtoData { Method = 0 };
        FrameBuilder.IsControlFrame(frame).Should().BeTrue();
    }

    [Fact]
    public void IsControlFrame_ShouldReturnFalse_WhenMethodIsOne()
    {
        var frame = new EventProtoData { Method = 1 };
        FrameBuilder.IsControlFrame(frame).Should().BeFalse();
    }

    [Fact]
    public void IsControlFrame_ShouldReturnFalse_WhenFrameIsNull()
    {
        FrameBuilder.IsControlFrame(null!).Should().BeFalse();
    }

    [Fact]
    public void IsDataFrame_ShouldReturnTrue_WhenMethodIsOne()
    {
        var frame = new EventProtoData { Method = 1 };
        FrameBuilder.IsDataFrame(frame).Should().BeTrue();
    }

    [Fact]
    public void IsDataFrame_ShouldReturnFalse_WhenMethodIsZero()
    {
        var frame = new EventProtoData { Method = 0 };
        FrameBuilder.IsDataFrame(frame).Should().BeFalse();
    }

    #endregion

    #region ExtractClientConfig

    [Fact]
    public void ExtractClientConfig_ShouldReturnConfig_WhenPayloadIsValidJson()
    {
        // Arrange
        var configJson = "{\"ReconnectCount\":5,\"ReconnectInterval\":120,\"ReconnectNonce\":30,\"PingInterval\":60}";
        var frame = new EventProtoData
        {
            Method = 0,
            Payload = Encoding.UTF8.GetBytes(configJson),
            Headers = new[] { new ProtoHeader { Key = "type", Value = "pong" } }
        };

        // Act
        var config = FrameBuilder.ExtractClientConfig(frame, NullLogger.Instance);

        // Assert
        config.Should().NotBeNull();
        config!.ReconnectCount.Should().Be(5);
        config.ReconnectInterval.Should().Be(120);
        config.ReconnectNonce.Should().Be(30);
        config.PingInterval.Should().Be(60);
    }

    [Fact]
    public void ExtractClientConfig_ShouldReturnNull_WhenPayloadIsNull()
    {
        var frame = new EventProtoData { Method = 0, Payload = null };

        FrameBuilder.ExtractClientConfig(frame, NullLogger.Instance).Should().BeNull();
    }

    [Fact]
    public void ExtractClientConfig_ShouldReturnNull_WhenPayloadIsEmpty()
    {
        var frame = new EventProtoData { Method = 0, Payload = Array.Empty<byte>() };

        FrameBuilder.ExtractClientConfig(frame, NullLogger.Instance).Should().BeNull();
    }

    [Fact]
    public void ExtractClientConfig_ShouldReturnNull_WhenPayloadIsInvalidJson()
    {
        var frame = new EventProtoData
        {
            Method = 0,
            Payload = Encoding.UTF8.GetBytes("not a json")
        };

        FrameBuilder.ExtractClientConfig(frame, NullLogger.Instance).Should().BeNull();
    }

    [Fact]
    public void ExtractClientConfig_ShouldReturnNull_WhenFrameIsNull()
    {
        FrameBuilder.ExtractClientConfig(null!, NullLogger.Instance).Should().BeNull();
    }

    #endregion

    #region ExtractServiceId

    [Fact]
    public void ExtractServiceId_ShouldReturnServiceId_WhenUrlContainsServiceId()
    {
        var url = "wss://example.com/ws?device_id=abc123&service_id=1001";

        var serviceId = FrameBuilder.ExtractServiceId(url);

        serviceId.Should().Be(1001);
    }

    [Fact]
    public void ExtractServiceId_ShouldReturnNull_WhenUrlDoesNotContainServiceId()
    {
        var url = "wss://example.com/ws?device_id=abc123";

        FrameBuilder.ExtractServiceId(url).Should().BeNull();
    }

    [Fact]
    public void ExtractServiceId_ShouldReturnNull_WhenUrlIsEmpty()
    {
        FrameBuilder.ExtractServiceId("").Should().BeNull();
        FrameBuilder.ExtractServiceId(null!).Should().BeNull();
    }

    [Fact]
    public void ExtractServiceId_ShouldReturnNull_WhenServiceIdIsNotNumeric()
    {
        var url = "wss://example.com/ws?service_id=abc";

        FrameBuilder.ExtractServiceId(url).Should().BeNull();
    }

    [Fact]
    public void ExtractServiceId_ShouldHandleMultipleQueryParams()
    {
        var url = "wss://example.com/ws?device_id=dev1&service_id=2002&token=xyz";

        var serviceId = FrameBuilder.ExtractServiceId(url);

        serviceId.Should().Be(2002);
    }

    #endregion
}
