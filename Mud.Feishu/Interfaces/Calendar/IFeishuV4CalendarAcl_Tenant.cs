// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;


/// <summary>
/// 访问控制（ACL）用于管理日历的成员权限。一个日历内可以创建多个 ACL，每一个 ACL 内可以为一个成员设置日历的访问权限，其中访问权限包括：
/// <para>
/// <list type="bullet">
/// <item>游客，只能看到日历日程忙闲信息</item>
/// <item>订阅者：可查看日历内的所有日程详情。</item>
/// <item>编辑者：可在日历内创建或修改日程。</item>
/// <item>管理员：可管理日历及共享设置。</item>
/// </list>
/// </para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar-acl/introduction"/></para>
/// </summary>
[HttpClientApi(RegistryGroupName = "Calendar", TokenManage = nameof(IFeishuAppManager), InheritedFrom = nameof(FeishuV4CalendarAcl))]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV4CalendarAcl : IFeishuV4CalendarAcl
{

}