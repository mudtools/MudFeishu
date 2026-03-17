// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 飞书服务配置内部状态
/// </summary>
internal class FeishuServiceConfiguration
{
    public bool OrganizationApiAdded { get; set; }
    public bool MessageApiAdded { get; set; }
    public bool ChatGroupApiAdded { get; set; }
    public bool ApprovalApiAdded { get; set; }
    public bool CardApiAdded { get; set; }
    public bool TaskApiAdded { get; set; }
    public bool AuthenticationApiAdded { get; set; }
    public bool AttendanceAdded { get; set; }
    public bool DriveApiAdded { get; set; }
    public bool WikeApiAdded { get; set; }

    /// <summary>
    /// 检查是否添加了任何服务
    /// </summary>
    /// <returns>是否添加了服务</returns>
    public bool HasAnyService()
    {
        return
               OrganizationApiAdded ||
               MessageApiAdded ||
               ChatGroupApiAdded ||
               AuthenticationApiAdded ||
               AttendanceAdded ||
               TaskApiAdded ||
               CardApiAdded ||
               ApprovalApiAdded ||
               WikeApiAdded ||
               DriveApiAdded;
    }
}
