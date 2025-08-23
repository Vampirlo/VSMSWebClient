using System.Text.Json;
using System.Text;
using VSMSWebClient.Models;

namespace VSMSWebClient.Services
{
    public class DataTransferService
    {
        private readonly IniFileService _iniService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<DataTransferService> _logger;

        public DataTransferService(IniFileService iniService, HttpClient httpClient, ILogger<DataTransferService> logger)
        {
            _iniService = iniService;
            _httpClient = httpClient;
            _logger = logger;

            // Настраиваем таймауты для HTTP запросов
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<bool> SendAllRequestsToServerAsync(List<Request> requests)
        {
            var serverIp = _iniService.ReadValue("VSMSWebServer", "ip");
            var serverPort = _iniService.ReadValue("VSMSWebServer", "port");

            if (string.IsNullOrEmpty(serverIp) || string.IsNullOrEmpty(serverPort))
            {
                _logger.LogWarning("Server IP or Port not configured in INI file");
                return false;
            }

            try
            {
                var url = $"http://{serverIp}:{serverPort}/api/requests/uploadAll";
                var json = JsonSerializer.Serialize(requests);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogDebug("Sending {Count} requests to {Url}", requests.Count, url);

                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully sent {Count} requests to server {Server}",
                        requests.Count, serverIp);
                    return true;
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to send requests. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode, responseContent);
                    return false;
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError("Timeout while sending requests to server");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending requests to server");
                return false;
            }
        }

        public (string ip, string port) GetServerSettings()
        {
            return (
                _iniService.ReadValue("VSMSWebServer", "ip"),
                _iniService.ReadValue("VSMSWebServer", "port")
            );
        }

        public void UpdateServerSettings(string ip, string port)
        {
            _iniService.WriteValue("VSMSWebServer", "ip", ip);
            _iniService.WriteValue("VSMSWebServer", "port", port);
            _logger.LogInformation("Server settings updated: {Ip}:{Port}", ip, port);
        }

        public bool IsServerConfigured()
        {
            var (ip, port) = GetServerSettings();
            return !string.IsNullOrEmpty(ip) && !string.IsNullOrEmpty(port);
        }
    }
}