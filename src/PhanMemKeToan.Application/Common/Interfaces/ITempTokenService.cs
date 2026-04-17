namespace PhanMemKeToan.Application.Common.Interfaces;

public record TempTokenClaims(Guid UserId, bool RememberMe);

public interface ITempTokenService
{
    string Generate(TempTokenClaims claims);
    TempTokenClaims? Validate(string token);
}
