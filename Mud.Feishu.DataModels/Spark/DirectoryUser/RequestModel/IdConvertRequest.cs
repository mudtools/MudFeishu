// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spark;

/// <summary>
/// 转换飞书妙搭与飞书开放平台之间的用户 ID 请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spark")]
public class IdConvertRequest
{
    /// <summary>
    /// <para>ID 转换类型。取 10/11/40 时 ids 须为妙搭用户 ID；取 20 时须为开放平台 OpenID；取 21 时须为开放平台 UnionID</para>
    /// <para>必填：是</para>
    /// <para>示例值：10</para>
    /// <para>可选值：<list type="bullet">
    /// <item>10：妙搭用户 ID → 开放平台 OpenID</item>
    /// <item>11：妙搭用户 ID → 开放平台 UnionID</item>
    /// <item>20：开放平台 OpenID → 妙搭用户 ID</item>
    /// <item>21：开放平台 UnionID → 妙搭用户 ID</item>
    /// <item>40：妙搭用户 ID → 飞书用户 ID</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("id_convert_type")]
    public int IdConvertType { get; set; }

    /// <summary>
    /// <para>待转换的 ID 列表，长度范围 1～100，为空时不返回结果</para>
    /// <para>必填：否</para>
    /// <para>示例值：["123456789837364"]</para>
    /// </summary>
    [JsonPropertyName("ids")]
    public string[]? Ids { get; set; }
}
