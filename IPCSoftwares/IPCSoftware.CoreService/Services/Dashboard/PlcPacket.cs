namespace IPCSoftware.CoreService.Services.Dashboard
{
    public class PlcPacket
    {
        public int OperatingMin { get; set; }
        public int DownTimeMin { get; set; }
        public int ActualCycle { get; set; }
        public int OKParts { get; set; }
        public int NGParts { get; set; }
        public int TotalParts { get; set; }
        public int IdealCycle { get; set; }
    }
}
