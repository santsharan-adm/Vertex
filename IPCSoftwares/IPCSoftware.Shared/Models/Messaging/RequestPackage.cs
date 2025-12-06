namespace IPCSoftware.Shared.Models.Messaging
{
    public class RequestPackage
    {
        public uint RequestId { get; set; }
        //public Dictionary<uint, object> Parameters { get; set; } = new();

        public object Parameters { get; set; }
    }
}
