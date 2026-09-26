// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Lingo;

/// <summary>
/// 模糊搜索词条响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Lingo")]
public class SearchEntityResult
{
    /// <summary>
    /// <para>搜索结果</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("entities")]
    public LingoEntity[]? Entities { get; set; }

    /// <summary>
    /// <para>分页标记，当还有下一页时会返回新的 page_token，否则 page_token 为空</para>
    /// <para>必填：否</para>
    /// <para>示例值：b152fa6e6f62a291019a04c3a93f365f8ac641910506ff15ff4cad6534e087cb4ed8fa2c</para>
    /// </summary>
    [JsonPropertyName("page_token")]
    public string? PageToken { get; set; }

    /// <summary>
    /// <para>是否有下一页</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("has_more")]
    public bool? HasMore { get; set; }
}
