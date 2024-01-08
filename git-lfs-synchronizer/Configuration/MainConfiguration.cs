using git_lfs_synchronizer.Configuration.Models;

namespace git_lfs_synchronizer.Configuration
{
    public class MainConfiguration
    {
        public bool IsServer { get; set; }
        public IEnumerable<RepoConfig> Repos { get; set; }
    }
}
