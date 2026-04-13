using CommonLibrary;

namespace IPCCoreService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            //builder.Services.AddHostedService<CoreEngine>();
            var plcTagConfigPath = Path.Combine("C:\\Users\\benny.kurian\\source\\repos\\IPCSoftware\\IPCPLCService\\bin\\Debug\\net8.0\\Database", "plctags.config");
            builder.Services.AddHostedService(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<CoreEngine>>();
                var tagHandler = new CsvFileHandler(plcTagConfigPath);
                return new CoreEngine(logger,  tagHandler);
            });

            var host = builder.Build();
            host.Run();
        }
    }
}