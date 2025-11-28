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
    public string OperatingTime { get; set; }   // hh:mm:ss
    public string Downtime { get; set; }        // hh:mm:ss
    public double AverageCycleTime { get; set; } // numeric
}
