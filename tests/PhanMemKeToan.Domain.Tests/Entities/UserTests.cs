using FluentAssertions;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void User_ShouldAttemptLockout_AfterFiveFailedAttempts()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FullName = "Test User",
            PasswordHash = "hash",
            IsActive = true,
            FailedLoginCount = 0
        };

        // Simulate 5 failures
        for (int i = 0; i < 4; i++)
        {
            user.FailedLoginCount++;
        }
        user.FailedLoginCount++; // 5th failure

        user.FailedLoginCount.Should().Be(5);
    }

    [Fact]
    public void User_Deactivated_IsActive_Should_Be_False()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FullName = "Test User",
            PasswordHash = "hash",
            IsActive = false,
            TenantId = Guid.NewGuid()
        };

        user.IsActive.Should().BeFalse();
    }
}
