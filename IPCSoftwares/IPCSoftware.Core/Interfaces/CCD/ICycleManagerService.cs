using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace IPCSoftware.Core.Interfaces.CCD
{
    public  interface ICycleManagerService
    {
        void HandleIncomingImage(string tempImagePath, string qrCodeString = null);
    }
}
