// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Mud.Feishu.DataModels;
using Xunit;

namespace Mud.Feishu.Tests;

/// <summary>
/// AOT 覆盖架构约束测试：防止 [HttpJsonSerializable] 被误加回开放泛型（AOT-2 复发防护）。
/// </summary>
/// <remarks>
/// 开放泛型无法被 <c>JsonSerializerContext</c> 覆盖（STJ 报 SYSLIB1030「未生成序列化元数据」），
/// 标注后既无序列化价值，又会持续产生 AOT006。该约束由测试守护，
/// 因为脚手架重新生成不会自动移除源码中的标注。
/// </remarks>
public class AotJsonSerializableCoverageTests
{
    [Fact]
    public void NoOpenGenericType_ShouldBeAnnotatedWithHttpJsonSerializable()
    {
        var offenders = typeof(PageRequest).Assembly
            .GetTypes()
            .Where(t => t.IsGenericTypeDefinition)
            .Where(HasHttpJsonSerializable)
            .Select(t => t.FullName ?? t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        offenders.Should().BeEmpty(
            "开放泛型不能被 JsonSerializerContext 覆盖，标注 [HttpJsonSerializable] 只会产生 AOT006 且无序列化收益。违规类型：{0}",
            string.Join(", ", offenders));
    }

    private static bool HasHttpJsonSerializable(Type type) =>
        type.GetCustomAttributes(inherit: false)
            .Any(a => a.GetType().Name == "HttpJsonSerializableAttribute");
}
