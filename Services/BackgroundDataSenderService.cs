namespace VSMSWebClient.Services
{
    public class BackgroundDataSenderService : BackgroundService
    {
        private readonly ILogger<BackgroundDataSenderService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(1);

        public BackgroundDataSenderService(
            ILogger<BackgroundDataSenderService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background Data Sender Service started. Interval: {Interval} seconds", _interval.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendDataAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in background data sending");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("Background Data Sender Service stopped");
        }

        private async Task SendDataAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            var dataTransferService = scope.ServiceProvider.GetRequiredService<DataTransferService>();
            var requestRepository = scope.ServiceProvider.GetRequiredService<RequestRepositoryService>();

            //  Checking if the server is configured
            var (ip, port) = dataTransferService.GetServerSettings();
            if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(port))
            {
                _logger.LogWarning("Server settings not configured. Skipping automatic send.");
                return;
            }

            try
            {
                // send requests table data to server
                var requests = await requestRepository.GetAllRequestsAsync();
                if (requests.Count != 0)
                {
                    _logger.LogInformation("Auto-sending {Count} requests to server {Ip}:{Port}",
                    requests.Count, ip, port);

                    var success = await dataTransferService.SendAllRequestsToServerAsync(requests);

                    if (success)
                        _logger.LogInformation("Auto-send successful: {Count} requests sent", requests.Count);
                    else
                        _logger.LogWarning("Auto-send failed for {Count} requests or server unreachable", requests.Count);
                }
                else 
                    _logger.LogInformation("No requests to send automatically");

                // get requestsFromServer table from server 
                var requestsFromServer = await dataTransferService.DownloadAllRequestsFromServerAsync();

                if (requestsFromServer == null)
                {
                    _logger.LogWarning("Failed to download requests from server - returned null");
                }

                if (requestsFromServer != null)
                {
                    var changesCount = await requestRepository.SyncRequestsFromServerAsync(requestsFromServer);
                    _logger.LogInformation("Auto-downloaded {Count} requests from server", requestsFromServer.Count);
                }
                else
                {
                    _logger.LogInformation("No requests found on server for download");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during automatic data sending");
            }
        }
    }
}