using CurrencyConverter.Models;
using CurrencyConverter.Repositories;
using CurrencyConverter.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace CurrencyConverter.Tests;

public class ExchangeRateServiceTests
{
    private readonly Mock<IExchangeRateRepository> _repositoryMock;
    private readonly Mock<ILogger<ExchangeRateService>> _loggerMock;
    private readonly ExchangeRateService _service;

    public ExchangeRateServiceTests()
    {
        _repositoryMock = new Mock<IExchangeRateRepository>();
        _loggerMock = new Mock<ILogger<ExchangeRateService>>();
        _service = new ExchangeRateService(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void GetExchangeRate_SameCurrency_ReturnsOne()
    {
        var rate = _service.GetExchangeRate("USD", "USD");

        Assert.Equal(1m, rate);
    }

    [Fact]
    public void GetExchangeRate_ValidPair_ReturnsRateFromRepository()
    {
        _repositoryMock.Setup(r => r.GetRateOverride("USD_TO_INR")).Returns((decimal?)null);
        _repositoryMock.Setup(r => r.GetAllRates()).Returns(new Dictionary<string, decimal>
        {
            { "USD_TO_INR", 74.00m }
        });

        var rate = _service.GetExchangeRate("USD", "INR");

        Assert.Equal(74.00m, rate);
    }

    [Fact]
    public void GetExchangeRate_EnvironmentOverride_TakesPrecedence()
    {
        _repositoryMock.Setup(r => r.GetRateOverride("USD_TO_INR")).Returns(81.00m);
        _repositoryMock.Setup(r => r.GetAllRates()).Returns(new Dictionary<string, decimal>
        {
            { "USD_TO_INR", 74.00m }
        });

        var rate = _service.GetExchangeRate("USD", "INR");

        Assert.Equal(81.00m, rate);
    }

    [Fact]
    public void GetExchangeRate_UnsupportedPair_ReturnsNull()
    {
        _repositoryMock.Setup(r => r.GetRateOverride("USD_TO_GBP")).Returns((decimal?)null);
        _repositoryMock.Setup(r => r.GetAllRates()).Returns(new Dictionary<string, decimal>
        {
            { "USD_TO_INR", 74.00m }
        });

        var rate = _service.GetExchangeRate("USD", "GBP");

        Assert.Null(rate);
    }

    [Theory]
    [InlineData("usd", "inr")]
    [InlineData("Usd", "Inr")]
    [InlineData("USD", "INR")]
    public void GetExchangeRate_CaseInsensitive_ReturnsRate(string source, string target)
    {
        _repositoryMock.Setup(r => r.GetRateOverride("USD_TO_INR")).Returns((decimal?)null);
        _repositoryMock.Setup(r => r.GetAllRates()).Returns(new Dictionary<string, decimal>
        {
            { "USD_TO_INR", 74.00m }
        });

        var rate = _service.GetExchangeRate(source, target);

        Assert.Equal(74.00m, rate);
    }

    [Fact]
    public void SupportedCurrencies_ContainsExpectedCurrencies()
    {
        _repositoryMock.Setup(r => r.SupportedCurrencies)
            .Returns(new List<string> { "USD", "INR", "EUR" });

        var supported = _repositoryMock.Object.SupportedCurrencies;

        Assert.Contains("USD", supported);
        Assert.Contains("INR", supported);
        Assert.Contains("EUR", supported);
        Assert.Equal(3, supported.Count);
    }
}
