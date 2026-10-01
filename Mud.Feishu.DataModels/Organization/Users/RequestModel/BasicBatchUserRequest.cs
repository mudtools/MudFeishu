// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 批量获取用户基础信息请求体
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/contact-v3/user/basic_batch"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Organization")]
public class BasicBatchUserRequest
{
    /// <summary>
    /// <para>待查询的用户 ID 列表，ID 类型需与查询参数 user_id_type 的取值保持一致。</para>
    /// <para>必填：是</para>
    /// <para>示例值：["ou_7dab8a3d3cdcc9da365777c7ad5abcef"]</para>
    /// </summary>
    [JsonPropertyName("user_ids")]
    public string[] UserIds { get; set; } = [];
}
