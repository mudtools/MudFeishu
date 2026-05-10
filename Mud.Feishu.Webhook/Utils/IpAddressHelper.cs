// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Net;

namespace Mud.Feishu.Webhook;

/// <summary>
/// IP 地址验证工具类
/// 支持精确 IP 匹配和 CIDR 格式
/// </summary>
internal static class IpAddressHelper
{
    /// <summary>
    /// 检查 IP 地址是否在允许列表中
    /// </summary>
    /// <param name="ipAddress">要检查的 IP 地址</param>
    /// <param name="allowedIps">允许的 IP 地址列表（支持精确 IP 和 CIDR 格式）</param>
    /// <returns>如果在允许列表中返回 true，否则返回 false</returns>
    public static bool IsIpAllowed(string? ipAddress, HashSet<string> allowedIps)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return false;

        if (allowedIps == null || allowedIps.Count == 0)
            return false;

        // 标准化 IP 地址
        if (!IPAddress.TryParse(ipAddress, out var ip))
            return false;

        // 检查每个允许的规则
        foreach (var allowedIp in allowedIps)
        {
            if (IsIpInRange(ip, allowedIp))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 检查 IP 地址是否匹配指定规则
    /// </summary>
    /// <param name="ip">要检查的 IP 地址</param>
    /// <param name="rule">IP 规则（精确 IP 或 CIDR 格式）</param>
    /// <returns>如果匹配返回 true，否则返回 false</returns>
    public static bool IsIpInRange(IPAddress ip, string rule)
    {
        if (string.IsNullOrEmpty(rule))
            return false;

        // 检查是否为 CIDR 格式
        if (rule.Contains('/'))
        {
            return IsIpInCidrRange(ip, rule);
        }

        // 精确 IP 匹配
        if (IPAddress.TryParse(rule, out var allowedIp))
        {
            return ip.Equals(allowedIp);
        }

        return false;
    }

    /// <summary>
    /// 检查 IP 地址是否在 CIDR 范围内
    /// </summary>
    /// <param name="ip">要检查的 IP 地址</param>
    /// <param name="cidr">CIDR 格式（如 "192.168.1.0/24"）</param>
    /// <returns>如果在范围内返回 true，否则返回 false</returns>
    public static bool IsIpInCidrRange(IPAddress ip, string cidr)
    {
        if (string.IsNullOrEmpty(cidr))
            return false;

        var parts = cidr.Split('/');
        if (parts.Length != 2)
            return false;

        if (!IPAddress.TryParse(parts[0], out var networkAddress))
            return false;

        if (!int.TryParse(parts[1], out var prefixLength))
            return false;

        if (ip.AddressFamily != networkAddress.AddressFamily)
            return false;

        var networkBytes = networkAddress.GetAddressBytes();
        var ipBytes = ip.GetAddressBytes();
        var mask = CreateSubnetMask(prefixLength, networkBytes.Length);

        for (var i = 0; i < networkBytes.Length; i++)
        {
            if ((networkBytes[i] & mask[i]) != (ipBytes[i] & mask[i]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 创建子网掩码
    /// </summary>
    /// <param name="prefixLength">前缀长度</param>
    /// <param name="bytesLength">地址字节数（IPv4=4, IPv6=16）</param>
    /// <returns>子网掩码字节数组</returns>
    private static byte[] CreateSubnetMask(int prefixLength, int bytesLength)
    {
        var mask = new byte[bytesLength];
        for (var i = 0; i < mask.Length; i++)
        {
            if (prefixLength >= 8)
            {
                mask[i] = 0xff;
                prefixLength -= 8;
            }
            else if (prefixLength > 0)
            {
                mask[i] = (byte)(0xff << (8 - prefixLength));
                prefixLength = 0;
            }
            else
            {
                mask[i] = 0;
            }
        }
        return mask;
    }

    /// <summary>
    /// 解析 IP 地址字符串（支持 IPv4 和 IPv6）
    /// </summary>
    /// <param name="ipString">IP 地址字符串</param>
    /// <returns>解析后的 IPAddress 对象，失败返回 null</returns>
    public static IPAddress? ParseIpAddress(string ipString)
    {
        if (string.IsNullOrEmpty(ipString))
            return null;

        if (IPAddress.TryParse(ipString, out var ipAddress))
            return ipAddress;

        return null;
    }

    /// <summary>
    /// 规范化 IP 地址字符串
    /// </summary>
    /// <param name="ipString">IP 地址字符串</param>
    /// <returns>规范化后的 IP 地址字符串</returns>
    public static string NormalizeIpAddress(string ipString)
    {
        var ip = ParseIpAddress(ipString);
        return ip?.ToString() ?? ipString;
    }
}
