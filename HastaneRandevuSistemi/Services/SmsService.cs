using System.Globalization;
using System.Net;
using System.Text;

namespace HastaneRandevuSistemi.Services
{
    public class SmsService
    {
        private const string DefaultNetGsmEndpoint = "https://api.netgsm.com.tr/sms/send/get/";

        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsService> _logger;
        private readonly HttpClient _httpClient;

        public SmsService(IConfiguration configuration, ILogger<SmsService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<bool> SendAppointmentSmsAsync(string? phoneNumber, string message)
        {
            if (!_configuration.GetValue("SmsSettings:Enabled", false))
            {
                _logger.LogInformation("SMS gonderimi pasif. SmsSettings:Enabled=false");
                return false;
            }

            var normalizedPhone = NormalizePhoneNumber(phoneNumber);
            if (string.IsNullOrWhiteSpace(normalizedPhone))
            {
                _logger.LogWarning("SMS atlandi. Telefon numarasi gecersiz.");
                return false;
            }

            var userCode = _configuration["SmsSettings:NetGsm:UserCode"];
            var password = _configuration["SmsSettings:NetGsm:Password"];
            var msgHeader = _configuration["SmsSettings:NetGsm:MsgHeader"];
            var endpoint = _configuration["SmsSettings:NetGsm:Endpoint"] ?? DefaultNetGsmEndpoint;

            if (string.IsNullOrWhiteSpace(userCode) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(msgHeader))
            {
                _logger.LogWarning("SMS ayarlari eksik (UserCode/Password/MsgHeader). Gonderim atlandi.");
                return false;
            }

            try
            {
                var query = new Dictionary<string, string>
                {
                    ["usercode"] = userCode,
                    ["password"] = password,
                    ["gsmno"] = normalizedPhone,
                    ["message"] = message,
                    ["msgheader"] = msgHeader,
                    ["filter"] = "0",
                    ["startdate"] = "",
                    ["stopdate"] = "",
                    ["encoding"] = "TR"
                };

                var requestUrl = BuildUrl(endpoint, query);
                using var response = await _httpClient.GetAsync(requestUrl);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("SMS gonderimi basarisiz. Status: {StatusCode}, Body: {Body}", response.StatusCode, body);
                    return false;
                }

                // NetGSM'de basarili sonuc genellikle numeric message id doner.
                if (!long.TryParse(body.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                {
                    _logger.LogWarning("SMS yaniti beklenen formatta degil: {Body}", body);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMS gonderimi sirasinda hata olustu.");
                return false;
            }
        }

        private static string BuildUrl(string endpoint, IDictionary<string, string> query)
        {
            var sb = new StringBuilder(endpoint.TrimEnd('/'));
            sb.Append("/?");
            sb.Append(string.Join("&", query.Select(kvp =>
                $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(kvp.Value)}")));
            return sb.ToString();
        }

        private static string? NormalizePhoneNumber(string? rawPhone)
        {
            if (string.IsNullOrWhiteSpace(rawPhone))
            {
                return null;
            }

            var digits = new string(rawPhone.Where(char.IsDigit).ToArray());
            if (digits.StartsWith("90") && digits.Length == 12)
            {
                return digits;
            }

            if (digits.StartsWith("0") && digits.Length == 11)
            {
                return "9" + digits;
            }

            if (digits.Length == 10)
            {
                return "90" + digits;
            }

            return null;
        }
    }
}
