using CommonLibrary.Models;
using IPCPLCService.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace IPCPLCService
{
    //public delegate void RaiseErrorDelegate(object sender, ErrorModel error);


    public class PLCInterface
    {
        

        protected bool ShudownInitiated;
        public event RaiseErrorDelegate? ErrorEvent;
        public event RaiseModbusResponseReceivedDelegate? RaiseModbusResponseReceivedEvent;

        public PLCModel PLC { get; set; }

        protected object obLockPlcBlock= new object();
        protected object obLockPlcWriteList = new object();

        public PLCInterface()
        {
            var exeFolder = AppContext.BaseDirectory;
            Directory.SetCurrentDirectory(exeFolder);
        }

        protected void RaiseError(ErrorModel error)
        {
            if (ErrorEvent != null)
            {
                ErrorEvent(this, error);
            }
        }
        public void Shutdown()
        {
            ShudownInitiated = true;
        }

        public virtual void Start(object? item)
        {
            if (item is PLCModel)
            {
                PLC = (PLCModel)item;

                RaiseError( new ErrorModel(0, Severity.Verbose, "PLC Interface",
                    $"Starting PLC Interface. PLC Name is {PLC.Name}, PLCNo is {PLC.PLCNo}, Address is {PLC.IPAddress}, PortNo is {PLC.PortNo}", ""));
                OnStart(item);
                LoadConfiguration(PLC);
                Run();

            }

        }
        public virtual void OnStart(object? item)
        {
        }

        protected TcpClient client;
        protected NetworkStream stream;
        public void Run()
        {
            if (PLC != null)
            {
                while (!ShudownInitiated)
                {
                    try
                    {
                        OpenConnection();

                        StartPolling();

                        
                    }
                    catch (Exception ex)
                    {
                        RaiseError(new ErrorModel(0, Severity.Error, "PLC Interface",
                                $"Failed to open communication with PLC. PLC - {PLC.Name} Pls refer error details.", ""));

                        string err = ErrorModel.GetErrorExceptionDetail(ex);
                        RaiseError(new ErrorModel(0, Severity.Error, "PLC Interface",
                                $"Communication Failed. PLC - {PLC.Name} Error - {err}", ""));
                        
                    }
                    finally
                    {
                        CloseConnection();
                    }
                    Thread.Sleep(1000);

                }
            }            
        }

        protected virtual bool OpenConnection()
        {
            RaiseError(new ErrorModel(0, Severity.Verbose, "PLC Interface", $"Openning connection to PLC - {PLC.Name}, IPAddress - {PLC.IPAddress}, Port is {PLC.PortNo}.", ""));
            
            client = new TcpClient(PLC.IPAddress, PLC.PortNo);
            RaiseError(new ErrorModel(0, Severity.Verbose, "PLC Interface",
                $"PLC Interface opened successfully. PLC - {PLC.Name}, IPAddress is {PLC.IPAddress}, PortNo is {PLC.PortNo}", ""));
            return true;
        }

        protected virtual void CloseConnection()
        {
            RaiseError(new ErrorModel(0, Severity.Verbose, "PLC Interface",
                $"Clossing PLC Interface. PLC - {PLC.Name}",""));
            if (client != null)
            {
                client.Close();
            }
            RaiseError(new ErrorModel(0, Severity.Verbose, "PLC Interface",
                $"Clossing PLC Interface. PLC - {PLC.Name}", ""));
        }

        protected virtual void LoadConfiguration(PLCModel plc)
        {
        }

        protected virtual void StartPolling()
        {            
        }

        protected virtual void ProcessResponse(byte[] response, uint startaddress, ushort noOfRegisters)
        {            
        }
        protected virtual void ProcessResponse(ResponsePackage request)
        {
            if (request != null)
            {
                // Process the response based on request.RequestId and request.Parameters
                if(request.RequestId == 1)
                {
                    // Example: Handle response for RequestId 1
                    if (request.Parameters.TryGetValue(1, out object? value))
                    {
                        // Process the value
                    }
                }

            }
        }

        protected void OnRaiseModbusResponseReceived(uint tagId, object value)
        {
            if (RaiseModbusResponseReceivedEvent != null)
            {
                RaiseModbusResponseReceivedEvent(this, tagId, value);
            }
        }

        protected Dictionary<PLCTagModel, object> plcWriteList = new Dictionary<PLCTagModel, object>();
        public void WriteTag(PLCTagModel tag, object value)
        {
            lock (obLockPlcWriteList)
            {
                plcWriteList.Add(tag,value);
            }
        }
    }
}
