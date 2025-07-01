using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.FileProviders;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Xml;

namespace MeshLabServer.Pages
{
    [IgnoreAntiforgeryToken]
    public class FiltersModel : PageModel
    {
        const string badRequestResponse =
@"<ApplyFilterResponse>
    <Status>400</Status> <!-- Bad Request -->
    <Message>%MESSAGE%</Message>
</ApplyFilterResponse>";

        const string serverErrorResponse =
@"<ApplyFilterResponse>
    <Status>500</Status> <!-- Internal Server Error -->
    <Message>%MESSAGE%</Message>
</ApplyFilterResponse>";

        const string successResponse =
@"<ApplyFilterResponse>
    <Status>200</Status>
    <Message>Command executed successfully.</Message>
</ApplyFilterResponse>";

        const string successResponseWithParameters = 
@"<ApplyFilterResponse>
    <Status>200</Status>
    <Message>Command executed successfully.</Message>
    <Parameters>
        %PARAMETERS%
    </Parameters>
</ApplyFilterResponse>";
        
        private readonly ILogger<FiltersModel> _logger;
        private readonly IConfiguration _configuration;
        private static readonly ConcurrentDictionary<string, System.Text.StringBuilder> ServerLogs = new();
        private readonly string LOG_CATEGORY = "MeshLabServer-Filters";

        public FiltersModel(ILogger<FiltersModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /*
        "<ApplyFilterRequest>
            <Name>%FILTER%</Name>
            <Parameters>
                <InputMesh>%INPUT_MESH%</InputMesh>
                <OutputMesh>%OUTPUT_MESH%</OutputMesh>
            </Parameters>
        </ApplyFilterRequest>";
        */
        public async Task<IActionResult> OnPostApplyAsync()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var xmlRequest = JsonSerializer.Deserialize<string>(body);
            if (string.IsNullOrEmpty(xmlRequest))
            {
                AppendApplyFilterLog("Received empty request.");
                return Content(badRequestResponse.Replace("%MESSAGE%", "Received empty request."));
            }

            AppendApplyFilterLog($"****** ApplyFilter ******\n{xmlRequest}");

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlRequest);

            var filter = xmlDoc.SelectSingleNode("//ApplyFilterRequest/Name")?.InnerText;
            if (string.IsNullOrEmpty(filter))
            {
                AppendApplyFilterLog("Bad request: 'Name'.");
                return Content(badRequestResponse.Replace("%MESSAGE%", "Bad request: 'Name'."));
            }

            if (filter != "meshing_decimation_quadric_edge_collapse_with_texture")
            {
                AppendApplyFilterLog($"Unknown filter: {filter}");
                return Content(badRequestResponse.Replace("%MESSAGE%", $"Unknown filter: {filter}"));
            }

            var inputMesh = xmlDoc.SelectSingleNode("//ApplyFilterRequest/Parameters/InputMesh")?.InnerText;
            if (string.IsNullOrEmpty(inputMesh))
            {
                AppendApplyFilterLog("Bad request: 'InputMesh'.");
                return Content(badRequestResponse.Replace("%MESSAGE%", "Bad request: 'InputMesh'."));
            }

            var outputMesh = xmlDoc.SelectSingleNode("//ApplyFilterRequest/Parameters/OutputMesh")?.InnerText;
            if (string.IsNullOrEmpty(outputMesh))
            {
                AppendApplyFilterLog("Bad request: 'OutputMesh'.");
                return Content(badRequestResponse.Replace("%MESSAGE%", "Bad request: 'OutputMesh'."));
            }

            var pythonPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                _configuration["ToolPaths:Python"]);
            var exePath = pythonPath + @"\python.exe";
            string pythonScript = Path.Combine(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot"),
                @"python\meshing_decimation_quadric_edge_collapse_with_texture.py");
            var args = $"{pythonScript} {inputMesh} {outputMesh}";

            var exitCode = ExecuteProcess(exePath, args, LOG_CATEGORY);
            if (exitCode != 0)
            {
                AppendApplyFilterLog($"Process exited with code {exitCode}");
                return Content(serverErrorResponse.Replace("%MESSAGE%", $"Process failed with exit code {exitCode}."));
            }

