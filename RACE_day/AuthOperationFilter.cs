using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

// Swagger filter for accurate auth lock icons.
namespace RaceDay.API;

public class AuthOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Check if the method or controller has [AllowAnonymous]
        var hasAllowAnonymous = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AllowAnonymousAttribute>()
            .Any()
            || (context.MethodInfo.DeclaringType?
                .GetCustomAttributes(true)
                .OfType<AllowAnonymousAttribute>()
                .Any() ?? false);

        if (hasAllowAnonymous)
        {
            // Public endpoint - no lock
            return;
        }

        // Check if method or class has [Authorize]
        var hasAuthorize = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>()
            .Any()
            || (context.MethodInfo.DeclaringType?
                .GetCustomAttributes(true)
                .OfType<AuthorizeAttribute>()
                .Any() ?? false);

        if (!hasAuthorize)
        {
            // No auth required - no lock
            return;
        }

        // Endpoint requires auth - add the lock icon
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });

        var securityScheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        };

        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new OpenApiSecurityRequirement
            {
                [securityScheme] = Array.Empty<string>()
            }
        };
    }
}