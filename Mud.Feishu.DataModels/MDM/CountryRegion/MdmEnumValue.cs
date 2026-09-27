// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.MDM;

/// <summary>
/// 主数据枚举值（enum）：value 为枚举编码，multilingual_name 为枚举的多语言名称
/// </summary>
[HttpJsonSerializable(SerializerClassName = "MDM")]
public class MdmEnumValue
{
    /// <summary>
    /// <para>枚举编码，如所属大洲：1（亚洲）/ 2（欧洲）/ 3（非洲）/ 4（北美洲）/ 5（南美洲）/ 6（大洋洲）/ 7（南极洲）</para>
    /// <para>示例值：2</para>
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    /// <summary>
    /// <para>枚举的多语言名称（键为语言代码，如 zh-CN / en-US）</para>
    /// </summary>
    [JsonPropertyName("multilingual_name")]
    public Dictionary<string, string>? MultilingualName { get; set; }
}
