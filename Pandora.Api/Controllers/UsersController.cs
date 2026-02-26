using Microsoft.AspNetCore.Mvc;
using Pandora.Core.Features.Users;

namespace Pandora.Api.Controllers;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<User>>> Get([FromQuery] UserQuery query, CancellationToken cancellationToken)
    {
        var results = new List<User>();

        await foreach (var user in _userService.QueryAsync(query, cancellationToken))
        {
            results.Add(user);
        }

        return Ok(results);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<User>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<User>> Create([FromBody] UpsertUserRequest request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Email = request.Email,
            DisplayName = request.DisplayName,
            IsActive = request.IsActive,
            Roles = request.Roles,
            Metadata = request.Metadata ?? new Dictionary<string, string?>()
        };

        var created = await _userService.CreateAsync(user, cancellationToken).ConfigureAwait(false);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<User>> Update(Guid id, [FromBody] UpsertUserRequest request, CancellationToken cancellationToken)
    {
        var existing = await _userService.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return NotFound();
        }

        var updated = await _userService.UpdateAsync(existing with
        {
            Email = request.Email,
            DisplayName = request.DisplayName,
            IsActive = request.IsActive,
            Roles = request.Roles,
            Metadata = request.Metadata ?? new Dictionary<string, string?>()
        }, cancellationToken).ConfigureAwait(false);

        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _userService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return deleted ? NoContent() : NotFound();
    }
}

public sealed class UpsertUserRequest
{
    public string Email { get; init; } = string.Empty;

    public string? DisplayName { get; init; }

    public bool IsActive { get; init; } = true;

    public List<string> Roles { get; init; } = new();

    public Dictionary<string, string?>? Metadata { get; init; }
}
