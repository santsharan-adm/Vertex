namespace IPCSoftware.CoreService.Services.Dashboard
{
    public class PlcPacket
    {
        public int PlcNo { get; set; }
        public DateTime Timestamp { get; set; }

        public Dictionary<uint, object> Values { get; set; } = new Dictionary<uint, object>();
        public int OperatingMin { get; set; }
        public int DownTimeMin { get; set; }
        public int ActualCycle { get; set; }
        public int OKParts { get; set; }
        public int NGParts { get; set; }
        public int TotalParts { get; set; }
        public int IdealCycle { get; set; }
    }
}
