namespace git_lfs_synchronizer.Models
{
    public class UploadTask
    {
        public string ClientAddress { get; set; } = string.Empty;
        public string RepoPath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;

        public UploadTask(string clientAddress, string repoPath, string fileName)
        {
            ClientAddress = clientAddress;
            RepoPath = repoPath;
            FileName = fileName;
        }
    }
}
