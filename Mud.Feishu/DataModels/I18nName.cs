// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace Mud.Feishu.DataModels;

/// <summary>
/// 国际化名称类，用于存储多种语言的名称表示
/// </summary>
public class I18nName
{
    /// <summary>
    /// 中文名称 (简体中文)
    /// </summary>
    [JsonPropertyName("zh_cn")]
    public string? ZhCn { get; set; }

    /// <summary>
    /// 日文名称
    /// </summary>
    [JsonPropertyName("ja_jp")]
    public string? JaJp { get; set; }

    /// <summary>
    /// 英文名称 (美式英语)
    /// </summary>
    [JsonPropertyName("en_us")]
    public string? EnUs { get; set; }
}