using CommonLibrary.Interfaces;
using CommonLibrary.Models;
using IPCCCDService.Events;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace IPCCoreService
{
    public class CoreEngine : BackgroundService
    {
        private readonly ILogger<CoreEngine> _logger;
        IFileHandler _plcTagConfigurationHandler;
        private TcpListener _tcpListener;

        public event RaiseErrorDelegate? ErrorEvent;
        public event RaisePLCResponseReceivedDelegate? RaisePLCResponseReceivedEvent;

        Dictionary<uint, DeviceInterface> dicDeviceInterface = new System.Collections.Generic.Dictionary<uint, DeviceInterface>();
        Dictionary<uint, Thread> dicDeviceThreads = new System.Collections.Generic.Dictionary<uint, Thread>();
        Dictionary<uint, PLCTagModel> dicPLCTags = new System.Collections.Generic.Dictionary<uint, PLCTagModel>();
        PLCs PLCs { get; set; } = new PLCs();
        bool ShudownInitiated;

        public CoreEngine(ILogger<CoreEngine> logger, IFileHandler plcTagConfigurationHandler)
        {
            _logger = logger;
            _plcTagConfigurationHandler = plcTagConfigurationHandler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // _logger.LogInformation("Loading PLC configuration...");
                RaiseError(new ErrorModel(0, Severity.Verbose, "Core Engine", $"Loading Device configuration...", ""));
                LoadConfiguration();
                RaiseError(new ErrorModel(0, Severity.Verbose, "Core Engine", $"Loading Device Tag configuration...", ""));
                LoadPLCTagConfiguration();
                //_logger.LogInformation("Run PLCs...");
                RaiseError(new ErrorModel(0, Severity.Verbose, "Core Engine", $"Running PLCs...", ""));
                RunDeviceInterface();
                //_logger.LogInformation("PLC Polling Service started at: {time}", DateTimeOffset.Now);
                RaiseError(new ErrorModel(0, Severity.Verbose, "Core Engine", "Device Polling Service started", ""));
                //StartListener(stoppingToken);

                //_logger.LogInformation("Device Server Started");
                RaiseError(new ErrorModel(0, Severity.Verbose, "Core Engine", $"Device Server Started...", ""));



                while (!stoppingToken.IsCancellationRequested)
                {
                    ProcessMachineLogic();
                    foreach (KeyValuePair<uint, Thread> plcthread in dicDeviceThreads)
                    {
                        if ((!plcthread.Value.IsAlive) && (!ShudownInitiated))
                        {
                            RaiseError(new ErrorModel(0, Severity.Error, "Core Engine", $"Device Interface Thread for Device-{plcthread.Key} is not alive. Restarting thread.", ""));
                            DeviceInterface DeviceInterface = dicDeviceInterface[(uint)plcthread.Key];
                            Thread thread = new Thread(new ParameterizedThreadStart(DeviceInterface.Start));
                            thread.Start(PLCs.GetPLCByNo(plcthread.Key));
                            dicDeviceThreads[(uint)plcthread.Key] = thread;
                        }
                    }
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                RaiseError(new ErrorModel(0, Severity.Error, "Core Engine", "Exception in Device Main ExecuteAsync.", ErrorModel.GetErrorExceptionDetail(ex)));
            }
            finally
            {
                ShudownInitiated = true;
                foreach (KeyValuePair<uint, DeviceInterface> DeviceInterface in dicDeviceInterface)
                {
                    DeviceInterface.Value.Shutdown();
                }
                // _logger.LogInformation("Device Polling Service is stopping at: {time}", DateTimeOffset.Now);
                RaiseError(new ErrorModel(0, Severity.Verbose, "Core Engine", "Device Polling Service is stopping ", ""));

            }
        }
        
        

        void LoadConfiguration()
        {
            // Load configuration settings 
            
        }


        void RunDeviceInterface()
        {
            for (int i = 0; i < 3; i++)
            {
                DeviceModel model = new DeviceModel()
                {
                    Id = i,
                    Name = (i==0)?"PLC": (i == 1) ? "CCD":"UI",
                    DeviceType = (i == 0) ? DeviceType.PLC: (i == 1) ? DeviceType.CCD:DeviceType.UI,
                    Make = "",
                    Description = "",
                    Remark = ""
                };
                
                DeviceInterface deviceInterface = (i == 0) ? new PLCInterface():(i == 1) ? new CCDInterface(): new UIInterface();
                deviceInterface.ErrorEvent += DeviceInterface_ErrorEvent;
                if (i == 0)
                {
                    deviceInterface.RaisePLCResponseReceivedEvent += DeviceInterface_PLCResponseReceivedEvent;
                }
                else if (i == 1)
                {
                    deviceInterface.RaiseGetCCDDataEvent += DeviceInterface_RaiseGetCCDDataEvent; ;
                }
                else if (i==2)
                {
                    deviceInterface.RaiseGetUIResponseEvent += DeviceInterface_RaiseGetUIResponseEvent;
                    deviceInterface.RaiseGetUITagsResponseEvent += DeviceInterface_RaiseGetUITagsResponseEvent;
                }

                Thread thread = new Thread(new ParameterizedThreadStart(deviceInterface.Start));

                RaiseError(new ErrorModel(0, Severity.Verbose, "Core Engine", $"Starting interface thread for Device-{model.Id}, Name is - {model.Name}.", ""));
                
                thread.Start(model);

                dicDeviceInterface.Add((uint)i, deviceInterface);
                dicDeviceThreads.Add((uint)i, thread);
            }


        }

        private CCDModel DeviceInterface_RaiseGetCCDDataEvent(object sender)
        {
            return new CCDModel()
            {
                ScanCode = "ABC123",
                XData = 12.34f,
                YData = 56.78f
            };
        }

        protected void LoadPLCTagConfiguration()
        {

            Dictionary<int, string[]> items = _plcTagConfigurationHandler.ReadFile();
            foreach (var item in items)
            {
                PLCTagModel tag = new PLCTagModel();
                tag.LoadFromStringArray(item.Value);
                tag.Value = 10;
                PLCModel plc = PLCs.GetPLCByNo(tag.PLCNo);
                if (plc != null)
                {
                    if (tag.PLCNo == plc.PLCNo)
                    {
                        PLCData data = new PLCData(plc.PLCNo, tag.ModbusAddress, 0, tag.AlgoNo);
                       
                        plc.Data.Add(tag, data);
                    }
                }
                dicPLCTags.Add(tag.Id, tag);
            }

        }
        
        void LogError(ErrorModel error)
        {
            if (error.Severity == Severity.Verbose)
            {
                _logger.LogTrace("Core Engine: {0}. {1}. {2}", error.Message, error.Description, error.Remark);
            }
            else if (error.Severity == Severity.Information)
            {
                _logger.LogInformation("Core Engine: {0}. {1}. {2}", error.Message, error.Description, error.Remark);
            }
            else if (error.Severity == Severity.Warning)
            {
                _logger.LogWarning("Core Engine: {0}. {1}. {2}", error.Message, error.Description, error.Remark);
            }
            else if (error.Severity == Severity.Error)
            {
                _logger.LogError("Core Engine: {0}. {1}. {2}", error.Message, error.Description, error.Remark);
            }
        }
        void RaiseError(ErrorModel error)
        {
            LogError(error);
            if (ErrorEvent != null)
            {
                ErrorEvent(this, error);
            }
        }

        private void DeviceInterface_ErrorEvent(object sender, ErrorModel error)
        {
            LogError(error);
            if (ErrorEvent != null)
            {
                ErrorEvent(sender, error);
            }
        }
        private void DeviceInterface_PLCResponseReceivedEvent(object sender, uint tagId, object value)
        {
            PLCTagModel plcTag = dicPLCTags[tagId];
            if (plcTag != null)
            {
                plcTag.Value = value;
            }
        }
        private UIDataModel DeviceInterface_RaiseGetUIResponseEvent(object sender)
        {
            return new UIDataModel()
            {
                AverageCycleTime = TimeSpan.FromSeconds(2),
                Downtime = TimeSpan.FromSeconds(100),
                OperatingTime = TimeSpan.FromSeconds(1000),
                UpTime = TimeSpan.FromSeconds(9000),
                Availability = 0.99,
                Performance = 0.98,
                Quality = 0.87
            };
        }
        private Dictionary<uint, object> DeviceInterface_RaiseGetUITagsResponseEvent(object sender)
        {
            Dictionary<uint, object> items=new Dictionary<uint, object>();
            foreach(var kvp in dicPLCTags)
            { 
                items.Add(kvp.Key, kvp.Value.Value);  
            }
            return items;
        }

        void ProcessMachineLogic()
        {

        }

        #region TCP Listener

        // Pseudocode / Plan (detailed):
        // 1. Accept client connections in a loop until cancellation is requested.
        // 2. For each connected client start a background task to handle the request.
        // 3. Read exactly what the client sends (copy bytesRead into payload).
        // 4. Attempt to deserialize the payload into RequestPackage (JSON). If that fails, fall back to interpret payload as UTF8 text for logging.
        // 5. Build a ResponsePackage that contains the RequestId and current tag values.
        // 6. Serialize the ResponsePackage to UTF8 JSON bytes.
        // 7. Write the serialized bytes back to the client stream and flush.
        // 8. Log success or any errors and ensure the client is closed in a finally block.
        // 9. Honor the cancellation token on reads/writes and on long-running operations.
        void StartListener(CancellationToken stoppingToken)
        {
            // Start TCP server
            _tcpListener = new TcpListener(IPAddress.Any, 6000);
            _tcpListener.Start();
            _logger.LogInformation("TCP Server listening on port 6000");
            _ = Task.Run(() => AcceptTcpClientsAsync(stoppingToken));
        }
        private async Task AcceptTcpClientsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                TcpClient client = null;
                try
                {
                    client = await _tcpListener.AcceptTcpClientAsync(token);
                }
                catch (OperationCanceledException)
                {
                    // cancellation requested - break loop
                    break;
                }
                catch (Exception ex)
                {
                    RaiseError(new ErrorModel(0, Severity.Warning, "Core Engine", "Error accepting TCP client.", ErrorModel.GetErrorExceptionDetail(ex)));
                    continue;
                }

                _logger.LogInformation("TCP client connected.");

                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var stream = client.GetStream();
                        var buffer = new byte[1024];
                        int bytesRead = 0;
                        try
                        {
                            bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogInformation("Read canceled for client.");
                            return;
                        }

                        if (bytesRead <= 0)
                        {
                            _logger.LogInformation("TCP client closed connection without sending data.");
                            return;
                        }

                        // copy exact data
                        var payload = new byte[bytesRead];
                        Array.Copy(buffer, 0, payload, 0, bytesRead);

                        RequestPackage? requestPackage = null;
                        string? requestText = null;
                        try
                        {
                            // Attempt to parse JSON payload directly from bytes
                            requestPackage = JsonSerializer.Deserialize<RequestPackage>(payload);
                        }
                        catch (Exception ex)
                        {
                            // fallback to text for logging and debugging
                            try
                            {
                                requestText = Encoding.UTF8.GetString(payload, 0, bytesRead);
                            }
                            catch { requestText = null; }

                            _logger.LogWarning(ex, "Failed to deserialize incoming TCP payload to RequestPackage. Payload as text: {payload}", requestText);
                        }

                        if (requestPackage != null)
                        {
                            // Successful parse - log minimal info. Replace 'ToString' with specific properties if available.
                            _logger.LogInformation("Received RequestPackage: {package}", JsonSerializer.Serialize(requestPackage));

                            // Build response with current PLC tag values
                            ResponsePackage responsePackage = new ResponsePackage();
                            responsePackage.RequestId = requestPackage.RequestId;
                            responsePackage.Parameters = new Dictionary<uint, object>();
                            if (requestPackage.RequestId == 1) // Read All Tags
                            {
                                foreach (var kvp in dicPLCTags)
                                {
                                    // Ensure we don't add null references unexpectedly
                                    responsePackage.Parameters.Add(kvp.Key, kvp.Value?.Value);
                                }
                            }
                            else if (requestPackage.RequestId == 2)// Write Tag
                            {

                                foreach (var kvp in requestPackage.Parameters)
                                {
                                    PLCTagModel tag = dicPLCTags[kvp.Key];
                                    DeviceInterface DeviceInterface = dicDeviceInterface[tag.PLCNo];
                                    //DeviceInterface.WriteTag(tag, kvp.Value);
                                }
                            }
                            // Serialize response to UTF8 JSON bytes
                            var options = new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                WriteIndented = false
                            };

                            byte[] responseBytes;
                            try
                            {
                                responseBytes = JsonSerializer.SerializeToUtf8Bytes(responsePackage, options);
                            }
                            catch (Exception ex)
                            {
                                RaiseError(new ErrorModel(0, Severity.Error, "Core Engine", "Failed to serialize ResponsePackage.", ErrorModel.GetErrorExceptionDetail(ex)));
                                return;
                            }

                            // Send response back to client
                            try
                            {
                                await stream.WriteAsync(responseBytes, 0, responseBytes.Length, token);
                                await stream.FlushAsync(token);
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
                        else
                        {
                            // If deserialization failed, log raw request text if available
                            if (requestText == null)
                            {
                                try
                                {
                                    requestText = Encoding.UTF8.GetString(payload, 0, bytesRead);
                                }
                                catch { requestText = null; }
                            }
                            RaiseError(new ErrorModel(0, Severity.Warning, "Core Engine", $"Received TCP request (raw): {requestText}", ""));
                        }
                    }
                    catch (Exception ex)
                    {
                        RaiseError(new ErrorModel(0, Severity.Error, "Core Engine", "Unhandled exception while processing TCP client.", ErrorModel.GetErrorExceptionDetail(ex)));
                    }
                    finally
                    {
                        try
                        {
                            client?.Close();
                        }
                        catch { }
                    }
                }, token);
            }
        }
        #endregion TCP Listener
    }
}
