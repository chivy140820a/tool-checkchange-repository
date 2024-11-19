using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.JSInterop;
using System.Diagnostics;

namespace CheckChange.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChangesController : ControllerBase
    {
        [HttpGet("{serviceName}")]
        public IActionResult GetChangedProjects(string serviceName)
        {
            string repositoryUrl = GetRepositoryUrl(serviceName);
            string localPath = Path.Combine("D:\\tmp", serviceName);
            

            // Clone hoặc pull repository
            if (!Directory.Exists(localPath))
            {
                RunCommand($"git clone {repositoryUrl} {localPath}");
            }
            else
            {
                RunCommand($"git -C {localPath} pull");
            }

            // Kiểm tra thay đổi
            var changedFiles = RunCommand($"git -C {localPath} diff --name-only master~1 master")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var result = new List<string>();

            var services = GetListService();
            var values = changedFiles.Select(x => x.Split('.').FirstOrDefault()).Distinct();
            if(services.Any() && values.Any())
                result = services.Intersect(values).ToList();
            return Ok(result);
        }

        private string GetRepositoryUrl(string serviceName)
        {
            // Trả về URL repository tương ứng với service
            return serviceName switch
            {
                "MicroArticle" => "https://github.com/chivy140820a/tool-checkchange-repository.git",
                //"ServiceB" => " https://github.com/chivy140820a/tool-checkchange-repository.git",
                _ => throw new ArgumentException("Invalid service name")
            };
        }

        private List<string> GetListService()
        {
            List<string> services = new List<string>()
            {
                "Article",
                "Notification"
            };
            return services;
        }
        private string RunCommand(string command)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output.Trim();
        }
    }
}
