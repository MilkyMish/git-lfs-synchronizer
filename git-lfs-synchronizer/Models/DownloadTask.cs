namespace git_lfs_synchronizer.Models
{
    public class DownloadTask
    {
        public DownloadTask(string savePath)
        {
            SavePath = savePath;
        }

        public string SavePath { get; set; } = string.Empty;
    }
}
