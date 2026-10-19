using Bookshelf.Client.Authentication;
using Bookshelf.Data;
using Microsoft.AspNetCore.Identity;

namespace Bookshelf.Endpoints;

public static class AuthenticationEndpoints
{
    private const string EndpointGroup = "/api/auth";

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup(EndpointGroup);

        auth.MapPost("/register", async (RegisterUserRequest request, UserManager<AppUser> users) =>
            {
                var user = new AppUser
                {
                    UserName = request.Username.Trim(),
                    Email = request.Email.Trim(),
                    FirstName = request.FirstName.Trim()
                };

                var result = await users.CreateAsync(user, request.Password);

                return result.Succeeded
                    ? Results.Ok()
                    : Results.ValidationProblem(result.Errors
                        .GroupBy(e => e.Code)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));
            });

        auth.MapPost("/login", async (LoginUserRequest request, UserManager<AppUser> users,
                SignInManager<AppUser> signIn) =>
            {
                var user = await users.FindByEmailAsync(request.Email);

                if (user is null) return Results.Unauthorized();

                var result = await signIn.PasswordSignInAsync(user, request.Password, request.RememberMe, true);

                if (result.Succeeded) return Results.Ok();
                if (result.IsLockedOut) return Results.Problem("Too many failed login attempts. Try again later.", statusCode: StatusCodes.Status429TooManyRequests);
                return Results.Unauthorized();
            });

        auth.MapPost("/logout", async (SignInManager<AppUser> signIn) =>
        {
            await signIn.SignOutAsync();
            return Results.Ok();
        }).RequireAuthorization();
    }
}