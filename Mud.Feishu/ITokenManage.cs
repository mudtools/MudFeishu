namespace Mud.Feishu;

/// <summary>
/// 全局令牌管理接口。
/// </summary>
public interface ITokenManage
{
    /// <summary>
    /// 获取访问令牌。
    /// </summary>
    /// <returns></returns>
    Task<string> GetTokenAsync();
}
