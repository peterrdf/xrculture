using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System.Xml;

namespace XRCultureMiddleware
{
    public class ViewerDescriptor
    {
        public string? Id { get; set; } = string.Empty;
        public string? EndPoint { get; set; }
        public string? BackEnd { get; set; } = string.Empty;
        public string? FrontEnd { get; set; } = string.Empty;
        public string? TimeStamp { get; set; } = string.Empty;

        public static List<ViewerDescriptor> GetViewers(ILogger logger, IConfiguration configuration)
        {
            List<ViewerDescriptor> lsViewerDescriptors = new();
            if (logger == null)
            {
                return lsViewerDescriptors;
            }
            if (configuration == null)
            {
                logger.LogError("Configuration is not set.");
                return lsViewerDescriptors;
            }            

            var viewersDir = configuration["FileStorage:ViewersDir"];
            if (string.IsNullOrEmpty(viewersDir))
            {
                logger.LogError("Viewers path is not configured.");
                return lsViewerDescriptors;
            }

            if (!Directory.Exists(viewersDir))
            {
                logger.LogError($"Viewers directory does not exist: {viewersDir}");
                return lsViewerDescriptors;
            }

            var provider = new PhysicalFileProvider(viewersDir);
            var xmlViewers = provider.GetDirectoryContents("/").Where((fileInfo) =>
            {
                if (fileInfo.IsDirectory)
                    return false;

                if (!fileInfo.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    return false;

                return true;
            });

            foreach (var fileInfo in xmlViewers)
            {
                logger.LogInformation($"Found viewer: {fileInfo.Name} at {fileInfo.PhysicalPath}");

                XmlDocument xmlDoc = new();
                xmlDoc.Load(fileInfo.PhysicalPath!);

                lsViewerDescriptors.Add(new()
                {
                    Id = xmlDoc.SelectSingleNode("//Viewer/Id")?.InnerText ?? "NA",
                    EndPoint = xmlDoc.SelectSingleNode("//Viewer/EndPoint")?.InnerText ?? "NA",
                    BackEnd = xmlDoc.SelectSingleNode("//Viewer/BackEnd")?.InnerText ?? "NA",
                    FrontEnd = xmlDoc.SelectSingleNode("//Viewer/FrontEnd")?.InnerText ?? "NA",
                    TimeStamp = xmlDoc.SelectSingleNode("//Viewer/TimeStamp")?.InnerText ?? "NA",
                });
            }

            return lsViewerDescriptors;
        }

        public static bool IsViewerRegistered(ILogger logger, IConfiguration configuration, string endPoint)
        {
            var viewers = ViewerDescriptor.GetViewers(logger, configuration);
            foreach (var viewer in viewers)
            {
                if (viewer.EndPoint == endPoint)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
