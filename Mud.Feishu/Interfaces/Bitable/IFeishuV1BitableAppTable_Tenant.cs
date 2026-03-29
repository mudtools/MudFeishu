// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------


namespace Mud.Feishu;

/// <summary>
/// <para>数据表 table是多维表格的数据容器，一个多维表格中至少有一个数据表（table），也可能有多个数据表。</para>
/// <para>每个数据表都有唯一标识 table_id。table_id 在一个多维表格 App 中唯一，在全局不一定唯一。</para>
/// <para>可通过多维表格 URL 获取 table_id，也可通过列出数据表接口获取 table_id。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/bitable-overview"/></para>
/// </summary>
[HttpClientApi(RegistryGroupName = "Bitable", TokenManage = nameof(IFeishuAppManager), InheritedFrom = nameof(FeishuV1BitableAppTable))]
[Header(Consts.Authorization)]
[Token(TokenType.TenantAccessToken)]
public interface IFeishuTenantV1BitableAppTable : IFeishuV1BitableAppTable
{
}