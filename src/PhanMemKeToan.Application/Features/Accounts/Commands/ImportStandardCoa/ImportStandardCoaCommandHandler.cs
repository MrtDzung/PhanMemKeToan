using MediatR;
using Microsoft.EntityFrameworkCore;
using PhanMemKeToan.Application.Common.Exceptions;
using PhanMemKeToan.Application.Common.Interfaces;
using PhanMemKeToan.Application.Features.Accounts.DTOs;
using PhanMemKeToan.Domain.Entities;
using PhanMemKeToan.Domain.Enums;
using System.Reflection;
using System.Text.Json;

namespace PhanMemKeToan.Application.Features.Accounts.Commands.ImportStandardCoa;

internal sealed class CoaEntryDto
{
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? AccountNameEnglish { get; set; }
    public string? ParentNumber { get; set; }
    public int Grade { get; set; } = 1;
    public int AccountCategoryKind { get; set; }
    public bool IsPostableInForeignCurrency { get; set; }
    public bool DetailByAccountObject { get; set; }
    public int AccountObjectType { get; set; }
    public bool DetailByBankAccount { get; set; }
    public bool DetailByJob { get; set; }
    public bool DetailByProjectWork { get; set; }
    public bool DetailByOrder { get; set; }
    public bool DetailByContract { get; set; }
    public bool DetailByExpenseItem { get; set; }
    public bool DetailByDepartment { get; set; }
    public bool DetailByListItem { get; set; }
    public bool DetailByPUContract { get; set; }
}

public class ImportStandardCoaCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    IAccountCacheService cacheService)
    : IRequestHandler<ImportStandardCoaCommand, ImportCoaResultDto>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<ImportCoaResultDto> Handle(ImportStandardCoaCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId ?? throw new ForbiddenAccessException();

        if (request.Standard is not ("TT99" or "TT133"))
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("Standard", "TiÃªu chuáº©n pháº£i lÃ  TT99 hoáº·c TT133.")]);

        if (request.ConflictResolution is not ("skip" or "overwrite"))
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("ConflictResolution", "Xá»­ lÃ½ xung Ä‘á»™t pháº£i lÃ  'skip' hoáº·c 'overwrite'.")]);

        var resourceName = request.Standard == "TT99"
            ? "PhanMemKeToan.Infrastructure.Resources.coa_tt99.json"
            : "PhanMemKeToan.Infrastructure.Resources.coa_tt133.json";

        // Load from Infrastructure assembly
        var assembly = Assembly.Load("PhanMemKeToan.Infrastructure");
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Resource '{resourceName}' not found.");

        var entries = await JsonSerializer.DeserializeAsync<List<CoaEntryDto>>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize CoA JSON.");

        var result = new ImportCoaResultDto();
        var errors = new List<string>();

        // Load existing accounts for conflict detection
        var existing = await dbContext.Accounts
            .ToDictionaryAsync(a => a.AccountNumber, cancellationToken);

        if (request.DryRun)
        {
            // Compute stats only â€” no persistence
            foreach (var entry in entries)
            {
                if (existing.ContainsKey(entry.AccountNumber))
                {
                    if (request.ConflictResolution == "skip") result.Skipped++;
                    else result.Overwritten++;
                }
                else result.Imported++;
            }
            result.Errors = errors;
            return result;
        }

        // Build numberâ†’guid mapping for parent resolution within import batch
        var importedMap = new Dictionary<string, Guid>(existing.ToDictionary(kv => kv.Key, kv => kv.Value.Id));
        var parentUpdates = new HashSet<string>();

        foreach (var entry in entries)
        {
            try
            {
                if (existing.TryGetValue(entry.AccountNumber, out var existingAcc))
                {
                    if (request.ConflictResolution == "skip")
                    {
                        result.Skipped++;
                        continue;
                    }
                    // overwrite
                    existingAcc.AccountName = entry.AccountName;
                    existingAcc.AccountNameEnglish = entry.AccountNameEnglish;
                    existingAcc.Grade = entry.Grade;
                    existingAcc.AccountCategoryKind = (AccountCategoryKind)entry.AccountCategoryKind;
                    existingAcc.IsPostableInForeignCurrency = entry.IsPostableInForeignCurrency;
                    existingAcc.DetailByAccountObject = entry.DetailByAccountObject;
                    existingAcc.AccountObjectType = (AccountObjectType)entry.AccountObjectType;
                    existingAcc.DetailByBankAccount = entry.DetailByBankAccount;
                    existingAcc.DetailByJob = entry.DetailByJob;
                    existingAcc.DetailByProjectWork = entry.DetailByProjectWork;
                    existingAcc.DetailByOrder = entry.DetailByOrder;
                    existingAcc.DetailByContract = entry.DetailByContract;
                    existingAcc.DetailByExpenseItem = entry.DetailByExpenseItem;
                    existingAcc.DetailByDepartment = entry.DetailByDepartment;
                    existingAcc.DetailByListItem = entry.DetailByListItem;
                    existingAcc.DetailByPUContract = entry.DetailByPUContract;
                    existingAcc.RowVersion++;
                    existingAcc.ModifiedAt = DateTimeOffset.UtcNow;
                    existingAcc.ModifiedBy = "import";
                    result.Overwritten++;
                }
                else
                {
                    Guid? parentId = null;
                    if (!string.IsNullOrEmpty(entry.ParentNumber) && importedMap.TryGetValue(entry.ParentNumber, out var pid))
                        parentId = pid;

                    var newAccount = new Account
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        AccountNumber = entry.AccountNumber,
                        AccountName = entry.AccountName,
                        AccountNameEnglish = entry.AccountNameEnglish,
                        ParentID = parentId,
                        Grade = entry.Grade,
                        IsParent = false,
                        AccountCategoryKind = (AccountCategoryKind)entry.AccountCategoryKind,
                        IsPostableInForeignCurrency = entry.IsPostableInForeignCurrency,
                        DetailByAccountObject = entry.DetailByAccountObject,
                        AccountObjectType = (AccountObjectType)entry.AccountObjectType,
                        DetailByBankAccount = entry.DetailByBankAccount,
                        DetailByJob = entry.DetailByJob,
                        DetailByProjectWork = entry.DetailByProjectWork,
                        DetailByOrder = entry.DetailByOrder,
                        DetailByContract = entry.DetailByContract,
                        DetailByExpenseItem = entry.DetailByExpenseItem,
                        DetailByDepartment = entry.DetailByDepartment,
                        DetailByListItem = entry.DetailByListItem,
                        DetailByPUContract = entry.DetailByPUContract,
                        RowVersion = 0,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = "import"
                    };

                    dbContext.Accounts.Add(newAccount);
                    importedMap[entry.AccountNumber] = newAccount.Id;
                    result.Imported++;

                    if (!string.IsNullOrEmpty(entry.ParentNumber))
                        parentUpdates.Add(entry.ParentNumber);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"TÃ i khoáº£n {entry.AccountNumber}: {ex.Message}");
            }
        }

        // Update IsParent flags for parent accounts
        foreach (var parentNumber in parentUpdates)
        {
            if (existing.TryGetValue(parentNumber, out var parentAcc))
                parentAcc.IsParent = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await cacheService.InvalidateTreeAsync(tenantId);

        result.Errors = errors;
        return result;
    }
}

