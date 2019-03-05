using DuaBot.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace DuaBot
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
            => services
                .AddCors(options =>
                {
                    options.AddDefaultPolicy((builder) =>
                    {
                        builder
                            .WithOrigins("https://login.microsoftonline.com")
                            .AllowAnyMethod();
                    });
                })
                .AddLogging(options =>
                {
                    options.SetMinimumLevel(LogLevel.Information);
                })
                .AddMemoryCache()
                .AddHttpClient(nameof(SlackService)).Services
                .AddHttpClient(nameof(CalendarService)).Services
                .AddHostedService<SlackService>()
                .AddHostedService<CalendarService>()
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Latest);

        public void Configure(IApplicationBuilder app) => app.UseMvc();
    }

    public class Program
    {
        public static void Main(string[] args) => CreateWebHostBuilder(args).Build().Run();
        private static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseKestrel()
                .UseUrls("http://localhost:5000")
                .UseStartup<Startup>()
                .UseSerilog((context, logConfig) =>
                {
                    logConfig.Enrich.FromLogContext()
                        .MinimumLevel.Information()
                        .MinimumLevel.Override("System", LogEventLevel.Error)
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                        .WriteTo.Console(theme: AnsiConsoleTheme.Literate);
                });
    }
}
