using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Payments.Tests.Shared;
using Payments.Tests.Shared.Fixtures;

namespace Payments.Api.Tests;

/// <summary>
/// The real API over the real records.json, with a fixed clock (midday 17 June: po_0001 in transit).
/// Runs as Production so error responses are tested as clients would see them.
/// </summary>
public sealed class PaymentsApiFactory : WebApplicationFactory<Program>
{
    public const string Organiser = "acct_demo";
    public const string AllowedOrigin = "http://localhost:4200";

    public FakeTimeProvider Clock { get; } = new(GoldenFigures.BeforePayoutArrived.AsOf);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("Payments:RecordsPath", RealRecords.Path);
        builder.UseSetting("Payments:OrganiserId", Organiser);
        builder.UseSetting("Payments:AllowedOrigin", AllowedOrigin);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }
}
