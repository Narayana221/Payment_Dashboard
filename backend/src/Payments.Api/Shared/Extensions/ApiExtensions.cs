using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using Payments.Api.Shared.Errors;
using Payments.Api.Shared.Options;

namespace Payments.Api.Shared.Extensions;

/// <summary>HTTP-edge concerns: errors, CORS, health and security headers.</summary>
public static class ApiExtensions
{
    private const string FrontendCorsPolicy = "Frontend";

    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        // Strict numbers: amounts are only ever JSON numbers (never numeric strings). They're capped at ±10^12,
        // well inside JavaScript's safe integer range.
        services.AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict);
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddHealthChecks();

        services.AddOptions<FrontendOptions>()
            .Bind(configuration.GetSection("Payments"))
            .Validate(o => o.HasValidOrigin(), "Payments:AllowedOrigin must be a bare origin such as http://localhost:4200.")
            .ValidateOnStart();

        // Read-only API: only the configured frontend origin, only GET. No credentials are involved.
        services.AddCors();
        services.AddOptions<CorsOptions>().Configure<IOptions<FrontendOptions>>((cors, frontend) =>
            cors.AddPolicy(FrontendCorsPolicy, policy => policy
                .WithOrigins(frontend.Value.AllowedOrigin)
                .WithMethods(HttpMethods.Get)
                .WithHeaders("Content-Type", "Accept")));

        return services;
    }

    public static WebApplication UseApi(this WebApplication app)
    {
        // Registered when the response starts, so it survives the exception handler clearing headers on a 500.
        app.Use((context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.XContentTypeOptions = "nosniff";
                return Task.CompletedTask;
            });
            return next(context);
        });

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseCors(FrontendCorsPolicy);

        app.MapControllers();
        app.MapHealthChecks("/health");

        return app;
    }
}
