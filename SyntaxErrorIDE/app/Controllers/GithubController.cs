using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SyntaxErrorIDE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GitHubController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;

        public GitHubController(IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            _clientFactory = clientFactory;
            _configuration = configuration;
        }

        [HttpGet("repo/{owner}/{repo}/contents/{*path}")]
        public async Task<IActionResult> GetRepositoryContents(
            string owner,
            string repo,
            string path = "")
        {
            try
            {
                var client = _clientFactory.CreateClient("GitHub");

                // Voeg accept header toe voor GitHub API v3
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                var encodedPath = Uri.EscapeDataString(path);
                var requestUrl = $"repos/{owner}/{repo}/contents/{encodedPath}";

                Console.WriteLine($"Requesting GitHub URL: {requestUrl}");

                var response = await client.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"GitHub API error: {response.StatusCode}, Details: {errorContent}");
                    return StatusCode((int)response.StatusCode, errorContent);
                }

                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("file/{owner}/{repo}/{*path}")]
        public async Task<IActionResult> GetFileContent(string owner, string repo, string path)
        {
            try
            {
                var client = _clientFactory.CreateClient("GitHub");
                // URL encode het pad
                var encodedPath = Uri.EscapeDataString(path);
                var response = await client.GetAsync($"repos/{owner}/{repo}/contents/{encodedPath}");
        
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new 
                    { 
                        error = "GitHub API error",
                        status = response.StatusCode,
                        message = errorContent
                    });
                }

                var content = await response.Content.ReadAsStringAsync();
                var fileInfo = JsonSerializer.Deserialize<GitHubFileInfo>(content);

                if (fileInfo == null)
                {
                    return BadRequest(new { error = "Could not parse GitHub response" });
                }

                if (string.IsNullOrEmpty(fileInfo.Content))
                {
                    return BadRequest(new { error = "File content is empty" });
                }
                
                byte[] data = Convert.FromBase64String(fileInfo.Content.Replace("\n", ""));
                var decodedContent = Encoding.UTF8.GetString(data);

                return Ok(new
                {
                    content = decodedContent,
                    name = fileInfo.Name,
                    path = fileInfo.Path,
                    sha = fileInfo.Sha,
                    encoding = "base64"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = "Internal server error",
                    message = ex.Message 
                });
            }
        }

        [HttpPost("save/{owner}/{repo}/{*path}")]
        public async Task<IActionResult> SaveFile(string owner, string repo, string path,
            [FromBody] GitHubSaveRequest request)
        {
            if (string.IsNullOrEmpty(request.Content) || string.IsNullOrEmpty(request.Sha))
            {
                return BadRequest("Content en sha zijn vereist");
            }

            var client = _clientFactory.CreateClient("GitHub");
            var content = Convert.ToBase64String(Encoding.UTF8.GetBytes(request.Content));

            var updateRequest = new
            {
                message = request.Message ?? "Update via Syntax Error IDE",
                content,
                sha = request.Sha
            };

            var jsonContent = JsonSerializer.Serialize(updateRequest);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Voor updates is een auth token nodig
            var githubToken = _configuration["GitHub:AccessToken"];
            if (!string.IsNullOrEmpty(githubToken))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("token", githubToken);
            }

            var response = await client.PutAsync($"repos/{owner}/{repo}/contents/{path}", httpContent);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return Content(responseContent, "application/json");
            }

            return StatusCode((int)response.StatusCode, $"GitHub API error: {response.StatusCode}");
        }
    }

    public class GitHubFileInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Sha { get; set; }
        public string Content { get; set; }
        public string Type { get; set; }
    }

    public class GitHubSaveRequest
    {
        public string Content { get; set; }
        public string Sha { get; set; }
        public string Message { get; set; }
    }
}