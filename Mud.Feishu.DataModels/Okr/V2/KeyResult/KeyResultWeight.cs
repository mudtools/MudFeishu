// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.OkrV2;

/// <summary>
/// OKR v2 关键结果权重项
/// </summary>
public class KeyResultWeight
{
    /// <summary>
    /// <para>关键结果 id（必填），长度 1 ~ 20 字符</para>
    /// <para>必填：是</para>
    /// <para>示例值：7342342398472398473</para>
    /// </summary>
    [JsonPropertyName("key_result_id")]
    public string KeyResultId { get; set; } = string.Empty;

    /// <summary>
    /// <para>关键结果权重（必填），取值范围 [0,1]，保留三位小数</para>
    /// <para>必填：是</para>
    /// <para>示例值：0.5</para>
    /// </summary>
    [JsonPropertyName("weight")]
    public double Weight { get; set; }
}
