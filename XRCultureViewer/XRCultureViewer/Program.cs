using Microsoft.AspNetCore.Mvc;
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

            // Create folders for models and logs
            var modelsDir = builder.Configuration["FileStorage:ModelsDir"];
            if (string.IsNullOrEmpty(modelsDir))
            {
                throw new InvalidOperationException("Models path is not configured.");
            }
            Directory.CreateDirectory(modelsDir);

            var logsDir = builder.Configuration["FileStorage:LogsDir"];
            if (string.IsNullOrEmpty(logsDir))
            {
                throw new InvalidOperationException("Logs path is not configured.");
            }
            Directory.CreateDirectory(logsDir);            

            // Serilog
            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .WriteTo.File(Path.Combine(logsDir, "log.txt"), rollingInterval: RollingInterval.Day)
            );

            builder.Services.AddControllersWithViews();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = "XRCultureViewerCookieAuth";
                options.DefaultChallengeScheme = "XRCultureViewerCookieAuth";
            })
            .AddCookie("XRCultureViewerCookieAuth", options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";                
                options.ReturnUrlParameter = "returnUrl";
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.None;
            }).AddNegotiate();

            builder.Services.AddAuthorization(options =>
            {
                // By default, all incoming requests will be authorized according to the default policy.
                options.FallbackPolicy = options.DefaultPolicy;
            });

            builder.Services.AddRazorPages(options =>
            {
                //#todo Remove when Authentication is fully implemented
                options.Conventions.AllowAnonymousToPage("/Index");
                options.Conventions.AllowAnonymousToPage("/Viewer");
                options.Conventions.AllowAnonymousToPage("/Storage");

                options.Conventions.AllowAnonymousToPage("/Account/Login");
                options.Conventions.AllowAnonymousToPage("/Account/Logout");
                options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");                
            });

            builder.Services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
            });

            builder.Services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
            });

            builder.Services.AddRazorPages();
            builder.Services.AddDirectoryBrowser();
            builder.Services.AddHttpContextAccessor();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Custom MIME types and other configuration
            var extensionProvider = new FileExtensionContentTypeProvider();
            extensionProvider.Mappings.Add(".data", "application/octet-stream");
            extensionProvider.Mappings.Add(".binz", "application/octet-stream");

            // Then protected viewer content only
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

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapRazorPages();

            app.Run();
        }
    }
}
