// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Redis.IntegrationTests;

/// <summary>
/// 需要真实 Redis（Testcontainers）才能执行的 <c>Fact</c>（T-R2-02）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么不用 <c>if (ShouldSkip()) return;</c></b>：方法体内早退会让 xUnit 把该用例记为
/// <b>passed</b>——"跳过即通过"使门禁的 <c>failed == 0</c> 断言在"全空跑"时也成立（假绿）。
/// 本特性在**测试发现期**设置 <see cref="FactAttribute.Skip"/>，TRX 中计为 <b>skipped</b>，
/// 从而让 <c>skipped == 0</c> 成为有效的"真的跑过"证明。
/// </para>
/// <para>门控条件：环境变量 <c>MUDFEISHU_REDIS_TESTS=1</c> 且本机 Docker 可用（见 <see cref="RedisFixture.ShouldRun"/>）。</para>
/// <para>不引入任何第三方跳过包，仅复用 xUnit 内建的 <see cref="FactAttribute.Skip"/>。</para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RedisFactAttribute : FactAttribute
{
    /// <summary>
    /// 初始化 <see cref="RedisFactAttribute"/>，未启用环境时置为显式跳过。
    /// </summary>
    public RedisFactAttribute()
    {
        if (!RedisFixture.ShouldRun)
        {
            Skip = "Redis 集成测试未启用：请设置环境变量 MUDFEISHU_REDIS_TESTS=1，并确保本机 Docker（Testcontainers）可用。";
        }
    }
}

/// <summary>
/// 需要真实 Redis（Testcontainers）才能执行的 <c>Theory</c>（见 <see cref="RedisFactAttribute"/> 的说明）。
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RedisTheoryAttribute : TheoryAttribute
{
    /// <summary>
    /// 初始化 <see cref="RedisTheoryAttribute"/>，未启用环境时置为显式跳过。
    /// </summary>
    public RedisTheoryAttribute()
    {
        if (!RedisFixture.ShouldRun)
        {
            Skip = "Redis 集成测试未启用：请设置环境变量 MUDFEISHU_REDIS_TESTS=1，并确保本机 Docker（Testcontainers）可用。";
        }
    }
}
