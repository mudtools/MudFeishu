// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Authentication;

internal static class SensitiveDataUtils
{
    public static string MaskSensitiveInfo(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "N/A";
        }

        if (value!.Length <= 6)
        {
            return "***";
        }

        return $"{value.Substring(0, 2)}***{value.Substring(value.Length - 2)}";
    }
}
