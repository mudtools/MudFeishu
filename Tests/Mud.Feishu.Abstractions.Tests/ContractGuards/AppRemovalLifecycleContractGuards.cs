// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;

namespace Mud.Feishu.Abstractions.Tests.ContractGuards;

/// <summary>
/// 应用移除生命周期与热更新失败隔离的契约守卫。
/// </summary>
/// <remarks>
/// 回归事实：<c>RemoveApp</c> 曾在 <c>_registryLock</c> 外摘除懒加载/基类注册表且不写配置快照——
/// 与热更新的差异比对并发时，被移除的应用会被同一次（或下一次）提交静默复活；
/// 其 fire-and-forget 清库也无世代校验，移除后立即 <c>AddApp</c> 时迟到的清库会删掉新令牌。
/// 行为回归由 <c>MultiAppHotReloadTests</c> / <c>FeishuAppManagerTests</c> 覆盖，本守卫锁实现结构，
/// 防止后续重构把原子段拆散或重新引入无隔离的提交循环。
/// </remarks>
public class AppRemovalLifecycleContractGuards
{
    [Fact]
    public void RemoveAppCore_ShouldPerformRegistryMutationAtomically_AndGuardPurgeByGeneration()
    {
        var content = ReadProductionSource();

        content.Should().Contain("RemoveAppCore(appKey, tombstone: true)",
            "public RemoveApp 必须走统一核心，并由墓碑标记其为显式移除");
        content.Should().Contain("RemoveAppCore(removedKey, tombstone: false)",
            "配置驱动的下线不得写墓碑（否则配置源重新声明时会被永久封禁）");
        content.Should().NotContain("RemoveApp(removedKey)",
            "热更新删除循环不得调用 public RemoveApp——它会写入墓碑，使配置源重新声明时永久无法复活");

        content.Should().Contain("_removedAppKeys.Add(appKey)",
            "显式移除必须写墓碑，阻止先于移除开始、晚于移除提交的热更新把应用复活");
        content.Should().Contain("BumpGenerationLocked(appKey)",
            "移除必须递增世代，使迟到的清库可识别 ABA");
        content.Should().Contain("IsRemovalPurgeStillValid(appKey, removedGeneration)",
            "移除路径的异步清库必须先做世代校验，避免删除重新添加后的新令牌");
        content.Should().Contain("TokenStorePurgeGate.Mark(appKey)",
            "移除清库必须在注册表锁内打门，使重新添加的上下文在恢复令牌时看到门");
        content.Should().Contain("_removedAppKeys.RemoveWhere",
            "配置源正式下线某键时必须撤销其墓碑，否则该键无法再次注册");
    }

    [Fact]
    public void HotReloadCommit_ShouldIsolatePerAppFailures_AndKeepSnapshotConsistent()
    {
        var content = ReadProductionSource();

        content.Should().Contain("failedAdds",
            "新增失败必须逐应用记账，且失败键不得写入配置快照（否则节流会让它永不重试）");
        content.Should().Contain("failedRemovals",
            "移除失败必须保留原快照条目（应用仍在注册表，快照丢失会让后续热更新误判其已下线）");
        content.Should().Contain("IsTombstoned(config.AppKey)",
            "热更新新增循环必须跳过墓碑键");
        content.Should().Contain("!failedAdds.Contains(c.AppKey) && !_removedAppKeys.Contains(c.AppKey)",
            "快照组装必须同时剔除失败的新增键与墓碑键");
    }

    [Fact]
    public void Constructor_ShouldRejectDuplicateKeys_AndMultipleDefaults()
    {
        var content = ReadProductionSource();

        content.Should().Contain("应用配置中存在重复的 AppKey",
            "三入口校验统一：构造期必须与 AddApp / 热更新一样硬失败重复 AppKey");
        content.Should().Contain("只允许一个默认应用",
            "多个 IsDefault 会让默认应用随注册顺序漂移，构造期必须硬失败");
    }

    private static string ReadProductionSource()
    {
        var solutionRoot = GetSolutionRoot();
        solutionRoot.Should().NotBeNull("找不到解决方案根目录时本守卫无法工作（宁可失败，也不要假绿）");

        var path = Directory
            .GetFiles(solutionRoot!, "FeishuAppManager.cs", SearchOption.AllDirectories)
            .FirstOrDefault(p => !IsExcluded(p));

        path.Should().NotBeNull("FeishuAppManager.cs 应存在于生产源码中");
        return File.ReadAllText(path!);
    }

    private static bool IsExcluded(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}Tests{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}Demos{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    private static string? GetSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir) && !File.Exists(Path.Combine(dir, "Mud.Feishu.slnx")))
        {
            dir = Path.GetDirectoryName(dir);
        }
        return dir;
    }
}
