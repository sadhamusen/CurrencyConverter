using CurrencyConverter.Models;
using CurrencyConverter.Repositories;
using CurrencyConverter.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.Controllers;

[ApiController]
[Route("[action]")]
public class CurrencyController : ControllerBase
{
    private readonly IExchangeRateService _exchangeRateService;
    private readonly IExchangeRateRepository _exchangeRateRepository;
    private readonly ILogger<CurrencyController> _logger;

    public CurrencyController(
        IExchangeRateService exchangeRateService,
        IExchangeRateRepository exchangeRateRepository,
        ILogger<CurrencyController> logger)
    {
        _exchangeRateService = exchangeRateService;
        _exchangeRateRepository = exchangeRateRepository;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Convert(
        [FromQuery] string? sourceCurrency,
        [FromQuery] string? targetCurrency,
        [FromQuery] decimal? amount)
    {
        if (string.IsNullOrWhiteSpace(sourceCurrency))
        {
            return BadRequest(new ErrorResponse { Error = "sourceCurrency is required." });
        }

        if (string.IsNullOrWhiteSpace(targetCurrency))
        {
            return BadRequest(new ErrorResponse { Error = "targetCurrency is required." });
        }

        if (amount is null)
        {
            return BadRequest(new ErrorResponse { Error = "amount is required." });
        }

        if (amount < 0)
        {
            return BadRequest(new ErrorResponse { Error = "amount must be a non-negative value." });
        }

        var supported = _exchangeRateRepository.SupportedCurrencies;

        if (!supported.Contains(sourceCurrency.ToUpperInvariant()))
        {
            return BadRequest(new ErrorResponse
            {
                Error = $"Unsupported source currency: '{sourceCurrency}'. Supported currencies: {string.Join(", ", supported)}"
            });
        }

        if (!supported.Contains(targetCurrency.ToUpperInvariant()))
        {
            return BadRequest(new ErrorResponse
            {
                Error = $"Unsupported target currency: '{targetCurrency}'. Supported currencies: {string.Join(", ", supported)}"
            });
        }

        var exchangeRate = _exchangeRateService.GetExchangeRate(sourceCurrency, targetCurrency);

        if (exchangeRate is null)
        {
            return NotFound(new ErrorResponse
            {
                Error = $"Exchange rate not found for {sourceCurrency.ToUpperInvariant()} to {targetCurrency.ToUpperInvariant()}."
            });
        }

        var convertedAmount = Math.Round(amount.Value * exchangeRate.Value, 2, MidpointRounding.AwayFromZero);

        _logger.LogInformation(
            "Converted {Amount} {Source} to {Converted} {Target} at rate {Rate}",
            amount.Value, sourceCurrency.ToUpperInvariant(), convertedAmount, targetCurrency.ToUpperInvariant(), exchangeRate.Value);

        return Ok(new ConversionResponse
        {
            ExchangeRate = exchangeRate.Value,
            ConvertedAmount = convertedAmount
        });
    }
}
