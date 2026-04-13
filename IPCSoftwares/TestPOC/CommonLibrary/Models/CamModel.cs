using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Models
{
   
    public class CameraModel 
    {
        public uint Id { get; set; } = 1;
        public string Name { get; set; } = "Cam1";
        
        public string IPAddress { get; set; } = "192.100.63.11";
        
        public int PortNo { get; set; } = 501;

        
        public string Gateway { get; set; } = "192.100.63.1";
        public string Subnet { get; set; } = "255.255.255.0";


        
        public Protocol Protocol { get; set; } = Protocol.FTP;

        public string Username { get; set; } = "admin";
        public string Password { get; set; } = "admin";

        public bool IsEnabled { get; set; } = true;
        bool _IsAnonymousAccess = false;
        public bool IsAnonymousAccess { get; set; } = false;
        public string RemotePath { get; set; } = "/";
        public string LocalPath { get; set; } = "C:\\Temp";


        public Dictionary<uint, PLCData> Data { get; set; } = new Dictionary<uint, PLCData>();
        public PLCBlocks Blocks { get; set; } = new PLCBlocks();
        
        public string Description { get; set; } 
        
        public string Remark { get; set; }


        public void Copy(CameraModel item)
        {;
            if (item is CameraModel)
            {
                this.Id = ((CameraModel)item).Id;
                this.Name = ((CameraModel)item).Name;
                this.IPAddress = ((CameraModel)item).IPAddress;
                this.PortNo = ((CameraModel)item).PortNo;
                this.Gateway = ((CameraModel)item).Gateway;
                this.Subnet = ((CameraModel)item).Subnet;
                this.IsAnonymousAccess = ((CameraModel)item).IsAnonymousAccess;
                this.IsEnabled = ((CameraModel  )item).IsEnabled;
                this.Username = ((CameraModel)item).Username;
                this.Password = ((CameraModel)item).Password;
                this.RemotePath = ((CameraModel)item).RemotePath;
                this.LocalPath = ((CameraModel)item).LocalPath;
                this.Description = ((CameraModel)item).Description;
                this.Remark = ((CameraModel)item).Remark;


            }
        }

        public CameraModel LoadFromStringArray(string[] str)
        {
            try
            {
                if (str.Length >= 12)
                {
                    this.Id = uint.Parse(str[0]);
                    this.Name = str[1];
                    this.IPAddress = str[2];
                    this.PortNo = int.Parse(str[3]);
                    this.Gateway = str[4];
                    this.Subnet = str[5];
                    this.IsAnonymousAccess = bool.Parse(str[6]);
                    this.Username = str[7];
                    this.Password = str[8];
                    this.RemotePath = str[9];
                    this.LocalPath = str[10];
                    this.IsEnabled = bool.Parse(str[11]);
                    if (str.Length >= 13)
                        this.Description = str[12];
                    if (str.Length >= 14)
                        this.Remark = str[13];
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"CameraModel LoadFromStringArray failed. {ErrorModel.GetErrorExceptionDetail(ex)}");
            }
            return this;
        }
    }

    public class Cameras : ObservableCollection<CameraModel>
    {

        public CameraModel GetCameraById(uint Id)
        {
            return this.FirstOrDefault(q => q.Id == Id);
        }



    }

    public enum Protocol
    {
        FTP,
        EthernetIP,
        EtherCat,
        Custom
    }
}
