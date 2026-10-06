// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;

using Moq;

using Mud.Feishu.AI.FeishuTools.Internal;
using Mud.Feishu.DataModels;
using Mud.Feishu.DataModels.Messages;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// R5 / F-6：<c>im.send_card</c> 执行器断言。
/// </summary>
/// <remarks>
/// 核心断言是 <b>F-6 的目的本身</b>：模型给的入参是 <b>DSL 文本</b>（不含任何 JSON），
/// 而下发给平台的是 <b>编译好的卡片 JSON</b> 且 <c>msg_type=interactive</c>。
/// </remarks>
public class ImSendCardTests
{
    private static Dictionary<string, object?> Args(params (string Key, object? Value)[] pairs)
        => pairs.ToDictionary(static p => p.Key, static p => p.Value, StringComparer.Ordinal);

    private sealed class Capture
    {
        public SendMessageRequest? Request { get; set; }

        public string? ReceiveIdType { get; set; }
    }

    private static (Mock<IFeishuTenantV1Message> Client, Capture Captured) CreateClient()
    {
        var captured = new Capture();
        var client = new Mock<IFeishuTenantV1Message>();

        client
            .Setup(c => c.SendMessageAsync(
                It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<SendMessageRequest, string, CancellationToken>(
                (request, receiveIdType, _) => { captured.Request = request; captured.ReceiveIdType = receiveIdType; })
            .ReturnsAsync(new FeishuApiResult<MessageDataResult> { Code = 0, Data = new MessageDataResult() });

        return (client, captured);
    }

    private static MessageWriteTools CreateTools(Mock<IFeishuTenantV1Message> client) => new(client.Object);

    /// <summary>
    /// <b>F-6 的核心</b>：入参零 JSON，下发的是编译产物，且 <c>msg_type=interactive</c>。
    /// </summary>
    [Fact]
    public async Task SendCard_ShouldCompileDslIntoInteractiveCard()
    {
        var (client, captured) = CreateClient();
        var tools = CreateTools(client);

        await tools.SendCardAsync(
            Args(
                ("receive_id", "oc_1"),
                ("body", "text:任务已分配"),
                ("title", "新任务"),
                ("buttons", "查看|url|https://feishu.cn/t/1")),
            CancellationToken.None);

        captured.Request.Should().NotBeNull("SendMessageAsync 未被调用");
        captured.Request!.MsgType.Should().Be("interactive");
        captured.Request.ReceiveId.Should().Be("oc_1");
        captured.ReceiveIdType.Should().Be("chat_id", "缺省为群聊");

        // 下发的 content 必须是**编译产物**（合法 JSON），而不是模型原样传入的 DSL。
        using var document = JsonDocument.Parse(captured.Request.Content!);
        var card = document.RootElement.GetProperty("zh_cn");
        card.GetProperty("title").GetString().Should().Be("新任务");

        var elements = card.GetProperty("elements");
        elements.GetArrayLength().Should().Be(2, "1 个正文元素 + 1 个按钮 action");
        elements[0].GetProperty("tag").GetString().Should().Be("div");
        elements[1].GetProperty("tag").GetString().Should().Be("action");
    }

    /// <summary>模型入参必须<b>不含任何 JSON 字符</b> —— 这是"零 JSON 字符串"的可执行判据。</summary>
    [Fact]
    public async Task SendCard_ModelArguments_ShouldContainNoJsonAtAll()
    {
        var (client, _) = CreateClient();
        var tools = CreateTools(client);

        var args = Args(
            ("receive_id", "oc_1"),
            ("body", "text:第一段\ndivider\nquote:请注意"),
            ("buttons", "已处理|value|done"));

        // 入参里不得出现 JSON 结构字符：证明模型无需构造 JSON。
        foreach (var value in args.Values.OfType<string>())
        {
            value.Should().NotContain("{").And.NotContain("}").And.NotContain("\"");
        }

        await tools.SendCardAsync(args, CancellationToken.None);
    }

    /// <summary>非法 DSL 必须在<b>编译期</b>被拒，且<b>不得</b>下发到平台。</summary>
    [Fact]
    public async Task SendCard_ShouldRejectIllegalDsl_BeforeCallingDownstream()
    {
        var (client, captured) = CreateClient();
        var tools = CreateTools(client);

        var result = await tools.SendCardAsync(
            Args(("receive_id", "oc_1"), ("body", "unknown:内容")),
            CancellationToken.None);

        captured.Request.Should().BeNull("非法 DSL 不得下发到平台");
        result.Text.Should().Contain("合法组合", "错误消息必须带合法组合，让模型可自我纠正");
    }

    /// <summary>dry_run：不调用下游，但<b>返回编译出的卡片结构</b>（便于模型自检将要发什么）。</summary>
    [Fact]
    public async Task SendCard_DryRun_ShouldNotCallDownstream_ButShowCompiledCard()
    {
        var (client, captured) = CreateClient();
        var tools = CreateTools(client);

        var result = await tools.SendCardAsync(
            Args(
                ("receive_id", "oc_1"),
                ("body", "text:x"),
                ("title", "T"),
                ("dry_run", true)),
            CancellationToken.None);

        captured.Request.Should().BeNull("dry_run 必须完全不触达下游");
        result.Text.Should().Contain("dry_run");
        result.Text.Should().Contain("zh_cn", "dry_run 应展示编译出的卡片结构，便于模型自检");
    }

    /// <summary>非法 receive_id_type 必须被拒（与 im.send_message 同口径）。</summary>
    [Fact]
    public async Task SendCard_ShouldRejectUnknownReceiveIdType()
    {
        var (client, captured) = CreateClient();
        var tools = CreateTools(client);

        var result = await tools.SendCardAsync(
            Args(("receive_id", "oc_1"), ("body", "text:x"), ("receive_id_type", "nope")),
            CancellationToken.None);

        captured.Request.Should().BeNull();
        result.Text.Should().Contain("receive_id_type");
    }
}
