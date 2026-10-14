// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Knowledge;

/// <summary>
/// Token 估算（<b>刻意不精确</b>，但确定、可测、AOT 友好）。
/// </summary>
/// <remarks>
/// <para>
/// <b>口径</b>：CJK / 全角字符按 1 token 计，其余（ASCII 等）按 4 字符 ≈ 1 token 计（向上取整）。
/// </para>
/// <para>
/// <b>为什么允许不精确</b>：它只用于<b>预算判定</b>（"这段是不是已经太长"），
/// 而非计费或截断展示——此处偏保守 10~20% 完全可接受。引入真分词器的成本（依赖/AOT/体积）
/// 与收益不成比例，故按近似实现并把口径写死在此处。
/// </para>
/// <para>
/// <b>单一实现（N2-e 收敛，方案 v2 P2 订正）</b>：迁入前仓内有<b>两份</b>估算器且字符集不同——
/// 产品分块器的版本把 U+3000–U+303F（中文标点）计宽、不含谚文；<c>ChatTokenCounter</c> 的
/// 回退路径含谚文、不含中文标点。二者对含标点文本给出不同估算值，"收敛且估算值不变"
/// 不可同时成立。定稿：收敛为<b>联合字符集</b>的单一实现（本类型），两份消费方都委托它；
/// 估算差是<b>刻意接受</b>的行为变更（标点密集文本的切片预算与 token 维度摘要触发时机
/// 轻微前移——方向是更接近真实 token 量）。
/// </para>
/// </remarks>
public static class TokenEstimator
{
    /// <summary>估算一段文本的 token 数（空串返回 0）。</summary>
    public static int Estimate(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        var wide = 0;
        var narrow = 0;
        foreach (var ch in value)
        {
            if (IsWide(ch))
            {
                wide++;
            }
            else
            {
                narrow++;
            }
        }

        return wide + (int)Math.Ceiling(narrow / 4d);
    }

    /// <summary>
    /// 是否按"宽字符"计 1 token：CJK 表意文字/谚文/兼容表意/全角形式 <b>及</b> CJK 符号与标点
    /// （U+3000–U+303F——中文标点在 Tiktoken 下通常也是 1 token/字符，故计宽）。
    /// </summary>
    private static bool IsWide(char ch) => ch >= 0x2E80 && ch <= 0x9FFF ||
        ch >= 0xAC00 && ch <= 0xD7AF ||
        ch >= 0xF900 && ch <= 0xFAFF ||
        ch >= 0xFF00 && ch <= 0xFF60 ||
        ch >= 0x3000 && ch <= 0x303F;
}
