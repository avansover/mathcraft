using Mathcraft.Server.Common;
using Microsoft.AspNetCore.Mvc;

namespace Mathcraft.Server.Controllers;

[ApiController]
public abstract class ApiController : ControllerBase
{
    protected IActionResult MapResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.Success)
            return onSuccess(result.Data!);

        return result.ErrorCode switch
        {
            ErrorCode.Validation => BadRequest(result.Error),
            ErrorCode.NotFound => NotFound(result.Error),
            ErrorCode.Conflict => Conflict(result.Error),
            ErrorCode.Unauthorized => Unauthorized(result.Error),
            ErrorCode.Forbidden => Forbid(),
            ErrorCode.RateLimited => StatusCode(429, result.Error),
            _ => StatusCode(500, "An unexpected error occurred.")
        };
    }
}
