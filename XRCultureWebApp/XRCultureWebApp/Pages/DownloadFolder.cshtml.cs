using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;
using System.Net.Http.Headers;

public class DownloadFolderModel : PageModel
{
    private readonly ILogger<DownloadFolderModel> _logger;
    private readonly IConfiguration _configuration;

    public DownloadFolderModel(ILogger<DownloadFolderModel> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<IActionResult> OnGetAsync(string owner, string repo, string folder, string branch = "main")
    {
        string zipFilePath = null;
        string extractPath = null;
        try
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AppName", "1.0"));

            var apiUrl = $"https://api.github.com/repos/{owner}/{repo}/contents/{folder}?ref={branch}";
            var response = await client.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub API returned {StatusCode} for {ApiUrl}", response.StatusCode, apiUrl);
                return NotFound("Folder not found on GitHub.");
            }

            var files = System.Text.Json.JsonSerializer.Deserialize<List<GitHubFile>>(
                await response.Content.ReadAsStringAsync(),
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // 1. Create ZIP in memory
            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                foreach (var file in files.Where(f => f.Type == "file"))
                {
                    var fileBytes = await client.GetByteArrayAsync(file.Download_Url);
                    var entry = archive.CreateEntry(file.Name);
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                }
            }

            // 2. Save ZIP to temp folder
            var tempPath = Path.GetTempPath();
            var zipFileName = $"{Guid.NewGuid()}.zip";
            zipFilePath = Path.Combine(tempPath, zipFileName);
            await System.IO.File.WriteAllBytesAsync(zipFilePath, ms.ToArray());

            // 3. Unzip to a new temp directory
            extractPath = Path.Combine(tempPath, Path.GetFileNameWithoutExtension(zipFileName));
            ZipFile.ExtractToDirectory(zipFilePath, extractPath + @"\images");

            _logger.LogInformation("Extracted files to {ExtractPath}", extractPath);

            return openMVG_opeMVSWorkflow(extractPath);
            
            // Optional: return a message or a file, or clean up files as needed
            //return Content($"ZIP saved to: {zipFilePath}\nExtracted to: {extractPath}\nProcess output: {output}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during download for {Owner}/{Repo}/{Folder}", owner, repo, folder);
            return StatusCode(503, "Error connecting to GitHub. Please try again later.");
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for {Owner}/{Repo}/{Folder}", owner, repo, folder);
            return StatusCode(500, "Error processing GitHub response.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during download for {Owner}/{Repo}/{Folder}", owner, repo, folder);
            return StatusCode(500, "An unexpected error occurred during the download process.");
        }
        finally
        {
            // Clean up temp files
            try
            {
                if (zipFilePath != null && System.IO.File.Exists(zipFilePath))
                    System.IO.File.Delete(zipFilePath);

                if (extractPath != null && System.IO.Directory.Exists(extractPath))
                    System.IO.Directory.Delete(extractPath, true); // true = recursive
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to clean up temporary files.");
            }
        }
    }

    private ObjectResult openMVG_opeMVSWorkflow(string inputDir)
    {
        // Execute external process
        var exePath = _configuration["ToolPaths:OpenMVG"] + @"\openMVG_main_SfMInit_ImageListing.exe";
        var args = $"--imageDirectory {inputDir}\\images --outputDirectory {inputDir}\\matches";

        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (sender, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        int exitCode = process.ExitCode;
        string output = outputBuilder.ToString();
        string error = errorBuilder.ToString();

        // Log output and errors
        _logger.LogInformation("Process output: {Output}", output);
        if (!string.IsNullOrWhiteSpace(error))
            _logger.LogError("Process error: {Error}", error);

        if (exitCode != 0)
        {
            _logger.LogError("Process exited with code {ExitCode}", exitCode);
            return StatusCode(500, $"Process failed with exit code {exitCode}.\nError: {error}");
        }

        return StatusCode(200, $"Process completed successfully.\nOutput: {output}");
    }

    public class GitHubFile
    {
        public string Name { get; set; }
        public string Download_Url { get; set; }
        public string Type { get; set; }
    }
}