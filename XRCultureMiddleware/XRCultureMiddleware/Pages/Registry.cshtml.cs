using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.FileProviders;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace XRCultureMiddleware.Pages
{
    [IgnoreAntiforgeryToken]
    public class RegistryModel : PageModel
    {
        private static readonly ConcurrentBag<string> LogBag = [];
        private static readonly ConcurrentDictionary<string, RegisterViewerRequest> RegisterViewerRequests = new();

        private readonly ILogger<RegistryModel> _logger;
        private readonly IConfiguration _configuration;
        private readonly IOperationSingleton _singletonOperation;
        private readonly IOperationSingletonInstance _singletonOperationInstance;

        public RegistryModel(ILogger<RegistryModel> logger, IConfiguration configuration, IOperationSingleton singletonOperation, IOperationSingletonInstance singletonOperationInstance    )
        {
            _logger = logger;
            _configuration = configuration;
            _singletonOperation = singletonOperation;
            _singletonOperationInstance = singletonOperationInstance;
        }

        const string registrationResponse =
@"<RegistrationResponse>
      <Status>202</Status> <!-- ACCEPTED / use standard HTML response status codes https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status -->
      <ServiceToken>%SERVICE_TOKEN%</ServiceToken>
      <ExpiresIn>3600</ExpiresIn> <!-- in seconds -->
      <Message>Service successfully registered.</Message>
  </RegistrationResponse>";

        const string registrationResponseError =
@"<RegistrationResponse>
      <Status>400</Status> <!-- Bad Request -->
      <Message>%MESSAGE%</Message>
</RegistrationResponse>";

        public async Task<IActionResult> OnPostRegisterAsync()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var xmlRequest = JsonSerializer.Deserialize<string>(body);
            if (string.IsNullOrEmpty(xmlRequest))
            {
                AppendLog("Received empty request.");
                return Content(registrationResponseError.Replace("%MESSAGE%", "Received empty request."));
            }

            AppendLog($"****** RegistrationRequest ******\n{xmlRequest}");

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlRequest);

            var endpoint = xmlDoc.SelectSingleNode("//Endpoint")?.InnerText;
            if (string.IsNullOrEmpty(endpoint))
            {
                AppendLog("Bad request: 'Endpoint'.");
                return Content(registrationResponseError.Replace("%MESSAGE%", "Bad request: 'Endpoint'."));
            }

            if (RegisterViewerRequests.Keys.Contains(endpoint))
            {
                AppendLog($"Viewer registration is in progress for 'Endpoint': {endpoint}");
                return Content(registrationResponseError.Replace("%MESSAGE%", "Viewer registration is in progress."));
            }

            if (_singletonOperationInstance.Viewers.Keys.Contains(endpoint))
            {
                AppendLog($"Viewer is already registered 'Endpoint': {endpoint}");
                return Content(registrationResponseError.Replace("%MESSAGE%", "Viewer is already registered."));
            }

            var serviceToken = Guid.NewGuid().ToString();

            RegisterViewerRequests.AddOrUpdate(endpoint, new RegisterViewerRequest
            {
                EndPoint = endpoint,
                ServiceToken = serviceToken
            }, (key, oldValue) => new RegisterViewerRequest
            {
                EndPoint = endpoint,
                ServiceToken = serviceToken
            });

            return Content(registrationResponse.Replace("%SERVICE_TOKEN%", serviceToken));
        }

        const string authorizationResponse =
@"<AuthorizationResponse>
      <Status>200</Status>
      <SessionToken>%SESSION_TOKEN%</SessionToken>
      <SessionExpires>2025-05-29T15:05:00Z</SessionExpires>
      <Message>Authorization successful.</Message>
  </AuthorizationResponse>";

        const string authorizationResponseError =
@"<AuthorizationResponse>
      <Status>400</Status> <!-- Bad Request -->
      <Message>%MESSAGE%</Message>
</AuthorizationResponse>";

        public async Task<IActionResult> OnPostAuthorizeAsync()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var xmlRequest = JsonSerializer.Deserialize<string>(body);

            if (string.IsNullOrEmpty(xmlRequest))
            {
                AppendLog("Received empty request.");
                return Content(authorizationResponseError.Replace("%MESSAGE%", "Received empty request."));
            }

            AppendLog($"****** AuthorizationRequest ******\n{xmlRequest}");

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlRequest);

            var sessionToken = xmlDoc.SelectSingleNode("//SessionToken")?.InnerText;
            if (string.IsNullOrEmpty(sessionToken))
            {
                AppendLog("Bad request: 'SessionToken'.");
                return Content(authorizationResponseError.Replace("%MESSAGE%", "Bad request: 'SessionToken'."));
            }

            var registerRequest = RegisterViewerRequests.Values.FirstOrDefault(r => r.ServiceToken == sessionToken);
            if (registerRequest == null)
            {
                AppendLog($"Invalid 'SessionToken': {sessionToken}");
                return Content(authorizationResponseError.Replace("%MESSAGE%", "Invalid 'SessionToken'."));
            }

            if (string.IsNullOrEmpty(registerRequest.EndPoint))
            {
                AppendLog("Bad request: 'Endpoint'.");
                return Content(authorizationResponseError.Replace("%MESSAGE%", "Internal error: 'Endpoint'."));
            }

            if (_singletonOperationInstance.Viewers.Keys.Contains(registerRequest.EndPoint))
            {
                AppendLog($"Viewer is already registered 'Endpoint': {registerRequest.EndPoint}");
                return Content(authorizationResponseError.Replace("%MESSAGE%", "Viewer is already registered."));
            }

            _singletonOperationInstance.Viewers.AddOrUpdate(registerRequest.EndPoint, new Viewer
            {
                EndPoint = registerRequest.EndPoint,
                XmlDefinition = xmlRequest,
            }, (key, oldValue) => new Viewer
            {
                EndPoint = registerRequest.EndPoint,
                XmlDefinition = xmlRequest,
            });

            RegisterViewerRequests.Remove(registerRequest.EndPoint, out _);

            return Content(authorizationResponse.Replace("%SESSION_TOKEN%", sessionToken));
        }

        public IActionResult OnGet()
        {
            throw new NotImplementedException("This method is not implemented. Use specific endpoints for operations.");
        }

        public IActionResult OnGetStart()
        {
            if (_singletonOperation.Started)
            {
                AppendLog("Registry is already started.");
                return BadRequest("Registry is already started.");
            }
            _singletonOperation.Started = true;
            AppendLog("Registry started successfully.");
            return Content("Registry started successfully.");
        }

        public IActionResult OnGetStop()
        {
            if (!_singletonOperation.Started)
            {
                AppendLog("Registry is not started.");
                return BadRequest("Registry is not started.");
            }
            _singletonOperation.Started = false;
            AppendLog("Registry stopped successfully.");
            return Content("Registry stopped successfully.");
        }

        public IActionResult OnGetLogs()
        {
             // Return logs as plain text
            var logBuilder = new StringBuilder();
            foreach (var entry in LogBag)
            {
                logBuilder.AppendLine(entry);
            }
            LogBag.Clear(); // Clear logs after retrieval to prevent memory bloat

            return Content(logBuilder.ToString());
        }

        public IActionResult OnGetViewers()
        {
            return Content(JsonSerializer.Serialize(_singletonOperationInstance.Viewers.Keys));
        }

        private void AppendLog(string message)
        {
            var formattedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
            LogBag.Add(formattedMessage);

            _logger.LogInformation(formattedMessage);
        }
    }

    public class RegisterViewerRequest
    {
        public string? EndPoint { get; set; }
        public string? ServiceToken { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }
}
