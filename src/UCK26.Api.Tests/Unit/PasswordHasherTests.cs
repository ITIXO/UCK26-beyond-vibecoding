using Microsoft.AspNetCore.DataProtection;
using UCK26.Api.Auth;

namespace UCK26.Api.Tests.Unit;

public class PasswordHasherTests
{
    private static IPasswordHasher CreateHasher() =>
        new DataProtectionPasswordHasher(new EphemeralDataProtectionProvider());

    [Test]
    public async Task Hash_DifferentEachCall_ButAllVerifyToSamePassword()
    {
        var hasher = CreateHasher();

        var a = hasher.Hash("Demo!2026");
        var b = hasher.Hash("Demo!2026");

        await Assert.That(a).IsNotEqualTo(b);
        await Assert.That(hasher.Verify("Demo!2026", a)).IsTrue();
        await Assert.That(hasher.Verify("Demo!2026", b)).IsTrue();
    }

    [Test]
    public async Task Verify_WrongPassword_ReturnsFalse()
    {
        var hasher = CreateHasher();
        var hash = hasher.Hash("correct");

        await Assert.That(hasher.Verify("wrong", hash)).IsFalse();
    }

    [Test]
    public async Task Verify_MalformedHash_ReturnsFalse()
    {
        var hasher = CreateHasher();

        await Assert.That(hasher.Verify("anything", "not-a-real-protected-blob")).IsFalse();
    }

    [Test]
    public async Task Verify_HashFromDifferentProvider_ReturnsFalse()
    {
        var hasherA = CreateHasher();
        var hasherB = CreateHasher();

        var hash = hasherA.Hash("secret");

        await Assert.That(hasherB.Verify("secret", hash)).IsFalse();
    }
}
