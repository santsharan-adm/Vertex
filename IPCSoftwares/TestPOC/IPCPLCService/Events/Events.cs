using CommonLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCPLCService.Events
{
    public delegate void RaiseErrorDelegate(object sender, ErrorModel error);
    public delegate void RaiseModbusResponseReceivedDelegate(object sender, uint tagId, object value);
    public delegate ushort RaiseModbusRequestReceivedDelegate(object sender, byte unitId, uint modbusRegister);
    
}
