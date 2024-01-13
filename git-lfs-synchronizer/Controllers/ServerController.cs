using git_lfs_synchronizer.Configuration;
using git_lfs_synchronizer.Controllers.Models;
using git_lfs_synchronizer.Services;
using Microsoft.AspNetCore.Mvc;
using git_lfs_synchronizer.Models;

namespace git_lfs_synchronizer.Controllers
{
    public class ServerController(ILogger<ServerController> logger, MainConfiguration config, LfsService lfsService, UploadsManager uploadsManager) : Controller
    {
        private readonly ILogger<ServerController> _logger = logger;
        private readonly MainConfiguration _config = config;
        private readonly LfsService _lfsService = lfsService;
        private readonly UploadsManager _uploadsManager = uploadsManager;

        [HttpGet("fileNames")]
        public IActionResult FetchFileNames([FromQuery] string[] repoNames)
        {
            if (!_config.IsServer)
            {
                return BadRequest("Trying to connect to client app, not server. Check etc/config.json on server side, there must be isServer = true");
            }

            var repos = new List<RepoResponse>();

            foreach (var repoName in repoNames)
            {
                if (!_config.Repos.Any(r => r.Name == repoName))
                {
                    _logger.LogWarning("{repoName} is not found on server side! Check server side config or client config!", repoName);
                    continue;
                }

                var lfsFileNames = _lfsService.GetLfsFileNames(_config.Repos.First(r => r.Name == repoName).Path);
                repos.Add(new RepoResponse(repoName, lfsFileNames.ToList()));
                _logger.Log(LogLevel.Information, "Fetched {count} files for {repoName} repo", lfsFileNames.Count(), repoName);
            }

            return Ok(repos);
        }

        [HttpGet("file")]
        public async Task<IActionResult> GetFile([FromQuery] string repoName, [FromQuery] string fileName)
        {
            if (!_config.IsServer)
            {
                return BadRequest("Trying to connect to client app, not server. Check etc/config.json on server side, there must be isServer = true");
            }

            var repoPath = _config.Repos.First(r => r.Name == repoName).Path;
            string ipAddress = HttpContext.Request.Host.Host;

            if (_lfsService.CheckIsFileBig(repoPath, fileName, _config.TcpFileSizeMb))
            {
                _logger.LogInformation("Sending big file {name} for repo {repo} to {address}", fileName, repoName, ipAddress);
                await _uploadsManager.AddTaskToQueue(new UploadTask(ipAddress, repoPath, fileName));
                return Ok();
            }

            _logger.LogInformation("Sent small file {name} for repo {repo} to {address}", fileName, repoName, ipAddress);
            return File(await _lfsService.GetLfsFileBytes(repoPath, fileName), "application/octet-stream");
        }
    }
}
