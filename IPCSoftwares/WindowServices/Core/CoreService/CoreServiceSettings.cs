public class CoreServiceSettings
{
    public string PlcPipeName { get; set; }
    public string WpfPipeName { get; set; }
    public string LogFilePath { get; set; }
}



// ===========================
// DASHBOARD DATA MODEL
// ===========================
public class DashboardData
{
    public string OperatingTime { get; set; } = string.Empty;
    public string Downtime { get; set; } = string.Empty;
    public double AverageCycleTime { get; set; }
}
