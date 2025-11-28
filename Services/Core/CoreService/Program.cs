using System;
using System.ServiceProcess;

namespace CoreService
{
    internal static class Program
    {
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new CoreServiceWorker()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
