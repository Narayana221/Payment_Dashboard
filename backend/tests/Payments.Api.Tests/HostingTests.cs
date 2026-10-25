using System.Net;

namespace Payments.Api.Tests;

public class HostingTests(PaymentsApiFactory factory) : IClassFixture<PaymentsApiFactory>
{
    private const string DashboardPath = $"/api/v1/organisers/{PaymentsApiFactory.Organiser}/dashboard";

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(DashboardPath)]
    [InlineData("/api/v1/organisers/acct_someone_else/dashboard")]
    public async Task AnyResponse_SetsNoSniffHeader(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
    }

    [Fact]
    public async Task Get_FromAllowedOrigin_GetsCorsHeader()
    {
        var response = await _client.SendAsync(WithOrigin(PaymentsApiFactory.AllowedOrigin));

        Assert.Equal(PaymentsApiFactory.AllowedOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task Preflight_FromAllowedOrigin_AllowsOnlyGetWithoutCredentials()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, DashboardPath);
        request.Headers.Add("Origin", PaymentsApiFactory.AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "DELETE");

        var response = await _client.SendAsync(request);

        Assert.Equal("GET", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Methods")));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task Get_FromOtherOrigin_GetsNoCorsHeader()
    {
        var response = await _client.SendAsync(WithOrigin("https://evil.example"));

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private static HttpRequestMessage WithOrigin(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, DashboardPath);
        request.Headers.Add("Origin", origin);
        return request;
    }
}
