using CommonLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace IPCCCDService.Events
{
    public delegate void RaiseErrorDelegate(object sender, ErrorModel error);
    public delegate void RaisePLCResponseReceivedDelegate(object sender, uint tagId, object value);
    public delegate ushort RaiseModbusRequestReceivedDelegate(object sender, byte unitId, uint modbusRegister);
    public delegate UIDataModel RaiseGetUIResponseDelegate(object sender); 
    public delegate Dictionary<uint,object> RaiseGetUITagsResponseDelegate(object sender);
    public delegate CCDModel RaiseGetCCDDataDelegate(object sender);

}
