namespace StockBacktest.Contracts;

public class PortfolioSnapshot
{
    public decimal Cash { get; set; }
    public decimal Shares { get; set; }
    public decimal CurrentPrice { get; set; }
}