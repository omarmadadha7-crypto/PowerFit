using Powerfit.Business.Security;
using Xunit;

namespace Powerfit.Tests.Business;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void A_hash_verifies_only_the_right_password()
    {
        var hash = _hasher.Hash("Passw0rdOk");

        Assert.StartsWith("v1.", hash);
        Assert.True(hash.Length < 255);
        Assert.True(_hasher.Verify("Passw0rdOk", hash));
        Assert.False(_hasher.Verify("passw0rdok", hash));
    }

    [Fact]
    public void Two_hashes_of_the_same_password_differ()
    {
        Assert.NotEqual(_hasher.Hash("Passw0rdOk"), _hasher.Hash("Passw0rdOk"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("v1.10.AAAA")]
    [InlineData("v2.210000.AAAA.AAAA")]
    public void Malformed_hashes_never_verify(string hash)
    {
        Assert.False(_hasher.Verify("Passw0rdOk", hash));
        Assert.True(_hasher.NeedsRehash(hash));
    }

    [Fact]
    public void A_current_hash_does_not_need_a_rehash()
    {
        Assert.False(_hasher.NeedsRehash(_hasher.Hash("Passw0rdOk")));
    }
}
