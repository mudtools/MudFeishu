// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Utilities;

/// <summary>
/// Token 工具类
/// 提供 Token 格式化和处理功能
/// </summary>
[Obsolete("此类在生产代码中未被使用，将在未来版本中移除。")]
public static class TokenUtils
{
    /// <summary>
    /// 格式化 Bearer Token
    /// 如果 token 已包含 "Bearer " 前缀，则不再添加
    /// </summary>
    /// <param name="token">原始 Token</param>
    /// <returns>格式化后的 Bearer Token</returns>
    public static string FormatBearerToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return "Bearer ";

        return token!.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? token
            : $"Bearer {token}";
    }

    /// <summary>
    /// 移除 Bearer 前缀，获取纯 Token 值
    /// </summary>
    /// <param name="bearerToken">Bearer Token</param>
    /// <returns>纯 Token 值（不包含 Bearer 前缀）</returns>
    public static string RemoveBearerPrefix(string? bearerToken)
    {
        if (string.IsNullOrEmpty(bearerToken))
            return string.Empty;

        return bearerToken!.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? bearerToken.Substring(7)
            : bearerToken;
    }
}
