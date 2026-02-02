namespace IPCSoftware.Shared.Models.ApiTest
{
    public class ApiTestSettings
    {
        public string Protocol { get; set; } = "https";
        public string Host { get; set; } = "localhost:5000";
        public string Endpoint { get; set; } = "sfc_post";
        public string TwoDCodeData { get; set; }
        public string PreviousStationCode { get; set; }
        public string CurrentMachineCode { get; set; }

        public static ApiTestSettings CreateDefault()
        {
            return new ApiTestSettings
            {
                Protocol = "https",
                Host = "localhost:5000",
                Endpoint = "sfc_post",
                TwoDCodeData = string.Empty,
                PreviousStationCode = string.Empty,
                CurrentMachineCode = string.Empty
            };
        }
    }
}
