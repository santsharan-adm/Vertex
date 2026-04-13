using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Models
{
    public class DeviceModel: ModelBase
    {
      
        public int Id { get; set; }
        public string Name { get; set; } = "PLC1";
        uint _plcNo = 1;
        public DeviceType DeviceType { get; set; } = DeviceType.PLC;
        public string Make { get; set; } = "Make";
        
       
        public string Description { get; set; }
        
        public string Remark { get; set; }


        public override void Copy(ModelBase item)
        {
            base.Copy(item);
            if (item is DeviceModel)
            {
                this.Name = ((DeviceModel)item).Name;
                this.DeviceType = ((DeviceModel)item).DeviceType;
                
                this.Description = ((DeviceModel)item).Description;
                this.Remark = ((DeviceModel)item).Remark;

            }


        }

        public void LoadFromStringArray(string[] data)
        {
            if (data.Length >= 7)
            {
                this.Id = int.Parse(data[0]);
                this.Name = data[1];
                this.DeviceType = Enum.Parse<DeviceType>(data[5]);
               
               
                this.Description = data[6];
                this.Remark = data[7];
            }
        }
    }

    public class Devices : ObservableCollection<DeviceModel>
    {

        public DeviceModel GetDeviceByNo(uint plcNo)
        {
            return this.FirstOrDefault(q => q.Id == plcNo);
        }



    }

    public enum DeviceType
    {
        PLC,
        CCD,
        UI
    }
}
