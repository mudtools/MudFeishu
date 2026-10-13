// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Generator.Tests;

/// <summary>
/// 仓库根目录定位器（源码级守卫类用例扫描生产源码时使用）。
/// </summary>
/// <remarks>
/// v2.3 P1-B：随引擎迁入。锚点用 <c>Mud.Feishu.slnx</c>（与仓内 <c>ContractGuards</c>
/// 系列的 <c>FindRepositoryRoot()</c> 同判据）；从测试二进制目录逐级上溯，
/// 与执行位置（<c>dotnet test</c> / IDE / <c>vstest</c> 各自的工作目录不同）解耦。
/// </remarks>
internal static class TestRepoRoot
{
    private static readonly string Root = Locate();

    /// <summary>仓库根目录绝对路径。</summary>
    public static string Value => Root;

    /// <summary>把若干相对段拼成仓库根下的绝对路径（段内已含分隔符亦可，逐段拼接）。</summary>
    public static string PathOf(params string[] segments)
    {
        var path = Root;
        foreach (var segment in segments)
        {
            path = Path.Combine(path, segment);
        }

        return path;
    }

    private static string Locate()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "Mud.Feishu.slnx")))
            {
                return directory;
            }

            directory = Path.GetDirectoryName(directory);
        }

        throw new InvalidOperationException(
            $"无法定位仓库根目录（未在任何上级目录找到 Mud.Feishu.slnx）；起点：{AppContext.BaseDirectory}");
    }
}
