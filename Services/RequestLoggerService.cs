using System.Text.Json;

namespace VSMSWebClient.Services
{
    public class RequestLoggerService
    {
        private readonly ILogger<RequestLoggerService> _logger;

        // /api/MegafonCallback DB status write logging
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
    }
}