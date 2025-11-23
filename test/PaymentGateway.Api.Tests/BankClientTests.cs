using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Clients;

namespace PaymentGateway.Api.Tests
{
    internal class BankClientTests
    {

        PostAcquirerRequest _request;

        [SetUp] 
        public void Setup()
        {
            _request = new PostAcquirerRequest
            {
                Amount = 100,
                Currency = "USD",
                CardNumber = "1234567812345678",
                ExpiryDate = "12/25",
                Cvv = "123"
            };
        }


        [Test]
        public void ValidPostAcquirerRequest_ReturnsTrue()
        {
            var httpClient = new HttpClient();
            var sut = new BankClient(httpClient);
            var sut2  = new BankClient(httpClient);

            var isValid = sut.ValidateRequest(_request, out string validationMessage);

            Assert.That(isValid, Is.True);
        }

        [Test]
        public void InvalidPostAcquirerRequest_AmountIsZero_ReturnsFalse()
        {
            var httpClient = new HttpClient();
            var sut = new BankClient(httpClient);

            _request.Amount = 0;

            var isValid = sut.ValidateRequest(_request, out string validationMessage);

            Assert.That(isValid, Is.False);
            Assert.That(validationMessage, Is.EqualTo("Amount cannot be zero"));
        }

        [TestCase("")]
        [TestCase(" ")]
        public void InvalidPostAcquirerRequest_Currency_ReturnsFalse(string currency)
        {
            var httpClient = new HttpClient();
            var sut = new BankClient(httpClient);

            _request.Currency = currency;

            var isValid = sut.ValidateRequest(_request, out string validationMessage);

            Assert.That(isValid, Is.False);
            Assert.That(validationMessage, Is.EqualTo("Currency is required"));
        }

        [TestCase("1234567812345678a")]
        [TestCase("")]
        public void InvalidPostAcquirerRequest_CardNumber_ReturnsFalse(string cardNumber)
        {
            var httpClient = new HttpClient();
            var sut = new BankClient(httpClient);

            _request.CardNumber = cardNumber;

            var isValid = sut.ValidateRequest(_request, out string validationMessage);

            Assert.That(isValid, Is.False);
            Assert.That(validationMessage, Is.EqualTo("Invalid card number"));
        }

        [TestCase("13/2025")]
        [TestCase("-12/226")]
        [TestCase("-12/2026a")]
        public void InvalidPostAcquirerRequest_ExpiryDate_ReturnsFalse(string expiryDate)
        {
            var httpClient = new HttpClient();
            var sut = new BankClient(httpClient);

            _request.ExpiryDate = expiryDate;

            var isValid = sut.ValidateRequest(_request, out string validationMessage);

            Assert.That(isValid, Is.False);
            Assert.That(validationMessage, Is.EqualTo("Invalid expiry date format. Use MM/YYYY"));
        }

        [TestCase("13/25")]
        [TestCase("-12/26")]
        [TestCase("-12/26a")]
        public void InvalidPostAcquirerRequest_SecurityCode_ReturnsFalse(string expiryDate)
        {
            var httpClient = new HttpClient();
            var sut = new BankClient(httpClient);

            _request.ExpiryDate = expiryDate;

            var isValid = sut.ValidateRequest(_request, out string validationMessage);

            Assert.That(isValid, Is.False);
            Assert.That(validationMessage, Is.EqualTo("Invalid expiry date format. Use MM/YY"));
        }
    }
}
