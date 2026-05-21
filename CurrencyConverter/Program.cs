using System.Diagnostics;
using Azure.Identity;
using CurrencyConverter.Models;
using CurrencyConverter.Repositories;
using CurrencyConverter.Services;

var builder = WebApplication.CreateBuilder(args);

// Add environment variables as configuration source for exchange rate overrides
builder.Configuration.AddEnvironmentVariables();

// Add Azure Key Vault as configuration source (graceful fallback if unavailable)
var keyVaultUri = builder.Configuration["CurrencySettings:KeyVaultUri"];
if (!string.IsNullOrEmpty(keyVaultUri))
{
    try
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUri),
            new DefaultAzureCredential());
    }
    catch (Exception ex)
    {
        // KV unavailable — fall back to local config
        Console.WriteLine($"Azure Key Vault not available, using local config. Reason: {ex.Message}");
    }
}

// Bind settings
builder.Services.Configure<CurrencySettings>(
    builder.Configuration.GetSection(CurrencySettings.SectionName));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();
builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var url = "http://localhost:5286/swagger";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    });
}

app.Run();
