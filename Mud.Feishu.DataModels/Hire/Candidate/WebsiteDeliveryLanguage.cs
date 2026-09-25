// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 官网投递语言能力（简历信息子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class WebsiteDeliveryLanguage
{
    /// <summary>
    /// <para>自定义字段</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_data")]
    public WebsiteDeliveryCustomizedData[]? CustomizedData { get; set; }

    /// <summary>
    /// <para>语言：1 英语 / 2 法语 / 3 日语 / 4 韩语 / 5 德语 / 6 俄语 / 7 西班牙语 / 8 葡萄牙语 / 9 阿拉伯语 / 10 印地语 / 11 印度斯坦语 / 12 孟加拉语 / 13 豪萨语 / 14 旁遮普语 / 15 波斯语 / 16 斯瓦希里语 / 17 泰卢固语 / 18 土耳其语 / 19 意大利语 / 20 爪哇语 / 21 泰米尔语 / 22 马拉地语 / 23 越南语 / 24 普通话 / 25 粤语</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("language")]
    public int? Language { get; set; }

    /// <summary>
    /// <para>熟悉程度：1 入门 / 2 日常交流 / 3 商务会议 / 4 无障碍沟通 / 5 母语</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("proficiency")]
    public int? Proficiency { get; set; }
}
