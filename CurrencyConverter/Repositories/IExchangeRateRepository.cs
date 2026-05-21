namespace CurrencyConverter.Repositories;

public interface IExchangeRateRepository
{
    Dictionary<string, decimal> GetAllRates();
    decimal? GetRateOverride(string key);
    IReadOnlyCollection<string> SupportedCurrencies { get; }
}
