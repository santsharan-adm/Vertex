using System;
using System.IO;
using CommonLibrary;
using CommonLibrary.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace IPCPLCService
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
            var plcConfigPath = tempConfig["AppSettings:plcConfigPath"];
            var plcTagConfigPath = tempConfig["AppSettings:plcTagConfigPath"];

            // Set working folder to the current executable folder
            //var exeFolder = AppContext.BaseDirectory;
            //Directory.SetCurrentDirectory(exeFolder);

            // Use a relative path (no leading slash) so it resolves against the working folder
            //var plcConfigPath = Path.Combine("Database", "plcs.config");
            //var plcTagConfigPath = Path.Combine("Database", "plctags.config");
            //builder.Services.AddSingleton<IFileHandler>(new CsvFileHandler(plcConfigPath));
            //builder.Services.AddSingleton<IFileHandler>(new CsvFileHandler(plcTagConfigPath));
            //builder.Services.AddHostedService<PLCEngine>();


            builder.Services.AddHostedService(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<PLCEngine>>();
                var configHandler = new CsvFileHandler(plcConfigPath);
                var tagHandler = new CsvFileHandler(plcTagConfigPath);
                return new PLCEngine(logger, configHandler, tagHandler);
            });


            var host = builder.Build();
            host.Run();
        }
    }
}