using System;
using System.Net;
using NLog.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace ASP_NET_CORE_Samples
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Stopped program because of exception");
                throw;
            }
            finally
            {
                NLog.LogManager.Flush();
                NLog.LogManager.Shutdown();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureLogging((hostingContext, logging) =>
                    {
                        logging.ClearProviders();
                        logging.SetMinimumLevel(LogLevel.Information);
                    });
                    webBuilder.UseNLog();
                    webBuilder.UseStartup<Startup>();
                    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == Environments.Development)
                        webBuilder.UseUrls("http://localhost/");
                    else
                        webBuilder.UseKestrel((host, options) =>
                        {
                            var filename = host.Configuration.GetValue("KestrelSettings:certfilename", "");
                            var password = host.Configuration.GetValue("KestrelSettings:certpassword", "");
                            options.Listen(IPAddress.Any, 443, listenOptions =>
                            {
                                listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
                                listenOptions.UseHttps(filename, password);
                            });
                            options.Listen(IPAddress.Any, 80, listenOptions =>
                            {
                                listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
                            });
                        });
                });
    }
}
