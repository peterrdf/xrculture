using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.FileProviders;
using System.Xml;
using XRCultureViewer.Pages.Shared;

namespace XRCultureViewer.Pages
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IConfiguration _configuration;

        public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public void OnGet()
        {
        }

        public List<ModelDescriptor> GetModels()
        {
            List<ModelDescriptor> lsModelDescriptors = new();
            if (_configuration == null)
            {
                _logger.LogError("Configuration is not set.");
                return lsModelDescriptors;
            }

            var modelsDir = _configuration["FileStorage:ModelsDir"];
            if (string.IsNullOrEmpty(modelsDir))
            {
                _logger.LogError("Models path is not configured.");
                return lsModelDescriptors;
            }

            if (!Directory.Exists(modelsDir))
            {
                _logger.LogError($"Models directory does not exist: {modelsDir}");
                return lsModelDescriptors;
            }

            var provider = new PhysicalFileProvider(modelsDir);
            var xmlModels = provider.GetDirectoryContents("/").Where((fileInfo) =>
            {
                if (fileInfo.IsDirectory)
                    return false;

                if (!fileInfo.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    return false;

                return true;
            });
            
            foreach (var fileInfo in xmlModels)
            {
                _logger.LogInformation($"Found model: {fileInfo.Name} at {fileInfo.PhysicalPath}");

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(fileInfo.PhysicalPath!);

                lsModelDescriptors.Add(new ()
                {
                    Id = xmlDoc.SelectSingleNode("//model/id")?.InnerText ?? "NA",
                    Extension = xmlDoc.SelectSingleNode("//model/extension")?.InnerText ?? "NA",
                    Name = xmlDoc.SelectSingleNode("//model/name")?.InnerText ?? "NA",
                    Description = xmlDoc.SelectSingleNode("//model/description")?.InnerText ?? "NA",
                    TimeStamp = xmlDoc.SelectSingleNode("//model/timeStamp")?.InnerText ?? "NA",
                });
            }

            return lsModelDescriptors;
        }
    }

    public class ModelDescriptor
    {
        public string? Id { get; set; }
        public string? Extension { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? TimeStamp { get; set; }
    }
}
