namespace PhanMemKeToan.Domain.Enums;

public enum AccountCategoryKind
{
    Debit = 0,    // Tính chất Nợ: Assets (1xx), Expenses (6xx, 8xx)
    Credit = 1    // Tính chất Có: Liabilities (3xx), Equity (4xx), Revenue (5xx, 7xx)
}
