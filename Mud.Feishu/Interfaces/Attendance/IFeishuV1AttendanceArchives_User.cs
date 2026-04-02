// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;


/// <summary>
/// 归档报表用于对应对应后台假勤管理-考勤统计-报表-归档报表功能。
/// <para>归档报表支持引用系统报表，可设置归档时间和数据归档周期，并且支持根据部门/人员、国家/地区、人员类型、工作地点、职级、序列、职务进行人员圈选。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/attendance-v1/user_task_remedy/create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Attendance", InheritedFrom = nameof(FeishuV1AttendanceArchives))]
[Token(TokenType.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1AttendanceArchives : IFeishuV1AttendanceArchives, ICurrentUserId
{

}
