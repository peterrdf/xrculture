using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.FileProviders;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Xml;
using XRCultureMiddleware.Pages.Shared;

namespace XRCultureMiddleware.Pages
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class RegistryModel : PageModel
    {
        const string registrationResponse =
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<RegistrationResponse>
      <Status>202</Status> <!-- ACCEPTED / use standard HTML response status codes https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status -->
      <ServiceToken>%SERVICE_TOKEN%</ServiceToken>
      <ExpiresIn>3600</ExpiresIn> <!-- in seconds -->
      <Message>Service successfully registered.</Message>
  </RegistrationResponse>";

        const string registrationResponseError =
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<RegistrationResponse>
      <Status>400</Status> <!-- Bad Request -->
      <Message>%MESSAGE%</Message>
</RegistrationResponse>";

        const string authorizationResponse =
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<AuthorizationResponse>
      <Status>200</Status>
      <SessionToken>%SESSION_TOKEN%</SessionToken>
      <SessionExpires>2025-05-29T15:05:00Z</SessionExpires>
      <Message>Authorization successful.</Message>
  </AuthorizationResponse>";

        const string authorizationResponseError =
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<AuthorizationResponse>
      <Status>400</Status> <!-- Bad Request -->
      <Message>%MESSAGE%</Message>
</AuthorizationResponse>";

        private readonly ILogger<RegistryModel> _logger;
        private readonly IConfiguration _configuration;
        private static readonly ConcurrentDictionary<string, RegisterViewerRequest> RegisterViewerRequests = new();

        public RegistryModel(ILogger<RegistryModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public void OnGet()
        {
        }

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

            var xmlRequest = JsonSerializer.Deserialize<string>(body);
            if (string.IsNullOrEmpty(xmlRequest))
            {
                _logger.LogInformation("Received empty request.");
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Received empty request."));
            }
            _logger.LogInformation($"****** XML Request ******\n{xmlRequest}");

            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.LoadXml(xmlRequest);
            }
            catch (XmlException ex)
            {
                _logger.LogError($"XML parsing error: {ex.Message}");
                return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "XML parsing error."));
            }

            var registrationRequest = xmlDoc.SelectSingleNode("/Protocol/RegistrationRequest");
            if (registrationRequest != null)
            {
                return RegisterViewer(xmlDoc);
            }

            var authorizationRequest = xmlDoc.SelectSingleNode("/Protocol/AuthorizationRequest");
            if (authorizationRequest != null)
            {
                return AuthorizeViewer(xmlDoc);
            }

            _logger.LogError("Unknown request.");
            return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Unknown request."), "application/xml");
        }

        public async Task<IActionResult> OnPostRegisterAsync()
        {
            _logger.LogInformation("Received registration request.");

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            if (string.IsNullOrEmpty(body))
            {
                _logger.LogInformation("Received empty request.");
                return Content(registrationResponseError.Replace("%MESSAGE%", "Received empty request."));
            }

            var xmlRequest = JsonSerializer.Deserialize<string>(body);
            if (string.IsNullOrEmpty(xmlRequest))
            {
                _logger.LogInformation("Received empty request.");
                return Content(registrationResponseError.Replace("%MESSAGE%", "Received empty request."));
            }

            _logger.LogInformation($"****** RegistrationRequest ******\n{xmlRequest}");

            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.LoadXml(xmlRequest);
            }
            catch (XmlException ex)
            {
                _logger.LogError($"XML parsing error: {ex.Message}");
                return Content(registrationResponseError.Replace("%MESSAGE%", "XML parsing error."));
            }

            return RegisterViewer(xmlDoc);
        }

        private IActionResult RegisterViewer(XmlDocument xmlDoc)
        {
            if (xmlDoc == null)
            {
                _logger.LogError("Received null XML document.");
                return Content(registrationResponseError.Replace("%MESSAGE%", "Received null XML document."));
            }

            var endPoint = xmlDoc.SelectSingleNode("//Endpoint")?.InnerText;
            if (string.IsNullOrEmpty(endPoint))
            {
                _logger.LogError("Bad request: 'Endpoint'.");
                return Content(registrationResponseError.Replace("%MESSAGE%", "Bad request: 'Endpoint'."));
            }

            if (RegisterViewerRequests.Keys.Contains(endPoint))
            {
                _logger.LogError($"Viewer registration is in progress for 'Endpoint': {endPoint}");
                return Content(registrationResponseError.Replace("%MESSAGE%", "Viewer registration is in progress."));
            }

            if (ViewersRegistry.IsViewerRegistered(_logger, _configuration, endPoint))
            {
                _logger.LogError($"Viewer is already registered 'Endpoint': {endPoint}");
                return Content(authorizationResponseError.Replace("%MESSAGE%", "Viewer is already registered."));
            }

            var serviceToken = Guid.NewGuid().ToString();
            _logger.LogInformation($"Generated service token: {serviceToken} for endpoint: {endPoint}");

            RegisterViewerRequests.AddOrUpdate(endPoint, new RegisterViewerRequest
            {
                EndPoint = endPoint,
                ServiceToken = serviceToken
            }, (key, oldValue) => new RegisterViewerRequest
            {
                EndPoint = endPoint,
                ServiceToken = serviceToken
            });

            return Content(registrationResponse.Replace("%SERVICE_TOKEN%", serviceToken));
        }

        public async Task<IActionResult> OnPostAuthorizeAsync()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            if (string.IsNullOrEmpty(body))
            {
                _logger.LogInformation("Received empty request.");
                return Content(authorizationResponseError.Replace("%MESSAGE%", "Received empty request."));
            }

            var xmlRequest = JsonSerializer.Deserialize<string>(body);
            if (string.IsNullOrEmpty(xmlRequest))
            {
                _logger.LogError("Received empty request.");
                return Content(authorizationResponseError.Replace("%MESSAGE%", "Received empty request."));
            }

            _logger.LogInformation($"****** AuthorizationRequest ******\n{xmlRequest}");

            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.LoadXml(xmlRequest);
            }
            catch (XmlException ex)
            {
                _logger.LogError($"XML parsing error: {ex.Message}");
                return Content(authorizationResponseError.Replace("%MESSAGE%", "XML parsing error."));
            }

            return AuthorizeViewer(xmlDoc);
        }

        private IActionResult AuthorizeViewer(XmlDocument xmlDoc)
        {
            if (xmlDoc == null)
            {
                _logger.LogError("Received null XML document.");
                return Content(authorizationResponseError.Replace("%MESSAGE%", "Received null XML document."));
            }

            var sessionToken = xmlDoc.SelectSingleNode("//SessionToken")?.InnerText;
            if (string.IsNullOrEmpty(sessionToken))
            {
                _logger.LogError("Bad request: 'SessionToken'.");
                return Content(authorizationResponseError.Replace("%MESSAGE%", "Bad request: 'SessionToken'."));
            }

            var registerRequest = RegisterViewerRequests.Values.FirstOrDefault(r => r.ServiceToken == sessionToken);
            if (registerRequest == null)
            {
                _logger.LogError($"Invalid 'SessionToken': {sessionToken}");
                return Content(authorizationResponseError.Replace("%MESSAGE%", "Invalid 'SessionToken'."));
            }

            if (string.IsNullOrEmpty(registerRequest.EndPoint))
            {
                _logger.LogError("Bad request: 'Endpoint'.");
                return Content(authorizationResponseError.Replace("%MESSAGE%", "Internal error: 'Endpoint'."));
            }

            if (ViewersRegistry.IsViewerRegistered(_logger, _configuration, registerRequest.EndPoint))
            {
                _logger.LogError($"Viewer is already registered 'Endpoint': {registerRequest.EndPoint}");
                return Content(authorizationResponseError.Replace("%MESSAGE%", "Viewer is already registered."));
            }

            var backEnd = xmlDoc.SelectSingleNode("/Protocol/AuthorizationRequest/BackEnd")?.InnerXml;
            if (string.IsNullOrEmpty(backEnd))
            {
                _logger.LogError("Bad request: 'BackEnd'.");
                return Content(authorizationResponseError.Replace("%MESSAGE%", "Bad request: 'BackEnd'."));
            }

            var frontEnd = xmlDoc.SelectSingleNode("/Protocol/AuthorizationRequest/FrontEnd")?.InnerXml;
            if (string.IsNullOrEmpty(frontEnd))
            {
                _logger.LogError("Bad request: 'FrontEnd'.");
                return Content(authorizationResponseError.Replace("%MESSAGE%", "Bad request: 'FrontEnd'."));
            }

            var viewersDir = _configuration["FileStorage:ViewersDir"];
            if (string.IsNullOrEmpty(viewersDir))
            {
                _logger.LogError("Viewers path is not configured.");
                return Content(HTTPResponse.ServerError.Replace("%MESSAGE%", "Viewers path is not configured."), "application/xml");
            }

            if (!Directory.Exists(viewersDir))
            {
                _logger.LogError($"Viewers directory does not exist: {viewersDir}");
                return Content(HTTPResponse.ServerError.Replace("%MESSAGE%", "Viewers directory does not exist."), "application/xml");
            }

            var viewerId = Guid.NewGuid().ToString();
            _logger.LogInformation($"Viewer registered with ID: {viewerId}");

            StringBuilder xml = new();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<Viewer>");
            xml.AppendLine($"\t<Id>{viewerId}</Id>");
            xml.AppendLine($"\t<EndPoint>{registerRequest.EndPoint}</EndPoint>");
            xml.AppendLine($"\t<BackEnd>{backEnd}</BackEnd>");
            xml.AppendLine($"\t<FrontEnd>{frontEnd}</FrontEnd>");
            xml.AppendLine($"\t<TimeStamp>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</TimeStamp>");
            xml.AppendLine("</Viewer>");
            System.IO.File.WriteAllText(Path.Combine(viewersDir, $"{viewerId}.xml"), xml.ToString());

            RegisterViewerRequests.Remove(registerRequest.EndPoint, out _);

            return Content(authorizationResponse.Replace("%SESSION_TOKEN%", sessionToken));
        }
    }

    public class RegisterViewerRequest
    {
        public string? EndPoint { get; set; }
        public string? ServiceToken { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }
}
