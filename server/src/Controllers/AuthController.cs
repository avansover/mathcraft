using Mathcraft.Server.Common;
using Mathcraft.Server.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mathcraft.Server.Controllers;

[Route("api/auth")]
public class AuthController(IMediator mediator, IRefreshTokenService refreshTokenService) : ApiController
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await mediator.Send(new RegisterCommand(request.Email, request.Password));
        return MapResult(result, data =>
        {
            refreshTokenService.SetCookie(Response, data.RefreshToken);
            return StatusCode(201, new { data.AccountId, data.Email });
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await mediator.Send(new LoginCommand(request.Email, request.Password));
        return MapResult(result, data =>
        {
            refreshTokenService.SetCookie(Response, data.RefreshToken);
            return Ok(new { data.AccessToken, data.AccountId });
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var cookie = Request.Cookies["refreshToken"];
        var result = await mediator.Send(new LogoutCommand(cookie));
        return MapResult(result, _ =>
        {
            refreshTokenService.ClearCookie(Response);
            return NoContent();
        });
    }

    [HttpGet("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var cookie = Request.Cookies["refreshToken"];
        var result = await mediator.Send(new RefreshTokenQuery(cookie));
        return MapResult(result, data =>
        {
            refreshTokenService.SetCookie(Response, data.NewRefreshToken);
            return Ok(new { data.AccessToken, data.AccountId });
        });
    }

    [HttpPost("request-password-reset")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetRequest request)
    {
        var result = await mediator.Send(new RequestPasswordResetCommand(request.Email));
        return MapResult(result, data => Ok(new { message = data }));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await mediator.Send(new ResetPasswordCommand(request.Token, request.NewPassword));
        return MapResult(result, data => Ok(new { message = data }));
    }
}

// Request DTOs
public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
public record RequestPasswordResetRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);
