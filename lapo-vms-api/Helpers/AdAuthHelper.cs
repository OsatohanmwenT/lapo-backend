
using lapo_vms_api.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;
using RestSharp.Authenticators;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace lapo_vms_api.API.Helpers
{
    public class AdAuthHelper
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdAuthHelper> _logger;

        public AdAuthHelper(IConfiguration configuration, ILogger<AdAuthHelper> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(bool success, string message)> ADLogin(Login login)
        {
            _logger.LogInformation("Starting AD login for user: {Username}", login.Username);

            var options = new RestClientOptions(_configuration["CentralAPI"])
            {
                Authenticator = new HttpBasicAuthenticator(
                    _configuration["BasicAuthUsername"],
                    _configuration["BasicAuthPassword"])
            };

            var client = new RestClient(options);

            try
            {
                var jsonString = JsonSerializer.Serialize(login);
                var encryptedJsonContent = Encrypt(jsonString, _configuration["EncDecPassword"]);

                var request = new RestRequest(_configuration["ADEndpoint"], Method.Post)
                    .AddJsonBody(new { dataValue = encryptedJsonContent });

                var response = await client.ExecuteAsync(request);

                if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(response.Content))
                {
                    _logger.LogWarning("AD login failed. StatusCode: {StatusCode}", response.StatusCode);
                    return (false, "Bad response from AD");
                }

                var encryptedContent = JsonSerializer.Deserialize<GeneralCentralApiResponse>(
                    response.Content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (encryptedContent?.responsedataValue == null)
                    return (false, "Invalid response from AD");

                var decryptedContent = Decrypt(encryptedContent.responsedataValue, _configuration["EncDecPassword"]);

                var resp = JsonSerializer.Deserialize<CentralApiADResponse>(
                    decryptedContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (resp?.success == true)
                {
                    _logger.LogInformation("AD login successful for user: {Username}", login.Username);
                    return (true, "Login successful");
                }

                return (false, "Invalid credentials");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AD login exception for user: {Username}", login.Username);
                return (false, "An error occurred during login");
            }
        }

        // ---------- ENCRYPT ----------
        private string Encrypt(string clearText, string password)
        {
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);

            using var aes = Aes.Create();
            var pdb = new Rfc2898DeriveBytes(password, new byte[]
            {
                0x49,0x76,0x61,0x6e,0x20,0x4d,0x65,0x64,0x76,0x65,0x64,0x65,0x76
            });

            aes.Key = pdb.GetBytes(32);
            aes.IV = pdb.GetBytes(16);

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cs.Write(clearBytes, 0, clearBytes.Length);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        // ---------- DECRYPT ----------
        private string Decrypt(string cipherText, string password)
        {
            cipherText = cipherText.Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            var pdb = new Rfc2898DeriveBytes(password, new byte[]
            {
                0x49,0x76,0x61,0x6e,0x20,0x4d,0x65,0x64,0x76,0x65,0x64,0x65,0x76
            });

            aes.Key = pdb.GetBytes(32);
            aes.IV = pdb.GetBytes(16);

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
            {
                cs.Write(cipherBytes, 0, cipherBytes.Length);
            }

            return Encoding.Unicode.GetString(ms.ToArray());
        }
    }
}