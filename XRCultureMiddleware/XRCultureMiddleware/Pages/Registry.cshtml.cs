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
using XRCultureMiddleware.Services;

namespace XRCultureMiddleware.Pages
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class RegistryModel : PageModel
    {
        const string registrationResponse =
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<RegistrationResponse>
      <Status>200</Status>
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
      <Status>202</Status> <!-- ACCEPTED / use standard HTML response status codes https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status -->
      <SessionToken>%SESSION_TOKEN%</SessionToken>
      <ExpiresIn>3600</ExpiresIn> <!-- in seconds -->
      <Message>Service successfully authorized.</Message>
  </AuthorizationResponse>";

        const string authorizationResponseError =
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<AuthorizationResponse>
      <Status>400</Status> <!-- Bad Request -->
      <Message>%MESSAGE%</Message>
</AuthorizationResponse>";

        private readonly ILogger<RegistryModel> _logger;
        private readonly IConfiguration _configuration;
        private static readonly ConcurrentDictionary<string, AuthorizationRequest> AuthorizationRequests = new();

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

            var authorizationRequest = xmlDoc.SelectSingleNode("/Protocol/AuthorizationRequest");
            if (authorizationRequest != null)
            {
                return Authorize(xmlDoc);
            }

            //#todo: Service Type?
            var registrationRequest = xmlDoc.SelectSingleNode("/Protocol/RegistrationRequest");
            if (registrationRequest != null)
            {
                return RegisterViewer(xmlDoc);
            }            

            _logger.LogError("Unknown request.");
            return Content(HTTPResponse.BadRequest.Replace("%MESSAGE%", "Unknown request."), "application/xml");
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

            return Authorize(xmlDoc);
        }

        private IActionResult Authorize(XmlDocument xmlDoc)
        {
            if (xmlDoc == null)
            {
                _logger.LogError("Received null XML document.");
                return Content(authorizationResponseError.Replace("%MESSAGE%", "Received null XML document."));
            }

            var providerId = xmlDoc.SelectSingleNode("/Protocol/AuthorizationRequest/ProviderID")?.InnerText;
            if (string.IsNullOrEmpty(providerId))
            {
                _logger.LogError("Bad request: 'ProviderID'.");
                return Content(authorizationResponseError.Replace("%MESSAGE%", "Bad request: 'ProviderID'."));
            }

            //#todo: validate providerId or move it to the OnPost method

            if (!AuthorizationRequests.TryGetValue(providerId, out var authorizationRequest))
            {
                authorizationRequest = new AuthorizationRequest
                {
                    ProviderId = providerId,
                    SessionToken = Guid.NewGuid().ToString()//#todo: JWT? expiration time?
                };
            }

            _logger.LogInformation($"Session token: {authorizationRequest.SessionToken} for provider ID: {providerId}");

            AuthorizationRequests.AddOrUpdate(providerId, authorizationRequest, (_, oldValue) =>
            {
                oldValue.TimeStamp = DateTime.Now;
                return oldValue;
            });

            return Content(authorizationResponse.Replace("%SESSION_TOKEN%", authorizationRequest.SessionToken));
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

            var providerId = xmlDoc.SelectSingleNode("/Protocol/RegistrationRequest/ProviderID")?.InnerText;
            if (string.IsNullOrEmpty(providerId))
            {
                _logger.LogError("Bad request: 'ProviderID'.");
                return Content(registrationResponseError.Replace("%MESSAGE%", "Bad request: 'ProviderID'."));
            }

            //#todo: validate providerId or move it to the OnPost method

            var sessionToken = xmlDoc.SelectSingleNode("/Protocol/RegistrationRequest/SessionToken")?.InnerText;
            if (string.IsNullOrEmpty(sessionToken))
            {
                _logger.LogError("Bad request: 'SessionToken'.");
                return Content(registrationResponseError.Replace("%MESSAGE%", "Bad request: 'SessionToken'."));
            }

            var endPoint = xmlDoc.SelectSingleNode("/Protocol/RegistrationRequest/EndPoint")?.InnerText;
            if (string.IsNullOrEmpty(endPoint))
            {
                _logger.LogError("Bad request: 'EndPoint'.");
                return Content(registrationResponseError.Replace("%MESSAGE%", "Bad request: 'EndPoint'."));
            }            

            if (ViewersRegistry.IsViewerRegistered(_logger, _configuration, endPoint))
            {
                _logger.LogError($"Viewer is already registered 'Endpoint': {endPoint}");
                return Content(registrationResponseError.Replace("%MESSAGE%", "Viewer is already registered."));
            }

            var backEnd = xmlDoc.SelectSingleNode("/Protocol/RegistrationRequest/BackEnd")?.InnerXml;
            if (string.IsNullOrEmpty(backEnd))
            {
                _logger.LogError("Bad request: 'BackEnd'.");
                return Content(registrationResponseError.Replace("%MESSAGE%", "Bad request: 'BackEnd'."));
            }

            var frontEnd = xmlDoc.SelectSingleNode("/Protocol/RegistrationRequest/FrontEnd")?.InnerXml;
            if (string.IsNullOrEmpty(frontEnd))
            {
                _logger.LogError("Bad request: 'FrontEnd'.");
                return Content(registrationResponseError.Replace("%MESSAGE%", "Bad request: 'FrontEnd'."));
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
            xml.AppendLine($"\t<ProviderId>{providerId}</ProviderId>");
            xml.AppendLine($"\t<Id>{viewerId}</Id>");
            xml.AppendLine($"\t<EndPoint>{endPoint}</EndPoint>");
            xml.AppendLine($"\t<BackEnd>{backEnd}</BackEnd>");
            xml.AppendLine($"\t<FrontEnd>{frontEnd}</FrontEnd>");
            xml.AppendLine($"\t<TimeStamp>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</TimeStamp>");
            xml.AppendLine("</Viewer>");
            System.IO.File.WriteAllText(Path.Combine(viewersDir, $"{viewerId}.xml"), xml.ToString());

            return Content(registrationResponse.Replace("%SESSION_TOKEN%", sessionToken));
        }
    }

    public class AuthorizationRequest
    {
        public string ProviderId { get; set; } = string.Empty;
        public string SessionToken { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }
}
