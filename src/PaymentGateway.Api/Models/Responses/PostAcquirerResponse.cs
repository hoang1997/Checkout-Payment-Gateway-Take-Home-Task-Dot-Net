namespace PaymentGateway.Api.Models.Responses
{
    public class PostAcquirerResponse
    {
        public bool Authorized { get; set; }
        public string AuthorizationCode { get; set; }
    }
}
