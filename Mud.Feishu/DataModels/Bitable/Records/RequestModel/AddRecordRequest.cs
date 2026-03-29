// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// 新增记录请求体
/// </summary>
public class AddRecordRequest
{
    /// <summary>
    /// <para>要新增的记录的数据。需先指定数据表中的字段（即指定列），再传入正确格式的数据作为一条记录。</para>
    /// <para>**注意**：</para>
    /// <para>该接口支持的字段类型及其描述如下所示：</para>
    /// <para>- 文本： 填写字符串格式的值</para>
    /// <para>- 数字：填写数字格式的值</para>
    /// <para>- 单选：填写选项值，对于新的选项值，将会创建一个新的选项</para>
    /// <para>- 多选：填写多个选项值，对于新的选项值，将会创建一个新的选项。如果填写多个相同的新选项值，将会创建多个相同的选项</para>
    /// <para>- 日期：填写毫秒级时间戳</para>
    /// <para>- 复选框：填写 true 或 false</para>
    /// <para>- 条码</para>
    /// <para>- 人员：填写用户的[open_id](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-openid)、[union_id](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-union-id) 或 [user_id](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-user-id)，类型需要与 user_id_type 指定的类型一致</para>
    /// <para>- 电话号码：填写文本内容</para>
    /// <para>- 超链接：参考以下示例，text 为文本值，link 为 URL 链接</para>
    /// <para>- 附件：填写附件 token，需要先调用[上传素材](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/media/upload_all)或[分片上传素材](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/media/upload_prepare)接口将附件上传至该多维表格中</para>
    /// <para>- 单向关联：填写被关联表的记录 ID</para>
    /// <para>- 双向关联：填写被关联表的记录 ID</para>
    /// <para>- 地理位置：填写经纬度坐标</para>
    /// <para>不同类型字段的数据结构请参考[多维表格记录数据结构](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-table-record/bitable-record-data-structure-overview)。</para>
    /// <para>必填：是</para>
    /// <para>示例值：{ "人员": [ { "id": "ou_2910013f1e6456f16a0ce75ede9abcef" } ] }</para>
    /// </summary>
    [JsonPropertyName("fields")]
    public object Fields { get; set; } = new();
}