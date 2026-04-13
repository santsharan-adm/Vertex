using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Formats.Asn1;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Models
{
    public class PLCModel:ModelBase
    {
        int _id;
        public int Id
        {
            get => _id;
            set
            {
                if (value != _id)
                {
                    _id = value;
                    RaisePropertyChanged(nameof(Id));
                }
            }
        }
        string _name="PLC1";
        public string Name 
        {
            get => _name;
            set
            {
                if (value != _name)
                {
                    _name = value;
                    RaisePropertyChanged(nameof(Name));
                }
            }
        }
        uint _plcNo = 1;
        public uint PLCNo
        {
            get => _plcNo;
            set
            {
                if (value != _plcNo)
                {
                    _plcNo = value;
                    RaisePropertyChanged(nameof(PLCNo));
                }
            }
        }
        string _ipAddress = "192.100.63.11";
        //string _ipAddress = "169.254.32.251";
        public string IPAddress
        {
            get => _ipAddress;
            set
            {
                if (value != _ipAddress)
                {
                    _ipAddress = value;
                    RaisePropertyChanged(nameof(IPAddress));
                }
            }
        }
        int _portNo  = 501;
        public int PortNo
        {
            get => _portNo;
            set
            {
                if (value != _portNo)
                {
                    _portNo = value;
                    RaisePropertyChanged(nameof(PortNo));
                }
            }
        }

        CommunicationProtocol _protocol;
        public CommunicationProtocol Protocol
        {
            get => _protocol;
            set
            {
                if (value != _protocol)
                {
                    _protocol = value;
                    RaisePropertyChanged(nameof(Protocol));
                }
            }
        }
        

       

        public Dictionary<PLCTagModel,PLCData> Data { get; set; }= new Dictionary<PLCTagModel, PLCData>();
        public PLCBlocks Blocks { get; set; } = new PLCBlocks();
        string _description;
        public string Description
        {
            get => _description;
            set
            {
                if (value != _description)
                {
                    _description = value;
                    RaisePropertyChanged(nameof(Description));
                }
            }
        }
        string _remark;
        public string Remark
        {
            get => _remark;
            set
            {
                if (value != _remark)
                {
                    _remark = value;
                    RaisePropertyChanged(nameof(Remark));
                }
            }
        }


        public override void Copy(ModelBase item)
        {
            base.Copy(item);
            if(item is PLCModel)
            {
                this.Name = ((PLCModel)item).Name;
                this.PLCNo= ((PLCModel)item).PLCNo;
                this.IPAddress = ((PLCModel)item).IPAddress;
                this.PortNo= ((PLCModel)item).PortNo;
                this.Protocol = ((PLCModel)item).Protocol;
                this.Description = ((PLCModel)item).Description;
                this.Remark = ((PLCModel)item).Remark;

            }

           
        }

        public void LoadFromStringArray(string[] data)
        {
            if (data.Length >= 7)
            {
                this.Id = int.Parse(data[0]);
                this.Name = data[1];
                this.PLCNo = uint.Parse(data[2]);
                this.IPAddress = data[3];
                this.PortNo = int.Parse(data[4]);
                this.Protocol = Enum.Parse<CommunicationProtocol>(data[5]);
                this.Description = data[6];
                if (data.Length >= 8)
                {
                    this.Remark = data[7];
                }
            }
        }


    }

    public class PLCs : ObservableCollection<PLCModel>
    {

        public PLCModel GetPLCByNo(uint plcNo)
        {
            return this.FirstOrDefault(q => q.PLCNo == plcNo);
        }



    }

    public enum CommunicationProtocol
    {        
        EthernetModbus,
        ModbusRTU,
        EthernetIP,
        EtherCat,
        Custom
    }
}
