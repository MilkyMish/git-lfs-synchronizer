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
                if (!config.Repos.Any(r => r.Name == repoName))
                {
                    _logger.LogWarning("{repoName} is not found on server side! Check server side config or client config!", repoName);
                    continue;
                }

                var lfsFileNames = _lfsService.GetLfsFileNames(config.Repos.First(r => r.Name == repoName).Path);
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

            var repoPath = config.Repos.First(r => r.Name == repoName).Path;
            string ipAddress = HttpContext.Request.Host.Host;
            await _uploadsManager.AddTaskToQueue(new UploadTask(ipAddress, repoPath, fileName));

            return Ok();
            /*try
            {
                _uploadsManager.AddTaskToQueue(new UploadTask())

                Ok();
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning("File {fileName} not found", fileName);
                NotFound(fileName);
            }*/

            //return BadRequest("Something gone wrong...");
        }

        /*        private string GetUserName(string ip)
                {
                    switch (ip)
                    {
                        case "26.58.143.118":
                            return "MEERITS";
                        case "26.96.147.169":
                            return "KUX";
                        case "26.202.79.18":
                            return "GRINOG4";
                        case "26.159.179.112":
                            return "ANTIHYPE.MILKYMISH";
                        case "26.117.145.233":
                            return "CHUPA";
                        default:
                            return ip;
                    }
                }*/

        /*
         private async Task SendFiles()
{
    string fileName = "CCCCCC.zip"; //It has size greater than 2 GB
    string filePath = @"C:\Users\CCCCCC\Downloads\";

    using (var fileStream = new FileStream(Path.Combine(filePath, fileName), FileMode.Open, FileAccess.Read))
}
         */
    }
}
