namespace IPCSoftware.Shared.Models.Messaging
{
    public class ResponsePackage
    {
        public int ResponseId { get; set; }
        public Dictionary<int, object> Parameters { get; set; }
        public bool Success { get; set; }   
        public string ErrorMessage { get; set; }  
    }

    
}
