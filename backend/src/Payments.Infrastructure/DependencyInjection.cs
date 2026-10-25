using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payments.Application.Abstractions;
using Payments.Infrastructure.Options;
using Payments.Infrastructure.Records;

namespace Payments.Infrastructure;

public static class DependencyInjection
{
    /// <param name="contentRootPath">Base for a relative <c>Payments:RecordsPath</c>.</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        services.AddOptions<PaymentsOptions>()
            .Bind(configuration.GetSection(PaymentsOptions.SectionName))
            .PostConfigure(o =>
            {
                if (!string.IsNullOrWhiteSpace(o.RecordsPath))
                {
                    o.RecordsPath = Path.GetFullPath(o.RecordsPath, contentRootPath);
                }
            })
            .Validate(o => !string.IsNullOrWhiteSpace(o.RecordsPath), "Payments:RecordsPath is required.")
            // Fail at start-up, not on the first request: a wrong path is the likeliest deployment mistake.
            .Validate(o => File.Exists(o.RecordsPath), "Payments:RecordsPath does not point to an existing file.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.OrganiserId), "Payments:OrganiserId is required.")
            .Validate(o => o.MaxFileBytes > 0, "Payments:MaxFileBytes must be positive.")
            .ValidateOnStart();

        // Singleton: loads the file once and shares the immutable ledger across requests.
        services.AddSingleton<IRecordSource, JsonFileRecordSource>();
        return services;
    }
}
