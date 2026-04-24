// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Bitable;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// <para>自动化流程 workflows是用户给多维表格设定的自动运行规则。设定“触发条件”和“执行操作”以后，多维表格会根据数据变更，自动执行下一步操作。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/docs/bitable-v1/app-workflow/list"/></para>
/// </summary>
[HttpClientApi(RegistryGroupName = "Bitable", TokenManage = nameof(IFeishuAppManager), InheritedFrom = nameof(FeishuV1BitableWorkflow))]
[Token("UserAccessToken", Name = Consts.Authorization)]
public interface IFeishuUserV1BitableWorkflow : IFeishuV1BitableWorkflow, ICurrentUserId
{

}