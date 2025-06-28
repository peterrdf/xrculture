using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.FileProviders;

namespace XRCultureWebApp.Pages
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class StorageModel : PageModel
    {
        private readonly ILogger<ViewerModel> _logger;
        private readonly IConfiguration _configuration;

        public StorageModel(ILogger<ViewerModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        public void OnGet()
        {
        }

        public IActionResult OnGetFile(string file)
        {
            try
            {
                _logger.LogInformation($"OnGetFile called with file: {file}");

                var dataPath = _configuration["ToolPaths:OpenMVG-OpenMVS-Output"];
                if (string.IsNullOrEmpty(dataPath))
                {
                    _logger.LogError("Viewer path is not configured");
                    return Content(HTTPResponse.ServerError.Replace("%MESSAGE%", "Viewer path is not configured."), "application/xml");
                }

                _logger.LogInformation($"Using viewer path: {dataPath}");

                if (string.IsNullOrEmpty(file))
                {
                    _logger.LogError("File name is required");
                    return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "File name is required."), "application/xml");
                }

                var provider = new PhysicalFileProvider(dataPath);
                var fileInfo = provider.GetFileInfo(file);

                _logger.LogInformation($"Looking for file: {file}, exists: {fileInfo.Exists}, physical path: {fileInfo.PhysicalPath}");

                if (!fileInfo.Exists)
                {
                    _logger.LogError($"File '{file}' not found at '{fileInfo.PhysicalPath}'");
                    return Content(HTTPResponse.NotFound.Replace("%MESSAGE%", $"File '{file}' not found."), "application/xml");
                }

                _logger.LogInformation($"Returning file: {fileInfo.PhysicalPath}, size: {fileInfo.Length} bytes");
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";

                return File(System.IO.File.ReadAllBytes(fileInfo.PhysicalPath), "application/octet-stream", Path.GetFileName(file));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnGetFile");
                return Content(HTTPResponse.ServerError.Replace("%MESSAGE%", $"Server error: {ex.Message}"), "application/xml");
            }
        }
    }
}
