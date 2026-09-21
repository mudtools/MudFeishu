// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using FluentAssertions;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// 源码规范守卫（P2-1）：模块内所有 <c>.cs</c> 文件必须以版权头作为第 1 行。
/// </summary>
/// <remarks>
/// 此前 <c>ServiceCollectionExtensions.cs</c> / <c>IFeishuWebSocketClient.cs</c> /
/// <c>FeishuWebSocketMessage.cs</c> 因文件开头存在空行，版权头被下移到第 2/3 行。
/// 本用例作为回归守卫，防止再次漂移。
/// </remarks>
public class SourceConventionTests
{
    private const string HeaderPrefix = "// ---";
    private const string AuthorMarker = "作者：Mud Studio";

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Mud.Feishu.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"未能从 {AppContext.BaseDirectory} 向上定位仓库根目录（Mud.Feishu.slnx）");
    }

    private static IEnumerable<string> EnumerateSourceFiles(string sourceRoot)
    {
        var separator = Path.DirectorySeparatorChar;

        return Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{separator}obj{separator}", StringComparison.OrdinalIgnoreCase)
                        && !path.Contains($"{separator}bin{separator}", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AllSourceFiles_ShouldStartWithCopyrightHeader()
    {
        // Arrange
        var repositoryRoot = FindRepositoryRoot();
        var sourceRoot = Path.Combine(repositoryRoot, "Mud.Feishu.WebSocket");
        var files = EnumerateSourceFiles(sourceRoot).ToList();

        files.Should().NotBeEmpty("应能在仓库中找到 WebSocket 模块的源码文件");

        // Act
        var offenders = new List<string>();
        foreach (var file in files)
        {
            using var reader = new StreamReader(file, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var firstLine = reader.ReadLine();

            if (firstLine == null || !firstLine.StartsWith(HeaderPrefix, StringComparison.Ordinal))
            {
                offenders.Add(Path.GetRelativePath(repositoryRoot, file));
            }
        }

        // Assert
        offenders.Should().BeEmpty(
            $"以下文件的第 1 行不是版权头（不得以空行开头）：{string.Join("、", offenders)}");
    }

    [Fact]
    public void AllSourceFiles_ShouldContainAuthorMarkerInHeader()
    {
        // Arrange
        var repositoryRoot = FindRepositoryRoot();
        var sourceRoot = Path.Combine(repositoryRoot, "Mud.Feishu.WebSocket");

        // Act
        var offenders = new List<string>();
        foreach (var file in EnumerateSourceFiles(sourceRoot))
        {
            var header = new StringBuilder();
            using (var reader = new StreamReader(file, Encoding.UTF8))
            {
                for (var i = 0; i < 6 && !reader.EndOfStream; i++)
                {
                    header.AppendLine(reader.ReadLine());
                }
            }

            if (!header.ToString().Contains(AuthorMarker, StringComparison.Ordinal))
            {
                offenders.Add(Path.GetRelativePath(repositoryRoot, file));
            }
        }

        // Assert
        offenders.Should().BeEmpty(
            $"以下文件缺少 6 行版权头（作者标识）：{string.Join("、", offenders)}");
    }
}
