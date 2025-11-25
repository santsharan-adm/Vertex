using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Shared.Models.ConfigModels
{
    public class CameraInterfaceModel
    {
        public int Id { get; set; }
        public int DeviceNo { get; set; }
        public string DeviceName { get; set; }
        public string Name { get; set; }
        public string Protocol { get; set; }  // FTP-Server, FTP-Client, EthernetIP, Ethercat, Custom
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public string Gateway { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool AnonymousLogin { get; set; }
        public string RemotePath { get; set; }  // For FTP Server: Physical Path, For FTP Client: Remote Path
        public string LocalDirectory { get; set; }
        public bool Enabled { get; set; }
        public string Description { get; set; }
        public string Remark { get; set; }

        public CameraInterfaceModel()
        {
            Enabled = true;
            Protocol = "FTP-Server";
            AnonymousLogin = false;
            Port = 21;
        }

        public CameraInterfaceModel Clone()
        {
            return new CameraInterfaceModel
            {
                Id = this.Id,
                DeviceNo = this.DeviceNo,
                DeviceName = this.DeviceName,
                Name = this.Name,
                Protocol = this.Protocol,
                IPAddress = this.IPAddress,
                Port = this.Port,
                Gateway = this.Gateway,
                Username = this.Username,
                Password = this.Password,
                AnonymousLogin = this.AnonymousLogin,
                RemotePath = this.RemotePath,
                LocalDirectory = this.LocalDirectory,
                Enabled = this.Enabled,
                Description = this.Description,
                Remark = this.Remark
            };
        }
    }
}
