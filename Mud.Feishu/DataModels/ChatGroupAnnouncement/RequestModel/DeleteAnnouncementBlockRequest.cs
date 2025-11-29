// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroupNotice;

/// <summary>
/// 删除群公告中的块 请求体
/// <para>指定需要操作的块，删除其指定范围的子块。如果操作成功，接口将返回应用删除操作后的群公告版本号。</para>
/// </summary>
public class DeleteAnnouncementBlockRequest
{
    /// <summary>
    /// <para>删除的起始索引（操作区间左闭右开）</para>
    /// <para>必填：是</para>
    /// <para>示例值：0</para>
    /// <para>最小值：0</para>
    /// </summary>
    [JsonPropertyName("start_index")]
    public int StartIndex { get; set; }

    /// <summary>
    /// <para>删除的末尾索引（操作区间左闭右开）</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>最小值：1</para>
    /// </summary>
    [JsonPropertyName("end_index")]
    public int EndIndex { get; set; }
}
