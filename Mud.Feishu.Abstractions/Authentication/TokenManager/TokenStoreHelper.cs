// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Authentication;

/// <summary>
/// 令牌存储编解码工具类
/// </summary>
/// <remarks>
/// 提供令牌值在持久化存储时的编码和解码功能，
/// 将过期时间戳与令牌值合并编码为 {expireTimestampMs}|{token} 格式，
/// 以便从存储中恢复时判断令牌是否仍然有效。
/// </remarks>
internal static class TokenStoreHelper
{
    public static string EncodeStoredToken(string token, long expireTimestampMs)
        => $"{expireTimestampMs}|{token}";

    public static (string Token, long ExpireTimestampMs) DecodeStoredToken(string storedValue)
    {
        var separatorIndex = storedValue.IndexOf('|');
        if (separatorIndex > 0 && long.TryParse(storedValue.Substring(0, separatorIndex), out var expireMs))
        {
            return (storedValue.Substring(separatorIndex + 1), expireMs);
        }

        return (storedValue, 0);
    }
}
