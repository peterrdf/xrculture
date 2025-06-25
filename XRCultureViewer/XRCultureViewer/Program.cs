using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Serilog;

namespace XRCultureViewer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Create folders for logs and viewer content
            var viewerPath = builder.Configuration["Paths:Viewer"];
            if (string.IsNullOrEmpty(viewerPath))
            {
                throw new InvalidOperationException("Viewer path is not configured.");
            }

            var logsDir = Path.Combine(viewerPath, @"logs");
            Directory.CreateDirectory(logsDir);
            var dataDir = Path.Combine(viewerPath, @"data");
            Directory.CreateDirectory(dataDir);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddHttpContextAccessor();

            // Serilog
            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .WriteTo.File(Path.Combine(logsDir, "log.txt"), rollingInterval: RollingInterval.Day)
            );

            builder.Services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
            });

            builder.Services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
            });

            var app = builder.Build();

            // Configure middleware
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Configure custom MIME types for WebGL content
            var extensionProvider = new FileExtensionContentTypeProvider();
            extensionProvider.Mappings.Add(".data", "application/octet-stream");
            extensionProvider.Mappings.Add(".binz", "application/octet-stream");

            // Serve viewer content with specific settings
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.WebRootPath, "viewer")),
                RequestPath = "/viewer",
                ContentTypeProvider = extensionProvider,
                ServeUnknownFileTypes = true,
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                    ctx.Context.Response.Headers["Pragma"] = "no-cache";
                    ctx.Context.Response.Headers["Expires"] = "0";
                }
            });

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseRouting();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            app.Run();
        }
    }
}
