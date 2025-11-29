public class PLCSettings
{
    public string Ip { get; set; }
    public int Port { get; set; }
    public int RegisterCount { get; set; }
}

public class LoggingSettings
{
    public string LogFilePath { get; set; }
}

public class PipeSettings
{
    public string PipeName { get; set; }
}