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

        public async Task<byte[]> GetLfsFile(string repoPath, string fileName)
        {
            if (_repos.Any(r => r.Path == repoPath) && _repos.First(r => r.Path == repoPath).LfsFiles.ContainsKey(fileName))
            {
                var filePath = _repos.First(r => r.Path == repoPath).LfsFiles[fileName];
                return await File.ReadAllBytesAsync(filePath);
            }

            throw new FileNotFoundException(fileName);
        }

        public void SaveFile(string path, string fileName, Stream stream)
        {
            var directoryPath = path.Replace(fileName, string.Empty);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            using (var fileStream = File.Create(path))
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(fileStream);
            }
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
