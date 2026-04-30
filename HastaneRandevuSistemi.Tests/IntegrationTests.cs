using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HastaneRandevuSistemi.Tests;

/// <summary>
/// Uygulama genelinde kritik endpoint'lerin erişilebilirliğini ve
/// güvenlik davranışlarını doğrulayan integration testleri.
/// </summary>
public class IntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public IntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // ── Genel Erişim ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("/")]
    [InlineData("/Account/Login")]
    [InlineData("/Account/Register")]
    [InlineData("/Symptom/Index")]
    public async Task Public_Pages_Should_Return_200(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Kimlik Doğrulama Gerektiren Sayfalar ─────────────────────────────────

    [Theory]
    [InlineData("/Patient/Dashboard")]
    [InlineData("/Patient/Profile")]
    [InlineData("/Patient/MedicalHistory")]
    [InlineData("/Patient/Notifications")]
    [InlineData("/Patient/ChangePassword")]
    public async Task Patient_Pages_Should_Redirect_When_Not_Authenticated(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found,
            $"{url} için 302 beklendi, {response.StatusCode} geldi.");
    }

    [Theory]
    [InlineData("/Home/AdminDashboard")]
    [InlineData("/Home/AdminReport")]
    [InlineData("/Home/AdminAccounting")]
    [InlineData("/Home/Tahsilat")]
    [InlineData("/Doctor/Index")]
    [InlineData("/Department/Index")]
    public async Task Admin_Pages_Should_Redirect_When_Not_Authenticated(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found ||
            response.StatusCode == HttpStatusCode.Forbidden,
            $"{url} için 302/403 beklendi, {response.StatusCode} geldi.");
    }

    [Theory]
    [InlineData("/DoctorTools/Index")]
    [InlineData("/DoctorTools/Schedule")]
    public async Task Doctor_Pages_Should_Redirect_When_Not_Authenticated(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found,
            $"{url} için 302 beklendi, {response.StatusCode} geldi.");
    }

    // ── API Güvenliği ─────────────────────────────────────────────────────────

    [Fact]
    public async Task AppointmentApi_Should_Require_Bearer_Token()
    {
        var response = await _client.GetAsync("/api/AppointmentApi");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AppointmentApi_With_Invalid_Token_Should_Return_401()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid.token.here");
        var response = await _client.GetAsync("/api/AppointmentApi");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    // ── Güvenlik Başlıkları ───────────────────────────────────────────────────

    [Fact]
    public async Task All_Responses_Should_Have_Security_Headers()
    {
        var response = await _client.GetAsync("/");

        Assert.True(response.Headers.Contains("X-Content-Type-Options"),
            "X-Content-Type-Options başlığı eksik.");
        Assert.True(response.Headers.Contains("X-Frame-Options"),
            "X-Frame-Options başlığı eksik.");
        Assert.True(response.Headers.Contains("X-XSS-Protection"),
            "X-XSS-Protection başlığı eksik.");
        Assert.True(response.Headers.Contains("Content-Security-Policy"),
            "Content-Security-Policy başlığı eksik.");
    }

    // ── Randevu Oluşturma Formu ───────────────────────────────────────────────

    [Fact]
    public async Task Appointment_Create_Should_Redirect_Unauthenticated_User()
    {
        var response = await _client.GetAsync("/Appointment/Create");
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found,
            $"Randevu oluşturma için 302 beklendi, {response.StatusCode} geldi.");
    }

    // ── Semptom Kontrolcüsü ───────────────────────────────────────────────────

    [Fact]
    public async Task Symptom_Index_Should_Return_200()
    {
        var response = await _client.GetAsync("/Symptom/Index");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Symptom_Analyze_Post_Without_Symptoms_Should_Return_200()
    {
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("SelectedSymptoms", "")
        });
        var response = await _client.PostAsync("/Symptom/Analyze", form);
        // CSRF token olmadan 400 veya redirect beklenir
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found ||
            response.StatusCode == HttpStatusCode.OK);
    }

    // ── Doktor Değerlendirme ──────────────────────────────────────────────────

    [Fact]
    public async Task DoctorReview_Profile_Should_Return_200_Or_NotFound()
    {
        var response = await _client.GetAsync("/DoctorReview/Profile/1");
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"200 veya 404 beklendi, {response.StatusCode} geldi.");
    }

    [Fact]
    public async Task DoctorReview_Submit_Should_Require_Authentication()
    {
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("doctorId", "1"),
            new KeyValuePair<string, string>("rating", "5"),
            new KeyValuePair<string, string>("comment", "Test")
        });
        var response = await _client.PostAsync("/DoctorReview/Submit", form);
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found,
            "Giriş yapılmadan değerlendirme için redirect beklendi.");
    }
}
