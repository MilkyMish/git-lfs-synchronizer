using git_lfs_synchronizer.Models;

namespace git_lfs_synchronizer.Services
{
    public class LfsService
    {
        private const int BytesInMegabyte = 1_000_000;
        private readonly string _lfsObjectsDirectory = $".git{Path.DirectorySeparatorChar}lfs{Path.DirectorySeparatorChar}objects";
        private readonly List<Repo> _repos = new();

        public IEnumerable<string> GetLfsFileNames(string path)
        {
            var lfsObjectsPath = Path.Combine(path, _lfsObjectsDirectory);
            var files = Directory.GetFiles(lfsObjectsPath, "*", SearchOption.AllDirectories);

            UpdateRepos(path, files);

            return files.Select(f => Path.GetFileName(f));
        }

        public FileStream GetLfsFileStream(string repoPath, string fileName)
        {
            if (_repos.Any(r => r.Path == repoPath) && _repos.First(r => r.Path == repoPath).LfsFiles.ContainsKey(fileName))
            {
                var filePath = _repos.First(r => r.Path == repoPath).LfsFiles[fileName];
                return new FileStream(filePath, FileMode.Open, FileAccess.Read);
            }

            throw new FileNotFoundException(fileName);
        }


        public async Task<byte[]> GetLfsFileBytes(string repoPath, string fileName)
        {
            if (_repos.Any(r => r.Path == repoPath) && _repos.First(r => r.Path == repoPath).LfsFiles.ContainsKey(fileName))
            {
                var filePath = _repos.First(r => r.Path == repoPath).LfsFiles[fileName];
                return await File.ReadAllBytesAsync(filePath);
            }

            throw new FileNotFoundException(fileName);
        }

        public bool CheckIsFileBig(string repoPath, string fileName, int biggerThanMb)
        {
            if (_repos.Any(r => r.Path == repoPath) && _repos.First(r => r.Path == repoPath).LfsFiles.ContainsKey(fileName))
            {
                var filePath = _repos.First(r => r.Path == repoPath).LfsFiles[fileName];
                return new FileInfo(filePath).Length > biggerThanMb * BytesInMegabyte;
            }

            throw new FileNotFoundException(fileName);
        }

        public void SaveFile(string path, string fileName, Stream stream)
        {
            var directoryPath = path.Replace(fileName, string.Empty);
            Directory.CreateDirectory(directoryPath);

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
