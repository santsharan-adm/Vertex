using CommonLibrary;
using CommonLibrary.Models;
using Microsoft.Extensions.Configuration; // Add this for IConfiguration
using Microsoft.Extensions.Hosting; // Add this for Host
using System.Collections.Concurrent;

namespace IPCCamService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Directly add appsettings.json to the ConfigurationManager
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var tempConfig = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            var camConfigPath = tempConfig["AppSettings:camConfigPath"];

            builder.Services.AddSingleton(new ConcurrentQueue<ResponsePackage>());
           // builder.Services.AddHostedService<CamEngine>();

            builder.Services.AddHostedService(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<CamEngine>>();
                var configHandler = sp.GetRequiredService<IConfiguration>();
                var camHandler = new CsvFileHandler(camConfigPath);
                return new CamEngine(logger, configHandler, camHandler);
            });

            var host = builder.Build();
            host.Run();
        }
    }
}