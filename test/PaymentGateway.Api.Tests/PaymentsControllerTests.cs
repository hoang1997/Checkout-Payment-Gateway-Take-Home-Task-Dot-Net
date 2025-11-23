using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Clients;

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerTests
{
    private readonly Random _random = new();
    private Mock<IBankClient> _bankClientMock;
    private Mock<IPaymentsRepository> _paymentsRepositoryMock;
    private HttpClient _httpClient;
    private WebApplicationFactory<PaymentsController> _webApplicationFactory;

    [SetUp]
    public void Setup()
    {
        _paymentsRepositoryMock = new Mock<IPaymentsRepository>();
        _bankClientMock = new Mock<IBankClient>();
        _webApplicationFactory = new WebApplicationFactory<PaymentsController>();
    }


    [Test]
    public async Task RetrievesAPaymentSuccessfully()
    {
        // Arrange
        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            ExpiryYear = DateTime.UtcNow.AddMonths(12).Year,
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumberLastFour = _random.Next(1111, 9999),
            Currency = "GBP"
        };

        _paymentsRepositoryMock.Setup(pr => pr.Get(payment.Id)).Returns(payment);

        SetHttpClient();

        // Act
        var response = await _httpClient.GetAsync($"/api/payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(paymentResponse, Is.Not.Null);
    }

    [Test]
    public async Task Returns404IfPaymentNotFound()
    {
        // Arrange
        _paymentsRepositoryMock.Setup(pr => pr.Get(It.IsAny<Guid>())).Returns((PostPaymentResponse)null);
        SetHttpClient();

        // Act
        var response = await _httpClient.GetAsync($"/api/payments/{Guid.NewGuid()}");
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }


    [Test]
    public async Task CreatePaymentSuccessfully()
    {
        // Arrange

        _bankClientMock.Setup(bc => bc.AuthorizeTransaction(It.IsAny<PostAcquirerRequest>()))
            .ReturnsAsync(new PostAcquirerResponse() { AuthorizationCode = Guid.NewGuid().ToString(), Authorized = true });

        SetHttpClient();

        // Act 
        var dateTime = DateTime.UtcNow.AddMonths(12);
        var request = new PostPaymentRequest() { Amount = -10, Currency = "GBP", CardNumber = 1234567890123457, ExpiryMonth = dateTime.Month, ExpiryYear = dateTime.Year, Cvv = 111 };
        var body = JsonContent.Create(request, options: new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        });
        var response = await _httpClient.PostAsync($"/api/payments", body);
        var responseContent = await response.Content.ReadFromJsonAsync<PostPaymentResponse>(new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower});

        // Assert

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(responseContent?.Id, Is.Not.Null);
        Assert.That(responseContent.Status, Is.EqualTo(PaymentStatus.Authorized));
        Assert.That(responseContent.Currency, Is.EqualTo(request.Currency));
        Assert.That(responseContent.CardNumberLastFour, Is.EqualTo((int)(request.CardNumber % 10000)));
        Assert.That(responseContent.ExpiryMonth, Is.EqualTo(request.ExpiryMonth)); 
        Assert.That(responseContent.ExpiryYear, Is.EqualTo(request.ExpiryYear));

        _paymentsRepositoryMock.Verify(pr => pr.Add(It.IsAny<PostPaymentResponse>()), Times.Once);
    }

    [TestCase(1234567890123, HttpStatusCode.BadRequest)]
    [TestCase(123456789012, HttpStatusCode.BadRequest)]
    [TestCase(12345678901, HttpStatusCode.BadRequest)]
    public async Task ValidateCardNumber_ReturnsBadRequest(long cardNumber, HttpStatusCode statusCode)
    {
        // Arrange
        SetHttpClient();

        // Act 
        var dateTime = DateTime.UtcNow.AddMonths(5);
        var request = new PostPaymentRequest() { Amount = -10, Currency = "GBP", CardNumber = cardNumber, ExpiryMonth = 13, ExpiryYear = dateTime.Year, Cvv = 111 };
        var body = JsonContent.Create(request, options: new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        });
        var response = await _httpClient.PostAsync($"/api/payments", body);

        // Assert

        Assert.That(response.StatusCode, Is.EqualTo(statusCode));
      
    }

    [TestCase(12, HttpStatusCode.OK)]
    [TestCase(11, HttpStatusCode.OK)]
    [TestCase(13, HttpStatusCode.BadRequest)]
    [TestCase(0, HttpStatusCode.BadRequest)]
    public async Task ValidateExpiryMonth_ReturnsBadRequest(int expiryMonth, HttpStatusCode statusCode)
    {
        // Arrange
        _bankClientMock.Setup(bc => bc.AuthorizeTransaction(It.IsAny<PostAcquirerRequest>()))
          .ReturnsAsync(new PostAcquirerResponse() { AuthorizationCode = Guid.NewGuid().ToString(), Authorized = true });

        SetHttpClient();

        // Act 
        var dateTime = DateTime.UtcNow.AddMonths(5);
        var request = new PostPaymentRequest() { Amount = -10, Currency = "GBP", CardNumber = 1234567890123457, ExpiryMonth = expiryMonth, ExpiryYear = dateTime.Year, Cvv = 111 };
        var body = JsonContent.Create(request, options: new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        });
        var response = await _httpClient.PostAsync($"/api/payments", body);

        // Assert

        Assert.That(response.StatusCode, Is.EqualTo(statusCode));
    }

    [TestCase(-13, HttpStatusCode.BadRequest, "Invalid expiry year")]
    [TestCase(-1, HttpStatusCode.BadRequest, "Card expiry date must be in the future")]
    public async Task ValidateDateTimeInFuture_ReturnsBadRequest(int minusMonths, HttpStatusCode statusCode, string validationMessage)
    {
        // Arrange
        _bankClientMock.Setup(bc => bc.AuthorizeTransaction(It.IsAny<PostAcquirerRequest>()))
          .ReturnsAsync(new PostAcquirerResponse() { AuthorizationCode = Guid.NewGuid().ToString(), Authorized = true });

        SetHttpClient();

        // Act 
        var dateTime = DateTime.UtcNow.AddMonths(minusMonths);
        var request = new PostPaymentRequest() { Amount = -10, Currency = "GBP", CardNumber = 1234567890123457, ExpiryMonth = dateTime.Month, ExpiryYear = dateTime.Year, Cvv = 111 };
        var body = JsonContent.Create(request, options: new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        });
        var response = await _httpClient.PostAsync($"/api/payments", body);
        var responseContent = await response.Content.ReadFromJsonAsync<PostPaymentRejectedResponse>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(statusCode));
        Assert.That(responseContent?.Status, Is.EqualTo(PaymentStatus.Rejected));
        Assert.That(responseContent.Reason, Is.EqualTo(validationMessage));
    }

    [TestCase("GBPP", HttpStatusCode.BadRequest)]
    [TestCase("GP", HttpStatusCode.BadRequest)]
    [TestCase("G", HttpStatusCode.BadRequest)]
    public async Task ValidateCurrency_ReturnsBadRequest(string currency, HttpStatusCode statusCode)
    {
        // Arrange
        SetHttpClient();

        // Act 
        var dateTime = DateTime.UtcNow.AddMonths(5);
        var request = new PostPaymentRequest() { Amount = -10, Currency = currency, CardNumber = 1234567890123457, ExpiryMonth = dateTime.Month, ExpiryYear = dateTime.Year, Cvv = 111 };
        var body = JsonContent.Create(request, options: new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        });
        var response = await _httpClient.PostAsync($"/api/Payments", body);
        var responseContent = await response.Content.ReadFromJsonAsync<PostPaymentRejectedResponse>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(statusCode));
        Assert.That(responseContent?.Status, Is.EqualTo(PaymentStatus.Rejected));
        Assert.That(responseContent.Reason, Is.EqualTo("Invalid currency code, must contain only 3 characters"));
    }

    [TestCase(12345, HttpStatusCode.BadRequest)]
    [TestCase(12, HttpStatusCode.BadRequest)]
    [TestCase(1, HttpStatusCode.BadRequest)]
    public async Task ValidateSecurityCode_ReturnsBadRequest(int cvv, HttpStatusCode statusCode)
    {
        // Arrange
        SetHttpClient();

        // Act 
        var dateTime = DateTime.UtcNow.AddMonths(5);
        var request = new PostPaymentRequest() { Amount = -10, Currency = "GBP", CardNumber = 1234567890123457, ExpiryMonth = dateTime.Month, ExpiryYear = dateTime.Year, Cvv = cvv };
        var body = JsonContent.Create(request, options: new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        });
        var response = await _httpClient.PostAsync($"/api/payments", body);
        var responseContent = await response.Content.ReadFromJsonAsync<PostPaymentRejectedResponse>();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(statusCode));
        Assert.That(responseContent?.Status, Is.EqualTo(PaymentStatus.Rejected));
        Assert.That(responseContent.Reason, Is.EqualTo("Invalid CVV, must contain only 3-4 numeric characters"));

    }


    public void SetHttpClient()
    {
        _httpClient = _webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
            .AddSingleton(_paymentsRepositoryMock.Object)
            .AddSingleton(_bankClientMock.Object)))
        .CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _webApplicationFactory.Dispose();
    }
}