using git_lfs_synchronizer.Configuration;
using git_lfs_synchronizer.Controllers.Models;
using git_lfs_synchronizer.Services;
using Microsoft.AspNetCore.Mvc;

namespace git_lfs_synchronizer.Controllers
{
    public class ServerController(ILogger<ServerController> logger, MainConfiguration config, LfsService lfsService) : Controller
    {
        private readonly ILogger<ServerController> _logger = logger;
        private readonly MainConfiguration _config = config;
        private readonly LfsService _lfsService = lfsService;


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
                if (!config.Repos.Any(r => r.Name == repoName))
                {
                    _logger.LogWarning("{repoName} is not found on server side! Check server side config or client config!", repoName);
                    continue;
                }

                var lfsFileNames = _lfsService.GetLfsFileNames(config.Repos.First(r => r.Name == repoName).Path);
                repos.Add(new RepoResponse(repoName, lfsFileNames.ToList()));
            }

            return Ok(repos);
        }

        [HttpGet("file")]
        public async Task<IActionResult> GetFile([FromQuery] string repoName, [FromQuery] string fileName)
        {
            var repoPath = config.Repos.First(r => r.Name == repoName).Path;

            try
            {
                return File(await _lfsService.GetLfsFile(repoPath, fileName), "application/octet-stream");
            }
            catch (FileNotFoundException)
            {
                NotFound(fileName);
            }

            return BadRequest("Something gone wrong...");
        }
    }
}
