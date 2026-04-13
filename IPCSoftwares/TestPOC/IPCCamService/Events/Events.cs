using CommonLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCCamService.Events
{
    public delegate void RaiseErrorDelegate(object sender, ErrorModel error);
    public delegate void RaiseCamResponseReceivedDelegate(object sender, uint unitno, uint modbusRegister, ushort value);
    public delegate ushort RaiseCamRequestReceivedDelegate(object sender, byte unitId, uint modbusRegister);
}
