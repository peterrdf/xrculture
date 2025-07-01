using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Serilog;
using System.Collections.Concurrent;

namespace XRCultureWebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            var logDir = Path.Combine(builder.Configuration["FileStorage:ModelsDir"], @"logs");
            Directory.CreateDirectory(logDir);

            // Add Serilog
            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .WriteTo.File(Path.Combine(logDir, "log.txt"), rollingInterval: RollingInterval.Day)
            );

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = "XRCultureCookieAuth";
                options.DefaultChallengeScheme = "XRCultureCookieAuth";
            })
            .AddCookie("XRCultureCookieAuth", options =>
            {
                options.LoginPath = "/Login";
                options.AccessDeniedPath = "/AccessDenied";
                options.LogoutPath = "/Logout";
                options.ReturnUrlParameter = "returnUrl";
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.None;
            }).AddNegotiate();
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddAuthorization(options =>
            {
                // By default, all incoming requests will be authorized according to the default policy.
                options.FallbackPolicy = options.DefaultPolicy;
            });

            builder.Services.AddTransient<IOperationTransient, Operation>();
            builder.Services.AddScoped<IOperationScoped, Operation>();
            builder.Services.AddSingleton<IOperationSingleton, Operation>();
            builder.Services.AddSingleton<IOperationSingletonInstance>(new Operation(Guid.Empty));
            builder.Services.AddTransient<OperationService, OperationService>();

            builder.Services.AddRazorPages();
            builder.Services.AddDirectoryBrowser();

            builder.Services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
            });

            builder.Services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
            });

            // For form uploads specifically
            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100MB
                options.ValueLengthLimit = 100 * 1024 * 1024;
            });

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

            var extensionProvider = new FileExtensionContentTypeProvider();
            extensionProvider.Mappings.Add(".data", "application/octet-stream");
            extensionProvider.Mappings.Add(".binz", "application/octet-stream");            

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.WebRootPath, "viewer")),
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

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.WebRootPath, "data")),
                RequestPath = "/data",
                ContentTypeProvider = extensionProvider,
                ServeUnknownFileTypes = true,
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                    ctx.Context.Response.Headers["Pragma"] = "no-cache";
                    ctx.Context.Response.Headers["Expires"] = "0";
                }
            });

            //app.UseFileServer(new FileServerOptions
            //{
            //    FileProvider = new PhysicalFileProvider(
            //        Path.Combine(builder.Environment.WebRootPath, "data")),
            //    RequestPath = "/data",
            //    EnableDirectoryBrowsing = true,
            //});

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapRazorPages();

            //app.MapControllerRoute(
            //    name: "default",
            //    pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
