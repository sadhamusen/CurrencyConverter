namespace CurrencyConverter.Services;

public interface IExchangeRateService
{
    decimal? GetExchangeRate(string sourceCurrency, string targetCurrency);
}
