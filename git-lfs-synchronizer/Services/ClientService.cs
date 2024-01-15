
using git_lfs_synchronizer.Configuration;
using git_lfs_synchronizer.Configuration.Models;
using git_lfs_synchronizer.Controllers;
using git_lfs_synchronizer.Controllers.Models;
using git_lfs_synchronizer.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using System.Net;
using System.Text.Json;

namespace git_lfs_synchronizer.Services
{
    public class ClientService : BackgroundService
    {
        private readonly MainConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly LfsService _lfsService;
        private readonly ILogger<ServerController> _logger;
        private readonly DownloadsManager _downloadsManager;

        public ClientService(MainConfiguration config, IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonSerializerOptions, LfsService lfsService, ILogger<ServerController> logger, DownloadsManager downloadsManager)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _serializerOptions = jsonSerializerOptions;
            _lfsService = lfsService;
            _logger = logger;
            _downloadsManager = downloadsManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_config.IsServer)
            {
                return;
            }

            if (!_config.Repos.Any())
            {
                throw new Exception("No repos found in config! Check etc/config.json!");
            }

            using var client = _httpClientFactory.CreateClient("GitLfsSynchronizerClient");

            var grouppedRepos = _config.Repos.GroupBy(r => r.Url);

            foreach (var repos in grouppedRepos)
            {
                List<RepoResponse>? reposResponse = await FetchRemoteFiles(client, repos, stoppingToken);

                if (reposResponse != null && reposResponse.Any())
                {
                    List<RepoResponse> reposWithMissingFiles = FindMissingFiles(repos, reposResponse);
                    await RequestMissingFiles(client, repos, reposWithMissingFiles, stoppingToken);
                }
            }
        }

        private async Task<List<RepoResponse>?> FetchRemoteFiles(HttpClient client, IGrouping<string, RepoConfig> repos, CancellationToken stoppingToken)
        {
            var parameters = new[]
            {
                   new KeyValuePair<string,string>("repoNames", new StringValues(repos.Select(r => r.Name).ToArray())!)
            };

            client.BaseAddress = new Uri(repos.Key);

            var fetchUrl = QueryHelpers.AddQueryString(client.BaseAddress + "fileNames", parameters!);

            var response = await client.GetAsync(fetchUrl, stoppingToken);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception(response.Content.ToString());
            }

            var reposResponse = JsonSerializer.Deserialize<List<RepoResponse>>(await response.Content.ReadAsStringAsync(stoppingToken), _serializerOptions);

            if (reposResponse is null || reposResponse.Count == 0)
            {
                throw new Exception("Failed to deserialize fetch fileNames response");
            }

            _logger.LogInformation("Fetched files from remote server");
            return reposResponse;
        }

        private async Task RequestMissingFiles(HttpClient client, IGrouping<string, RepoConfig> repos, List<RepoResponse> reposWithMissingFiles, CancellationToken stoppingToken)
        {
            foreach (var repoWithMissingFiles in reposWithMissingFiles)
            {
                var localRepo = repos.First(r => r.Name == repoWithMissingFiles.Name);
                var fileCount = 1;
                var bigFilesCount = 0;

                foreach (var missingFile in repoWithMissingFiles.LfsFileNames)
                {
                    fileCount++;
                    var getFileParameters = new[]
                    {
                            new KeyValuePair<string,string>("repoName", repoWithMissingFiles.Name),
                            new KeyValuePair<string,string>("fileName", missingFile)
                        };

                    var getFileUrl = QueryHelpers.AddQueryString(client.BaseAddress + "file", getFileParameters!);

                    _logger.LogDebug("Requesting missing file {name} for {repo}...", missingFile, repoWithMissingFiles.Name);
                    var savePath = Path.Combine(localRepo.Path, ".git", "lfs", "objects", missingFile[..2], missingFile.Substring(2, 2), missingFile);

                    var getFileResponse = await client.GetAsync(getFileUrl, stoppingToken);

                    if (getFileResponse == null || !getFileResponse.IsSuccessStatusCode)
                    {
                        _logger.LogWarning($"Response is not successfull: {getFileResponse?.StatusCode}");
                        continue;
                    }

                    if (IsSmallFile(getFileResponse))
                    {
                        var fileStream = await getFileResponse.Content.ReadAsStreamAsync();
                        _lfsService.SaveFile(savePath, missingFile, fileStream);
                        _logger.LogDebug("Saved small missing file {name} for {repo}", missingFile, repoWithMissingFiles.Name);
                        continue;
                    }

                    if (getFileResponse.IsSuccessStatusCode)
                    {
                        bigFilesCount++;
                        await _downloadsManager.AddTaskToQueue(new DownloadTask(savePath));
                        _logger.LogDebug("Requested missing file {name} for {repo} added to queue", missingFile, repoWithMissingFiles.Name);
                    }
                }

                _logger.LogInformation("Saved {count} files for {repo}. {bigCount} count of big files that can be still downloading...", fileCount, localRepo.Name, bigFilesCount);
            }
        }

        private static bool IsSmallFile(HttpResponseMessage getFileResponse)
        {
            return getFileResponse.IsSuccessStatusCode && getFileResponse.Content.Headers.ContentType?.MediaType == "application/octet-stream";
        }

        private List<RepoResponse> FindMissingFiles(IGrouping<string, RepoConfig> repos, List<RepoResponse> reposResponse)
        {
            bool missingFilesFound = false;

            var reposWithMissingFiles = new List<RepoResponse>();

            foreach (var remoteRepo in reposResponse)
            {
                var localRepo = repos.First(r => r.Name == remoteRepo.Name);

                var localFiles = _lfsService.GetLfsFileNames(localRepo.Path);

                var missedFiles = new List<string>();

                foreach (var remoteFile in remoteRepo.LfsFileNames)
                {
                    if (!localFiles.Contains(remoteFile))
                    {
                        missedFiles.Add(remoteFile);
                        _logger.LogInformation("Found missing file {name} in {repo}", remoteFile, remoteRepo.Name);
                        missingFilesFound = true;
                    }
                }

                reposWithMissingFiles.Add(new(localRepo.Name, missedFiles));

                _logger.LogWarning("Found {count} missing files in repo {name}!", missedFiles.Count, localRepo.Name);
            }


            if (!missingFilesFound)
            {
                _logger.LogWarning("Missing files not found! Check your config file, maybe you forgot to add a repo");
                Environment.Exit(1);
            }


            return reposWithMissingFiles;
        }
    }
}
