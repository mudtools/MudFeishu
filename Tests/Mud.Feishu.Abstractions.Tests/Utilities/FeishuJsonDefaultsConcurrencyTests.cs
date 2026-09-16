// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using FluentAssertions;
using Mud.Feishu.Abstractions.Utilities;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Utilities;

/// <summary>
/// 验证 <see cref="FeishuJsonDefaults"/> 的静态状态治理（ARC-3）。
/// </summary>
/// <remarks>
/// 回归背景：net8+ 分支原实现仅组合源生成 Context，缺少反射兜底，导致未被任何
/// JsonSerializerContext 覆盖的类型（典型为用户自定义事件负载）在 net8+/net10 上
/// 反序列化直接抛 <see cref="NotSupportedException"/>，而 net6.0/netstandard2.0 正常，
/// 构成跨 TFM 行为不一致。同时 <c>ConfigureUserResolver</c> 非幂等，重复调用会让解析器链无限膨胀。
/// </remarks>
public class FeishuJsonDefaultsConcurrencyTests
{
    /// <summary>
    /// 未被任何源生成 Context 覆盖的类型，在所有 TFM 上都应能正常序列化/反序列化。
    /// 修复前：net8+/net10 抛 NotSupportedException（本用例失败）。
    /// </summary>
    [Fact]
    public void DeserializerOptions_ShouldSerializeUncoveredType_WhenResolverConfigured()
    {
        try
        {
            var json = JsonSerializer.Serialize(new UncoveredUserPayload { Name = "mud", Age = 7 },
                FeishuJsonDefaults.DeserializerOptions);

            var back = JsonSerializer.Deserialize<UncoveredUserPayload>(json, FeishuJsonDefaults.DeserializerOptions);

            back.Should().NotBeNull();
            back!.Name.Should().Be("mud");
            back.Age.Should().Be(7);
        }
        finally
        {
            FeishuJsonDefaults.Reset();
        }
    }

    /// <summary>
    /// 并发调用 ConfigureUserResolver 不应抛异常，且结果仍可用于序列化。
    /// </summary>
    [Fact]
    public void ConfigureUserResolver_ShouldBeThreadSafe_WhenCalledConcurrently()
    {
        try
        {
            var resolver = FeishuJsonContext.Default;
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            System.Threading.Tasks.Parallel.For(0, 32, _ =>
            {
                try
                {
                    FeishuJsonDefaults.ConfigureUserResolver(resolver);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            exceptions.Should().BeEmpty();

            JsonSerializer
                .Serialize(new UncoveredUserPayload { Name = "concurrent" }, FeishuJsonDefaults.SerializerOptions)
                .Should().Contain("concurrent");
        }
        finally
        {
            FeishuJsonDefaults.Reset();
        }
    }

    /// <summary>
    /// 序列化选项与反序列化选项的 PropertyNameCaseInsensitive 必须一致（修复前两者不一致）。
    /// </summary>
    [Fact]
    public void SerializerAndDeserializerOptions_ShouldHaveConsistentCaseSensitivity()
    {
        try
        {
            FeishuJsonDefaults.SerializerOptions.PropertyNameCaseInsensitive
                .Should().Be(FeishuJsonDefaults.DeserializerOptions.PropertyNameCaseInsensitive);
        }
        finally
        {
            FeishuJsonDefaults.Reset();
        }
    }

    /// <summary>
    /// Reset 应清空用户 resolver 并恢复可序列化任意类型的默认状态。
    /// </summary>
    [Fact]
    public void Reset_ShouldRestoreUsableDefaults()
    {
        FeishuJsonDefaults.ConfigureUserResolver(FeishuJsonContext.Default);
        FeishuJsonDefaults.Reset();

        JsonSerializer
            .Serialize(new UncoveredUserPayload { Name = "after-reset" }, FeishuJsonDefaults.SerializerOptions)
            .Should().Contain("after-reset");
    }

    private sealed class UncoveredUserPayload
    {
        public string? Name { get; set; }

        public int Age { get; set; }
    }
}
