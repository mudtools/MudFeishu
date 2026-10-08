// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Messages;
using Mud.Feishu.DataModels.Users;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// AT-F11「姓名 → open_id → 发消息」两跳写链路（§8.2 #15 / #15b）。
/// </summary>
/// <remarks>
/// <b>为什么必须测两跳</b>：<c>contact.search_user</c> 单独可用并不等于"解锁了发给张三"——
/// 真正的闭环要求 ① 它回填 <c>open_id</c>；② <c>im.send_message</c> 接受
/// <c>receive_id_type=open_id</c>。任一侧缺失，链路仍然断在中间（原方案只验到第一跳）。
/// </remarks>
public class ContactSearchChainTests
{
    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static FeishuAgentOptions AgentOptions() => new() { Instructions = "test" };

    [Fact]
    public async Task ContactSearchUser_ShouldResolveNameToOpenId_EndToEnd()
    {
        string? capturedQuery = null;
        int? capturedPageSize = null;
        var client = new Mock<Mud.Feishu.IFeishuTenantV3User>();
        client
            .Setup(c => c.GetUsersByKeywordAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((string query, int? pageSize, string? _, CancellationToken __) =>
            {
                capturedQuery = query;
                capturedPageSize = pageSize;
            })
            .ReturnsAsync(new FeishuApiResult<UserSearchListResult>
            {
                Code = 0,
                Data = new UserSearchListResult
                {
                    HasMore = false,
                    Users =
                    [
                        new UserSearchResult
                        {
                            OpenId = "ou_zhangsan",
                            UserId = "u_zhangsan",
                            Name = "张三",
                            DepartmentIds = ["od_研发"],
                        },
                    ],
                },
            });

        var tools = new ContactTools(client.Object, Options.Create(AgentOptions()));
        var result = await tools.SearchUsersAsync(Args(("query", "张三")), CancellationToken.None);

        capturedQuery.Should().Be("张三", "关键词直通 SDK 的 query 参数（不裁剪、不改写）");
        capturedPageSize.Should().Be(PageSizes.ContactSearch, "page_size 为绑定层钳制的隐藏参数");

        using var document = JsonDocument.Parse(result.ToString()!);
        var item = document.RootElement.GetProperty("items")[0];
        item.GetProperty("open_id").GetString().Should().Be("ou_zhangsan",
            "open_id 是「发给张三」第二跳的必需入参——投影漏掉它链路就断在这里（C-7）");
        item.GetProperty("name").GetString().Should().Be("张三");
        item.GetProperty("department_ids")[0].GetString().Should().Be("od_研发");
        item.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            ["open_id", "user_id", "name", "department_ids"],
            "投影白名单：avatar 等长 URL 字段有意省略（纯上下文开销）");
    }

    [Fact]
    public async Task ContactSearchUser_ShouldBackfillStructuredError_WhenQueryMissing()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV3User>();
        var tools = new ContactTools(client.Object, Options.Create(AgentOptions()));

        var result = await tools.SearchUsersAsync(Args(), CancellationToken.None);

        result.ToString().Should().StartWith("[tool_error] contact.search_user");
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SearchUserThenSendMessage_ShouldCompleteTheTwoHopWriteChain()
    {
        // ── 第一跳：姓名 → open_id ──
        var userClient = new Mock<Mud.Feishu.IFeishuTenantV3User>();
        userClient
            .Setup(c => c.GetUsersByKeywordAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<UserSearchListResult>
            {
                Code = 0,
                Data = new UserSearchListResult
                {
                    Users = [new UserSearchResult { OpenId = "ou_zhangsan", Name = "张三" }],
                },
            });

        var contactTools = new ContactTools(userClient.Object, Options.Create(AgentOptions()));
        var searchResult = await contactTools.SearchUsersAsync(Args(("query", "张三")), CancellationToken.None);

        using var searchDocument = JsonDocument.Parse(searchResult.ToString()!);
        var openId = searchDocument.RootElement.GetProperty("items")[0].GetProperty("open_id").GetString();
        openId.Should().Be("ou_zhangsan");

        // ── 第二跳：open_id → 发消息（receive_id_type 必须是 open_id） ──
        SendMessageRequest? capturedRequest = null;
        string? capturedReceiveIdType = null;
        var messageClient = new Mock<Mud.Feishu.IFeishuTenantV1Message>();
        messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((SendMessageRequest request, string receiveIdType, CancellationToken _) =>
            {
                capturedRequest = request;
                capturedReceiveIdType = receiveIdType;
            })
            .ReturnsAsync(new FeishuApiResult<MessageDataResult>
            {
                Code = 0,
                Data = new MessageDataResult { MessageId = "om_1" },
            });

        var writeTools = new MessageWriteTools(messageClient.Object);
        var sendResult = await writeTools.SendMessageAsync(
            Args(("receive_id", openId!), ("text", "张三你好"), ("receive_id_type", "open_id")),
            CancellationToken.None);

        capturedReceiveIdType.Should().Be("open_id", "receive_id 是 open_id 时必须同步指定 receive_id_type（否则飞书按 chat_id 找群失败）");
        capturedRequest.Should().NotBeNull();
        capturedRequest!.ReceiveId.Should().Be("ou_zhangsan");
        capturedRequest.MsgType.Should().Be("text");

        // content 是 JSON 文本；默认编码器会把中文转义为 \uXXXX，故按 JSON 语义断言而非子串。
        using var contentDocument = JsonDocument.Parse(capturedRequest.Content);
        contentDocument.RootElement.GetProperty("text").GetString().Should().Be("张三你好");

        using var sendDocument = JsonDocument.Parse(sendResult.ToString()!);
        sendDocument.RootElement.GetProperty("message_id").GetString().Should().Be("om_1",
            "两跳闭环：「发给张三」从姓名一路走到消息已发出");
    }
}
