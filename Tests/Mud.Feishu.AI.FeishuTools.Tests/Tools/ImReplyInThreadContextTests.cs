// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;

using Moq;

using Mud.Feishu.AI;
using Mud.Feishu.AI.FeishuTools.Internal;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// <b>R5 / F-4</b>：<c>thread_id</c> 上下文链路的行为断言（F-4 DoD ①②）。
/// </summary>
/// <remarks>
/// <para>
/// <b>缺陷背景</b>：<c>ImTools</c> 内 <c>IFeishuToolContextAccessor</c> <b>0 命中</b>，
/// <c>reply_in_thread</c> 恒为 <c>args.ReplyInThread ?? false</c> ⇒ 模型不显式传参就拿不到话题串，
/// 真实群话题场景里回复会<b>掉回主会话</b>。
/// </para>
/// <para>
/// <b>不变式</b>：流式目标<b>恒为 chat_id</b>（thread 只作上下文与路由维度，不改流式目标）。
/// </para>
/// </remarks>
public class ImReplyInThreadContextTests
{
    private static FeishuToolContext Ctx(string? threadId)
        => new("app_key", "conv_key", ChatId: "oc_1", UserId: "ou_1", ThreadId: threadId);

    private static Dictionary<string, object?> Args(object? replyInThread)
    {
        var args = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["message_id"] = "om_src_1",
            ["msg_type"] = "text",
            ["content"] = "{\"text\":\"hi\"}",
        };

        if (replyInThread is not null)
        {
            args["reply_in_thread"] = replyInThread;
        }

        return args;
    }

    /// <summary>
    /// 捕获器。<b>必须是引用类型</b>：若直接返回 <c>ReplyMessageRequest?</c> 局部变量的值，
    /// 拿到的是<b>调用时刻的快照</b>（恒为 null），而非Moq 回调后续赋的值——
    /// 这是闭包变量按值返回的经典陷阱（本项首版即踩中，4 条用例全红）。
    /// </summary>
    private sealed class Captured
    {
        public Mud.Feishu.DataModels.Messages.ReplyMessageRequest? Request { get; set; }
    }

    private static (Mock<IFeishuTenantV1Message> Client, Captured Captured) CreateClient()
    {
        var captured = new Captured();
        var client = new Mock<IFeishuTenantV1Message>();

        client
            .Setup(c => c.ReplyMessageAsync(
                It.IsAny<string>(),
                It.IsAny<Mud.Feishu.DataModels.Messages.ReplyMessageRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Mud.Feishu.DataModels.Messages.ReplyMessageRequest, CancellationToken>((_, request, _) => captured.Request = request)
            .ReturnsAsync(new Mud.Feishu.DataModels.FeishuApiResult<Mud.Feishu.DataModels.Messages.MessageDataResult>
            {
                Code = 0,
                Data = new Mud.Feishu.DataModels.Messages.MessageDataResult { MessageId = "om_reply_1" },
            });

        return (client, captured);
    }

    private static ImTools CreateTools(Mock<IFeishuTenantV1Message> client, IFeishuToolContextAccessor accessor)
        => new(
            client.Object,
            new Mock<Mud.Feishu.IFeishuTenantV1ChatGroupMember>().Object,
            Options.Create(new FeishuAgentOptions { Instructions = "test" }),
            chatGroupClient: null,
            accessor);

    /// <summary>
    /// <b>DoD ②</b>：处于话题上下文（<c>ThreadId</c> 非空）且模型未显式传参时，
    /// <c>reply_in_thread</c> <b>必须自动为 true</b>。
    /// </summary>
    [Fact]
    public async Task ReplyInThread_ShouldAutoBeTrue_WhenSessionIsInThread_AndModelOmitsIt()
    {
        var (client, captured) = CreateClient();
        var accessor = new FeishuToolContextAccessor();
        var tools = CreateTools(client, accessor);

        using var _ = accessor.Begin(Ctx("omt_1"));

        await tools.ReplyMessageAsync(Args(null), CancellationToken.None);

        captured.Request.Should().NotBeNull("ReplyMessageAsync 未被调用");
        captured.Request!.ReplyInThread.Should().BeTrue(
            "会话处于话题中且模型未显式传参 ⇒ 必须自动以话题形式回复，否则回复会掉回主会话");
    }

    /// <summary>
    /// <b>不变式</b>：非话题上下文时行为与改造前一致 ⇒ <c>false</c>（普通群聊不得被误接话题）。
    /// </summary>
    [Fact]
    public async Task ReplyInThread_ShouldBeFalse_WhenSessionIsNotInThread()
    {
        var (client, captured) = CreateClient();
        var accessor = new FeishuToolContextAccessor();
        var tools = CreateTools(client, accessor);

        using var _ = accessor.Begin(Ctx(null));

        await tools.ReplyMessageAsync(Args(null), CancellationToken.None);

        captured.Request!.ReplyInThread.Should().BeFalse("非话题会话不得自动开启话题回复");
    }

    /// <summary>
    /// <b>优先级</b>：模型显式传 <c>false</c> 时必须为 false —— 显式传参是明确指令，
    /// 自动推断不得覆盖它（如话题中要求"直接回主会话"）。
    /// </summary>
    [Fact]
    public async Task ExplicitModelValue_ShouldWinOverAutoDerivation()
    {
        var (client, captured) = CreateClient();
        var accessor = new FeishuToolContextAccessor();
        var tools = CreateTools(client, accessor);

        using var _ = accessor.Begin(Ctx("omt_1"));

        await tools.ReplyMessageAsync(Args(false), CancellationToken.None);

        captured.Request!.ReplyInThread.Should().BeFalse(
            "模型显式传 false 是明确指令，自动推断（话题中=true）不得覆盖它");
    }

    /// <summary>
    /// <b>缺上下文不得退化</b>：未注册 accessor（依赖为可选软缺席）时执行器仍须能工作，
    /// 行为退化为改造前的 <c>args.ReplyInThread ?? false</c>。
    /// </summary>
    [Fact]
    public async Task MissingToolContextAccessor_ShouldDegradeToModelValueOnly()
    {
        var (client, captured) = CreateClient();

        // 第四参数缺席 ⇒ 对应 DI 未注册 IFeishuToolContextAccessor 的情形。
        var tools = new ImTools(
            client.Object,
            new Mock<Mud.Feishu.IFeishuTenantV1ChatGroupMember>().Object,
            Options.Create(new FeishuAgentOptions { Instructions = "test" }));

        await tools.ReplyMessageAsync(Args(null), CancellationToken.None);

        captured.Request!.ReplyInThread.Should().BeFalse("缺上下文时必须退化为原行为，不得抛异常");
    }
}