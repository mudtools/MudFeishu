// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AI;

/// <summary>
/// <para>身份证信息</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "AI")]
public class IdCardRecognize
{
    /// <summary>
    /// <para>识别的实体列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("entities")]
    public IdEntity[]? Entities { get; set; }

    /// <summary>
    /// <para>正反面，1为身份证-姓名页，0为身份证-国徽页</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("side")]
    public int? Side { get; set; }

    /// <summary>
    /// <para>四角坐标[x0,y0,x1,y1,x2,y2,x3,y3]</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("conners")]
    public int[]? Conners { get; set; }
}
