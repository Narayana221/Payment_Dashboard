using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Payments.Api.Shared.Extensions;
using Payments.Api.Shared.Options;
using Payments.Infrastructure;
using Payments.Infrastructure.Options;
using Payments.Tests.Shared.Fixtures;

namespace Payments.Api.Tests;

/// <summary>Misconfiguration must stop the app at start-up, not surface as 500s (or silent CORS failures) later.</summary>
public class StartupValidationTests
{
    private static readonly (string Key, string Value, string ExpectedInMessage)[] Invalid =
    [
        ("Payments:RecordsPath", "C:/definitely/missing/records.json", "RecordsPath"),
        ("Payments:AllowedOrigin", "http://localhost:4200/", "AllowedOrigin"),
        ("Payments:AllowedOrigin", "not a url", "AllowedOrigin"),
        ("Payments:OrganiserId", "", "OrganiserId"),
    ];

    public static TheoryData<string, string, string> InvalidSettings
    {
        get
        {
            var data = new TheoryData<string, string, string>();
            foreach (var (key, value, expected) in Invalid)
            {
                data.Add(key, value, expected);
            }

            return data;
        }
    }

    public static TheoryData<string, string> InvalidKeysAndValues
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var (key, value, _) in Invalid)
            {
                data.Add(key, value);
            }

            return data;
        }
    }

    /// <summary>
    /// The rules and their messages, checked against the app's real registrations. No host is started, so the result
    /// is deterministic: the test host's start-up failure path can surface either the validation error or an
    /// ObjectDisposedException from its own teardown.
    /// </summary>
    [Theory]
    [MemberData(nameof(InvalidSettings))]
    public void Options_WithInvalidSetting_FailValidationWithAClearMessage(string key, string value, string expectedInMessage)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payments:OrganiserId"] = PaymentsApiFactory.Organiser,
                ["Payments:RecordsPath"] = RealRecords.Path,
                ["Payments:AllowedOrigin"] = PaymentsApiFactory.AllowedOrigin,
                [key] = value,
            })
            .Build();
        var services = new ServiceCollection()
            .AddInfrastructure(configuration, AppContext.BaseDirectory)
            .AddApi(configuration);
        using var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<OptionsValidationException>(() =>
        {
            _ = provider.GetRequiredService<IOptions<PaymentsOptions>>().Value;
            _ = provider.GetRequiredService<IOptions<FrontendOptions>>().Value;
        });
        Assert.Contains(expectedInMessage, ex.Message, StringComparison.Ordinal);
    }

    /// <summary>And the real app refuses to start: the options are validated on start, not on first use.</summary>
    [Theory]
    [MemberData(nameof(InvalidKeysAndValues))]
    public void Start_WithInvalidSetting_DoesNotStart(string key, string value)
    {
        using var factory = new PaymentsApiFactory();
        using var misconfigured = factory.WithWebHostBuilder(builder => builder.UseSetting(key, value));

        Assert.ThrowsAny<Exception>(() => misconfigured.CreateClient());
    }
}
