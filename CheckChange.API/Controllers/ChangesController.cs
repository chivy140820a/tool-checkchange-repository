using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.JSInterop;
using System.Diagnostics;
using System.Linq;

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

            // Kiểm tra số lượng commit trong nhánh master
            var commitCount = RunCommand($"git -C {localPath} rev-list --count master").Trim();
            int commits = int.TryParse(commitCount, out var count) ? count : 0;

            // Nếu có ít nhất 2 commit, kiểm tra sự thay đổi
            string diffCommand = commits > 1
                ? "git -C {localPath} diff --name-only master~1 master"
                : "git -C {localPath} diff --name-only master";  // So sánh với commit đầu tiên nếu chỉ có 1 commit.

            // Lấy danh sách tệp thay đổi
            var changedFiles = RunCommand(diffCommand)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var result = new List<string>();
            var services = GetListService();
            var values = changedFiles.Select(x => x.Split('/').FirstOrDefault()).Distinct();  // Sử dụng '/' thay vì '.'

            if (services.Any() && values.Any())
                result = services.Intersect(values).ToList();  // Lọc các service bị thay đổi

            return Ok(result);
        }

        private string GetRepositoryUrl(string serviceName)
        {
            // Trả về URL repository tương ứng với service
            return serviceName switch
            {
                "MicroArticle" => "https://github.com/chivy140820a/tool-checkchange-repository.git",
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
                    FileName = "",
                    Arguments = command,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Git command failed: {error}");
                }

                return output.Trim();
            }
            catch (Exception ex)
            {
                // Xử lý lỗi và trả về thông báo
                return $"Error executing command: {ex.Message}";
            }
        }
    }
}
