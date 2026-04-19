namespace PhanMemKeToan.Application.Features.AccountObjects.DTOs;

public class OpeningBalanceDto
{
    public Guid Id { get; set; }
    public Guid CurrencyId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal DebitAmountOC { get; set; }
    public decimal CreditAmountOC { get; set; }
    public decimal ExchangeRate { get; set; }
}
