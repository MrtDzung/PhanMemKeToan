using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Entities;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Application.Features.AccountObjects.Commands.CreateAccountObject;

public class CreateAccountObjectCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<CreateAccountObjectCommand, CreateAccountObjectResult>
{
    public async Task<CreateAccountObjectResult> Handle(
        CreateAccountObjectCommand cmd, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        // Unique ObjectCode check
        if (await dbContext.AccountObjects.AnyAsync(e => e.ObjectCode == cmd.ObjectCode, cancellationToken))
            throw new ConflictException($"ObjectCode '{cmd.ObjectCode}' already exists.");

        var entity = new AccountObject
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ObjectCode = cmd.ObjectCode,
            ObjectName = cmd.ObjectName,
            ObjectNameEnglish = cmd.ObjectNameEnglish,
            Address = cmd.Address,
            TaxCode = cmd.TaxCode,
            Email = cmd.Email,
            Phone = cmd.Phone,
            Fax = cmd.Fax,
            Website = cmd.Website,
            ContactPerson = cmd.ContactPerson,
            ContactPhone = cmd.ContactPhone,
            Description = cmd.Description,
            ObjectType = cmd.ObjectType,
            CreditLimit = cmd.CreditLimit,
            PaymentTermDays = cmd.PaymentTermDays,
            IsActive = cmd.IsActive,
            AccountObjectGroupId = cmd.AccountObjectGroupId,
            RowVersion = 0,
        };

        dbContext.AccountObjects.Add(entity);

        // Bank accounts
        var bankAccounts = cmd.BankAccounts.Select(ba => new AccountObjectBankAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AccountObjectId = entity.Id,
            BankName = ba.BankName,
            BankBranch = ba.BankBranch,
            AccountNumber = ba.AccountNumber,
            SwiftCode = ba.SwiftCode,
        }).ToList();

        if (bankAccounts.Count > 0)
            dbContext.AccountObjectBankAccounts.AddRange(bankAccounts);

        // Opening balances
        var openingBalances = cmd.OpeningBalances.Select(ob => new AccountObjectOpeningBalance
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AccountObjectId = entity.Id,
            CurrencyId = ob.CurrencyId,
            DebitAmount = ob.DebitAmount,
            CreditAmount = ob.CreditAmount,
            DebitAmountOC = ob.DebitAmountOC,
            CreditAmountOC = ob.CreditAmountOC,
            ExchangeRate = ob.ExchangeRate,
        }).ToList();

        if (openingBalances.Count > 0)
            dbContext.AccountObjectOpeningBalances.AddRange(openingBalances);

        // Employee profile (only if Employee bit set)
        if ((cmd.ObjectType & ObjectType.Employee) != 0 && cmd.EmployeeProfile is { } ep)
        {
            if (!string.IsNullOrEmpty(ep.CitizenId))
            {
                bool citizenIdExists = await dbContext.AccountObjectEmployeeProfiles
                    .AnyAsync(x => x.CitizenId == ep.CitizenId, cancellationToken);
                if (citizenIdExists)
                    throw new ConflictException($"CitizenId '{ep.CitizenId}' already exists.");
            }

            if (!string.IsNullOrEmpty(ep.SocialInsuranceNumber))
            {
                bool sinExists = await dbContext.AccountObjectEmployeeProfiles
                    .AnyAsync(x => x.SocialInsuranceNumber == ep.SocialInsuranceNumber, cancellationToken);
                if (sinExists)
                    throw new ConflictException($"SocialInsuranceNumber '{ep.SocialInsuranceNumber}' already exists.");
            }

            var profile = new AccountObjectEmployeeProfile
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AccountObjectId = entity.Id,
                CitizenId = ep.CitizenId,
                DateOfBirth = ep.DateOfBirth.HasValue ? DateTime.SpecifyKind(ep.DateOfBirth.Value, DateTimeKind.Utc) : null,
                Gender = ep.Gender.HasValue ? (Gender?)ep.Gender.Value : null,
                SocialInsuranceNumber = ep.SocialInsuranceNumber,
                HireDate = ep.HireDate.HasValue ? DateTime.SpecifyKind(ep.HireDate.Value, DateTimeKind.Utc) : null,
                DepartmentId = ep.DepartmentId,
                DependentCount = ep.DependentCount,
            };

            dbContext.AccountObjectEmployeeProfiles.Add(profile);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateAccountObjectResult(entity.Id, entity.RowVersion);
    }
}
