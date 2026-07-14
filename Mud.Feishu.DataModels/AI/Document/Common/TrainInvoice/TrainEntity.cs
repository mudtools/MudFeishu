// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AI;

/// <summary>
/// <para>识别出的实体列表</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "AI")]
public class TrainEntity
{
    /// <summary>
    /// <para>识别的字段种类</para>
    /// <para>必填：否</para>
    /// <para>示例值：end_station</para>
    /// <para>可选值：<list type="bullet">
    /// <item>start_station：出发站</item>
    /// <item>end_station：到达站</item>
    /// <item>train_num：车次编号</item>
    /// <item>name：火车票姓名</item>
    /// <item>seat_num：座位号</item>
    /// <item>ticket_num：车票编号</item>
    /// <item>total_amount：价格</item>
    /// <item>time：出发时间</item>
    /// <item>price：金额</item>
    /// <item>seat_num：座位号</item>
    /// <item>seat_cls：座位类型</item>
    /// <item>id_num：身份证号</item>
    /// <item>sale_num：售卖号</item>
    /// <item>sale_station：售卖车站</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>识别出字段的文本信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：长沙南</para>
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
