using System.Text.Json;

namespace PaymentGateway.Api.Serializer
{
    public static class SerializerOptions
    {
        public static readonly JsonSerializerOptions JsonSerializerOptionsInstance = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };
    }
}
