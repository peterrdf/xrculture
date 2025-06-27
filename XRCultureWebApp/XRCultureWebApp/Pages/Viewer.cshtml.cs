using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Xml;

namespace XRCultureWebApp.Pages
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class ViewerModel : PageModel
    {
        private readonly string successResponse =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><ViewModelResponse><Status>200</Status></ViewModelResponse>";
        private readonly string successResponseWithParameters =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><ViewModelResponse><Status>200</Status><Parameters>%PARAMETERS%</Parameters></ViewModelResponse>";
        private readonly string badRequestResponse =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><ViewModelResponse><Status>400</Status><Message>%MESSAGE%</Message></ViewModelResponse>";
        private readonly string serverErrorResponse =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><ViewModelResponse><Status>500</Status><Message>%MESSAGE%</Message></ViewModelResponse>";
        private readonly string notFoundResponse =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><ViewModelResponse><Status>404</Status><Message>%MESSAGE%</Message></ViewModelResponse>";

        private readonly ILogger<ViewerModel> _logger;
        private readonly IConfiguration _configuration;
        private static readonly ConcurrentDictionary<string, System.Text.StringBuilder> ServerLogs = new();
        
        public ViewerModel(ILogger<ViewerModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public void OnGet()
        {
        }

        /*
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>
        <ViewModelRequest>
            <Name>%NAME%</Name>
                <Parameters>%PARAMETERS%</Parameters>
        </ViewModelRequest>";
        */
        public async Task<IActionResult> OnPostViewModelAsync()
        {
            if (!Request.HasFormContentType)
                return BadRequest("Content-Type must be multipart/form-data");

            var form = await Request.ReadFormAsync();

            // Get XML request part
            var xmlRequest = form.Files.FirstOrDefault(f => f.Name == "request");
            string? xmlString = null;
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

            // Validate XML
            if (!xmlString.Trim().StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Invalid XML format.");
            if (!xmlString.Contains("<ViewModelRequest", StringComparison.OrdinalIgnoreCase))
                return BadRequest("XML does not contain <ViewModelRequest> element.");
            if (!xmlString.Contains("<Name", StringComparison.OrdinalIgnoreCase))
                return BadRequest("XML does not contain <Name> element.");
            if (!xmlString.Contains("<Parameters", StringComparison.OrdinalIgnoreCase))
                return BadRequest("XML does not contain <Parameters> element.");
            // Validate XML structure
            if (xmlString.Length > 1000000) // 1 MB limit
                return BadRequest("XML request is too large. Maximum size is 1 MB.");

            XmlDocument viewModelRequestXml = new();
            try
            {
                viewModelRequestXml.LoadXml(xmlString);
                var root = viewModelRequestXml.DocumentElement;
                if (root == null || root.Name != "ViewModelRequest")
                    return BadRequest("XML root element must be <ViewModelRequest>.");
                if (root.SelectSingleNode("Name") == null)
                    return BadRequest("XML must contain <Name> element.");
                if (root.SelectSingleNode("Parameters") == null)
                    return BadRequest("XML must contain <Parameters> element.");
            }
            catch (XmlException ex)
            {
                return BadRequest($"Invalid XML format: {ex.Message}");
            }

            var model = viewModelRequestXml.SelectSingleNode("//ViewModelRequest/Name")?.InnerText;
            if (string.IsNullOrEmpty(model))
                return Content(badRequestResponse.Replace("%MESSAGE%", "Bad request: 'Name'."));

            // Get zip file part
            var zipFile = form.Files.FirstOrDefault(f => f.Name == "file");
            if (zipFile == null || zipFile.Length == 0)
                return BadRequest("Missing or empty zip file.");

            if (zipFile.ContentType != "application/zip")   
                return BadRequest("Invalid file type. Expected application/zip.");

            string dataDir = _configuration["ToolPaths:OpenMVG-OpenMVS-Output"];

            // Save zip
            var resultId = Guid.NewGuid().ToString();
            var tempZipPath = Path.Combine(dataDir, $"{resultId}{Path.GetExtension(zipFile.FileName)}");
            using (var fs = System.IO.File.Create(tempZipPath))
            using (var zipStream = zipFile.OpenReadStream())
            {
                await zipStream.CopyToAsync(fs);
            }

            //# todo:check file extension and extract if needed
            //var extractDir = Path.Combine(Path.GetTempPath(), resultId);
            //Directory.CreateDirectory(extractDir);
            //System.IO.Compression.ZipFile.ExtractToDirectory(tempZipPath, extractDir);
            //System.IO.File.Delete(tempZipPath);

            var serviceUrl = GetServiceRootUrl();
            var response = successResponseWithParameters.Replace("%PARAMETERS%",
                $"<ResultId>{resultId}</ResultId><URL>{serviceUrl}viewer/viewer.html?model={resultId}.binz</URL>");

            return Content(response, "application/xml");
        }

        private void AppendApplyFilterLog(string message)
        {
            var formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            _logger.LogInformation(formattedMessage);
        }

        private string GetServiceRootUrl()
        {
            var request = HttpContext.Request;
            return $"{request.Scheme}://{request.Host}{request.PathBase}/";
        }
    }
}
