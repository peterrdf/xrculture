using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent; // Add this at the top if not present
using System.IO.Compression;
using System.Net.Http.Headers;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class DownloadFolderModel : PageModel
{
    // Add this static dictionary for workflow logs
    private static readonly ConcurrentDictionary<string, System.Text.StringBuilder> WorkflowLogs = new();

    private readonly ILogger<DownloadFolderModel> _logger;
    private readonly IConfiguration _configuration;

    public DownloadFolderModel(ILogger<DownloadFolderModel> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<IActionResult> OnGetAsync(string owner, string repo, string folder, string branch = "main", string workflowId = null)
    {
        workflowId ??= Guid.NewGuid().ToString();
        AppendLog(workflowId, "Workflow started.");

        string zipFilePath = null;
        string extractPath = null;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            AppendLog(workflowId, "Downloading file list from GitHub...");
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

            AppendLog(workflowId, $"Downloaded file list in {sw.ElapsedMilliseconds} ms.");

            // 1. Create ZIP in memory
            AppendLog(workflowId, "Creating ZIP in memory...");
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

            AppendLog(workflowId, $"ZIP created in {sw.ElapsedMilliseconds} ms.");

            // 2. Save ZIP to temp folder
            AppendLog(workflowId, "Saving ZIP to temp folder...");
            var tempPath = Path.GetTempPath();
            var zipFileName = $"{Guid.NewGuid()}.zip";
            zipFilePath = Path.Combine(tempPath, zipFileName);
            await System.IO.File.WriteAllBytesAsync(zipFilePath, ms.ToArray());

            AppendLog(workflowId, $"ZIP saved in {sw.ElapsedMilliseconds} ms.");

            // 3. Unzip to a new temp directory
            AppendLog(workflowId, "Extracting ZIP...");
            extractPath = Path.Combine(tempPath, Path.GetFileNameWithoutExtension(zipFileName));
            ZipFile.ExtractToDirectory(zipFilePath, extractPath + @"\images");

            AppendLog(workflowId, $"ZIP extracted in {sw.ElapsedMilliseconds} ms.");

            AppendLog(workflowId, "Running workflow...");
            var result = openMVG_openMVSWorkflow(extractPath, workflowId);
            AppendLog(workflowId, "Workflow finished.");

            return Content(workflowId); // Return workflowId to client
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
            AppendLog(workflowId, $"Error: {ex.Message}");
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

    public IActionResult OnGetLogs(string workflowId)
    {
        if (string.IsNullOrEmpty(workflowId) || !WorkflowLogs.TryGetValue(workflowId, out var log))
            return NotFound("No logs found.");
        return Content(log.ToString());
    }

    public IActionResult OnGetStartWorkflow(string owner, string repo, string folder, string branch = "main")
    {
        var workflowId = Guid.NewGuid().ToString();
        // Start the workflow in the background
        Task.Run(() => RunWorkflowAsync(owner, repo, folder, branch, workflowId));
        return Content(workflowId);
    }

    private ObjectResult openMVG_openMVSWorkflow(string inputDir, string workflowId)
    {
        AppendLog(workflowId, "openMVG - openMVS Workflow started...");
        var stopWatch = System.Diagnostics.Stopwatch.StartNew();

        var result = openMVG_CreateSfM(inputDir, workflowId);
        if (result.StatusCode != 200)
        {
            return result;
        }

        AppendLog(workflowId, $"openMVG - openMVS Workflow completed successfully in {stopWatch.ElapsedMilliseconds} ms.");

        return StatusCode(200, "OK");
    }

    private ObjectResult openMVG_CreateSfM(string inputDir, string workflowId)
    {
        AppendLog(workflowId, "openMVG: Create structure from Motion started...");
        var stopWatch = System.Diagnostics.Stopwatch.StartNew();
        
        {
            AppendLog(workflowId, "*** Intrinsics analysis started...");

            var exePath = _configuration["ToolPaths:OpenMVG"] + @"\openMVG_main_SfMInit_ImageListing.exe";
            var args = $"--imageDirectory {inputDir}\\images --outputDirectory {inputDir}\\matches -f 1920";

            var exitCode = ExecuteProcess(exePath, args, workflowId);
            if (exitCode != 0)
            {
                _logger.LogError("Process exited with code {ExitCode}", exitCode);
                return StatusCode(500, $"Process failed with exit code {exitCode}.");
            }

            AppendLog(workflowId, $"*** Intrinsics analysis completed successfully in {stopWatch.ElapsedMilliseconds} ms.");
        }
        
        {
            AppendLog(workflowId, "*** Compute features started...");

            var exePath = _configuration["ToolPaths:OpenMVG"] + @"\openMVG_main_ComputeFeatures.exe";
            var args = $"--input_file {inputDir}\\matches\\sfm_data.json --outdir {inputDir}\\matches --describerMethod \"SIFT\" --describerPreset \"HIGH\"";
            var exitCode = ExecuteProcess(exePath, args, workflowId);
            if (exitCode != 0)
            {
                _logger.LogError("Process exited with code {ExitCode}", exitCode);
                return StatusCode(500, $"Process failed with exit code {exitCode}.");
            }

            AppendLog(workflowId, $"*** Compute features completed successfully in {stopWatch.ElapsedMilliseconds} ms.");
        }

        {
            AppendLog(workflowId, "*** Compute matching pairs started...");

            var exePath = _configuration["ToolPaths:OpenMVG"] + @"\openMVG_main_PairGenerator.exe";
            var args = $"--input_file {inputDir}\\matches\\sfm_data.json --output_file {inputDir}\\matches\\pairs.bin";
            var exitCode = ExecuteProcess(exePath, args, workflowId);
            if (exitCode != 0)
            {
                _logger.LogError("Process exited with code {ExitCode}", exitCode);
                return StatusCode(500, $"Process failed with exit code {exitCode}.");
            }

            AppendLog(workflowId, $"*** Compute matching pairs completed successfully in {stopWatch.ElapsedMilliseconds} ms.");
        }

        {
            AppendLog(workflowId, "*** Compute matches started...");

            var exePath = _configuration["ToolPaths:OpenMVG"] + @"\openMVG_main_ComputeMatches.exe";
            var args = $"--input_file {inputDir}\\matches\\sfm_data.json --pair_list {inputDir}\\matches\\pairs.bin --output_file {inputDir}\\matches\\matches.putative.bin";
            var exitCode = ExecuteProcess(exePath, args, workflowId);
            if (exitCode != 0)
            {
                _logger.LogError("Process exited with code {ExitCode}", exitCode);
                return StatusCode(500, $"Process failed with exit code {exitCode}.");
            }

            AppendLog(workflowId, $"*** Compute matches completed successfully in {stopWatch.ElapsedMilliseconds} ms.");
        }

        {
            AppendLog(workflowId, "*** Filter matches (INCREMENTAL) started...");

            var exePath = _configuration["ToolPaths:OpenMVG"] + @"\openMVG_main_GeometricFilter.exe";
            var args = $"--input_file {inputDir}\\matches\\sfm_data.json --matches {inputDir}\\matches\\matches.putative.bin -g f --output_file {inputDir}\\matches\\matches.f.bin";
            var exitCode = ExecuteProcess(exePath, args, workflowId);
            if (exitCode != 0)
            {
                _logger.LogError("Process exited with code {ExitCode}", exitCode);
                return StatusCode(500, $"Process failed with exit code {exitCode}.");
            }

            AppendLog(workflowId, $"*** Filter matches completed successfully in {stopWatch.ElapsedMilliseconds} ms.");
        }

        {
            AppendLog(workflowId, "*** Reconstruction (INCREMENTAL) started...");

            var exePath = _configuration["ToolPaths:OpenMVG"] + @"\openMVG_main_SfM.exe";
            var args = $"--sfm_engine \"INCREMENTAL\" --input_file {inputDir}\\matches\\sfm_data.json --match_dir {inputDir}\\matches --output_dir {inputDir}\\reconstruction";
            var exitCode = ExecuteProcess(exePath, args, workflowId);
            if (exitCode != 0)
            {
                _logger.LogError("Process exited with code {ExitCode}", exitCode);
                return StatusCode(500, $"Process failed with exit code {exitCode}.");
            }

            AppendLog(workflowId, $"*** Reconstruction completed successfully in {stopWatch.ElapsedMilliseconds} ms.");
        }

        {
            AppendLog(workflowId, "*** Colorize structure started...");

            var exePath = _configuration["ToolPaths:OpenMVG"] + @"\openMVG_main_ComputeSfM_DataColor.exe";
            var args = $"--input_file {inputDir}\\reconstruction\\sfm_data.bin --output_file {inputDir}\\reconstruction\\colorized.ply";
            var exitCode = ExecuteProcess(exePath, args, workflowId);
            if (exitCode != 0)
            {
                _logger.LogError("Process exited with code {ExitCode}", exitCode);
                return StatusCode(500, $"Process failed with exit code {exitCode}.");
            }

            AppendLog(workflowId, $"*** Colorize structure completed successfully in {stopWatch.ElapsedMilliseconds} ms.");
        }

        AppendLog(workflowId, $"openMVG: - Create structure from Motion completed successfully in {stopWatch.ElapsedMilliseconds} ms.");

        return StatusCode(200, "OK");
    }

    private int ExecuteProcess(string exePath, string args, string workflowId)
    {
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

        process.OutputDataReceived += (sender, e) => { if (e.Data != null) { AppendLog(workflowId, e.Data); outputBuilder.AppendLine(e.Data); } };
        process.ErrorDataReceived += (sender, e) => { if (e.Data != null) { AppendLog(workflowId, e.Data); errorBuilder.AppendLine(e.Data); } };

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

        AppendLog(workflowId, "External process finished.");

        if (exitCode != 0)
        {
            _logger.LogError("Process exited with code {ExitCode}", exitCode);
        }

        return exitCode;
    }

    private void AppendLog(string workflowId, string message)
    {
        var log = WorkflowLogs.GetOrAdd(workflowId, _ => new System.Text.StringBuilder());
        log.AppendLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        _logger.LogInformation("[{WorkflowId}] {Message}", workflowId, message);
    }

    private async Task RunWorkflowAsync(string owner, string repo, string folder, string branch, string workflowId)
    {
        string zipFilePath = null;
        string extractPath = null;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            AppendLog(workflowId, "Workflow started.");
            AppendLog(workflowId, "Downloading file list from GitHub...");
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AppName", "1.0"));

            var apiUrl = $"https://api.github.com/repos/{owner}/{repo}/contents/{folder}?ref={branch}";
            var response = await client.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
            {
                AppendLog(workflowId, $"GitHub API returned {response.StatusCode} for {apiUrl}");
                AppendLog(workflowId, "Folder not found on GitHub.");
                return;
            }

            var files = System.Text.Json.JsonSerializer.Deserialize<List<GitHubFile>>(
                await response.Content.ReadAsStringAsync(),
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            AppendLog(workflowId, $"Downloaded file list in {sw.ElapsedMilliseconds} ms.");

            // 1. Create ZIP in memory
            AppendLog(workflowId, "Creating ZIP in memory...");
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

            AppendLog(workflowId, $"ZIP created in {sw.ElapsedMilliseconds} ms.");

            // 2. Save ZIP to temp folder
            AppendLog(workflowId, "Saving ZIP to temp folder...");
            var tempPath = Path.GetTempPath();
            var zipFileName = $"{Guid.NewGuid()}.zip";
            zipFilePath = Path.Combine(tempPath, zipFileName);
            await System.IO.File.WriteAllBytesAsync(zipFilePath, ms.ToArray());

            AppendLog(workflowId, $"ZIP saved in {sw.ElapsedMilliseconds} ms.");

            // 3. Unzip to a new temp directory
            AppendLog(workflowId, "Extracting ZIP...");
            extractPath = Path.Combine(tempPath, Path.GetFileNameWithoutExtension(zipFileName));
            ZipFile.ExtractToDirectory(zipFilePath, extractPath + @"\images");

            AppendLog(workflowId, $"ZIP extracted in {sw.ElapsedMilliseconds} ms.");

            AppendLog(workflowId, "Running workflow...");
            openMVG_openMVSWorkflow(extractPath, workflowId);
            AppendLog(workflowId, "Workflow finished.");
        }
        catch (Exception ex)
        {
            AppendLog(workflowId, $"Error: {ex.Message}");
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
                AppendLog(workflowId, $"Cleanup error: {cleanupEx.Message}");
            }
        }
    }

    public class GitHubFile
    {
        public string Name { get; set; }
        public string Download_Url { get; set; }
        public string Type { get; set; }
    }
}