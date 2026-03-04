using Microsoft.AspNetCore.Mvc;
using Pandora.Core.Features.Identity;

namespace Pandora.Api.Controllers;

[ApiController]
[Route("identity")]
public class IdentityController : ControllerBase
{
    private readonly IIdentityAuthService _authService;

    public IdentityController(IIdentityAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var token = ReadBearerToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized();
        }

        var identity = await _authService.GetIdentityAsync(token, cancellationToken).ConfigureAwait(false);
        return identity is null ? Unauthorized() : Ok(identity);
    }

    private string? ReadBearerToken()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            return null;
        }

        const string bearerPrefix = "Bearer ";
        return header.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? header[bearerPrefix.Length..].Trim()
            : null;
    }
}
