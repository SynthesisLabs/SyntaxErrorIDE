using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

[Route("Editor")]
public class GithubController : ControllerBase
{
    private readonly string _rootPath;

    public GithubController()
    {
        _rootPath = Path.Combine(Directory.GetCurrentDirectory(), "UserRepositories");
        Directory.CreateDirectory(_rootPath);
    }

    [HttpPost("DownloadRepo")]
    public async Task<IActionResult> DownloadRepo([FromBody] GitHubRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GithubUrl)) 
            return BadRequest("GitHub URL is vereist");

        try
        {
            var (user, repo) = ExtractRepoInfo(request.GithubUrl);
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(repo)) 
                return BadRequest("Ongeldige GitHub URL");

            string repoPath = Path.Combine(_rootPath, user, repo);
            
            if (Directory.Exists(repoPath)) Directory.Delete(repoPath, true);
            await CloneGitHubRepository(request.GithubUrl, repoPath);
            
            var files = Directory.GetFiles(repoPath, "*", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar))
                .Select(f => Path.GetRelativePath(repoPath, f))
                .ToList();

            return Ok(files);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Fout bij downloaden repository: {ex.Message}");
        }
    }

    [HttpGet("GetFileContent")]
    public IActionResult GetFileContent(string file)
    {
        if (string.IsNullOrWhiteSpace(file)) 
            return BadRequest("Bestandsnaam is vereist");

        try
        {
            var fullPath = Path.Combine(_rootPath, file);
            if (!System.IO.File.Exists(fullPath)) return NotFound("Bestand niet gevonden");

            var content = System.IO.File.ReadAllText(fullPath);
            return Content(content);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Fout bij lezen bestand: {ex.Message}");
        }
    }

    private static (string user, string repo) ExtractRepoInfo(string githubUrl)
    {
        try
        {
            var uri = new Uri(githubUrl);
            var segments = uri.Segments;
            if (segments.Length < 3) return (null, null);

            var user = segments[1].Trim('/');
            var repo = segments[2].Trim('/');
            return (user, repo);
        }
        catch
        {
            return (null, null);
        }
    }

    private static async Task CloneGitHubRepository(string githubUrl, string targetPath)
    {
        var processInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"clone {githubUrl} \"{targetPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = new System.Diagnostics.Process())
        {
            process.StartInfo = processInfo;
            process.Start();
            
            await Task.Run(() => process.WaitForExit());
            
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"Git clone failed: {error}");
            }
        }
    }

    public class GitHubRequest
    {
        public string GithubUrl { get; set; }
    }
}