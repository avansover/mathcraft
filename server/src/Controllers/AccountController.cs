using System.Security.Claims;
using Mathcraft.Server.Features.Account;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mathcraft.Server.Controllers;

[Authorize]
[Route("api/account")]
public class AccountController(IMediator mediator) : ApiController
{
    private Guid AccountId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new InvalidOperationException("Account ID claim missing."));

    [HttpPost("set-pin")]
    public async Task<IActionResult> SetPin([FromBody] SetPinRequest request)
    {
        var result = await mediator.Send(new SetPinCommand(AccountId, request.Pin));
        return MapResult(result, data => Ok(new { message = data }));
    }

    [HttpPost("verify-pin")]
    public async Task<IActionResult> VerifyPin([FromBody] VerifyPinRequest request)
    {
        var result = await mediator.Send(new VerifyPinCommand(AccountId, request.Pin));
        return MapResult(result, data => Ok(new { verified = data }));
    }

    [HttpDelete("pin")]
    public async Task<IActionResult> DeletePin()
    {
        var result = await mediator.Send(new DeletePinCommand(AccountId));
        return MapResult(result, _ => NoContent());
    }
}

// Request DTOs
public record SetPinRequest(string Pin);
public record VerifyPinRequest(string Pin);
