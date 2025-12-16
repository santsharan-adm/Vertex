namespace IPCSoftware.Shared.Models.Messaging
{
    public class RequestPackage
    {
        public int RequestId { get; set; }
        //public Dictionary<uint, object> Parameters { get; set; } = new();

        public object Parameters { get; set; }
    }
}
