namespace CurrencyConverter.Models;

public class CurrencySettings
{
    public const string SectionName = "CurrencySettings";

    public List<string> SupportedCurrencies { get; set; } = new() { "USD", "INR", "EUR" };

    public string? KeyVaultUri { get; set; }
}
