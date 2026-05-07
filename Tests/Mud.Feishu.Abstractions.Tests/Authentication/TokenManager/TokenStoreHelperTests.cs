// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Authentication;

namespace Mud.Feishu.Abstractions.Tests.Authentication.TokenManager;

public class TokenStoreHelperTests
{
    [Fact]
    public void EncodeStoredToken_ShouldCombineTimestampAndToken()
    {
        var result = TokenStoreHelper.EncodeStoredToken("abc123", 1700000000000);
        Assert.Equal("1700000000000|abc123", result);
    }

    [Fact]
    public void DecodeStoredToken_ShouldParseValidEncodedValue()
    {
        var (token, expireMs) = TokenStoreHelper.DecodeStoredToken("1700000000000|abc123");
        Assert.Equal("abc123", token);
        Assert.Equal(1700000000000, expireMs);
    }

    [Fact]
    public void DecodeStoredToken_ShouldReturnOriginalValue_WhenNoSeparator()
    {
        var (token, expireMs) = TokenStoreHelper.DecodeStoredToken("plain_token");
        Assert.Equal("plain_token", token);
        Assert.Equal(0, expireMs);
    }

    [Fact]
    public void DecodeStoredToken_ShouldReturnOriginalValue_WhenSeparatorAtStart()
    {
        var (token, expireMs) = TokenStoreHelper.DecodeStoredToken("|token_value");
        Assert.Equal("|token_value", token);
        Assert.Equal(0, expireMs);
    }

    [Fact]
    public void DecodeStoredToken_ShouldReturnOriginalValue_WhenPrefixIsNotNumeric()
    {
        var (token, expireMs) = TokenStoreHelper.DecodeStoredToken("notanumber|token_value");
        Assert.Equal("notanumber|token_value", token);
        Assert.Equal(0, expireMs);
    }

    [Fact]
    public void DecodeStoredToken_ShouldHandleTokenContainingSeparator()
    {
        var (token, expireMs) = TokenStoreHelper.DecodeStoredToken("1700000000000|token|with|pipes");
        Assert.Equal("token|with|pipes", token);
        Assert.Equal(1700000000000, expireMs);
    }

    [Fact]
    public void EncodeThenDecode_ShouldRoundTrip()
    {
        const string originalToken = "test_access_token_12345";
        const long originalExpire = 1700000000000;
        var encoded = TokenStoreHelper.EncodeStoredToken(originalToken, originalExpire);
        var (token, expireMs) = TokenStoreHelper.DecodeStoredToken(encoded);
        Assert.Equal(originalToken, token);
        Assert.Equal(originalExpire, expireMs);
    }
}
