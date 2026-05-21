using CurrencyConverter.Repositories;

namespace CurrencyConverter.Services;

public class ExchangeRateService : IExchangeRateService
{
    private readonly IExchangeRateRepository _repository;
    private readonly ILogger<ExchangeRateService> _logger;

    public ExchangeRateService(
        IExchangeRateRepository repository,
        ILogger<ExchangeRateService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public decimal? GetExchangeRate(string sourceCurrency, string targetCurrency)
    {
        if (string.Equals(sourceCurrency, targetCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return 1m;
        }

        var key = $"{sourceCurrency.ToUpperInvariant()}_TO_{targetCurrency.ToUpperInvariant()}";

        // Check environment variable override first (via repository)
        var overrideRate = _repository.GetRateOverride(key);
        if (overrideRate.HasValue)
        {
            _logger.LogInformation("Using environment variable override for {Key}: {Rate}", key, overrideRate.Value);
            return overrideRate.Value;
        }

        // Fall back to file-based rates
        var rates = _repository.GetAllRates();
        if (rates.TryGetValue(key, out var rate))
        {
            return rate;
        }

        return null;
    }
}

