namespace PaymentGateway.Api.Models.Requests
{
    public class PostAcquirerRequest
    {
        public string CardNumber { get; set; }
        public string ExpiryDate { get; set; }

        public int Amount { get; set; }

        public string Currency { get; set; }

        public string Cvv { get; set; }
    }
}
