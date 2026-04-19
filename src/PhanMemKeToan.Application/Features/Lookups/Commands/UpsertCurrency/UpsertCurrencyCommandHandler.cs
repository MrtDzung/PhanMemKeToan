using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.Lookups.DTOs;
using PhanMemKeToan.Domain.Entities;

namespace PhanMemKeToan.Application.Features.Lookups.Commands.UpsertCurrency;

public class UpsertCurrencyCommandHandler(IApplicationDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<UpsertCurrencyCommand, CurrencyDto>
{
    public async Task<CurrencyDto> Handle(UpsertCurrencyCommand cmd, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        // Uniqueness check: CurrencyCode per tenant (exclude self on update)
        var codeExists = await dbContext.Currencies.AnyAsync(
            c => c.TenantId == tenantId && c.CurrencyCode == cmd.CurrencyCode && c.Id != cmd.Id,
            cancellationToken);

        if (codeExists)
            throw new ConflictException($"CurrencyCode '{cmd.CurrencyCode}' đã tồn tại.");

        Currency entity;

        if (cmd.Id == null)
        {
            entity = new Currency
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CurrencyCode = cmd.CurrencyCode,
                CurrencyName = cmd.CurrencyName,
                CurrencyNameEnglish = cmd.CurrencyNameEnglish,
                Symbol = cmd.Symbol ?? string.Empty,
                ExchangeRate = cmd.ExchangeRate,
                IsActive = cmd.IsActive,
            };
            dbContext.Currencies.Add(entity);
        }
        else
        {
            entity = await dbContext.Currencies
                .FirstOrDefaultAsync(c => c.Id == cmd.Id, cancellationToken)
                ?? throw new NotFoundException(nameof(Currency), cmd.Id);

            entity.CurrencyCode = cmd.CurrencyCode;
            entity.CurrencyName = cmd.CurrencyName;
            entity.CurrencyNameEnglish = cmd.CurrencyNameEnglish;
            entity.Symbol = cmd.Symbol ?? string.Empty;
            entity.ExchangeRate = cmd.ExchangeRate;
            entity.IsActive = cmd.IsActive;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CurrencyDto(
            entity.Id,
            entity.CurrencyCode,
            entity.CurrencyName,
            entity.CurrencyNameEnglish,
            entity.Symbol,
            entity.ExchangeRate,
            entity.IsActive);
    }
}
