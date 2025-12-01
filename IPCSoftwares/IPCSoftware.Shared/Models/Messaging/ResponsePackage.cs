namespace IPCSoftware.Shared.Models.Messaging
{
    public class ResponsePackage
    {
        public int ResponseId { get; set; }
        public Dictionary<uint, object> Parameters { get; set; }
    }
}
