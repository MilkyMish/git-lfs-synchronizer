namespace git_lfs_synchronizer.Models
{
    public class Repo
    {
        public Repo(string path, Dictionary<string, string> lfsFiles)
        {
            Path = path;
            LfsFiles = lfsFiles;
        }

        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Key value pair. Key - name. Value - path
        /// </summary>
        public Dictionary<string, string> LfsFiles { get; set; } = new();
    }
}
