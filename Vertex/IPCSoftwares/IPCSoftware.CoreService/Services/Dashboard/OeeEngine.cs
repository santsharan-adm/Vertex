// OeeEngine.cs
namespace IPCSoftware.CoreService.Services.Dashboard
{
    public class OeeResult
    {
        public double Availability { get; set; }
        public double Performance { get; set; }
        public double Quality { get; set; }
        public double OverallOEE { get; set; }

        public string OperatingTime { get; set; }
        public string Downtime { get; set; }

        public int OKParts { get; set; }
        public int NGParts { get; set; }
    }

    public class OeeEngine
    {
        public OeeResult Calculate(PlcPacket p)
        {
            OeeResult r = new OeeResult();

            // 1. Availability (A) Calculation
            double totalTimeMin = p.OperatingMin + p.DownTimeMin;
            r.Availability = 0.0;
            if (totalTimeMin > 0)
              
                r.Availability = (double)p.OperatingMin / totalTimeMin;

          
            r.Quality = 0.0;
            if (p.TotalParts > 0)
               
                r.Quality = (double)p.OKParts / (double)p.TotalParts;

           
            r.Performance = 0.0;
            if (p.OperatingMin > 0 && p.IdealCycle > 0)
            {
               
                double operatingSeconds = (double)p.OperatingMin * 60.0;

                if (operatingSeconds > 0)
                {
                    r.Performance = ((double)p.IdealCycle * p.TotalParts) / operatingSeconds;
                }
            }

            r.OverallOEE = r.Availability * r.Performance * r.Quality;

            // Raw values
            r.OKParts = p.OKParts;
            r.NGParts = p.NGParts;

           
            r.OperatingTime = $"{p.OperatingMin} min";
            r.Downtime = $"{p.DownTimeMin} min";

            return r;
        }
    }
}