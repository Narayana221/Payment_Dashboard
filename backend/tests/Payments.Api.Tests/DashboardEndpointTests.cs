using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Payments.Application.Features.Dashboard;
using Payments.Application.Features.Dashboard.Dtos;
using Payments.Tests.Shared;

namespace Payments.Api.Tests;

public class DashboardEndpointTests(PaymentsApiFactory factory) : IClassFixture<PaymentsApiFactory>
{
    private const string DashboardPath = $"/api/v1/organisers/{PaymentsApiFactory.Organiser}/dashboard";

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_AsOfEndOfWeekend_ReturnsGoldenFigures()
    {
        var response = await _client.GetFromJsonAsync<DashboardResponseDto>($"{DashboardPath}?asOf=2026-06-21T00:00:00Z");

        Assert.NotNull(response);
        var gbp = Assert.Single(response.Summaries);
        Assert.Equal(GoldenFigures.GrossSales, gbp.AllTime.GrossSales.AmountMinor);
        Assert.Equal(GoldenFigures.NetEarned, gbp.AllTime.NetEarned.AmountMinor);
        Assert.Equal(GoldenFigures.PaidOut, gbp.PaidOut.AmountMinor);
        Assert.Equal(GoldenFigures.StillToCome, gbp.StillToCome.AmountMinor);
        Assert.Equal(
            GoldenFigures.StillToComeByDate,
            gbp.StillToComeByDate.Select(b => (b.AvailableOn, b.Amount.AmountMinor)).ToList());
        Assert.All(ContractIdentities.All(gbp), r => Assert.True(r.Residual == 0, $"{r.Check} is off by {r.Residual}"));
    }

    [Fact]
    public async Task Get_AsOfEndOfWeekend_UsesCamelCaseNamesAndStringEnums()
    {
        using var json = JsonDocument.Parse(await _client.GetStringAsync($"{DashboardPath}?asOf=2026-06-21T00:00:00Z"));
        var root = json.RootElement;

        var gross = root.GetProperty("summaries")[0].GetProperty("allTime").GetProperty("grossSales");
        Assert.Equal(GoldenFigures.GrossSales, gross.GetProperty("amountMinor").GetInt64());
        Assert.Equal("GBP", gross.GetProperty("currency").GetString());
        Assert.Equal("day", root.GetProperty("summaries")[0].GetProperty("periods")[0].GetProperty("period").GetString());
        Assert.Equal("duplicateRecord", root.GetProperty("dataQuality")[0].GetProperty("type").GetString());
    }

    [Fact]
    public async Task Get_WithoutAsOf_UsesTheServerClock()
    {
        var response = await _client.GetFromJsonAsync<DashboardResponseDto>(DashboardPath);

        Assert.NotNull(response);
        Assert.Equal(GoldenFigures.BeforePayoutArrived.AsOf, response.AsOf);
        Assert.Equal(GoldenFigures.BeforePayoutArrived.InTransit, Assert.Single(response.Summaries).InTransit.AmountMinor);
    }

    [Theory]
    [InlineData("2026-06-21T00:00:00")] // no offset
    [InlineData("21/06/2026")]
    [InlineData("not-a-date")]
    [InlineData("1999-12-31T23:59:59Z")] // out of range
    [InlineData("2100-01-01T00:00:00Z")]
    public async Task Get_InvalidAsOf_Returns400ProblemDetails(string asOf)
    {
        var response = await _client.GetAsync($"{DashboardPath}?asOf={Uri.EscapeDataString(asOf)}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("asOf", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Get_UnknownOrganiser_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync("/api/v1/organisers/acct_someone_else/dashboard");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Send_WriteMethod_Returns405ProblemDetails(string method)
    {
        var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod(method), DashboardPath));

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_ServiceThrows_Returns500WithoutExceptionDetails()
    {
        using var client = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDashboardService>();
            services.AddScoped<IDashboardService, ThrowingDashboardService>();
        })).CreateClient();

        var response = await client.GetAsync(DashboardPath);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain(ThrowingDashboardService.Secret, body, StringComparison.Ordinal);
        Assert.DoesNotContain("at Payments.", body, StringComparison.Ordinal);
        // The exception handler clears headers set earlier; nosniff must survive it.
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
    }

    private sealed class ThrowingDashboardService : IDashboardService
    {
        public const string Secret = "C:\\secret\\path\\records.json";

        public Task<DashboardResponseDto?> GetSummaryAsync(string organiserId, DateTimeOffset? asOf, CancellationToken cancellationToken) =>
            throw new InvalidOperationException($"Could not read {Secret}");
    }
}
