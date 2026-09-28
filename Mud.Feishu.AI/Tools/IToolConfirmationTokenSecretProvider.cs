// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 确认令牌签名密钥提供者（T4-2 / 决策 D-3）：HITL「真挂起」的无状态确认令牌所需的宿主签名密钥。
/// </summary>
/// <remarks>
/// <para>
/// <b>SDK 不新造密钥管理</b>：密钥的生成、存储与轮换全部是宿主职责（与
/// <see cref="IToolExecutionAuthorizer"/> 同款定位——SDK 只定契约）。宿主实现本接口并注册到 DI
/// （<see cref="Mud.Feishu.AI.FeishuTools.FeishuToolBinding"/> 构造注入）；<b>未注册或返回
/// <see langword="null"/>/空白 = 确认令牌能力降级为不可用</b>——
/// <c>needs_confirmation</c> 退化为纯提示文案（与既有行为一致）。
/// </para>
/// <para>
/// 令牌与 <c>toolName + 参数摘要 + appKey + userId</c> 绑定（HMAC-SHA256），换参数/换应用/换用户
/// 即失效——模型无法把 A 场景的批准搬到 B 场景。有效期默认 10 分钟（签发时写入令牌）。
/// </para>
/// </remarks>
public interface IToolConfirmationTokenSecretProvider
{
    /// <summary>返回宿主签名密钥（<see langword="null"/> 或空白 = 未配置，确认令牌能力降级）。</summary>
    string? GetSecret();
}
