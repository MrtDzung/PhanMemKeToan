using FluentAssertions;
using PhanMemKeToan.Application.Features.Auth.Commands.Login;

namespace PhanMemKeToan.Application.Tests.Features.Auth;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ValidCommand_ShouldPass()
    {
        var command = new LoginCommand("user@example.com", "Password123!", false, "127.0.0.1");
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task Validate_InvalidEmail_ShouldFail(string email)
    {
        var command = new LoginCommand(email, "Password123!", false, "127.0.0.1");
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_EmptyPassword_ShouldFail()
    {
        var command = new LoginCommand("user@example.com", "", false, "127.0.0.1");
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Validate_PasswordTooLong_ShouldFail()
    {
        var command = new LoginCommand("user@example.com", new string('a', 129), false, "127.0.0.1");
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
    }
}
