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

        public DataTransferService(IniFileService iniService, ILogger<DataTransferService> logger)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
            (sender, cert, chain, sslPolicyErrors) => true
            };

            _iniService = iniService;
            _httpClient = new HttpClient(handler);
            _logger = logger;

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
                var url = $"https://{serverIp}:{serverPort}/api/requests/uploadAll";
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

        public async Task<List<RequestFromServer>?> DownloadAllRequestsFromServerAsync()
        {
            var serverIp = _iniService.ReadValue("VSMSWebServer", "ip");
            var serverPort = _iniService.ReadValue("VSMSWebServer", "port");

            if (string.IsNullOrEmpty(serverIp) || string.IsNullOrEmpty(serverPort))
            {
                _logger.LogWarning("Server IP or Port not configured in INI file");
                return null;
            }

            try
            {
                var url = $"https://{serverIp}:{serverPort}/api/requests/downloadAll";

                _logger.LogDebug("Downloading requests from {Url}", url);

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    
                    _logger.LogInformation("Received JSON from server: {Json}", json);
                    _logger.LogInformation("JSON length: {Length} characters", json.Length);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var requests = JsonSerializer.Deserialize<List<RequestFromServer>>(json, options);

                    _logger.LogInformation("Successfully downloaded {Count} requests from server {Server}",
                        requests?.Count ?? 0, serverIp);
                    return requests;
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to download requests. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode, responseContent);
                    return null;
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError("Timeout while downloading requests from server");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading requests from server");
                return null;
            }

        }

        public async Task<List<RequestFromServer>?> SyncFromServerAsync()
        {
            var serverIp = _iniService.ReadValue("VSMSWebServer", "ip");
            var serverPort = _iniService.ReadValue("VSMSWebServer", "port");

            if (string.IsNullOrEmpty(serverIp) || string.IsNullOrEmpty(serverPort))
            {
                _logger.LogWarning("Server IP or Port not configured in INI file");
                return null;
            }

            try
            {
                var url = $"https://{serverIp}:{serverPort}/api/requests/sync";

                _logger.LogDebug("Downloading requests from {Url}", url);

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    _logger.LogInformation("Received JSON from server: {Json}", json);
                    _logger.LogInformation("JSON length: {Length} characters", json.Length);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var requests = JsonSerializer.Deserialize<List<RequestFromServer>>(json, options);

                    _logger.LogInformation("Successfully downloaded {Count} requests from server {Server}",
                        requests?.Count ?? 0, serverIp);
                    return requests;
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to download requests. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode, responseContent);
                    return null;
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError("Timeout while downloading requests from server");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading requests from server");
                return null;
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