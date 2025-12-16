using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models.ConfigModels
{
    public class DeviceModel
    {
        public int Id { get; set; }
        public int DeviceNo { get; set; }
        public string DeviceName { get; set; }
        public string DeviceType { get; set; }  // PLC, Robo, CCD
        public string Make { get; set; }
        public string Model { get; set; }
        public string Description { get; set; }
        public string Remark { get; set; }
        public bool Enabled { get; set; }

        public DeviceModel()
        {
            Enabled = true;
        }

        public DeviceModel Clone()
        {
            return new DeviceModel
            {
                Id = this.Id,
                DeviceNo = this.DeviceNo,
                DeviceName = this.DeviceName,
                DeviceType = this.DeviceType,
                Make = this.Make,
                Model = this.Model,
                Description = this.Description,
                Remark = this.Remark,
                Enabled = this.Enabled
            };
        }
    }
}
