using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Services.AcquirerService;

namespace PaymentGateway.Api.Tests
{
    internal class BankClientIntegrationTests
    {
        private const string AuthorizedCardNumber = "2222405343248875";
        private const string UnauthorizedCardNumber = "2222405343248876";
        private const string ErrorCardNumber = "2222405343248870";
        BankClient _sut;
        HttpClient _httpClient;
        PostAcquirerRequest _paymentRequest;


        [SetUp]
        public void Setup()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new System.Uri("http://localhost:8080");
            _sut = new BankClient(_httpClient);
            _paymentRequest = new PostAcquirerRequest
            {
                CardNumber = AuthorizedCardNumber,
                ExpiryDate = "04/25",
                Cvv = "123",
                Amount = 100,
                Currency = "GBP"
            };
        }

        [Test]
        public void AcquirerService_AuthorizesTransaction_ReturnsAuthorized()
        {
            _paymentRequest.CardNumber = AuthorizedCardNumber;
            // Act
            var result = _sut.AuthorizeTransaction(_paymentRequest);

            // Assert
            Assert.That(result.Result.Authorized, Is.True);
            Assert.That(result.Result.AuthorizationCode, Is.Not.Null);
        }

        [Test]
        public void AcquirerService_AuthorizesTransaction_ReturnsUnauthorized()
        {
            
            _paymentRequest.CardNumber = UnauthorizedCardNumber;

            var result = _sut.AuthorizeTransaction(_paymentRequest);

            Assert.That(result.Result.Authorized, Is.False);
        }

        [Test]
        public void AcquirerService_AuthorizesTransaction_ReturnsInternalServerError()
        {

            _paymentRequest.CardNumber = ErrorCardNumber;

            Assert.ThrowsAsync<HttpRequestException>(async () => await _sut.AuthorizeTransaction(_paymentRequest));
        }

        [TearDown]
        public void TearDown()
        {
            _httpClient.Dispose();
        }
    }
}
