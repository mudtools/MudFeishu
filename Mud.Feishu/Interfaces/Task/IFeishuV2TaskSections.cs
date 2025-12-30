// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;

/// <summary>
/// 飞书自定义分组可以方便的在我负责的和清单中对任务进行自定义归类。通过自定义分组。
/// <para>可以：<list type="bullet">按状态分组，待启动-进行中-已完成
/// <item>按优先级分组，P0-重要且紧急，P1-重要但不紧急，...</item>
/// <item>按类别分组，市场相关、人事相关，...</item></list></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(ITokenManager), IsAbstract = true)]
[Header(Consts.Authorization)]
public interface IFeishuV2TaskSections
{
}
