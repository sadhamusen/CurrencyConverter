using System.Text.Json;
using CurrencyConverter.Models;
using Microsoft.Extensions.Options;

namespace CurrencyConverter.Repositories;

public class ExchangeRateRepository : IExchangeRateRepository
{
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<CurrencySettings> _settings;
    private readonly ILogger<ExchangeRateRepository> _logger;
    private readonly string _filePath;
    private Dictionary<string, decimal> _rates = new(StringComparer.OrdinalIgnoreCase);
    private DateTime _lastLoaded = DateTime.MinValue;

    private static readonly List<string> _defaultCurrencies = new() { "USD", "INR", "EUR" };

    public IReadOnlyCollection<string> SupportedCurrencies
    {
        get
        {
            var currencies = _settings.CurrentValue.SupportedCurrencies;
            return (currencies != null && currencies.Count > 0)
                ? currencies
                : _defaultCurrencies;
        }
    }

    public ExchangeRateRepository(IConfiguration configuration, IOptionsMonitor<CurrencySettings> settings, ILogger<ExchangeRateRepository> logger)
    {
        _configuration = configuration;
        _settings = settings;
        _logger = logger;
        _filePath = Path.Combine(AppContext.BaseDirectory, "exchangeRates.json");
    }

    public Dictionary<string, decimal> GetAllRates()
    {
        // Priority: appsettings.json "ExchangeRates" section (reloads on change) > local file
        var configRates = GetRatesFromConfiguration();
        if (configRates.Count > 0)
        {
            return configRates;
        }

        LoadRatesFromFile();
        return new Dictionary<string, decimal>(_rates, StringComparer.OrdinalIgnoreCase);
    }

    public decimal? GetRateOverride(string key)
    {
        // Environment variable override (highest priority)
        var envValue = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrEmpty(envValue) && decimal.TryParse(envValue, out var envRate))
        {
            return envRate;
        }

        // Also check IConfiguration (covers env vars, KV, appsettings)
        var configValue = _configuration[key];
        if (!string.IsNullOrEmpty(configValue) && decimal.TryParse(configValue, out var rate))
        {
            return rate;
        }

        return null;
    }

    private Dictionary<string, decimal> GetRatesFromConfiguration()
    {
        var section = _configuration.GetSection("ExchangeRates");
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        if (!section.Exists()) return rates;

        foreach (var child in section.GetChildren())
        {
            if (decimal.TryParse(child.Value, out var value))
            {
                rates[child.Key] = value;
            }
        }

        return rates;
    }

    private void LoadRatesFromFile()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogWarning("Exchange rates file not found at {Path}", _filePath);
                return;
            }

            var lastWrite = File.GetLastWriteTimeUtc(_filePath);
            if (lastWrite <= _lastLoaded)
            {
                return; // File hasn't changed
            }

            var json = File.ReadAllText(_filePath);
            var rates = JsonSerializer.Deserialize<Dictionary<string, decimal>>(json);

            if (rates != null)
            {
                _rates = new Dictionary<string, decimal>(rates, StringComparer.OrdinalIgnoreCase);
                _lastLoaded = lastWrite;
                _logger.LogInformation("Exchange rates loaded successfully from {Path}", _filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading exchange rates from file");
        }
    }
}
