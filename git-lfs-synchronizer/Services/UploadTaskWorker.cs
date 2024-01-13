using git_lfs_synchronizer.Configuration;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace git_lfs_synchronizer.Services
{
    public class UploadTaskWorker : BackgroundService
    {
        private readonly UploadsManager _uploadsManager;
        private readonly ILogger<UploadTaskWorker> _logger;
        private readonly MainConfiguration _config;
        private readonly LfsService _lfsService;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public UploadTaskWorker(UploadsManager uploadsManager, ILogger<UploadTaskWorker> logger, MainConfiguration mainConfiguration, LfsService lfsService)
        {
            _uploadsManager = uploadsManager;
            _logger = logger;
            _config = mainConfiguration;
            _lfsService = lfsService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested && _config.IsServer)
            {
                await _semaphore.WaitAsync();

                try
                {
                    var uploadTask = await _uploadsManager.GetTaskFromQueueAsync(stoppingToken);

                    _logger.LogInformation("Uploading {fileName} file to {ip}...", uploadTask.FileName, uploadTask.ClientAddress);
                    using (var fileStream = _lfsService.GetLfsFileStream(uploadTask.RepoPath, uploadTask.FileName))
                    using (var client = new TcpClient())
                    {
                        await client.ConnectAsync(uploadTask.ClientAddress, _config.TcpPort);
                        using (var netStream = client.GetStream())
                        {
                            await fileStream.CopyToAsync(netStream);
                            _logger.LogInformation($"Uploaded {uploadTask.FileName}");
                        }
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
    }
}
