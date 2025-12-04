// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Task;


/// <summary>
/// <para>任务所在清单的信息</para>
/// </summary>
public class TaskInTaskListInfo
{
    /// <summary>
    /// <para>指定在某个清单中创建任务，需要该清单的可编辑权限。不填写表示不在清单中创建任务。</para>
    /// <para>必填：否</para>
    /// <para>示例值：cc371766-6584-cf50-a222-c22cd9055004</para>
    /// <para>最大长度：100</para>
    /// </summary>
    [JsonPropertyName("tasklist_guid")]
    public string? TasklistGuid { get; set; }

    /// <summary>
    /// <para>清单中自定义分组的GUID，用于指定在某个清单中创建任务的同时把任务加入到某个具体的分组中。如果填写了清单的GUID，却没填写分组的GUID，则自动加入该清单的默认分组中。</para>
    /// <para>必填：否</para>
    /// <para>示例值：e6e37dcc-f75a-5936-f589-12fb4b5c80c2</para>
    /// </summary>
    [JsonPropertyName("section_guid")]
    public string? SectionGuid { get; set; }
}