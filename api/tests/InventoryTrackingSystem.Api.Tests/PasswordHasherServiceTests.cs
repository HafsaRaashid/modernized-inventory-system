using InventoryTrackingSystem.Infrastructure.Auth;
using Xunit;

namespace InventoryTrackingSystem.Api.Tests;

/// <summary>
/// Unit tests for <see cref="PasswordHasherService"/> covering spec.md AC-5:
/// Hash/Verify round-trip, salted uniqueness, wrong-password rejection, and
/// graceful (non-throwing) handling of a malformed stored hash.
/// </summary>
public class PasswordHasherServiceTests
{
    [Fact]
    public void Verify_ReturnsTrue_ForCorrectPasswordAgainstItsOwnHash()
    {
        var hasher = new PasswordHasherService();
        var hash = hasher.Hash("correct horse battery staple");

        var result = hasher.Verify("correct horse battery staple", hash);

        Assert.True(result);
    }

    [Fact]
    public void Hash_ProducesDifferentOutput_ForSamePasswordHashedTwice()
    {
        var hasher = new PasswordHasherService();

        var hash1 = hasher.Hash("same-password");
        var hash2 = hasher.Hash("same-password");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        var hasher = new PasswordHasherService();
        var hash = hasher.Hash("the-real-password");

        var result = hasher.Verify("not-the-real-password", hash);

        Assert.False(result);
    }

    [Fact]
    public void Verify_ReturnsFalse_ForMalformedStoredHash_DoesNotThrow()
    {
        var hasher = new PasswordHasherService();

        var result = hasher.Verify("anything", "not-a-valid-stored-hash");

        Assert.False(result);
    }
}