            return Content(successResponse);
        }

        public IActionResult OnGetLogs()
        {
            AppendLog(LOG_CATEGORY, "Logs requested.");
            if (ServerLogs.TryGetValue(LOG_CATEGORY, out var logContent))
            {
                return Content(logContent.ToString(), "text/plain");
            }
            else
            {
                return NotFound("No logs available.");
            }
        }

        public IActionResult OnGetFile(string file)
        {
            AppendLog(LOG_CATEGORY, $"File requested: {file}");
            if (string.IsNullOrEmpty(file))
            {
                AppendLog(LOG_CATEGORY, "Invalid file request.");
                return BadRequest("Invalid file request.");
            }
            var provider = new PhysicalFileProvider(Path.GetDirectoryName(file)!);
            var fileInfo = provider.GetFileInfo(Path.GetFileName(file));
            if (!fileInfo.Exists)
                return NotFound();

            var result = PhysicalFile(fileInfo.PhysicalPath, "application/octet-stream", file);
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            
            return result;
        }

        public IActionResult OnGetDirectoryContents(string folder)
        {
            AppendLog(LOG_CATEGORY, $"Directory contents requested: {folder}");
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                AppendLog(LOG_CATEGORY, "Invalid or non-existent folder.");
                return BadRequest("Invalid or non-existent folder.");
            }

            var files = Directory.GetFiles(folder)
                .Select(Path.GetFileName)
                .ToList();

            // Return as JSON
            return new JsonResult(files);
        }

        private void AppendApplyFilterLog(string message)
        {
            AppendLog(LOG_CATEGORY, message);
        }

        private void AppendLog(string category, string message)
        {
            var log = ServerLogs.GetOrAdd(category, _ => new System.Text.StringBuilder());
            log.AppendLine($"[{DateTime.Now:HH:mm:ss}] {message}");
            _logger.LogInformation($"[{category}] {message}");
        }

        private int ExecuteProcess(string exePath, string args, string category)
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

            process.OutputDataReceived += (sender, e) => { if (e.Data != null) { AppendApplyFilterLog(e.Data); outputBuilder.AppendLine(e.Data); } };
            process.ErrorDataReceived += (sender, e) => { if (e.Data != null) { AppendApplyFilterLog(e.Data); errorBuilder.AppendLine(e.Data); } };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            int exitCode = process.ExitCode;
            string output = outputBuilder.ToString();
            string error = errorBuilder.ToString();

            // Log output and errors
            AppendApplyFilterLog($"Process exited with code {output}");
            if (!string.IsNullOrWhiteSpace(error))
                AppendApplyFilterLog($"Process exited with code {error}");

            AppendLog(LOG_CATEGORY, "External process finished.");

            if (exitCode != 0)
            {
                AppendApplyFilterLog($"Process exited with code {exitCode}");
            }

            return exitCode;
        }

        // POST handler for applying the filter with a zip file containing the mesh
        public async Task<IActionResult> OnPostApply2Async()
        {
            if (!Request.HasFormContentType)
                return BadRequest("Content-Type must be multipart/form-data");

            var form = await Request.ReadFormAsync();

            // Get XML request part
            var xmlRequest = form.Files.FirstOrDefault(f => f.Name == "request");
            string xmlString = null;
            if (xmlRequest != null)
            {
                using var reader = new StreamReader(xmlRequest.OpenReadStream());
                xmlString = await reader.ReadToEndAsync();
            }
            else if (form.TryGetValue("request", out var xmlField))
            {
                xmlString = xmlField.ToString();
            }
            if (string.IsNullOrEmpty(xmlString))
                return BadRequest("Missing XML request part.");

            // Get zip file part
            var zipFile = form.Files.FirstOrDefault(f => f.Name == "file");
            if (zipFile == null || zipFile.Length == 0)
                return BadRequest("Missing or empty zip file.");

            // Save zip to temp
            var resultId = Guid.NewGuid().ToString();
            var tempZipPath = Path.Combine(Path.GetTempPath(), $"{resultId}.zip");
            using (var fs = System.IO.File.Create(tempZipPath))
            using (var zipStream = zipFile.OpenReadStream())
            {
                await zipStream.CopyToAsync(fs);
            }

            // Extract zip
            var extractDir = Path.Combine(Path.GetTempPath(), resultId);
            Directory.CreateDirectory(extractDir);
            System.IO.Compression.ZipFile.ExtractToDirectory(tempZipPath, extractDir);
            System.IO.File.Delete(tempZipPath);

            // Parse XML and process as needed
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlString);

            var filter = xmlDoc.SelectSingleNode("//ApplyFilterRequest/Name")?.InnerText;
            if (string.IsNullOrEmpty(filter))
                return Content(badRequestResponse.Replace("%MESSAGE%", "Bad request: 'Name'."));

            var inputMesh = xmlDoc.SelectSingleNode("//ApplyFilterRequest/Parameters/InputMesh")?.InnerText;
            var outputMesh = xmlDoc.SelectSingleNode("//ApplyFilterRequest/Parameters/OutputMesh")?.InnerText;
            if (string.IsNullOrEmpty(inputMesh) || string.IsNullOrEmpty(outputMesh))
                return Content(badRequestResponse.Replace("%MESSAGE%", "Bad request: 'InputMesh' or 'OutputMesh'."));

            // Compose full paths
            var inputMeshPath = Path.Combine(extractDir, inputMesh);
            var outDir = Path.Combine(extractDir, "out");
            Directory.CreateDirectory(outDir);
            var outputMeshPath = Path.Combine(outDir, outputMesh);

            // Run your process
            var pythonPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                _configuration["ToolPaths:Python"]);
            var exePath = pythonPath + @"\python.exe";
            string pythonScript = Path.Combine(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot"),
                @"python\meshing_decimation_quadric_edge_collapse_with_texture.py");
            var args = $"{pythonScript} {inputMeshPath} {outputMeshPath}";

            var exitCode = ExecuteProcess(exePath, args, LOG_CATEGORY);
            if (exitCode != 0)
            {
                AppendApplyFilterLog($"Process exited with code {exitCode}");
                return Content(serverErrorResponse.Replace("%MESSAGE%", $"Process failed with exit code {exitCode}."));
            }

            var response = successResponseWithParameters.Replace("%PARAMETERS%",
                $"<ResultId>{resultId}</ResultId>");

            return Content(response, "application/xml");
        }

        public IActionResult OnGetResultContents(string resultId)
        {
            AppendLog(LOG_CATEGORY, $"Result contents requested: {resultId}");

            if (string.IsNullOrEmpty(resultId))
            {
                AppendLog(LOG_CATEGORY, "Invalid result request.");
                return BadRequest("Invalid result request.");
            }

            var resultDir = Path.Combine(Path.GetTempPath(), resultId, "out");
            if (string.IsNullOrEmpty(resultDir) || !Directory.Exists(resultDir))
            {
                AppendLog(LOG_CATEGORY, "Invalid or non-existent folder.");
                return BadRequest("Invalid or non-existent folder.");
            }

            var files = Directory.GetFiles(resultDir)
                .Select(Path.GetFileName)
                .ToList();

            // Return as JSON
            return new JsonResult(files);
        }

        public IActionResult OnGetResultFile(string resultId, string file)
        {
            AppendLog(LOG_CATEGORY, $"File requested: {resultId} - {file}");

            if (string.IsNullOrEmpty(resultId))
            {
                AppendLog(LOG_CATEGORY, "Invalid result request.");
                return BadRequest("Invalid result request.");
            }

            if (string.IsNullOrEmpty(file))
            {
                AppendLog(LOG_CATEGORY, "Invalid file request.");
                return BadRequest("Invalid file request.");
            }

            var resultDir = Path.Combine(Path.GetTempPath(), resultId, "out");
            if (string.IsNullOrEmpty(resultDir) || !Directory.Exists(resultDir))
            {
                AppendLog(LOG_CATEGORY, "Invalid or non-existent folder.");
                return BadRequest("Invalid or non-existent folder.");
            }

            var provider = new PhysicalFileProvider(resultDir);
            var fileInfo = provider.GetFileInfo(file);
            if (!fileInfo.Exists)
                return NotFound();

            var result = PhysicalFile(fileInfo.PhysicalPath, "application/octet-stream", file);
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return result;
        }

        public IActionResult OnDeleteResult(string resultId)
        {
            AppendLog(LOG_CATEGORY, $"Delete requested: {resultId}");

            if (string.IsNullOrEmpty(resultId))
                return BadRequest("Invalid resultId.");

            var resultDir = Path.Combine(Path.GetTempPath(), resultId);
            if (!Directory.Exists(resultDir))
                return NotFound("Result folder not found.");

            try
            {
                Directory.Delete(resultDir, true);
                return StatusCode(200, "OK");
            }
            catch (Exception ex)
            {
                AppendLog(LOG_CATEGORY, $"Delete failed: {ex.Message}");
                return StatusCode(500, "Failed to delete result folder.");
            }
        }
    }
}
