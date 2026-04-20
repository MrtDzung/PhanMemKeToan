using FluentAssertions;
using PhanMemKeToan.Infrastructure.Services;

namespace PhanMemKeToan.Infrastructure.Tests.Services;

public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ShouldReturn_NonEmptyHash()
    {
        var hash = _hasher.HashPassword("Password123!");
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe("Password123!");
    }

    [Fact]
    public void Verify_CorrectPassword_ShouldReturn_True()
    {
        var password = "Password123!";
        var hash = _hasher.HashPassword(password);
        _hasher.VerifyPassword(password, hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_ShouldReturn_False()
    {
        var hash = _hasher.HashPassword("RightPassword1!");
        _hasher.VerifyPassword("WrongPassword1!", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_SamePlaintext_ProducesDifferentHashes()
    {
        var password = "Password123!";
        var hash1 = _hasher.HashPassword(password);
        var hash2 = _hasher.HashPassword(password);
        hash1.Should().NotBe(hash2); // BCrypt salts should differ
    }
}
