using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.AccountObjects.DTOs;

namespace PhanMemKeToan.Application.Features.AccountObjects.Queries.GetAccountObjectById;

public class GetAccountObjectByIdQueryHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<GetAccountObjectByIdQuery, AccountObjectDetailDto>
{
    public async Task<AccountObjectDetailDto> Handle(
        GetAccountObjectByIdQuery request, CancellationToken cancellationToken)
    {
        _ = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var entity = await dbContext.AccountObjects
            .AsNoTracking()
            .Include(e => e.BankAccounts)
            .Include(e => e.OpeningBalances)
                .ThenInclude(ob => ob.Currency)
            .Include(e => e.EmployeeProfile)
                .ThenInclude(ep => ep!.Department)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.AccountObject), request.Id);

        return new AccountObjectDetailDto
        {
            Id = entity.Id,
            ObjectCode = entity.ObjectCode,
            ObjectName = entity.ObjectName,
            ObjectNameEnglish = entity.ObjectNameEnglish,
            Address = entity.Address,
            TaxCode = entity.TaxCode,
            Email = entity.Email,
            Phone = entity.Phone,
            Fax = entity.Fax,
            Website = entity.Website,
            ContactPerson = entity.ContactPerson,
            ContactPhone = entity.ContactPhone,
            Description = entity.Description,
            ObjectType = entity.ObjectType,
            CreditLimit = entity.CreditLimit,
            PaymentTermDays = entity.PaymentTermDays,
            IsActive = entity.IsActive,
            RowVersion = entity.RowVersion,
            AccountObjectGroupId = entity.AccountObjectGroupId,
            CreatedAt = entity.CreatedAt,
            BankAccounts = entity.BankAccounts.Select(ba => new BankAccountDto
            {
                Id = ba.Id,
                BankName = ba.BankName,
                BankBranch = ba.BankBranch,
                AccountNumber = ba.AccountNumber,
                SwiftCode = ba.SwiftCode,
            }).ToList(),
            OpeningBalances = entity.OpeningBalances.Select(ob => new OpeningBalanceDto
            {
                Id = ob.Id,
                CurrencyId = ob.CurrencyId,
                CurrencyCode = ob.Currency.CurrencyCode,
                DebitAmount = ob.DebitAmount,
                CreditAmount = ob.CreditAmount,
                DebitAmountOC = ob.DebitAmountOC,
                CreditAmountOC = ob.CreditAmountOC,
                ExchangeRate = ob.ExchangeRate,
            }).ToList(),
            EmployeeProfile = entity.EmployeeProfile is null ? null : new EmployeeProfileDto
            {
                Id = entity.EmployeeProfile.Id,
                CitizenId = entity.EmployeeProfile.CitizenId,
                DateOfBirth = entity.EmployeeProfile.DateOfBirth,
                Gender = entity.EmployeeProfile.Gender.HasValue ? (int)entity.EmployeeProfile.Gender.Value : null,
                SocialInsuranceNumber = entity.EmployeeProfile.SocialInsuranceNumber,
                HireDate = entity.EmployeeProfile.HireDate,
                DepartmentId = entity.EmployeeProfile.DepartmentId,
                DepartmentName = entity.EmployeeProfile.Department?.DepartmentName,
                DependentCount = entity.EmployeeProfile.DependentCount,
            },
        };
    }
}
