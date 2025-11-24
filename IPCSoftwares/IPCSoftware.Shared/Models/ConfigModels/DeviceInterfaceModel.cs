using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models.ConfigModels
{
    public class DeviceInterfaceModel
    {
        public int Id { get; set; }
        public int DeviceNo { get; set; }
        public string DeviceName { get; set; }
        public int UnitNo { get; set; }
        public string Name { get; set; }
        public string ComProtocol { get; set; }  // Modbus Ethernet, EthernetIP, EtherCat, RTU
        public string IPAddress { get; set; }
        public int PortNo { get; set; }
        public string Gateway { get; set; }
        public string Description { get; set; }
        public string Remark { get; set; }
        public bool Enabled { get; set; }

        public DeviceInterfaceModel()
        {
            Enabled = false;
            PortNo = 502;
        }

        public DeviceInterfaceModel Clone()
        {
            return new DeviceInterfaceModel
            {
                Id = this.Id,
                DeviceNo = this.DeviceNo,
                DeviceName = this.DeviceName,
                UnitNo = this.UnitNo,
                Name = this.Name,
                ComProtocol = this.ComProtocol,
                IPAddress = this.IPAddress,
                PortNo = this.PortNo,
                Gateway = this.Gateway,
                Description = this.Description,
                Remark = this.Remark,
                Enabled = this.Enabled
            };
        }
    }
}
