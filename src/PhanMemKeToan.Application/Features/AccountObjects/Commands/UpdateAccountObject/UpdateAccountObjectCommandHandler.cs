using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Domain.Entities;
using PhanMemKeToan.Domain.Enums;

namespace PhanMemKeToan.Application.Features.AccountObjects.Commands.UpdateAccountObject;

public class UpdateAccountObjectCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext)
    : IRequestHandler<UpdateAccountObjectCommand, UpdateAccountObjectResult>
{
    public async Task<UpdateAccountObjectResult> Handle(
        UpdateAccountObjectCommand cmd, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        var entity = await dbContext.AccountObjects
            .Include(e => e.BankAccounts)
            .Include(e => e.OpeningBalances)
            .Include(e => e.EmployeeProfile)
            .FirstOrDefaultAsync(e => e.Id == cmd.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(AccountObject), cmd.Id);

        // Optimistic concurrency check
        if (entity.RowVersion != cmd.RowVersion)
            throw new ConflictException("Record has been modified by another user. Please refresh and try again.");

        // ObjectCode uniqueness check (only if changed)
        if (!string.Equals(entity.ObjectCode, cmd.ObjectCode, StringComparison.OrdinalIgnoreCase))
        {
            if (await dbContext.AccountObjects.AnyAsync(e => e.ObjectCode == cmd.ObjectCode, cancellationToken))
                throw new ConflictException($"ObjectCode '{cmd.ObjectCode}' already exists.");
        }

        // Full-replace BankAccounts
        dbContext.AccountObjectBankAccounts.RemoveRange(entity.BankAccounts);
        entity.BankAccounts.Clear();

        var newBankAccounts = cmd.BankAccounts.Select(ba => new AccountObjectBankAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AccountObjectId = entity.Id,
            BankName = ba.BankName,
            BankBranch = ba.BankBranch,
            AccountNumber = ba.AccountNumber,
            SwiftCode = ba.SwiftCode,
        }).ToList();

        if (newBankAccounts.Count > 0)
            dbContext.AccountObjectBankAccounts.AddRange(newBankAccounts);

        // Full-replace OpeningBalances
        dbContext.AccountObjectOpeningBalances.RemoveRange(entity.OpeningBalances);
        entity.OpeningBalances.Clear();

        var newOpeningBalances = cmd.OpeningBalances.Select(ob => new AccountObjectOpeningBalance
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

        if (newOpeningBalances.Count > 0)
            dbContext.AccountObjectOpeningBalances.AddRange(newOpeningBalances);

        // EmployeeProfile toggle logic
        bool wasEmployee = entity.EmployeeProfile is not null;
        bool isEmployee = (cmd.ObjectType & ObjectType.Employee) != 0;

        if (wasEmployee && !isEmployee)
        {
            if (!cmd.ConfirmRemoveProfile)
                throw new BusinessRuleException("confirm_required", "Employee profile removal requires confirmation.");

            dbContext.AccountObjectEmployeeProfiles.Remove(entity.EmployeeProfile!);
        }
        else if (!wasEmployee && isEmployee && cmd.EmployeeProfile is { } newEp)
        {
            await ValidateEmployeeProfileUniquenessAsync(newEp, excludeAccountObjectId: null, cancellationToken);

            var profile = new AccountObjectEmployeeProfile
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AccountObjectId = entity.Id,
                CitizenId = newEp.CitizenId,
                DateOfBirth = newEp.DateOfBirth.HasValue ? DateTime.SpecifyKind(newEp.DateOfBirth.Value, DateTimeKind.Utc) : null,
                Gender = newEp.Gender.HasValue ? (Gender?)newEp.Gender.Value : null,
                SocialInsuranceNumber = newEp.SocialInsuranceNumber,
                HireDate = newEp.HireDate.HasValue ? DateTime.SpecifyKind(newEp.HireDate.Value, DateTimeKind.Utc) : null,
                DepartmentId = newEp.DepartmentId,
                DependentCount = newEp.DependentCount,
            };

            dbContext.AccountObjectEmployeeProfiles.Add(profile);
        }
        else if (wasEmployee && isEmployee && cmd.EmployeeProfile is { } updateEp)
        {
            await ValidateEmployeeProfileUniquenessAsync(updateEp, excludeAccountObjectId: cmd.Id, cancellationToken);

            entity.EmployeeProfile!.CitizenId = updateEp.CitizenId;
            entity.EmployeeProfile.DateOfBirth = updateEp.DateOfBirth.HasValue ? DateTime.SpecifyKind(updateEp.DateOfBirth.Value, DateTimeKind.Utc) : null;
            entity.EmployeeProfile.Gender = updateEp.Gender.HasValue ? (Gender?)updateEp.Gender.Value : null;
            entity.EmployeeProfile.SocialInsuranceNumber = updateEp.SocialInsuranceNumber;
            entity.EmployeeProfile.HireDate = updateEp.HireDate.HasValue ? DateTime.SpecifyKind(updateEp.HireDate.Value, DateTimeKind.Utc) : null;
            entity.EmployeeProfile.DepartmentId = updateEp.DepartmentId;
            entity.EmployeeProfile.DependentCount = updateEp.DependentCount;
        }

        // Update entity fields
        entity.ObjectCode = cmd.ObjectCode;
        entity.ObjectName = cmd.ObjectName;
        entity.ObjectNameEnglish = cmd.ObjectNameEnglish;
        entity.Address = cmd.Address;
        entity.TaxCode = cmd.TaxCode;
        entity.Email = cmd.Email;
        entity.Phone = cmd.Phone;
        entity.Fax = cmd.Fax;
        entity.Website = cmd.Website;
        entity.ContactPerson = cmd.ContactPerson;
        entity.ContactPhone = cmd.ContactPhone;
        entity.Description = cmd.Description;
        entity.ObjectType = cmd.ObjectType;
        entity.CreditLimit = cmd.CreditLimit;
        entity.PaymentTermDays = cmd.PaymentTermDays;
        entity.IsActive = cmd.IsActive;
        entity.AccountObjectGroupId = cmd.AccountObjectGroupId;
        entity.RowVersion++;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateAccountObjectResult(entity.Id, entity.RowVersion);
    }

    private async Task ValidateEmployeeProfileUniquenessAsync(
        UpdateEmployeeProfileDto ep,
        Guid? excludeAccountObjectId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(ep.CitizenId))
        {
            var q = dbContext.AccountObjectEmployeeProfiles
                .Where(x => x.CitizenId == ep.CitizenId);
            if (excludeAccountObjectId.HasValue)
                q = q.Where(x => x.AccountObjectId != excludeAccountObjectId.Value);
            if (await q.AnyAsync(cancellationToken))
                throw new ConflictException($"CitizenId '{ep.CitizenId}' already exists.");
        }

        if (!string.IsNullOrEmpty(ep.SocialInsuranceNumber))
        {
            var q = dbContext.AccountObjectEmployeeProfiles
                .Where(x => x.SocialInsuranceNumber == ep.SocialInsuranceNumber);
            if (excludeAccountObjectId.HasValue)
                q = q.Where(x => x.AccountObjectId != excludeAccountObjectId.Value);
            if (await q.AnyAsync(cancellationToken))
                throw new ConflictException($"SocialInsuranceNumber '{ep.SocialInsuranceNumber}' already exists.");
        }
    }
}
