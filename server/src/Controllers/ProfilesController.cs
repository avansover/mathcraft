using System.Security.Claims;
using Mathcraft.Server.Features.Profiles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mathcraft.Server.Controllers;

[Authorize]
[Route("api/profiles")]
public class ProfilesController(IMediator mediator) : ApiController
{
    private Guid AccountId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new InvalidOperationException("Account ID claim missing."));

    [HttpGet]
    public async Task<IActionResult> GetProfiles()
    {
        var result = await mediator.Send(new GetProfilesQuery(AccountId));
        return MapResult(result, Ok);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProfile([FromBody] CreateProfileRequest request)
    {
        var result = await mediator.Send(new CreateProfileCommand(AccountId, request.DisplayName, request.AvatarId, request.Age));
        return MapResult(result, data => StatusCode(201, data));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdateProfileRequest request)
    {
        var result = await mediator.Send(new UpdateProfileCommand(AccountId, id, request.DisplayName, request.AvatarId, request.Age));
        return MapResult(result, Ok);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProfile(Guid id)
    {
        var result = await mediator.Send(new DeleteProfileCommand(AccountId, id));
        return MapResult(result, _ => NoContent());
    }
}

// Request DTOs
public record CreateProfileRequest(string DisplayName, int AvatarId, int Age);
public record UpdateProfileRequest(string? DisplayName, int? AvatarId, int? Age);
