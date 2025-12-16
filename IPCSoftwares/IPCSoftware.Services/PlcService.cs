using IPCSoftware.Core.Interfaces;
using System;

namespace IPCSoftware.Services
{
    public class PlcService : IPLCService
    {
        private DateTime _simulatedPlcTime;

        public PlcService()
        {
            // Initial PLC time = machine boot-up time (simulated)
            _simulatedPlcTime = DateTime.Now.AddSeconds(-5);
        }

        
        // READ PLC TIME (Dummy)
        
        public DateTime? ReadPlcDateTime()
        {
            try
            {
                // Simulating PLC time running forward
                _simulatedPlcTime = _simulatedPlcTime.AddSeconds(1);

                return _simulatedPlcTime;
            }
            catch
            {
                return null;
            }
        }

        
        // WRITE PLC TIME (Dummy)
        
        public bool WritePlcDateTime(DateTime dt)
        {
            try
            {
                // Simulate writing to PLC
                _simulatedPlcTime = dt;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
