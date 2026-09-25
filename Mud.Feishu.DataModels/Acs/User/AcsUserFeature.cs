// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Acs;

/// <summary>
/// 智能门禁用户特征（卡号与人脸录入状态）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Acs")]
public class AcsUserFeature
{
    /// <summary>
    /// <para>卡号（韦根协议门禁系统在人脸识别成功后输出的用户卡号）</para>
    /// <para>必填：否</para>
    /// <para>示例值：123456</para>
    /// </summary>
    [JsonPropertyName("card")]
    public int? Card { get; set; }

    /// <summary>
    /// <para>是否已上传人脸图片</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("face_uploaded")]
    public bool? FaceUploaded { get; set; }
}
