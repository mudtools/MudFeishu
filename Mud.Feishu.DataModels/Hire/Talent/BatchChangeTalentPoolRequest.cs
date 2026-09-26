// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 批量加入/移除人才库中人才请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class BatchChangeTalentPoolRequest
{
    /// <summary>
    /// <para>人才 ID 列表，长度范围 1～50；不存在的 ID 报错返回，已在/不在库中则静默处理</para>
    /// <para>必填：是</para>
    /// <para>示例值：["6930815272790114324"]</para>
    /// </summary>
    [JsonPropertyName("talent_id_list")]
    public string[]? TalentIdList { get; set; }

    /// <summary>
    /// <para>操作类型：1 将人才添加至指定人才库 / 2 将人才从指定人才库中移除</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("option_type")]
    public int? OptionType { get; set; }
}
