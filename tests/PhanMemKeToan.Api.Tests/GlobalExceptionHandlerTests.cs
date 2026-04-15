using FluentAssertions;
using PhanMemKeToan.Domain.Common.Exceptions;

namespace PhanMemKeToan.Api.Tests;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public void InvalidCredentialsException_ShouldHave_CorrectMessage()
    {
        var ex = new InvalidCredentialsException();
        ex.Should().NotBeNull();
        ex.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void AccountLockedException_ShouldHave_LockedUntil()
    {
        var lockedUntil = DateTimeOffset.UtcNow.AddMinutes(15);
        var ex = new AccountLockedException(lockedUntil);
        ex.LockedUntil.Should().Be(lockedUntil);
    }

    [Fact]
    public void DuplicateEmailException_ShouldHave_Email()
    {
        var ex = new DuplicateEmailException("test@example.com");
        ex.Email.Should().Be("test@example.com");
    }

    [Fact]
    public void RoleInUseException_ShouldHave_AffectedUserNames()
    {
        var names = new[] { "User1", "User2" };
        var ex = new RoleInUseException(names);
        ex.AffectedUserNames.Should().BeEquivalentTo(names);
    }
}
