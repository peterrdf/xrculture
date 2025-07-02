using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.Xml;

namespace XRCultureWebApp.Pages
{
    [Authorize]
    public class LibraryModel : PageModel
    {
        private readonly ILogger<DownloadFolderModel> _logger;
        private readonly IConfiguration _configuration;
        private readonly IOperationSingleton _singletonOperation;

        public LibraryModel(ILogger<DownloadFolderModel> logger, IConfiguration configuration, IOperationSingleton singletonOperation)
        {
            _logger = logger;
            _configuration = configuration;
            _singletonOperation = singletonOperation;
        }

        public void OnGet()
        {
        }

        public List<ModelInfo> GetModelInfos()
        {
            var provider = new PhysicalFileProvider(_configuration["FileStorage:ModelsDir"]);
            var xmlModels = provider.GetDirectoryContents("/").Where((fileInfo) =>
            {
                if (fileInfo.IsDirectory)
                    return false;

                if (!fileInfo.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    return false;

                return true;
            });

            List<ModelInfo> xmlModelInfos = new();
            foreach (var fileInfo in xmlModels)
            {
                _logger.LogInformation($"Found model: {fileInfo.Name} at {fileInfo.PhysicalPath}");

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(fileInfo.PhysicalPath);

                xmlModelInfos.Add(new ModelInfo
                {
                    Id = Path.GetFileNameWithoutExtension(fileInfo.Name),
                    Input = xmlDoc.SelectSingleNode("//model/input")?.InnerText,                    
                    WorkflowName = xmlDoc.SelectSingleNode("//workflow/name")?.InnerText ?? "Unknown",
                    TimeStamp = xmlDoc.SelectSingleNode("//model/timeStamp")?.InnerText ?? "Unknown",
                });
            }

            return xmlModelInfos = xmlModelInfos
                .OrderByDescending(m => {
                    if (string.IsNullOrEmpty(m.TimeStamp) || m.TimeStamp == "Unknown")
                        return DateTime.MinValue;
                    return DateTime.TryParse(m.TimeStamp, out var date) ? date : DateTime.MinValue;
                })
                .ToList();
        }
    }

    public class ModelInfo
    {
        public string? Id { get; set; }
        public string? Input { get; set; }       
        public string? WorkflowName { get; set; }
        public string? TimeStamp { get; set; }
    }
}
