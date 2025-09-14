using System.Text.Json;

namespace VSMSWebClient.Services
{
    public class RequestLoggerService
    {
        private readonly ILogger<RequestLoggerService> _logger;

        public RequestLoggerService(ILogger<RequestLoggerService> logger)
        {
            _logger = logger;
        }

        public void LogRequestStatusUpdate(string uuid, string status, bool success, string? errorMessage = null)
        {
            if (success)
                _logger.LogInformation("Status update SUCCESS: UUID={Uuid}, Status={Status}", uuid, status);
            else
                _logger.LogWarning("Status update FAILED: UUID={Uuid}, Status={Status}, Error={ErrorMessage}", uuid, status, errorMessage ?? "Request not found");
        }
        public void LogInformation(string str)
        {

            _logger.LogInformation(str);

        }

        public void LogWarning(string str)
        {
            _logger.LogWarning(str);
        }

        public void LogError(Exception ex, string str)
        {
            _logger.LogError(ex, str);
        }
    }
}