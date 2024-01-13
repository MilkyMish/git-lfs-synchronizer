
using git_lfs_synchronizer.Models;
using System.Threading.Channels;

namespace git_lfs_synchronizer.Services
{
    public class UploadsManager
    {
        private readonly Channel<UploadTask> _uploadTaskQueue;

        public UploadsManager()
        {
            _uploadTaskQueue = Channel.CreateUnbounded<UploadTask>();
        }

        public async Task AddTaskToQueue(UploadTask task)
        {
            await _uploadTaskQueue.Writer.WriteAsync(task);
        }

        public async Task<UploadTask> GetTaskFromQueueAsync(CancellationToken ct)
        {
            return await _uploadTaskQueue.Reader.ReadAsync(ct);
        }
    }
}
