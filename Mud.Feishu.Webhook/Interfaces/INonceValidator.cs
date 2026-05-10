// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook;

/// <summary>
/// 飞书事件 Nonce 验证器接口
/// 负责防重放攻击的 Nonce 去重验证，支持多应用场景下的隔离
/// </summary>
public interface INonceValidator
{
    /// <summary>
    /// 设置当前应用键（多应用场景）
    /// </summary>
    /// <param name="appKey">应用键，用于多应用场景下的 Nonce 隔离</param>
    void SetCurrentAppKey(string appKey);

    /// <summary>
    /// 检查并标记 Nonce 为已使用
    /// </summary>
    /// <param name="nonce">随机数</param>
    /// <returns>如果 Nonce 已被使用返回 true，否则标记为已使用并返回 false</returns>
    /// <remarks>
    /// 此方法用于防重放攻击：
    /// - 返回 true 表示 Nonce 已被使用过（检测到重放攻击）
    /// - 返回 false 表示 Nonce 未被使用，并已成功标记为已使用
    /// - 支持多应用场景下的 Nonce 隔离
    /// </remarks>
    Task<bool> TryMarkNonceAsUsedAsync(string nonce);

    /// <summary>
    /// 验证 Nonce 是否有效（未被使用且不为空）
    /// </summary>
    /// <param name="nonce">随机数</param>
    /// <param name="isProductionEnvironment">是否为生产环境，默认为 true</param>
    /// <returns>如果 Nonce 有效返回 true，否则返回 false</returns>
    /// <remarks>
    /// 验证规则：
    /// - 生产环境：Nonce 不能为空，且不能已被使用
    /// - 开发环境：允许 Nonce 为空（兼容性考虑，但会记录警告）
    /// - 所有环境：已使用的 Nonce 都会被拒绝
    /// </remarks>
    Task<bool> ValidateNonceAsync(string nonce, bool isProductionEnvironment = true);
}