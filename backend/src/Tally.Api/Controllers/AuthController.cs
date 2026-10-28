using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Tally.Api.Dtos;
using Tally.Api.Infrastructure;
using Tally.Api.Models;
using Tally.Api.Services;

namespace Tally.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(UserManager<AppUser> users, ITokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (await users.FindByEmailAsync(request.Email) is not null)
            return Conflict(new ProblemDetails { Title = "Email already registered", Detail = "Sign in instead, or use a different email." });

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName.Trim()
        };

        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(error.Code, error.Description);
            return ValidationProblem(ModelState);
        }

        await users.AddToRoleAsync(user, Roles.User);
        return await BuildResponse(user);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null || !await users.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new ProblemDetails { Title = "Sign in failed", Detail = "Email or password is incorrect." });

        return await BuildResponse(user);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me()
    {
        var user = await users.FindByIdAsync(User.GetUserId());
        if (user is null) return Unauthorized();
        return await ToDto(user);
    }

    private async Task<AuthResponse> BuildResponse(AppUser user)
    {
        var dto = await ToDto(user);
        var (token, expires) = tokens.CreateToken(user, dto.Roles);
        return new AuthResponse(token, expires, dto);
    }

    private async Task<UserDto> ToDto(AppUser user)
    {
        var roles = await users.GetRolesAsync(user);
        return new UserDto(user.Id, user.Email!, user.DisplayName, user.Currency, roles.ToList());
    }
}
