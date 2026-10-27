// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;

namespace Mud.Feishu.AI.Tests.ContractGuards;

/// <summary>
/// CardKit 流式回复的<b>路由契约</b>：机械锁定「AI 通道打的到底是哪个端点」。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要这条守卫</b>：流式回复曾接过 <c>im/v1/messages/{id}</c>（编辑已发消息）与
/// <c>app_feed_card</c>（工作台消息流）两条完全不同的渲染面，而单元测试都在<b>接口层</b>打桩——
/// 「调了哪个 HTTP 端点」只能在 SDK 的路由特性上验证。把 method + 模板读出来比对，
/// 日后 SDK 路由被改名/迁移时这条守卫立刻报红，而不是等线上用户看不到打字机。
/// </para>
/// <para>
/// <b>为什么不启 HttpListener</b>：起真实服务器只能验证「能发出去」，验证不了「发到哪」；
/// 而路由是生成期的静态事实，反射读特性既零依赖也零端口占用。
/// </para>
/// </remarks>
public class CardKitRouteContractGuards
{
    /// <summary>流式文本更新端点（cardkit v1，卡片元素内容）。</summary>
    private const string StreamUpdatePath = "/open-apis/cardkit/v1/cards/{card_id}/elements/{element_id}/content";

    /// <summary>卡片实体创建端点（cardkit v1）。</summary>
    private const string CreateCardPath = "/open-apis/cardkit/v1/cards";

    [Fact]
    public void CardKitStreamUpdate_ShouldUseCardKitElementContentRoute()
    {
        var route = ReadRoute(
            typeof(Mud.Feishu.IFeishuTenantV1CardElements),
            "StreamUpdateCardTextByIdAsync",
            parameterCount: 4);

        route.Method.Should().Be("Put", "流式更新是 PUT（同一元素内容的全量替换语义）");
        route.Path.Should().Be(StreamUpdatePath,
            "流式更新必须命中 cardkit 元素内容端点——接到 im/v1/messages 或 app_feed_card 都会表现为「用户看不到打字机」");
    }

    [Fact]
    public void CardEntityCreate_ShouldUseCardKitCreateRoute()
    {
        var route = ReadRoute(typeof(Mud.Feishu.IFeishuTenantV1Card), "CreateCardAsync", parameterCount: 2);

        route.Method.Should().Be("Post");
        route.Path.Should().Be(CreateCardPath, "卡片实体必须在 cardkit v1 下创建（卡片 JSON 2.0 + streaming_mode 的前置）");
    }

    /// <summary>
    /// 自证：读取器必须按<b>方法</b>取路由，而不是返回常量——否则上面两条断言恒绿。
    /// </summary>
    [Fact]
    public void RouteReader_ShouldReadPerMethod_OtherwiseGuardIsFalseGreen()
    {
        var cardKit = ReadRoute(typeof(Mud.Feishu.IFeishuTenantV1CardElements), "StreamUpdateCardTextByIdAsync", parameterCount: 4);
        var imMessage = ReadRoute(typeof(Mud.Feishu.IFeishuTenantV1Message), "SendMessageAsync", parameterCount: 3);

        imMessage.Path.Should().StartWith("/open-apis/im/", "读取器必须读到 im 域的真实模板（前置：读得到特性）");
        imMessage.Path.Should().NotBe(cardKit.Path, "两个方法的路由必须不同——相同即读取器退化为常量（前提失效，上面的断言全无判别力）");
    }

    private readonly record struct HttpRoute(string Method, string Path);

    /// <summary>
    /// 反射读出方法的 HTTP 方法 + 路由模板：取带单个字符串实参的「动词型」特性（<c>Put</c> / <c>Post</c> / …）。
    /// </summary>
    /// <param name="contract">生成器生成的客户端契约接口。</param>
    /// <param name="methodName">方法名。</param>
    /// <param name="parameterCount">参数个数（同名重载的判据）。</param>
    /// <returns>HTTP 方法与路由模板。</returns>
    private static HttpRoute ReadRoute(Type contract, string methodName, int parameterCount)
    {
        var method = contract
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(m => m.Name == methodName && m.GetParameters().Length == parameterCount);

        foreach (var attribute in method.GetCustomAttributes(inherit: false))
        {
            var attributeType = attribute.GetType();
            var verb = attributeType.Name.Replace("Attribute", string.Empty);
            if (verb is not ("Get" or "Post" or "Put" or "Patch" or "Delete"))
            {
                continue;
            }

            // 路由模板是特性上的「以 / 开头的字符串成员」——生成器把它放在构造实参对应的字段/属性里，
            // 两种形态都读（不依赖具体成员名，避免 SDK 改成员名时守卫静默失效）。
            var template = attributeType
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(f => f.GetValue(attribute) as string)
                .Concat(attributeType
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(static p => p.CanRead && p.GetIndexParameters().Length == 0)
                    .Select(p => p.GetValue(attribute) as string))
                .FirstOrDefault(static value => !string.IsNullOrEmpty(value) && value!.StartsWith("/", StringComparison.Ordinal));

            if (template is not null)
            {
                return new HttpRoute(verb, template);
            }
        }

        return new HttpRoute(string.Empty, string.Empty);
    }
}
