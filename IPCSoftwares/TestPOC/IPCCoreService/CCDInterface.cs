using CommonLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IPCCoreService
{

    
    public class CCDInterface: DeviceInterface
    {

        protected override bool OpenConnection()
        {
            if (base.OpenConnection())
            {
                string ipAddress = "127.0.0.1";
                int port = 6001;
                RaiseError(new ErrorModel(0, Severity.Verbose, "PLC Interface", $"Openning connection to PLC - {Device.Name}, IPAddress - {ipAddress}, Port is {port}.", ""));

                client = new TcpClient(ipAddress, port);
                RaiseError(new ErrorModel(0, Severity.Verbose, "Device Interface",
                    $"Device Interface opened successfully. PLC - {Device.Name}, IPAddress is {ipAddress}, PortNo is {port}", ""));
                return true;
            }
            return false;

        }

        Mutex mutexCCDTrigger = new Mutex(false);
        protected override void StartPolling()
        {
            NetworkStream stream = client.GetStream();
            var writer = new StreamWriter(stream, Encoding.UTF8);
            int  counter = 0;
            if (Device.DeviceType == DeviceType.CCD)
            {
                while (!ShudownInitiated)
                {
                    try
                    {
                        mutexCCDTrigger.WaitOne();
                        // Build response 
                        ResponsePackage responsePackage = new ResponsePackage();
                        CCDModel item = OnRaiseGetCCDData();
                        responsePackage.RequestId = 3; // CCD Data
                        responsePackage.Parameters = new Dictionary<uint, object>();
                        {
                            responsePackage.Parameters[1] = item.ScanCode;// $"CDFERHTYJMK-{counter++}"; // 2D Code
                            responsePackage.Parameters[2] = item.XData;// 32;// X Data
                            responsePackage.Parameters[3] = item.YData;// 53;// Y Data
                        }

                        // Serialize response to UTF8 JSON bytes
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true

                            //PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            //WriteIndented = false
                        };

                        byte[] responseBytes;
                        string responseString;
                        try
                        {
                            responseBytes = JsonSerializer.SerializeToUtf8Bytes(responsePackage, options);
                            responseString = Encoding.UTF8.GetString(responseBytes);

                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Failed to serialize ResponsePackage. {ErrorModel.GetErrorExceptionDetail(ex)}");

                        }

                        // Send response back to client
                        try
                        {
                            //stream.Write(responseBytes, 0, responseBytes.Length);
                            //stream.Flush();
                            writer.WriteLine(responseString);
                            writer.Flush();
                            RaiseError(new ErrorModel(0, Severity.Warning, "Core Engine", $"Sent ResponsePackage (RequestId: {responsePackage.RequestId}) to client. Bytes: {responseBytes.Length}", ""));
                        }
                        catch (OperationCanceledException)
                        {
                            RaiseError(new ErrorModel(0, Severity.Warning, "Core Engine", "Write canceled while sending response to client.", ""));
                        }
                        catch (Exception ex)
                        {
                            RaiseError(new ErrorModel(0, Severity.Warning, "Core Engine", "Failed to send response to TCP client.", ErrorModel.GetErrorExceptionDetail(ex)));
                        }
                       


                    }
                    catch (Exception ex)
                    {                       
                        string err = ErrorModel.GetErrorExceptionDetail(ex);
                        RaiseError(new ErrorModel(0, Severity.Error, "PLC Interface", err, ""));
                    }
                    mutexCCDTrigger.ReleaseMutex();
                    Thread.Sleep(10000);
                }
            }
        }
    }
}
