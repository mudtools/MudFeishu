// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;

/// <summary>
/// 任务功能支持在任务中扩充自定义字段，更清晰地添加任务关键信息，高效管理任务，辅助协作推进。
/// <para>任务的使用者可以在使用“任务截止时间”，“任务负责人”……等系统字段之外，自行定义如”优先级“，”项目发布日期“，”价格“等和使用场景密切相关的字段。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/task-v2/custom_field/custom-field-overview"/></para>
/// </summary> 
[HttpClientApi(TokenManage = nameof(IUserTokenManager), RegistryGroupName = "Task", InheritedFrom = nameof(FeishuV2TaskCustomFields))]
[Header(Consts.Authorization)]
public interface IFeishuUserV2TaskCustomFields : IFeishuV2TaskCustomFields
{
}
