
using System.Text.Json;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services.AcquirerService
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

            HttpContent content = JsonContent.Create(request, options: new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            });

            var response = await _httpClient.PostAsync($"/payments", content);

            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();

            var acquirerResponse = await response.Content.ReadFromJsonAsync<PostAcquirerResponse>(new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

            return acquirerResponse;
        }

        public bool ValidateRequest(PostAcquirerRequest request, out string validationMessage)
        {
            validationMessage = string.Empty;
            if (string.IsNullOrEmpty(request.Currency))
            {
                validationMessage = "Currency is required";
                return false;
            }
            if (string.IsNullOrEmpty(request.CardNumber) || request.CardNumber.Length != 16 || !long.TryParse(request.CardNumber, out _))
            {
                validationMessage = "Invalid card number";
                return false;
            }
            if (string.IsNullOrEmpty(request.ExpiryDate) || !System.Text.RegularExpressions.Regex.IsMatch(request.ExpiryDate, @"^(0[1-9]|1[0-2])\/\d{2}$"))
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
