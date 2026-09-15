// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书多应用管理器的行为选项。
/// </summary>
public class FeishuAppOptions
{
    /// <summary>
    /// 配置节名称。
    /// </summary>
    public const string SectionName = "FeishuAppOptions";

    /// <summary>
    /// 是否启用应用配置热更新（ARC-1）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 启用后 <c>FeishuAppManager</c> 订阅 <c>IOptionsMonitor&lt;List&lt;FeishuAppConfig&gt;&gt;.OnChange</c>，
    /// 在 <c>appsettings.json</c> 变更时按 AppKey 计算差异并增量应用：
    /// 新增应用注册、更新应用重建上下文、删除应用移除、默认应用切换同步。
    /// 变更的应用会丢弃旧 <c>FeishuAppContext</c> 与其客户端实例（旧上下文延迟回收，
    /// 避免在途请求抛 <c>ObjectDisposedException</c>），令牌经由含 appKey 维度的存储键天然热迁移。
    /// </para>
    /// <para>
    /// <b>默认 <c>true</c></b>：热更新是正确性收益（避免误以为改了配置却仍走旧配置），
    /// 且本项目尚无需要保持的存量行为。如需回滚，显式置为 <c>false</c> 即可恢复「配置变更需重启」的旧语义。
    /// </para>
    /// <para>
    /// <b>注意</b>：应用自身的 <c>BaseAddress</c> / <c>Timeout</c> 由 <c>AddMudHttpClient</c> 的
    /// 命名客户端注册固化（组件 <c>IEnhancedHttpClientFactory.Invalidate</c> 只会清空客户端实例缓存，
    /// 不会改写已注册的 <c>HttpClient</c> 配置）。因此 <b>BaseUrl / TimeOut 的变更需要重建命名客户端才能生效</b>，
    /// 本开关对这类字段的生效范围以「重建应用上下文」为界。
    /// </para>
    /// </remarks>
    public bool EnableConfigReload { get; set; } = true;
}
