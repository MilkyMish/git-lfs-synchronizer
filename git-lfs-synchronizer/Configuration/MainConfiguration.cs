using git_lfs_synchronizer.Configuration.Models;

namespace git_lfs_synchronizer.Configuration
{
    public class MainConfiguration
    {
        public int TcpFileSizeMb{ get; set; }
        public int TcpPort{ get; set; }
        public bool IsServer { get; set; }
        public IEnumerable<RepoConfig> Repos { get; set; } = Enumerable.Empty<RepoConfig>();
    }
}
