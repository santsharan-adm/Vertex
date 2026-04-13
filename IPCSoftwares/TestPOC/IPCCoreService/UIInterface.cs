using CommonLibrary.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IPCCoreService
{
    internal class UIInterface : DeviceInterface
    {
        TcpListener listener;
        protected override bool OpenConnection()
        {
            if (base.OpenConnection())
            {
                int port = 6002;
                RaiseError(new ErrorModel(0, Severity.Verbose, "UI Interface", $"Openning connection to UI - {Device.Name}, Port is {port}.", ""));

                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                RaiseError(new ErrorModel(0, Severity.Verbose, "UI Interface", $"UI listener started on port {port}", ""));

                return true;
            }
            return false;

        }

        protected override void StartPolling()
        {
            while (!ShudownInitiated)
            {
                client = listener.AcceptTcpClient();
               // ThreadPool.QueueUserWorkItem(HandleClient, client);
                Task.Run(() => HandleClientAsync(client));
            }

        }


        async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                var readBuffer = new byte[4096];
                var receiveAccumulator = new MemoryStream();

                try
                {
                    while (!ShudownInitiated)
                    {
                        int bytesRead = await stream.ReadAsync(readBuffer, 0, readBuffer.Length);
                        if (bytesRead == 0)
                        {
                            // Client disconnected
                            RaiseError(new ErrorModel(0, Severity.Warning, "UI Interface", "Client disconnected", ""));
                            break;
                        }

                        // Append received data
                        receiveAccumulator.Write(readBuffer, 0, bytesRead);

                        // Try to parse JSON from accumulated data
                        try
                        {
                            var accumulatedBytes = receiveAccumulator.ToArray();
                            var reqPackage = JsonSerializer.Deserialize<RequestPackage>(accumulatedBytes);

                            if (reqPackage != null)
                            {
                                RaiseError(new ErrorModel(0, Severity.Verbose, "UI Interface",
                                    $"Received request (RequestId: {reqPackage.RequestId}). Bytes: {accumulatedBytes.Length}", ""));

                                // Prepare response
                                var responsePackage = new ResponsePackage { RequestId = reqPackage.RequestId,
                                Parameters=new Dictionary<uint, object>()};

                                if (reqPackage.RequestId == 1)
                                {
                                    var items = OnRaiseGetUITagsResponseData();
                                    foreach (var kvp in items)
                                        responsePackage.Parameters.Add(kvp.Key, kvp.Value);
                                }
                                else if (reqPackage.RequestId == 4)
                                {
                                    var item = OnRaiseGetUIResponseData();
                                    responsePackage.Parameters.Add(1, item.OperatingTime);
                                    responsePackage.Parameters.Add(2, item.Downtime);
                                    responsePackage.Parameters.Add(3, item.UpTime);
                                    responsePackage.Parameters.Add(4, item.AverageCycleTime);
                                    responsePackage.Parameters.Add(5, item.Availability);
                                    responsePackage.Parameters.Add(6, item.Performance);
                                    responsePackage.Parameters.Add(7, item.Quality);
                                }
                                else
                                {
                                    throw new Exception("Invalid request from client");
                                }

                                // Send response
                                var responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(responsePackage));
                                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);

                                RaiseError(new ErrorModel(0, Severity.Verbose, "UI Interface",
                                    $"Sent ResponsePackage (ResponseId: {responsePackage.RequestId}) to client. Bytes: {responseBytes.Length}", ""));

                                // Clear accumulator for next message
                                receiveAccumulator.SetLength(0);
                            }
                        }
                        catch (JsonException)
                        {
                            // Incomplete JSON, keep accumulating
                        }
                    }
                }
                catch (Exception ex)
                {
                    RaiseError(new ErrorModel(0, Severity.Error, "PLC Interface",
                        $"Error processing incoming request: {ErrorModel.GetErrorExceptionDetail(ex)}", ""));
                }
                finally
                {
                    Console.WriteLine("Client disconnected.");
                }
            }

        }
        void HandleClient2(object obj)
        {
            TcpClient client2 = (TcpClient)obj;
            NetworkStream stream = client2.GetStream();

            try
            {
                while (!ShudownInitiated)
                {
                    int i = 0;
                    while (!stream.DataAvailable)
                    {
                        Task.Delay(100).Wait();
                        i++;
                        if (i > 5)
                        {
                            //throw new Exception("Timeout waiting for data");
                            RaiseError(new ErrorModel(0, Severity.Warning, "UI Interface", $"Timeout waiting for data", ""));
                            break;
                        }
                    }
                    if (i > 3) continue;
                    var readBuffer = new byte[4096];
                    var receiveAccumulator = new System.IO.MemoryStream();
                    while (stream.DataAvailable && !ShudownInitiated)
                    {
                        int bytesRead = stream.Read(readBuffer, 0, readBuffer.Length);
                        receiveAccumulator.Write(readBuffer, 0, bytesRead);
                        Task.Delay(100).Wait();
                    }

                    // Try to parse accumulated bytes as JSON
                    try
                    {
                        var accumulatedBytes = receiveAccumulator.ToArray();
                        RequestPackage reqPackage = JsonSerializer.Deserialize<RequestPackage>(accumulatedBytes);

                        if (reqPackage != null)
                        {
                            // Successfully parsed complete response
                            RaiseError(new ErrorModel(0, Severity.Verbose, "UI Interface",
                                $"Received request (RequestId: {reqPackage.RequestId}). Bytes: {accumulatedBytes.Length}", ""));

                            // TODO: process reqPackage as needed (populate internal state, events, etc.)

                            ResponsePackage responsePackage = new ResponsePackage();
                            responsePackage.RequestId = reqPackage.RequestId;
                            if (reqPackage.RequestId == 1)
                            {
                                Dictionary<uint, object> items = OnRaiseGetUITagsResponseData();
                                foreach (var kvp in items)
                                {
                                    responsePackage.Parameters.Add(kvp.Key, kvp.Value);
                                }
                            }
                            else if (reqPackage.RequestId == 4)
                            {
                                UIDataModel item = OnRaiseGetUIResponseData();
                                responsePackage.Parameters.Add(1, item.OperatingTime);//Operating Time
                                responsePackage.Parameters.Add(2, item.Downtime);//Down Time
                                responsePackage.Parameters.Add(3, item.UpTime);//UpTime Time
                                responsePackage.Parameters.Add(4, item.AverageCycleTime);//AverageCycleTime
                                responsePackage.Parameters.Add(5, item.Availability);//Availability
                                responsePackage.Parameters.Add(6, item.Performance);//Performance
                                responsePackage.Parameters.Add(7, item.Quality);//Quality
                            }
                            else
                            {
                                throw new Exception("Invalid request from client");
                            }

                            byte[] responseBytes;
                            try
                            {
                                responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(responsePackage));
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Failed to serialize ResponsePackage. {ErrorModel.GetErrorExceptionDetail(ex)}");

                            }
                            // Send response back to clent
                            try
                            {
                                stream.Write(responseBytes, 0, responseBytes.Length);
                                RaiseError(new ErrorModel(0, Severity.Warning, "UI Interface", $"Sent ResponsePackage (ResponseId: {responsePackage.RequestId}) to server. Bytes: {responseBytes.Length}", ""));
                            }
                            catch (OperationCanceledException ex)
                            {
                                throw new Exception("Write canceled while sending response to client.", ex);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("Failed to send response to TCP client.", ex);
                            }



                        }
                        // If resp is null, treat as incomplete and continue reading

                        // Clear accumulator for next message
                        receiveAccumulator.SetLength(0);
                    }
                    catch (JsonException)
                    {
                        // Incomplete JSON received; continue reading more data
                        // Do not clear accumulator
                    }


                }
            }
            catch (Exception ex)
            {
                RaiseError(new ErrorModel(0, Severity.Error, "PLC Interface", $"Error processing incomming request. Error is {ErrorModel.GetErrorExceptionDetail(ex)}", ""));
            }

            finally
            {
                stream.Close();
                client2.Close();
                Console.WriteLine("Client disconnected.");
            }

        }


        void HandleClient3(object obj)
        {
            TcpClient client2 = (TcpClient)obj;

            try
            {
                using (NetworkStream stream = client2.GetStream())
                {
                    var readBuffer = new byte[4096];
                    var receiveAccumulator = new MemoryStream();

                    while (!ShudownInitiated)
                    {
                        // Read available data
                        while (stream.DataAvailable && !ShudownInitiated)
                        {
                            int bytesRead = stream.Read(readBuffer, 0, readBuffer.Length);
                            if (bytesRead == 0)
                            {
                                // Client disconnected
                                RaiseError(new ErrorModel(0, Severity.Warning, "UI Interface", "Client disconnected", ""));
                                return;
                            }
                            receiveAccumulator.Write(readBuffer, 0, bytesRead);
                        }

                        // Try to parse accumulated bytes as JSON
                        try
                        {
                            var accumulatedBytes = receiveAccumulator.ToArray();
                            RequestPackage reqPackage = JsonSerializer.Deserialize<RequestPackage>(accumulatedBytes);

                            if (reqPackage != null)
                            {
                                RaiseError(new ErrorModel(0, Severity.Verbose, "UI Interface",
                                    $"Received request (RequestId: {reqPackage.RequestId}). Bytes: {accumulatedBytes.Length}", ""));

                                // Prepare response
                                ResponsePackage responsePackage = new ResponsePackage { RequestId = reqPackage.RequestId };

                                if (reqPackage.RequestId == 1)
                                {
                                    var items = OnRaiseGetUITagsResponseData();
                                    foreach (var kvp in items)
                                        responsePackage.Parameters.Add(kvp.Key, kvp.Value);
                                }
                                else if (reqPackage.RequestId == 4)
                                {
                                    var item = OnRaiseGetUIResponseData();
                                    responsePackage.Parameters.Add(1, item.OperatingTime);
                                    responsePackage.Parameters.Add(2, item.Downtime);
                                    responsePackage.Parameters.Add(3, item.UpTime);
                                    responsePackage.Parameters.Add(4, item.AverageCycleTime);
                                    responsePackage.Parameters.Add(5, item.Availability);
                                    responsePackage.Parameters.Add(6, item.Performance);
                                    responsePackage.Parameters.Add(7, item.Quality);
                                }
                                else
                                {
                                    throw new Exception("Invalid request from client");
                                }

                                // Send response
                                var responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(responsePackage));
                                stream.Write(responseBytes, 0, responseBytes.Length);
                                RaiseError(new ErrorModel(0, Severity.Verbose, "UI Interface",
                                    $"Sent ResponsePackage (ResponseId: {responsePackage.RequestId}) to client. Bytes: {responseBytes.Length}", ""));
                            }

                            // Clear accumulator for next message
                            receiveAccumulator.SetLength(0);
                        }
                        catch (JsonException)
                        {
                            // Incomplete JSON, keep accumulating
                        }
                    }
                }
            }
            catch (IOException ioEx)
            {
                RaiseError(new ErrorModel(0, Severity.Error, "UI Interface", $"Network error: {ErrorModel.GetErrorExceptionDetail(ioEx)}", ""));
            }
            catch (Exception ex)
            {
                RaiseError(new ErrorModel(0, Severity.Error, "UI Interface", $"Error processing incoming request: {ErrorModel.GetErrorExceptionDetail(ex)}", ""));
            }
            finally
            {
                client2.Close();
                RaiseError(new ErrorModel(0, Severity.Error, "UI Interface", $"Client disconnected.", ""));
            }

        }
    }
}
