using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text;
using System.Xml;
using XRCultureViewer.Pages.Shared;

namespace XRCultureViewer.Pages
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class ViewerModel : PageModel
    {
        private readonly ILogger<ViewerModel> _logger;
        private readonly IConfiguration _configuration;
        private static readonly ConcurrentDictionary<string, System.Text.StringBuilder> ServerLogs = new();
        
        public ViewerModel(ILogger<ViewerModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [BindProperty(SupportsGet = true)]
        public string Model { get; set; }

        public void OnGet()
        {
            // Default model if none is provided #todo: set default model
            //if (string.IsNullOrEmpty(Model))
            //{
            //    Model = "f7aa9163-2d18-404c-a2ef-65693a5960d6.binz";
            //}
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!Request.HasFormContentType)
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Content-Type must be multipart/form-data."));

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
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Missing XML request part."));

            // Validate XML
            if (!xmlString.Trim().StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Invalid XML format."));
            if (!xmlString.Contains("<ViewModelRequest", StringComparison.OrdinalIgnoreCase))
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "XML does not contain <ViewModelRequest> element."));
            if (!xmlString.Contains("<Name", StringComparison.OrdinalIgnoreCase))
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "XML does not contain <Name> element."));
            if (!xmlString.Contains("<Parameters", StringComparison.OrdinalIgnoreCase))
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "XML does not contain <Parameters> element."));
            // Validate XML structure
            if (xmlString.Length > 1000000) // 1 MB limit
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "XML request is too large. Maximum size is 1 MB."));

            XmlDocument viewModelRequestXml = new();
            try
            {
                viewModelRequestXml.LoadXml(xmlString);
                var root = viewModelRequestXml.DocumentElement;
                if (root == null || root.Name != "ViewModelRequest")
                    return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "XML root element must be <ViewModelRequest>."));
                if (root.SelectSingleNode("Name") == null)
                    return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "XML must contain <Name> element."));
                if (root.SelectSingleNode("Parameters") == null)
                    return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "XML must contain <Parameters> element."));
            }
            catch (XmlException ex)
            {
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", $"Invalid XML format: {ex.Message}"));
            }

            var model = viewModelRequestXml.SelectSingleNode("//ViewModelRequest/Name")?.InnerText;
            if (string.IsNullOrEmpty(model))
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Bad request: 'Name'."));

            // Get zip file part
            var zipFile = form.Files.FirstOrDefault(f => f.Name == "file");
            if (zipFile == null || zipFile.Length == 0)
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Missing or empty zip file."));

            if (zipFile.ContentType != "application/zip")
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Invalid file type. Expected application/zip."));

            var modelsDir = _configuration["FileStorage:Models"];
            if (string.IsNullOrEmpty(modelsDir))
            {
                return Content(HTTPResponse.ServerError.Replace("%MESSAGE%", "Models path is not configured."), "application/xml");
            }

            // Save zip
            var resultId = Guid.NewGuid().ToString();
            var tempZipPath = Path.Combine(modelsDir, $"{resultId}{Path.GetExtension(zipFile.FileName)}");
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

            StringBuilder xml = new();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<model>");
            xml.AppendLine($"\t<name>{model}</name>");
            xml.AppendLine($"\t<description>{model}</description>"); //#todo: set description
            xml.AppendLine($"\t<timeStamp>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</timeStamp>");
            xml.AppendLine("</model>");

            // Add XML file
            System.IO.File.WriteAllText(Path.Combine(modelsDir, $"{resultId}.xml"), xml.ToString());

            var serviceUrl = GetServiceRootUrl();
            var response = HTTPResponse.SuccessWithParameters.Replace("%PARAMETERS%",
                $"<ResultId>{resultId}</ResultId><URL>{serviceUrl}Viewer?model={resultId}{Path.GetExtension(zipFile.FileName)}</URL>");

            return Content(response, "application/xml");
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
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Content-Type must be multipart/form-data."));

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
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Missing XML request part."));

            // Validate XML
            if (!xmlString.Trim().StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Invalid XML format."));
            if (!xmlString.Contains("<ViewModelRequest", StringComparison.OrdinalIgnoreCase))
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "XML does not contain <ViewModelRequest> element."));
            if (!xmlString.Contains("<Name", StringComparison.OrdinalIgnoreCase))
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "XML does not contain <Name> element."));
            if (!xmlString.Contains("<Parameters", StringComparison.OrdinalIgnoreCase))
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "XML does not contain <Parameters> element."));
            // Validate XML structure
            if (xmlString.Length > 1000000) // 1 MB limit
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "XML request is too large. Maximum size is 1 MB."));

            XmlDocument viewModelRequestXml = new();
            try
            {
                viewModelRequestXml.LoadXml(xmlString);
                var root = viewModelRequestXml.DocumentElement;
                if (root == null || root.Name != "ViewModelRequest")
                    return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "XML root element must be <ViewModelRequest>."));
                if (root.SelectSingleNode("Name") == null)
                    return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "XML must contain <Name> element."));
                if (root.SelectSingleNode("Parameters") == null)
                    return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "XML must contain <Parameters> element."));
            }
            catch (XmlException ex)
            {
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", $"Invalid XML format: {ex.Message}"));
            }

            var model = viewModelRequestXml.SelectSingleNode("//ViewModelRequest/Name")?.InnerText;
            if (string.IsNullOrEmpty(model))
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Bad request: 'Name'."));

            // Get zip file part
            var zipFile = form.Files.FirstOrDefault(f => f.Name == "file");
            if (zipFile == null || zipFile.Length == 0)
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Missing or empty zip file."));

            if (zipFile.ContentType != "application/zip")
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Invalid file type. Expected application/zip."));

            var viewerPath = _configuration["Paths:Viewer"];
            if (string.IsNullOrEmpty(viewerPath))
            {
                return Content(HTTPResponse.ServerError.Replace("%MESSAGE%", "Models path is not configured."), "application/xml");
            }

            var dataDir = Path.Combine(viewerPath, @"data");

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
            var response = HTTPResponse.SuccessWithParameters.Replace("%PARAMETERS%",
                $"<ResultId>{resultId}</ResultId><URL>{serviceUrl}Viewer?model={resultId}{Path.GetExtension(zipFile.FileName)}</URL>");

            return Content(response, "application/xml");
        }

        private string GetServiceRootUrl()
        {
            var request = HttpContext.Request;
            return $"{request.Scheme}://{request.Host}{request.PathBase}/";
        }
    }
}
