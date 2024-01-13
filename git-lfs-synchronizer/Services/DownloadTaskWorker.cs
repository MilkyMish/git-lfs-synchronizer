
using System.Net.Sockets;
using System.Net;
using git_lfs_synchronizer.Configuration;

namespace git_lfs_synchronizer.Services
{
    public class DownloadTaskWorker : BackgroundService
    {
        private readonly DownloadsManager _downloadsManager;
        private readonly ILogger<DownloadTaskWorker> _logger;
        private readonly MainConfiguration _config;

        public DownloadTaskWorker(DownloadsManager downloadsManager, ILogger<DownloadTaskWorker> logger, MainConfiguration mainConfiguration)
        {
            _downloadsManager = downloadsManager;
            _logger = logger;
            _config = mainConfiguration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested && !_config.IsServer)
            {
                var listener = new TcpListener(IPAddress.Any, _config.TcpPort);  // local IP to listen at, do not change
                try
                {
                    var downloadTask = await _downloadsManager.GetTaskFromQueueAsync(stoppingToken);
                    _logger.LogInformation("Downloading {SavePath} ...", downloadTask.SavePath);

                    listener.Start();

                    using (var client = await listener.AcceptTcpClientAsync(stoppingToken))
                    using (var networkStream = client.GetStream())
                    using (var fileStream = new FileStream(downloadTask.SavePath, FileMode.Create, FileAccess.Write))
                    {
                        await networkStream.CopyToAsync(fileStream, stoppingToken);
                        _logger.LogInformation("Downloaded {SavePath}",  downloadTask.SavePath);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                    throw;
                }
            }
        }
    }
}
