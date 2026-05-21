using CurrencyConverter.Controllers;
using CurrencyConverter.Models;
using CurrencyConverter.Repositories;
using CurrencyConverter.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace CurrencyConverter.Tests;

public class CurrencyControllerTests
{
    private readonly Mock<IExchangeRateService> _serviceMock;
    private readonly Mock<IExchangeRateRepository> _repositoryMock;
    private readonly Mock<ILogger<CurrencyController>> _loggerMock;
    private readonly CurrencyController _controller;

    public CurrencyControllerTests()
    {
        _serviceMock = new Mock<IExchangeRateService>();
        _repositoryMock = new Mock<IExchangeRateRepository>();
        _loggerMock = new Mock<ILogger<CurrencyController>>();
        _repositoryMock.Setup(r => r.SupportedCurrencies)
            .Returns(new List<string> { "USD", "INR", "EUR" });
        _controller = new CurrencyController(_serviceMock.Object, _repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Convert_ValidRequest_ReturnsOkWithConversion()
    {
        _serviceMock.Setup(s => s.GetExchangeRate("USD", "INR")).Returns(74.00m);

        var result = _controller.Convert("USD", "INR", 100m) as OkObjectResult;

        Assert.NotNull(result);
        var response = result.Value as ConversionResponse;
        Assert.NotNull(response);
        Assert.Equal(74.00m, response.ExchangeRate);
        Assert.Equal(7400.00m, response.ConvertedAmount);
    }

    [Fact]
    public void Convert_MissingSourceCurrency_ReturnsBadRequest()
    {
        var result = _controller.Convert(null, "INR", 100m) as BadRequestObjectResult;

        Assert.NotNull(result);
        var error = result.Value as ErrorResponse;
        Assert.NotNull(error);
        Assert.Contains("sourceCurrency", error.Error);
    }

    [Fact]
    public void Convert_MissingTargetCurrency_ReturnsBadRequest()
    {
        var result = _controller.Convert("USD", null, 100m) as BadRequestObjectResult;

        Assert.NotNull(result);
        var error = result.Value as ErrorResponse;
        Assert.NotNull(error);
        Assert.Contains("targetCurrency", error.Error);
    }

    [Fact]
    public void Convert_MissingAmount_ReturnsBadRequest()
    {
        var result = _controller.Convert("USD", "INR", null) as BadRequestObjectResult;

        Assert.NotNull(result);
        var error = result.Value as ErrorResponse;
        Assert.NotNull(error);
        Assert.Contains("amount", error.Error);
    }

    [Fact]
    public void Convert_NegativeAmount_ReturnsBadRequest()
    {
        var result = _controller.Convert("USD", "INR", -10m) as BadRequestObjectResult;

        Assert.NotNull(result);
        var error = result.Value as ErrorResponse;
        Assert.NotNull(error);
        Assert.Contains("non-negative", error.Error);
    }

    [Fact]
    public void Convert_UnsupportedSourceCurrency_ReturnsBadRequest()
    {
        var result = _controller.Convert("GBP", "INR", 100m) as BadRequestObjectResult;

        Assert.NotNull(result);
        var error = result.Value as ErrorResponse;
        Assert.NotNull(error);
        Assert.Contains("Unsupported source currency", error.Error);
    }

    [Fact]
    public void Convert_UnsupportedTargetCurrency_ReturnsBadRequest()
    {
        var result = _controller.Convert("USD", "GBP", 100m) as BadRequestObjectResult;

        Assert.NotNull(result);
        var error = result.Value as ErrorResponse;
        Assert.NotNull(error);
        Assert.Contains("Unsupported target currency", error.Error);
    }

    [Fact]
    public void Convert_RateNotFound_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.GetExchangeRate("USD", "INR")).Returns((decimal?)null);

        var result = _controller.Convert("USD", "INR", 100m) as NotFoundObjectResult;

        Assert.NotNull(result);
        var error = result.Value as ErrorResponse;
        Assert.NotNull(error);
        Assert.Contains("Exchange rate not found", error.Error);
    }

    [Fact]
    public void Convert_ZeroAmount_ReturnsOkWithZero()
    {
        _serviceMock.Setup(s => s.GetExchangeRate("USD", "INR")).Returns(74.00m);

        var result = _controller.Convert("USD", "INR", 0m) as OkObjectResult;

        Assert.NotNull(result);
        var response = result.Value as ConversionResponse;
        Assert.NotNull(response);
        Assert.Equal(0m, response.ConvertedAmount);
    }

    [Fact]
    public void Convert_DecimalPrecision_RoundsToTwoPlaces()
    {
        _serviceMock.Setup(s => s.GetExchangeRate("INR", "USD")).Returns(0.013m);

        var result = _controller.Convert("INR", "USD", 100m) as OkObjectResult;

        Assert.NotNull(result);
        var response = result.Value as ConversionResponse;
        Assert.NotNull(response);
        Assert.Equal(1.30m, response.ConvertedAmount);
    }
}
