using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Services.AcquirerService;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly IBankClient _bankClient;
    private readonly IPaymentsRepository _paymentsRepository;

    public PaymentsController(IBankClient bankClient, IPaymentsRepository paymentsRepository)
    {
        _paymentsRepository = paymentsRepository;
        _bankClient = bankClient;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetPaymentResponse?>> GetPaymentAsync(Guid id)
    {
        var payment = _paymentsRepository.Get(id);

        if(payment == null)
        {
            return NotFound();
        }

        return new OkObjectResult(payment);
    }

    [HttpPost()]
    public async Task<ActionResult<PostPaymentResponse?>> PostPaymentAsync([FromBody]PostPaymentRequest request)
    {
        ValidateRequest(request, out string validationMessage);

        if(!string.IsNullOrEmpty(validationMessage))
        {
            return BadRequest(validationMessage);
        }

        var acquirerResponse = await _bankClient.AuthorizeTranaction(new PostAcquirerRequest());

        var response = new PostPaymentResponse()
        {
            Id = Guid.NewGuid(),
            Amount = request.Amount,
            Currency = request.Currency,
            CardNumberLastFour = (int)(request.CardNumber % 10000),
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Status = acquirerResponse.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined
        };

        _paymentsRepository.Add(response);

        return new OkObjectResult(response);
    }

    private bool ValidateRequest(PostPaymentRequest request, out string validationMessage)
    {
        var cardNumLength = request.CardNumber.ToString().Length;
        var cvvLength = request.Cvv.ToString().Length;
        validationMessage = "";
        if (cardNumLength < 14 || cardNumLength > 19)
        {
            validationMessage = "Invalid card number length";
        }

        if (request.ExpiryMonth < 1 || request.ExpiryMonth > 12)
        {
            validationMessage = "Invalid expiry month";
        }

        if (request.ExpiryYear < DateTime.UtcNow.Year)
        {
            validationMessage = "Invalid expiry year";
        }

        if (request.ExpiryYear == DateTime.UtcNow.Year && request.ExpiryMonth < DateTime.UtcNow.Month)
        {
            validationMessage = "Card expiry date must be in the future";
        }

        if (request.Currency.Length != 3)
        {
            validationMessage = "Invalid currency code, must contain only 3 characters";
        }

        if (cvvLength < 3 || cvvLength > 4)
        {
            validationMessage = "Invalid CVV, must contain only 3-4 numeric characters";
        }

        return validationMessage == "" ? true : false;
    }
}