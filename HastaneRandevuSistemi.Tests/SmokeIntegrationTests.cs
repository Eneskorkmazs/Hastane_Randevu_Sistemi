using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HastaneRandevuSistemi.Tests;

public class SmokeIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SmokeIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Home_Page_Should_Load_With_Security_Headers()
    {
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Ana Sayfa", content);
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
    }

    [Fact]
    public async Task Login_Page_Should_Render_Without_Redirect()
    {
        var response = await _client.GetAsync("/Account/Login");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("loginForm", content);
        Assert.Contains("Hesap türünüzü seçin", content);
    }

    [Fact]
    public async Task Appointment_Api_Should_Require_Authorization()
    {
        var response = await _client.GetAsync("/api/AppointmentApi");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
