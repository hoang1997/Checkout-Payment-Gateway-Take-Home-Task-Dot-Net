namespace PaymentGateway.Api.Models.Responses
{
    public class PostPaymentRejectedResponse
    {
        public PostPaymentRejectedResponse(string reason) {
            Reason = reason;
        }
        public PaymentStatus Status { get; private set; } = PaymentStatus.Rejected;

        public string Reason { get; set; }
    }
}
