using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Clients
{
    public interface IBankClient
    {
        public Task<PostAcquirerResponse> AuthorizeTransaction(PostAcquirerRequest request);
    }
}
