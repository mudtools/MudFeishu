// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AI;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// R7 / §3.B6：二进制<b>出入成对</b>宿主契约（<c>IFeishuBinaryArtifactSink</c> 出向 /
/// <c>IFeishuBinaryArtifactSource</c> 入向）的形态与软缺席语义断言。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么单列这一组</b>：这两个契约的全部价值在于"字节不穿越工具面"——
/// 一旦有人把字节放进<b>结果类型</b>（而不是只放句柄），A10 二进制防线就从"结论"退化成"约定"，
/// 而约定不会被任何人发现。故用反射把该不变量钉住。
/// </para>
/// <para>
/// <b>为什么断言"默认未注册"</b>：软缺席机制的前提是"SDK 不提供默认实现"——
/// 若某天有人给 sink 加了个默认内存实现，依赖它的二进制工具就会在<b>所有</b>宿主上悄悄出现
/// （原本设计是宿主显式开启）。
/// </para>
/// </remarks>
public class BinaryArtifactContractTests
{
    private static readonly Type[] ByteBearingTypes =
    [
        typeof(byte[]),
        typeof(ReadOnlyMemory<byte>),
        typeof(Memory<byte>),
        typeof(Stream),
    ];

    /// <summary>
    /// 该属性类型是否承载字节。
    /// </summary>
    /// <remarks>
    /// ⚠️ 必须解包 <see cref="Nullable{T}"/>：反射对 <c>ReadOnlyMemory&lt;byte&gt;?</c> 返回的
    /// <c>PropertyType</c> 是 <c>Nullable&lt;ReadOnlyMemory&lt;byte&gt;&gt;</c> 而<b>不是</b>底层类型——
    /// 只比底层类型会让"可空字节字段"漏判，这正是本守卫的自证用例要拦住的假绿形态。
    /// </remarks>
    private static bool IsByteBearing(Type type)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;
        return ByteBearingTypes.Contains(actual);
    }

    /// <summary>出向结果类型只带句柄与元信息——<b>字节不得出现在工具可见的返回值里</b>。</summary>
    [Fact]
    public void StoredArtifact_ShouldExposeHandleOnly_NotBytes()
    {
        var bearing = typeof(StoredArtifact)
            .GetProperties()
            .Where(p => IsByteBearing(p.PropertyType))
            .Select(static p => p.Name)
            .ToArray();

        bearing.Should().BeEmpty(
            "StoredArtifact 是工具结果的形状：字节留在宿主侧，只回传句柄（A10 二进制防线）");
    }

    /// <summary>入向结果类型必须携带清理钩子——临时文件/句柄的生命周期由执行链在 finally 释放。</summary>
    [Fact]
    public async Task ResolvedBinaryArtifact_ShouldCarryCleanupHook()
    {
        var cleanup = typeof(ResolvedBinaryArtifact).GetProperty(nameof(ResolvedBinaryArtifact.Cleanup));

        cleanup.Should().NotBeNull("入向素材必须带回清理钩子（与 StagedAttachment 同款 finally 语义）");
        cleanup!.PropertyType.Should().Be(typeof(Func<ValueTask>));

        var invoked = false;
        var artifact = new ResolvedBinaryArtifact(
            new FileUploadRequest("/tmp/ocr-input.png"),
            Size: 3,
            ContentType: "image/png",
            Cleanup: () =>
            {
                invoked = true;
                return default;
            });

        artifact.Upload.FileName.Should().Be("/tmp/ocr-input.png", "SDK 的 [FilePath] 参数消费本地绝对路径");
        await artifact.Cleanup();
        invoked.Should().BeTrue("清理钩子必须可调用（否则临时文件泄漏）");
    }

    /// <summary>两个契约都不由 SDK 默认注册（软缺席的前提：宿主不实现 ⇒ 相关能力不出现）。</summary>
    [Fact]
    public void BinaryArtifactContracts_ShouldNotBeRegisteredByDefault()
    {
        using var provider = GuardProviderFactory.CreateProvider(_ => { });

        provider.GetService<IFeishuBinaryArtifactSink>().Should().BeNull(
            "落盘/上云位置是宿主策略：SDK 不提供默认实现（软缺席语义与 IFeishuAttachmentStager 对称）");
        provider.GetService<IFeishuBinaryArtifactSource>().Should().BeNull(
            "素材来源（域名白名单/大小/MIME）同样是宿主策略：SDK 不提供默认实现");
    }

    /// <summary>
    /// 契约类型<b>不得</b>出现在工具参数里：它们承载字节，进 Schema 即"MB 级参数 + 上下文爆炸"
    /// （DP-C3-1 否决的正是这个形态）。
    /// </summary>
    [Fact]
    public void BinaryArtifactContracts_ShouldNotAppearInToolParameters()
    {
        var curation = Path.Combine(FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Curation");
        var files = Directory.GetFiles(curation, "*.cs", SearchOption.TopDirectoryOnly);
        files.Should().NotBeEmpty("工具契约目录扫描面不得为空（否则本用例假绿）");

        var offenders = new List<string>();
        foreach (var file in files)
        {
            // ⚠️ 只扫**代码行**：XML 文档注释里引用契约名（说明"为什么这些入参不可策展"）是正确写法，
            // 一并扫会把合规文档判成违规（本守卫首次运行即被这条自证逮住）。
            var code = string.Join(
                '\n',
                File.ReadAllLines(file).Where(static line => !line.TrimStart().StartsWith("///", StringComparison.Ordinal)));

            foreach (var forbidden in new[] { "BinaryArtifactRequest", "ResolvedBinaryArtifact", "StoredArtifact", "BinaryArtifact" })
            {
                if (code.Contains(forbidden, StringComparison.Ordinal))
                {
                    offenders.Add($"{Path.GetFileName(file)} 引用了 {forbidden}");
                }
            }
        }

        offenders.Should().BeEmpty(
            "字节载体不得进入工具参数/返回契约（应经宿主侧代码调用，见 Guidance/ai.md 与 §3.B6）：{0}",
            string.Join(" | ", offenders));
    }

    /// <summary>反射面自证：判据必须真的能识别"带字节的类型"，否则上面第一条是假绿。</summary>
    [Fact]
    public void ByteBearingDetection_ShouldActuallyDetectBytes()
    {
        var synthetic = typeof(SyntheticByteCarrier)
            .GetProperties()
            .Where(p => IsByteBearing(p.PropertyType))
            .Select(static p => p.Name)
            .ToArray();

        synthetic.Should().Contain(nameof(SyntheticByteCarrier.Payload),
                "判据失效：带 byte[] 的属性未被识别 ⇒ StoredArtifact 那条断言会恒绿")
            .And.Contain(nameof(SyntheticByteCarrier.Memory));
    }

    private sealed class SyntheticByteCarrier
    {
        public byte[]? Payload { get; set; }

        public ReadOnlyMemory<byte>? Memory { get; set; }
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "Mud.Feishu.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        directory.Should().NotBeNullOrEmpty("测试必须能定位仓库根目录（以 Mud.Feishu.slnx 为锚）");
        return directory!;
    }
}
