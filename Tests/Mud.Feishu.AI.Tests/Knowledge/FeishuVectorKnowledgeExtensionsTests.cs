// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mud.AI.Abstractions.Knowledge;

namespace Mud.Feishu.AI.Tests.Knowledge;

/// <summary>
/// RAG-B 产品 DI 装配面（<c>AddFeishuVectorKnowledge</c>）的"先注册者生效"互斥口径。
/// </summary>
/// <remarks>
/// N2-e：检索/索引机制已中立化（<c>Mud.AI.Agent.Knowledge</c>），其行为用例随类型迁入
/// <c>Tests/Mud.AI.Agent.Tests</c>；本文件只留<b>产品装配入口</b>用例——它断言语义的是
/// 飞书侧 DI 扩展与 Aily 托管问答的互斥，不是中立机制。
/// </remarks>
public class FeishuVectorKnowledgeExtensionsTests
{
    /// <summary>
    /// RAG-B 与 RAG-A 都注册 <see cref="IRetriever"/> ⇒ <b>先注册者生效</b>，
    /// 且同时注册不得抛解析异常（"换检索面"不应要求卸载既有包）。
    /// </summary>
    [Fact]
    public void VectorRetriever_Should_BeFirstRegistrationWins_WithoutBreakingExistingOne()
    {
        var store = new Mock<IVectorStore>().Object;

        var services = new ServiceCollection();
        services.AddFeishuVectorKnowledge(store);

        // 模拟"后注册的另一个检索面"（Aily 侧同样是 TryAddSingleton ⇒ 先注册者生效）。
        services.TryAddSingleton<IRetriever>(_ => new StubRetriever());

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IRetriever>().Should().BeAssignableTo<Mud.AI.Agent.Knowledge.VectorRetriever>(
            "两个检索面并存时不得抛解析异常，也不得让后注册者静默夺权（换检索面应显式先注册）");
        provider.GetRequiredService<IVectorStore>().Should().BeSameAs(store, "宿主注入的存储必须原样可解析");
    }

    /// <summary>反向口径：先注册的实现生效（宿主想换检索面时，只需先注册它）。</summary>
    [Fact]
    public void VectorRetriever_Should_NotOverride_ExistingRetrieverRegistration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRetriever>(_ => new StubRetriever());
        services.AddFeishuVectorKnowledge(new Mock<IVectorStore>().Object);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IRetriever>().Should().BeOfType<StubRetriever>(
            "后注册的向量检索器不得覆盖宿主已有的检索面（否则升级即行为变更）");
    }

    private sealed class StubRetriever : IRetriever
    {
        public Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(string query, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RetrievedChunk>>([]);
    }
}
