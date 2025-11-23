
using System.Text.Json;

using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

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

        public async Task<PostAcquirerResponse> AuthorizeTranaction(PostAcquirerRequest request)
        {
            HttpContent content = JsonContent.Create(request, options: new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            });

            var response = await _httpClient.PostAsync($"/payments", content);

            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };

            var acquirerResponse = JsonConvert.DeserializeObject<PostAcquirerResponse>(jsonString, settings)!;
            return acquirerResponse;
        }
    }
}
