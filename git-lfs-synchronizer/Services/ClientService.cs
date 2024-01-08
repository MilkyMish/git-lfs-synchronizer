
using git_lfs_synchronizer.Configuration;
using git_lfs_synchronizer.Configuration.Models;
using git_lfs_synchronizer.Controllers.Models;
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

        public ClientService(MainConfiguration config, IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonSerializerOptions, LfsService lfsService)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _serializerOptions = jsonSerializerOptions;
            _lfsService = lfsService;
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

                List<RepoResponse> reposWithMissingFiles = FindMissingFiles(repos, reposResponse);

                await RequestMissingFiles(client, repos, reposWithMissingFiles, stoppingToken);
            }
        }

        private async Task<List<RepoResponse>?> FetchRemoteFiles(HttpClient client, IGrouping<string, RepoConfig> repos, CancellationToken stoppingToken)
        {
            var parameters = new[]
            {
                   new KeyValuePair<string,string>("repoName", new StringValues(repos.Select(r => r.Name).ToArray())!)
            };

            client.BaseAddress = new Uri(repos.Key);

            var fetchUrl = QueryHelpers.AddQueryString(client.BaseAddress + "/fileNames", parameters!);

            var response = await client.PostAsync(fetchUrl, null, stoppingToken);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception(response.Content.ToString());
            }

            var reposResponse = JsonSerializer.Deserialize<List<RepoResponse>>(await response.Content.ReadAsStringAsync(stoppingToken), _serializerOptions);

            if (reposResponse is null || reposResponse.Count == 0)
            {
                throw new Exception("Failed to deserialize fetch fileNames response");
            }

            return reposResponse;
        }

        private async Task RequestMissingFiles(HttpClient client, IGrouping<string, RepoConfig> repos, List<RepoResponse> reposWithMissingFiles, CancellationToken stoppingToken)
        {
            foreach (var repoWithMissingFiles in reposWithMissingFiles)
            {
                var localRepo = repos.First(r => r.Name == repoWithMissingFiles.Name);

                foreach (var missingFile in repoWithMissingFiles.LfsFileNames)
                {
                    var getFileParameters = new[]
                    {
                            new KeyValuePair<string,string>("repoName", repoWithMissingFiles.Name),
                            new KeyValuePair<string,string>("fileName", missingFile)
                        };

                    var getFileUrl = QueryHelpers.AddQueryString(client.BaseAddress + "/file", getFileParameters!);

                    var getFileResponse = await client.PostAsync(getFileUrl, null, stoppingToken);
                    var fileStream = await getFileResponse.Content.ReadAsStreamAsync();

                    var savePath = Path.Combine(localRepo.Path, missingFile[..2], missingFile.Substring(2, 2), missingFile);
                    _lfsService.SaveFile(savePath, missingFile, fileStream);
                }
            }
        }

        private List<RepoResponse> FindMissingFiles(IGrouping<string, RepoConfig> repos, List<RepoResponse>? reposResponse)
        {
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
                    }
                }

                reposWithMissingFiles.Add(new(localRepo.Name, missedFiles));
            }

            return reposWithMissingFiles;
        }
    }
}
