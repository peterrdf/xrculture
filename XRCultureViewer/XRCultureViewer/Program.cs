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
                    //#todo
                    //Only protect the viewer content, not CSS/JS
                    //if (ctx.Context.Request.Path.StartsWithSegments("/viewer"))
                    //{
                    //    if (!ctx.Context.User.Identity.IsAuthenticated)
                    //    {
                    //        string redirectUrl = "/Account/AccessDenied" +
                    //            Uri.EscapeDataString(ctx.Context.Request.Path + ctx.Context.Request.QueryString);

                    //        ctx.Context.Response.Clear();
                    //        ctx.Context.Response.StatusCode = 302;
                    //        ctx.Context.Response.Headers["Location"] = redirectUrl;

                    //        // Prevent further processing
                    //        ctx.Context.Response.Body = Stream.Null;
                    //        return;
                    //    }
                    //}

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
