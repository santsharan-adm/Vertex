using CommonLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IPCCoreService
{
    public class PLCInterface : DeviceInterface
    {
        protected override bool OpenConnection()
        {
            if (base.OpenConnection())
            {
                string ipAddress = "127.0.0.1";
                int port = 6000;
                RaiseError(new ErrorModel(0, Severity.Verbose, "PLC Interface", $"Openning connection to PLC - {Device.Name}, IPAddress - {ipAddress}, Port is {port}.", ""));

                client = new TcpClient(ipAddress, port);
                RaiseError(new ErrorModel(0, Severity.Verbose, "Device Interface",
                    $"Device Interface opened successfully. PLC - {Device.Name}, IPAddress is {ipAddress}, PortNo is {port}", ""));
                return true;
            }
            return false;

        }

        protected override void StartPolling()
        {
            if (Device.DeviceType == DeviceType.PLC)
            {
                NetworkStream stream = client.GetStream();
                while (!ShudownInitiated)
                {
                    //try
                    //{

                    // Build response 
                    RequestPackage reqPackage = new RequestPackage();
                    reqPackage.RequestId = 1; // Readall tags Data
                    reqPackage.Parameters = new Dictionary<uint, object>();


                    // Serialize response to UTF8 JSON bytes
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    };

                    byte[] requestBytes;
                    try
                    {
                        requestBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(reqPackage));
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to serialize ResponsePackage. {ErrorModel.GetErrorExceptionDetail(ex)}");

                    }

                    // Send request to server
                    try
                    {
                        stream.Write(requestBytes, 0, requestBytes.Length);
                        RaiseError(new ErrorModel(0, Severity.Warning, "PLCInterface", $"Sent RequestPackage (RequestId: {reqPackage.RequestId}) to server. Bytes: {requestBytes.Length}", ""));
                    }
                    catch (OperationCanceledException ex)
                    {
                        throw new Exception("Write canceled while sending response to client.", ex);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Failed to send response to TCP client.", ex);
                    }
                    
                    var readBuffer = new byte[4096];
                    var receiveAccumulator = new System.IO.MemoryStream();
                    int bytesRead = stream.Read(readBuffer, 0, readBuffer.Length);
                    receiveAccumulator.Write(readBuffer, 0, bytesRead);
                    // Try to parse accumulated bytes as JSON
                    try
                    {
                        var accumulatedBytes = receiveAccumulator.ToArray();
                        var resp = JsonSerializer.Deserialize<ResponsePackage>(accumulatedBytes, options);
                        if (resp != null)
                        {
                            // Successfully parsed complete response
                            RaiseError(new ErrorModel(0, Severity.Verbose, "TCP Service",
                                $"Received response (RequestId: {resp.RequestId}). Bytes: {accumulatedBytes.Length}", ""));

                            // TODO: process resp as needed (populate internal state, events, etc.)

                            if (resp != null)
                            {
                                if (resp.RequestId != reqPackage.RequestId)
                                {
                                    RaiseError(new ErrorModel(0, Severity.Warning, "TCP Service",
                                        $"Mismatched RequestId in response. Expected: {reqPackage.RequestId}, Received: {resp.RequestId}", ""));
                                    continue;
                                }
                                // Example processing: log parameters
                                foreach (var param in resp.Parameters)
                                {


                                    RaiseError(new ErrorModel(0, Severity.Verbose, "TCP Service",
                                        $"Parameter ID: {param.Key}, Value: {param.Value}", ""));
                                    OnRaisePLCResponseReceived(param.Key, param.Value);

                                }
                            }

                            // Clear accumulator for next message
                            receiveAccumulator.SetLength(0);
                        }
                        // If resp is null, treat as incomplete and continue reading
                    }
                    catch (JsonException)
                    {
                        // Incomplete JSON received; continue reading more data
                        // Do not clear accumulator
                    }

                    //}

                    //catch (Exception ex)
                    //{
                    //    Console.WriteLine("Error: " + ex.Message);
                    //    string err = ErrorModel.GetErrorExceptionDetail(ex);
                    //    RaiseError(new ErrorModel(0, Severity.Error, "PLC Interface", err, ""));
                    //}

                    Thread.Sleep(1000);
                }
            }
        }
    }
}
