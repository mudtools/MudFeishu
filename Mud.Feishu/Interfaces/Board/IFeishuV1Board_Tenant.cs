// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;

/// <summary>
/// 画板是全新的图形创作工具，使用门槛低、简洁高效且协作方便，能用画板轻松画出好看的流程图、规划图和方案图，并且可以和团队一起在画板上进行实时的图形化协作。
/// <para>通过画板 API，可以让画板接入内部业务系统，让画板成为业务流程的一部分。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/docs/board-v1/overview"/></para>
/// </summary>
[HttpClientApi(RegistryGroupName = "Drive", TokenManage = nameof(IFeishuAppManager), InheritedFrom = nameof(FeishuV1Board))]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuTenantV1Board : IFeishuV1Board
{

}