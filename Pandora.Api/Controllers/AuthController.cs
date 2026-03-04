using Microsoft.AspNetCore.Mvc;
using Pandora.Core.Features.Identity;

namespace Pandora.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IIdentityAuthService _authService;

    public AuthController(IIdentityAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request.Email, cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            return Unauthorized();
        }

        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request, CancellationToken cancellationToken)
    {
        var token = request?.AccessToken ?? ReadBearerToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest();
        }

        var removed = await _authService.LogoutAsync(token, cancellationToken).ConfigureAwait(false);
        return removed ? NoContent() : NotFound();
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

public sealed class LoginRequest
{
    public string Email { get; init; } = string.Empty;
}

public sealed class LogoutRequest
{
    public string? AccessToken { get; init; }
}
