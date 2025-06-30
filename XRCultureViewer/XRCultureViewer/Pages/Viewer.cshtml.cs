using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text;
using System.Xml;

namespace XRCultureViewer.Pages
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class ViewerModel : PageModel
    {
        private readonly ILogger<ViewerModel> _logger;
        private readonly IConfiguration _configuration;

        public ViewerModel(ILogger<ViewerModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [BindProperty(SupportsGet = true)]
        public string? Model { get; set; }

        public void OnGet()
        {
            // Default model if none is provided #todo: set default model
            //if (string.IsNullOrEmpty(Model))
            //{
            //    Model = "f7aa9163-2d18-404c-a2ef-65693a5960d6.binz";
            //}
        }

        /*
         * <?xml version=""1.0"" encoding=""UTF-8""?>
        <ModelLoadingRequest>
            <Source>
              <LocalSource>
                <Name>%NAME%</Name>
                <Description>3D Model file</Description>
                <FileContent dimension=""%SIZE%"" extension=""%EXTENSION%"">%BASE64_CONTENT%</FileContent>
              </LocalSource>
            </Source>
        </ModelLoadingRequest>
        */
        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("Received request.");

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            if (string.IsNullOrEmpty(body))
            {
                _logger.LogInformation("Received empty request.");
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Received empty request."));
            }

            var xmlRequest = JsonConvert.DeserializeObject<string>(body);
            if (string.IsNullOrEmpty(xmlRequest))
            {
                _logger.LogInformation("Received empty request.");
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Received empty request."));
            }
            _logger.LogInformation($"****** XML Request ******\n{xmlRequest}");

            XmlDocument viewModelRequestXml = new();
            try
            {
                viewModelRequestXml.LoadXml(xmlRequest);
            }
            catch (XmlException ex)
            {
                _logger.LogError(ex, "Invalid XML format.");
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", $"Invalid XML format: {ex.Message}"));
            }

            var modelName = viewModelRequestXml.SelectSingleNode("/ModelLoadingRequest/Source/LocalSource/Name")?.InnerText;
            if (string.IsNullOrEmpty(modelName))
            {
                _logger.LogError("Bad request: 'Name'.");
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Bad request: 'Name'."));
            }

            var base64Content = viewModelRequestXml.SelectSingleNode("/ModelLoadingRequest/Source/LocalSource/FileContent")?.InnerText;
            if (string.IsNullOrEmpty(base64Content))
            {
                _logger.LogError("Bad request: 'FileContent'.");
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Bad request: 'FileContent'."));
            }

            var modelsDir = _configuration["FileStorage:ModelsDir"];
            if (string.IsNullOrEmpty(modelsDir))
            {
                _logger.LogError("Models path is not configured.");
                return Content(HTTPResponse.ServerError.Replace("%MESSAGE%", "Models path is not configured."), "application/xml");
            }

            // Save
            var resultId = Guid.NewGuid().ToString();
            _logger.LogInformation("Generated new model ID: {ResultId}", resultId);

            var modelPath = Path.Combine(modelsDir, $"{resultId}{Path.GetExtension(modelName)}");
            try
            {
                byte[] fileBytes = Convert.FromBase64String(base64Content);
                using (var fs = System.IO.File.Create(modelPath))
                {
                    await fs.WriteAsync(fileBytes, 0, fileBytes.Length);
                }

                _logger.LogInformation("Successfully saved file from base64 content to {FilePath}", modelPath);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Invalid base64 string format");
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Invalid base64 content format"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file");
                return Content(HTTPResponse.ServerError.Replace("%MESSAGE%", $"Error saving file: {ex.Message}"));
            }

            StringBuilder xml = new();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<Model>");
            xml.AppendLine($"\t<Id>{resultId}</Id>");
            xml.AppendLine($"\t<Extension>{Path.GetExtension(modelName)}</Extension>");
            xml.AppendLine($"\t<Name>{modelName}</Name>");
            xml.AppendLine($"\t<Description>{modelName}</Description>"); //#todo: set description
            xml.AppendLine($"\t<TimeStamp>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</TimeStamp>");
            xml.AppendLine("</Model>");
            System.IO.File.WriteAllText(Path.Combine(modelsDir, $"{resultId}.xml"), xml.ToString());

            var serviceUrl = GetServiceRootUrl();
            var response = HTTPResponse.SuccessWithParameters.Replace("%PARAMETERS%",
                $"<ResultId>{resultId}</ResultId><URL>{serviceUrl}Viewer?model={resultId}{Path.GetExtension(modelName)}</URL>");
            _logger.LogInformation("Model uploaded successfully with ID: {ResultId}", resultId);
            return Content(response, "application/xml");
        }

        /*
         * multipart/form-data
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>
        <ViewModelRequest>
            <Name>%NAME%</Name>
                <Parameters>%PARAMETERS%</Parameters>
        </ViewModelRequest>";
        */
        public async Task<IActionResult> OnPostViewModelAsync()
        {
            _logger.BeginScope("ViewerModel.OnPostViewModelAsync");
            _logger.LogInformation("Processing model upload request.");

            // Check if the request is multipart/form-data
            if (!Request.HasFormContentType)
            {
                _logger.LogWarning("Invalid request content type: {ContentType}", Request.ContentType);
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Content-Type must be multipart/form-data."));
            }

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
            {
                _logger.LogWarning("Missing XML request part.");
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Missing XML request part."));
            }

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
                _logger.LogError(ex, "Invalid XML format.");
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", $"Invalid XML format: {ex.Message}"));
            }

            var model = viewModelRequestXml.SelectSingleNode("//ViewModelRequest/Name")?.InnerText;
            if (string.IsNullOrEmpty(model))
            {
                _logger.LogError("Bad request: 'Name'.");
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Bad request: 'Name'."));
            }

            // Get zip file part
            var zipFile = form.Files.FirstOrDefault(f => f.Name == "file");
            if (zipFile == null || zipFile.Length == 0)
            {
                _logger.LogWarning("Missing or empty zip file.");
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Missing or empty zip file."));
            }

            if (zipFile.ContentType != "application/zip")
            {
                _logger.LogWarning("Invalid file type: {ContentType}. Expected application/zip.", zipFile.ContentType);
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Invalid file type. Expected application/zip."));
            }

            var modelsDir = _configuration["FileStorage:ModelsDir"];
            if (string.IsNullOrEmpty(modelsDir))
            {
                _logger.LogError("Models path is not configured.");
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
            xml.AppendLine("<Model>");
            xml.AppendLine($"\t<Id>{resultId}</Id>");
            xml.AppendLine($"\t<Extension>{Path.GetExtension(zipFile.FileName)}</Extension>");
            xml.AppendLine($"\t<Name>{model}</Name>");
            xml.AppendLine($"\t<Description>{model}</Description>"); //#todo: set description
            xml.AppendLine($"\t<TimeStamp>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</TimeStamp>");
            xml.AppendLine("</Model>");
            System.IO.File.WriteAllText(Path.Combine(modelsDir, $"{resultId}.xml"), xml.ToString());

            var serviceUrl = GetServiceRootUrl();
            var response = HTTPResponse.SuccessWithParameters.Replace("%PARAMETERS%",
                $"<ResultId>{resultId}</ResultId><URL>{serviceUrl}Viewer?model={resultId}{Path.GetExtension(zipFile.FileName)}</URL>");
            _logger.LogInformation("Model uploaded successfully with ID: {ResultId}", resultId);
            return Content(response, "application/xml");
        }

        private string GetServiceRootUrl()
        {
            var request = HttpContext.Request;
            return $"{request.Scheme}://{request.Host}{request.PathBase}/";
        }
    }
}
