
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
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public DownloadTaskWorker(DownloadsManager downloadsManager, ILogger<DownloadTaskWorker> logger, MainConfiguration mainConfiguration)
        {
            _downloadsManager = downloadsManager;
            _logger = logger;
            _config = mainConfiguration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_config.IsServer)
            {
                return;
            }

            var listener = new TcpListener(IPAddress.Any, _config.TcpPort);  // local IP to listen at, do not change
            listener.Start();

            while (!stoppingToken.IsCancellationRequested)
            {
                await _semaphore.WaitAsync();
                try
                {
                    var downloadTask = await _downloadsManager.GetTaskFromQueueAsync(stoppingToken);
                    _logger.LogInformation("Downloading {SavePath} ...", downloadTask.SavePath);

                    CreateDirectory(downloadTask.SavePath);

                    using (var client = await listener.AcceptTcpClientAsync(stoppingToken))
                    using (var networkStream = client.GetStream())
                    using (var fileStream = new FileStream(downloadTask.SavePath, FileMode.Create, FileAccess.Write))
                    {
                        await networkStream.CopyToAsync(fileStream, stoppingToken);
                        _logger.LogInformation("Downloaded {SavePath}", downloadTask.SavePath);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                    throw;
                }
                finally { _semaphore.Release(); }
            }
        }

        private static void CreateDirectory(string path)
        {
            var fileName = Path.GetFileName(path);
            var directoryPath = path.Replace(fileName, string.Empty);

            Directory.CreateDirectory(directoryPath);
        }
    }
}
