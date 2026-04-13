using CommonLibrary.Models;
using IPCCCDService.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IPCCoreService
{
    public class DeviceInterface
    {
        protected bool ShudownInitiated;
        public event RaiseErrorDelegate? ErrorEvent;
        public event RaisePLCResponseReceivedDelegate? RaisePLCResponseReceivedEvent;
        public event RaiseGetUIResponseDelegate? RaiseGetUIResponseEvent;
        public event RaiseGetUITagsResponseDelegate? RaiseGetUITagsResponseEvent;
        public event RaiseGetCCDDataDelegate? RaiseGetCCDDataEvent;

        public DeviceModel Device { get; set; }

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
            if (item is DeviceModel)
            {
                Device = (DeviceModel)item;

                RaiseError(new ErrorModel(0, Severity.Verbose, "Device Interface",
                    $"Starting Device Interface. Device Name is {Device.Name}, Device Id is {Device.Id}, Name is {Device.Name}", ""));
                OnStart(item);
                LoadConfiguration(Device);
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
            if (Device != null)
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
                        RaiseError(new ErrorModel(0, Severity.Error, "Device Interface",
                                $"Failed to open communication with Device. Device - {Device.Name}. Pls refer error details.", ""));

                        string err = ErrorModel.GetErrorExceptionDetail(ex);
                        RaiseError(new ErrorModel(0, Severity.Error, "Device Interface",
                                $"Communication Failed. Device - {Device.Name} Error - {err}", ""));

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
            return true;
        }

        protected virtual void CloseConnection()
        {
            RaiseError(new ErrorModel(0, Severity.Verbose, "Device Interface",
                $"Clossing Device Interface. Device - {Device.Name}", ""));
            if (client != null)
            {
                client.Close();
            }
            RaiseError(new ErrorModel(0, Severity.Verbose, "Device Interface",
                $"Clossing Device Interface. Device - {Device.Name}", ""));
        }

        protected virtual void LoadConfiguration(DeviceModel device)
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
                if (request.RequestId == 1)
                {
                    // Example: Handle response for RequestId 1
                    if (request.Parameters.TryGetValue(1, out object? value))
                    {
                        // Process the value
                    }
                }

            }
        }

        protected void OnRaisePLCResponseReceived(uint tagId, object value)
        {
            if (RaisePLCResponseReceivedEvent != null)
            {
                RaisePLCResponseReceivedEvent(this, tagId, value);
            }
        }

        protected UIDataModel OnRaiseGetUIResponseData()
        {
            if (RaiseGetUIResponseEvent != null)
            {
                return RaiseGetUIResponseEvent(this);
            }
            return null;
        }
        protected Dictionary<uint,object> OnRaiseGetUITagsResponseData()
        {
            if (RaiseGetUITagsResponseEvent != null)
            {
                return RaiseGetUITagsResponseEvent(this);
            }
            return null;
        }

        protected CCDModel OnRaiseGetCCDData()
        {
            if (RaiseGetCCDDataEvent != null)
            {
                return RaiseGetCCDDataEvent(this);
            }
            return null;
        }


    }
}
