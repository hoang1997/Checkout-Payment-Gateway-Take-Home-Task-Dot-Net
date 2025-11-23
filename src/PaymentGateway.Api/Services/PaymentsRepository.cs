using System.Reflection.Metadata;

using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public class PaymentsRepository : IPaymentsRepository
{
    public Dictionary<Guid,PostPaymentResponse> Payments = new();
    
    public void Add(PostPaymentResponse payment)
    {
        Payments.TryAdd(payment.Id, payment);
    }

    public PostPaymentResponse? Get(Guid id)
    {
        Payments.TryGetValue(id, out PostPaymentResponse response);

        return response;
    }
}