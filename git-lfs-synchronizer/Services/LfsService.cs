using git_lfs_synchronizer.Models;
using System.Linq;

namespace git_lfs_synchronizer.Services
{
    public class LfsService
    {
        private readonly string LfsObjectsDirectory = $".git{Path.DirectorySeparatorChar}lfs{Path.DirectorySeparatorChar}objects";
        private readonly List<Repo> _repos = new();

        public IEnumerable<string> GetLfsFileNames(string path)
        {
            var lfsObjectsPath = Path.Combine(path, LfsObjectsDirectory);
            var objectsDirectory = new DirectoryInfo(lfsObjectsPath);
            var files = GetFilesFromSubDir(objectsDirectory);

            UpdateRepos(path, files);

            return files.Select(f => f.Name);
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
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            using (var fileStream = File.Create(path))
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(fileStream);
            }
        }

        private void UpdateRepos(string path, FileInfo[] files)
        {
            if (_repos.Any(r => r.Path == path))
            {
                var repo = _repos.First(r => r.Path == path);

                repo.LfsFiles = files.ToDictionary(f => f.Name, f => f.FullName);
            }
            else
            {
                _repos.Add(new(path, files.ToDictionary(f => f.Name, f => f.FullName)));
            }
        }

        private static FileInfo[] GetFilesFromSubDir(DirectoryInfo dir)
        {
            if (dir.GetDirectories().Any())
            {
                foreach (var subDir in dir.GetDirectories())
                {
                    GetFilesFromSubDir(subDir);
                }
            }
            else
            {
                return dir.GetFiles();
            }

            return Array.Empty<FileInfo>();
        }

        private static FileInfo[] GetFilesFromSubDir(DirectoryInfo dir, IEnumerable<string> fileNames)
        {
            if (dir.GetDirectories().Any())
            {
                foreach (var subDir in dir.GetDirectories())
                {
                    GetFilesFromSubDir(subDir);
                }
            }
            else
            {
                return dir.GetFiles().Where(f => fileNames.Contains(f.Name)).ToArray();
            }

            return Array.Empty<FileInfo>();
        }
    }
}
