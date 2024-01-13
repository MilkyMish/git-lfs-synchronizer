using git_lfs_synchronizer.Models;

namespace git_lfs_synchronizer.Services
{
    public class LfsService
    {
        private readonly string LfsObjectsDirectory = $".git{Path.DirectorySeparatorChar}lfs{Path.DirectorySeparatorChar}objects";
        private readonly List<Repo> _repos = new();

        public IEnumerable<string> GetLfsFileNames(string path)
        {
            var lfsObjectsPath = Path.Combine(path, LfsObjectsDirectory);
            var files = Directory.GetFiles(lfsObjectsPath, "*", SearchOption.AllDirectories);

            UpdateRepos(path, files);

            return files.Select(f => Path.GetFileName(f));
        }

        public FileStream GetLfsFile(string repoPath, string fileName)
        {
            if (_repos.Any(r => r.Path == repoPath) && _repos.First(r => r.Path == repoPath).LfsFiles.ContainsKey(fileName))
            {
                var filePath = _repos.First(r => r.Path == repoPath).LfsFiles[fileName];
                return new FileStream(filePath, FileMode.Open, FileAccess.Read);
            }

            throw new FileNotFoundException(fileName);
        }

        private void UpdateRepos(string path, IEnumerable<string> files)
        {
            if (_repos.Any(r => r.Path == path))
            {
                var repo = _repos.First(r => r.Path == path);

                repo.LfsFiles = files.ToDictionary(f => Path.GetFileName(f), f => f);
            }
            else
            {
                _repos.Add(new(path, files.ToDictionary(f => Path.GetFileName(f), f => f)));
            }
        }
    }
}
