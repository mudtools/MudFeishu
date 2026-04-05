// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;

/// <summary>
/// 文件版本是基于文件生成的新版本，版本依附于文件而存在。飞书开放平台支持基于在线文档和电子表格创建、删除和获取版本信息。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/drive-v1/file-version/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Drive", InheritedFrom = nameof(FeishuV1DriveFilesVersions))]
[Token("UserAccessToken", Name = Consts.Authorization)]
public interface IFeishuUserV1DriveFilesVersions : IFeishuV1DriveFilesVersions, ICurrentUserId
{
}
