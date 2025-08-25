using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace WebApi.Apis;

public static class BffApi
{
    public static void AddApisFromBff(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bff")
            .WithTags("bff");

        group.MapGet("/Login", async httpContext =>
        {
            await httpContext.ChallengeAsync("cookie", new()
                {RedirectUri = "/"});
        }).AllowAnonymous();

        group.MapGet("/SignInUser", async (HttpContext httpContext) =>
        {
            var claims = new List<Claim>()
            {
                // NOTE: These are hardcoded claims for demonstration purposes.
                new("sub", "1234"),
                new("name", "Bob"),
                new("email", "bob@tn-data.se"),
                new("role", "developer")
            };

            var identity = new ClaimsIdentity(claims: claims,
                authenticationType: "pwd",
                nameType: "name",
                roleType: "role");

            var principal = new ClaimsPrincipal(identity);

            var prop = new AuthenticationProperties()
            {
                RedirectUri = "/",
                Items =
                {
                    {"IpAddress", "192.168.0.3"},
                    {"ComputerName", "MyComputer"},
                    // WARNING: Hardcoding sensitive information like an API key is a security risk.
                    {"ApiKey", "Summer2025!!"}
                }
            };

            await httpContext.SignInAsync(scheme: "cookie",
                principal: principal,
                properties: prop);

            return Results.Redirect("/");
        }).AllowAnonymous();

        group.MapGet("/Logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync("cookie");
            return Results.Redirect("/");
        }).RequireAuthorization();

        group.MapPost("/Session", async (HttpContext httpContext) =>
        {
            // Get authentication result to access properties
            var authResult = await httpContext.AuthenticateAsync("cookie");

            // Extract user claims
            var claims = httpContext.User.Claims.Select(c => new
            {
                type = c.Type,
                value = c.Value
            }).ToArray();

            // Extract identity information
            var identity = new
            {
                authenticationType = httpContext.User.Identity?.AuthenticationType ?? "",
                isAuthenticated = httpContext.User.Identity?.IsAuthenticated ?? false,
                name = httpContext.User.Identity?.Name ?? "[Unknown]"
            };

            // Extract authentication properties
            var authProperties = new Dictionary<string, object>();
            if (authResult.Properties != null)
            {
                // Add standard properties
                if (authResult.Properties.IssuedUtc.HasValue)
                    authProperties["issuedUtc"] = authResult.Properties.IssuedUtc.Value;

                if (authResult.Properties.ExpiresUtc.HasValue)
                    authProperties["expiresUtc"] = authResult.Properties.ExpiresUtc.Value;

                if (!string.IsNullOrEmpty(authResult.Properties.RedirectUri))
                    authProperties["redirectUri"] = authResult.Properties.RedirectUri;

                // Add custom items
                foreach (var item in authResult.Properties.Items)
                {
                    authProperties[item.Key] = item.Value;
                }
            }

            // Calculate cookie lifetime information
            TimeSpan? remainingTime = null;
            if (authResult.Properties?.ExpiresUtc.HasValue == true)
            {
                remainingTime = authResult.Properties.ExpiresUtc.Value - DateTimeOffset.UtcNow;
            }

            var cookieLifetime = new
            {
                issuedUtc = authResult.Properties?.IssuedUtc,
                expiresUtc = authResult.Properties?.ExpiresUtc,
                remainingTime,
            };

            // Create comprehensive response
            var sessionInfo = new
            {
                timestamp = DateTimeOffset.UtcNow,
                claims,
                identity,
                authenticationProperties = authProperties,
                cookieLifetime
            };

            return Results.Json(sessionInfo, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }).RequireAuthorization();
    }
}