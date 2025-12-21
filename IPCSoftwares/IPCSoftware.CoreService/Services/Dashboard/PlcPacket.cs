namespace IPCSoftware.CoreService.Services.Dashboard
{
    public class PlcPacket
    {
        public int PlcNo { get; set; }
        public DateTime Timestamp { get; set; }

        public Dictionary<int, object> Values { get; set; } = new Dictionary<int, object>();
     
    }
}
    