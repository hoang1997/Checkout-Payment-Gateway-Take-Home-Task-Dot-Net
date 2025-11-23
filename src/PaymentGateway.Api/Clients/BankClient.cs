
using System.Text.RegularExpressions;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Serializer;

namespace PaymentGateway.Api.Clients
{
    public class BankClient : IBankClient
    {
        HttpClient _httpClient;
        public BankClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PostAcquirerResponse> AuthorizeTransaction(PostAcquirerRequest request)
        {
            var validateRequest = ValidateRequest(request, out string validationMessage);
            if (!validateRequest)
            {
                throw new ArgumentException(validationMessage);
            }

            HttpContent content = JsonContent.Create(request, options: SerializerOptions.JsonSerializerOptionsInstance);

            var response = await _httpClient.PostAsync($"/payments", content);

            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();

            var acquirerResponse = await response.Content.ReadFromJsonAsync<PostAcquirerResponse>(SerializerOptions.JsonSerializerOptionsInstance);

            return acquirerResponse;
        }

        public bool ValidateRequest(PostAcquirerRequest request, out string validationMessage)
        {
            validationMessage = string.Empty;

            if(request.Amount <= 0)
            {
                validationMessage = "Amount cannot be zero";
                return false;
            }

            if (string.IsNullOrEmpty(request.Currency) || request.Currency.Length != 3)
            {
                validationMessage = "Currency is required";
                return false;
            }
            if (string.IsNullOrEmpty(request.CardNumber) || !long.TryParse(request.CardNumber, out _))
            {
                validationMessage = "Invalid card number";
                return false;
            }
            if (string.IsNullOrEmpty(request.ExpiryDate) || !Regex.IsMatch(request.ExpiryDate, @"^(0[1-9]|1[0-2])\/\d{2}$"))
            {
                validationMessage = "Invalid expiry date format. Use MM/YY";
                return false;
            }
            if (string.IsNullOrEmpty(request.Cvv) || request.Cvv.Length != 3 || !int.TryParse(request.Cvv, out _))
            {
                validationMessage = "Invalid CVV";
                return false;
            }
            return true;
        }
    }
}
