namespace git_lfs_synchronizer.Controllers.Models
{
    public class RepoResponse
    {
        public RepoResponse(string name, List<string> lfsFileNames)
        {
            Name = name;
            LfsFileNames = lfsFileNames;
        }

        public string Name { get; set; } = string.Empty;
        public List<string> LfsFileNames { get; set; } = new();
    }
}
