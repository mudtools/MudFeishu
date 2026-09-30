// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.HelpDesk;

/// <summary>
/// 提交审批响应体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "HelpDesk")]
public class SubmitApproveNotificationResult
{
    /// <summary>
    /// <para>是否有创建或管理审批流程的权限范围。以下两种情况下用户没有该权限范围：
    /// 1. 用户未安装服务台小组件（<see href="https://app.feishu.cn/app/cli_9f9f8825d53b900d"/>）；
    /// 2. 用户安装的服务台小组件版本过低。</para>
    /// <para>必填：否</para>
    /// <para>**示例值**：true</para>
    /// </summary>
    [JsonPropertyName("has_access")]
    public bool? HasAccess { get; set; }
}
