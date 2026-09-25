// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 猎头供应商管理员
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class AgencySupplierAdmin
{
    /// <summary>
    /// <para>管理员 ID，与入参 user_id_type 类型一致</para>
    /// <para>必填：否</para>
    /// <para>示例值：7398493486516799788</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>管理员名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public I18nName? Name { get; set; }

    /// <summary>
    /// <para>管理员邮箱（需 hire:agency.email:readonly 字段权限）</para>
    /// <para>必填：否</para>
    /// <para>示例值：283xxxx2171813@qq.com</para>
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}
