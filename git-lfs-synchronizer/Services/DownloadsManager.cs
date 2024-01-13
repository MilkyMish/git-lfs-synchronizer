
using git_lfs_synchronizer.Models;
using System.Threading.Channels;

namespace git_lfs_synchronizer.Services
{
    public class DownloadsManager
    {
        private readonly Channel<DownloadTask> _downloadTaskQueue;

        public DownloadsManager()
        {
            _downloadTaskQueue = Channel.CreateUnbounded<DownloadTask>();
        }

        public async Task AddTaskToQueue(DownloadTask task)
        {
            await _downloadTaskQueue.Writer.WriteAsync(task);
        }

        public async Task<DownloadTask> GetTaskFromQueueAsync(CancellationToken ct)
        {
            return await _downloadTaskQueue.Reader.ReadAsync(ct);
        }
    }
}
