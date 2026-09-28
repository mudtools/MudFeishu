// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Channels;

namespace Mud.Feishu.AI.Tests.Channels;

/// <summary>
/// 流式会话请求环境量（P2-3）：作用域释放必须<b>恢复进入前的值</b>，
/// 直接置 <c>null</c> 会让嵌套 <c>Begin</c> 少读一层的环境，且破坏「同层多次 Begin」的幂等预期。
/// </summary>
public class StreamingRequestContextTests
{
    private static ConversationRequest Request(string subject)
        => new("app-a", ConversationScope.Group(), subject, "ou_sender", "om_1", "问题");

    [Fact]
    public void Begin_ShouldExposeRequest_WithinScope_AndClearAfterDispose()
    {
        using (StreamingRequestContext.Begin(Request("oc_outer")))
        {
            StreamingRequestContext.Current.Should().NotBeNull();
            StreamingRequestContext.Current!.SubjectId.Should().Be("oc_outer");
        }

        StreamingRequestContext.Current.Should().BeNull("作用域结束后环境量必须清空（无跨事件残留）");
    }

    [Fact]
    public void Begin_ShouldRestorePrevious_OnNestedDispose()
    {
        using (StreamingRequestContext.Begin(Request("oc_outer")))
        {
            using (StreamingRequestContext.Begin(Request("oc_inner")))
            {
                StreamingRequestContext.Current!.SubjectId.Should().Be("oc_inner", "内层作用域覆盖外层");
            }

            StreamingRequestContext.Current.Should().NotBeNull("内层释放后必须恢复外层，而不是清空");
            StreamingRequestContext.Current!.SubjectId.Should().Be("oc_outer");
        }

        StreamingRequestContext.Current.Should().BeNull();
    }

    [Fact]
    public void Begin_ShouldBeIdempotent_OnDoubleDispose()
    {
        var inner = Request("oc_inner");
        using (StreamingRequestContext.Begin(Request("oc_outer")))
        {
            var scope = StreamingRequestContext.Begin(inner);
            scope.Dispose();
            scope.Dispose(); // 双重释放容错：不得把外层环境一并清空

            StreamingRequestContext.Current!.SubjectId.Should().Be("oc_outer");
        }
    }
}
