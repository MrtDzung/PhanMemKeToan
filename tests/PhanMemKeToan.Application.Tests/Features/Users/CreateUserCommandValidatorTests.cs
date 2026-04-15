using FluentAssertions;
using PhanMemKeToan.Application.Features.Users.Commands.CreateUser;

namespace PhanMemKeToan.Application.Tests.Features.Users;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ValidCommand_ShouldPass()
    {
        var command = new CreateUserCommand(
            "john@example.com",
            "P@ssword123!",
            "John Doe",
            [Guid.NewGuid()]
        );
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    public async Task Validate_InvalidEmail_ShouldFail(string email)
    {
        var command = new CreateUserCommand(email, "P@ssword123!", "Name", []);
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("short7!")]       // length < 8
    [InlineData("nouppercase1!")]  // no uppercase
    [InlineData("NOLOWERCASE1!")]  // no lowercase
    [InlineData("NoSpecialChar1")] // no special char
    public async Task Validate_WeakPassword_ShouldFail(string password)
    {
        var command = new CreateUserCommand("user@example.com", password, "Name", []);
        var result = await _validator.ValidateAsync(command);
        result.IsValid.Should().BeFalse();
    }
}
