using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace SyntaxErrorIDE.app.Controllers;

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
                
            var apiPath = string.IsNullOrEmpty(path) ? "" : path;
                
            if (!string.IsNullOrEmpty(apiPath))
            {
                var segments = apiPath.Split('/');
                var encodedSegments = segments.Select(Uri.EscapeDataString);
                apiPath = string.Join("/", encodedSegments);
            }

            var requestUrl = $"repos/{owner}/{repo}/contents/{apiPath}";
                
            var response = await client.GetAsync(requestUrl);

            if (response.IsSuccessStatusCode)
                return Content(await response.Content.ReadAsStringAsync(), "application/json");
                
            var errorContent = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, $"GitHub API error: {errorContent}");

        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal error: {ex.Message}");
        }
    }

    [HttpGet("file/{owner}/{repo}/{*path}")]
    public async Task<IActionResult> GetFileContent(string owner, string repo, string path)
    {
        try
        {
            var token = _configuration["GitHub:Token"];
            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MyApp", "1.0"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", token);
                
            var segments = path.Split('/');
            var encodedSegments = segments.Select(Uri.EscapeDataString);
            var encodedPath = string.Join("/", encodedSegments);

            var apiUrl = $"https://api.github.com/repos/{owner}/{repo}/contents/{encodedPath}";
            Console.WriteLine($"Fetching: {apiUrl}");

            var response = await client.GetAsync(apiUrl);

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

            var json = await response.Content.ReadAsStringAsync();
            var fileInfo = JsonSerializer.Deserialize<GitHubFileInfo>(json);

            if (fileInfo == null || string.IsNullOrEmpty(fileInfo.Content)) 
                return BadRequest(new { error = "Ongeldige GitHub response of lege inhoud" });

            var data = Convert.FromBase64String(fileInfo.Content.Replace("\n", ""));
            var decodedContent = Encoding.UTF8.GetString(data);

            return Ok(new
            {
                content = decodedContent,
                name = fileInfo.Name,
                path = fileInfo.Path,
                sha = fileInfo.Sha,
                encoding = fileInfo.Encoding
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
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
    public string Encoding { get; set; }
}

public class GitHubSaveRequest
{
    public string Content { get; set; }
    public string Sha { get; set; }
    public string Message { get; set; }
}