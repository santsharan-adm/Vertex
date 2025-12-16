using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Core.Interfaces
{
    public interface IAppConfig
    {
        string DataFolder { get; }
        string PlcTagsFile { get; }
        string QrCodeImagePath { get; }


    
    }
}
