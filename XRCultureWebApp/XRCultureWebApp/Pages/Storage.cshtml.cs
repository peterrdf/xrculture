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

        public IActionResult OnGetModel(string id)
        {
            try
            {
                _logger.LogInformation($"OnGetModel called with id: {id}");

                var modelsDir = _configuration["FileStorage:ModelsDir"];
                if (string.IsNullOrEmpty(modelsDir))
                {
                    _logger.LogError("Models path is not configured");
                    return Content(HTTPResponse.ServerError.Replace("%MESSAGE%", "Models path is not configured."), "application/xml");
                }

                _logger.LogInformation($"Using viewer path: {modelsDir}");

                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogError("id is required");
                    return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "id is required."), "application/xml");
                }

                var provider = new PhysicalFileProvider(modelsDir);
                var fileInfo = provider.GetFileInfo(id);

                _logger.LogInformation($"Looking for file: {id}, exists: {fileInfo.Exists}, physical path: {fileInfo.PhysicalPath}");

                if (!fileInfo.Exists || string.IsNullOrEmpty(fileInfo.PhysicalPath))
                {
                    _logger.LogError($"File '{id}' not found at '{fileInfo.PhysicalPath}'");
                    return Content(HTTPResponse.NotFound.Replace("%MESSAGE%", $"File '{id}' not found."), "application/xml");
                }

                _logger.LogInformation($"Returning file: {fileInfo.PhysicalPath}, size: {fileInfo.Length} bytes");
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";

                return File(System.IO.File.ReadAllBytes(fileInfo.PhysicalPath), "application/octet-stream", Path.GetFileName(id));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnGetModel");
                return Content(HTTPResponse.ServerError.Replace("%MESSAGE%", $"Server error: {ex.Message}"), "application/xml");
            }
        }
    }
}
