namespace StockBacktest.Calculators;

public static class ReturnCalculator
{
    public static (decimal Absolute, decimal Percent, decimal Annualized, int HoldingDays) Calculate(
        decimal buyPrice, decimal sellPrice, DateOnly buyDate, DateOnly sellDate)
    {
        var holdingDays = sellDate.DayNumber - buyDate.DayNumber;
        var holdingYears = holdingDays / 365.25;

        var absolute = Math.Round(sellPrice - buyPrice, 2);
        var percent = Math.Round((sellPrice - buyPrice) / buyPrice * 100m, 2);

        decimal annualized;
        if (holdingYears <= 0)
            annualized = percent;
        else
            annualized = Math.Round(
                (decimal)(Math.Pow((double)(sellPrice / buyPrice), 1.0 / holdingYears) - 1) * 100m, 2);

        return (absolute, percent, annualized, holdingDays);
    }
}