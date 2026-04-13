using CommonLibrary;
using CommonLibrary.Interfaces;
using CommonLibrary.Models;
using IPCPLCService.Events;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Windows.Markup;

namespace IPCPLCService
{
    public class PLCEngine : BackgroundService
    {
        private readonly ILogger<PLCEngine> _logger;
        IFileHandler _plcConfigurationHandler;
        IFileHandler _plcTagConfigurationHandler;
        private TcpListener _tcpListener;

        public event RaiseErrorDelegate? ErrorEvent;
        public event RaiseModbusResponseReceivedDelegate? RaiseModbusResponseReceivedEvent;

        Dictionary<uint, PLCInterface> dicPLCInterface = new System.Collections.Generic.Dictionary<uint, PLCInterface>();
        Dictionary<uint, Thread> dicPLCThreads = new System.Collections.Generic.Dictionary<uint, Thread>();
        Dictionary<uint, PLCTagModel> dicPLCTags = new System.Collections.Generic.Dictionary<uint, PLCTagModel>();
        PLCs PLCs { get; set; }= new PLCs();
        bool ShudownInitiated;

        public PLCEngine(ILogger<PLCEngine> logger, IFileHandler plcLoader, IFileHandler plcTagConfigurationHandler)
        {
            _logger = logger;
            _plcConfigurationHandler = plcLoader;
            _plcTagConfigurationHandler = plcTagConfigurationHandler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
               // _logger.LogInformation("Loading PLC configuration...");
                RaiseError(new ErrorModel(0, Severity.Verbose, "PLC Engine", $"Loading PLC configuration...", ""));
                LoadConfiguration();
                RaiseError(new ErrorModel(0, Severity.Verbose, "PLC Engine", $"Loading PLC Tag configuration...", ""));
                LoadPLCTagConfiguration();
                //_logger.LogInformation("Run PLCs...");
                RaiseError(new ErrorModel(0, Severity.Verbose, "PLC Engine", $"Running PLCs...", ""));
                RunPLCs();
                //_logger.LogInformation("PLC Polling Service started at: {time}", DateTimeOffset.Now);
                RaiseError(new ErrorModel(0, Severity.Verbose, "PLC Engine", "PLC Polling Service started", ""));
                StartListener(stoppingToken);

                //_logger.LogInformation("PLC Server Started");
                RaiseError(new ErrorModel(0, Severity.Verbose, "PLC Engine", $"PLC Server Started...", ""));



                while (!stoppingToken.IsCancellationRequested)
                {
                    Random val = new Random();
                    foreach (var item in dicPLCTags)
                    {
                        item.Value.Value= val.NextDouble();
                    }
                    foreach (KeyValuePair<uint, Thread> plcthread in dicPLCThreads)
                    {
                        if ((!plcthread.Value.IsAlive) && (!ShudownInitiated))
                        {
                            RaiseError(new ErrorModel(0, Severity.Error, "PLC Engine", $"PLC Interface Thread for PLC-{plcthread.Key} is not alive. Restarting thread.", ""));
                            PLCInterface plcInterface = dicPLCInterface[(uint)plcthread.Key];
                            Thread thread = new Thread(new ParameterizedThreadStart(plcInterface.Start));
                            thread.Start(PLCs.GetPLCByNo(plcthread.Key));
                            dicPLCThreads[(uint)plcthread.Key] = thread;
                        }
                    }
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                RaiseError(new ErrorModel(0, Severity.Error, "PLC Engine", "Exception in PLC Main ExecuteAsync.", ErrorModel.GetErrorExceptionDetail(ex)));
            }
            finally
            {
                ShudownInitiated = true;
                foreach (KeyValuePair<uint, PLCInterface> plcinterface in dicPLCInterface)
                {
                    plcinterface.Value.Shutdown();
                }
               // _logger.LogInformation("PLC Polling Service is stopping at: {time}", DateTimeOffset.Now);
                RaiseError(new ErrorModel(0, Severity.Verbose, "PLC Engine", "PLC Polling Service is stopping ", ""));

            }
        }

        void LoadConfiguration()
        {
            // Load configuration settings 
            Dictionary<int, string[]> items = _plcConfigurationHandler.ReadFile();
            foreach (var item in items)
            {
                try
                {
                    PLCModel plc = new PLCModel();
                    plc.LoadFromStringArray(item.Value);
                    PLCs.Add(plc);
                }
                catch(Exception ex)
                {                    
                    RaiseError(new ErrorModel(0, Severity.Error, "PLC Engine", "Exception in LoadConfiguration while loading PLC configuration.", ErrorModel.GetErrorExceptionDetail(ex)));
                }
            }
        }

        protected  void LoadPLCTagConfiguration()
        {
            
            Dictionary<int, string[]> items = _plcTagConfigurationHandler.ReadFile();
            foreach (var item in items)
            {
                PLCTagModel tag = new PLCTagModel();
                tag.LoadFromStringArray(item.Value);
                PLCModel plc = PLCs.GetPLCByNo(tag.PLCNo);
                if (plc != null)
                {
                    if (tag.PLCNo == plc.PLCNo)
                    {
                        PLCData data = new PLCData(plc.PLCNo, tag.ModbusAddress, 0,tag.AlgoNo);
                        plc.Data.Add(tag, data);
                    }
                }
                dicPLCTags.Add(tag.Id, tag);
            }

        }
        void RunPLCs()
        {
            foreach (PLCModel plc in PLCs)
            {
                if (plc.Protocol == CommunicationProtocol.EthernetModbus)
                {
                    

                    // Use a relative path (no leading slash) so it resolves against the working folder
                    //var configPath = Path.Combine("Database", @"\PLCTags.config");
                    //IFileHandler _plcTagConfigurationHandle = new CsvFileHandler(configPath);
                    PLCInterface plcInterface = new PLCEthernetModbusInterface();
                    plcInterface.ErrorEvent += PLCInterface_ErrorEvent;
                    plcInterface.RaiseModbusResponseReceivedEvent += PLCInterface_ResponseReceivedEvent;  

                    Thread thread = new Thread(new ParameterizedThreadStart(plcInterface.Start));

                    RaiseError(new ErrorModel(0, Severity.Verbose, "PLC Engine", $"Starting interface thread for PLC-{plc.PLCNo}, Name is - {plc.Name}.", ""));

                    thread.Start(plc);

                    dicPLCInterface.Add((uint)plc.PLCNo, plcInterface);
                    dicPLCThreads.Add((uint)plc.PLCNo, thread);
                }
            }

            
        }
        void LogError(ErrorModel error)
        {
            if (error.Severity == Severity.Verbose)
            {
                _logger.LogTrace("PLC Error: {0}. {1}. {2}", error.Message, error.Description, error.Remark);
            }
            else if (error.Severity == Severity.Information)
            {
                _logger.LogInformation("PLC Error: {0}. {1}. {2}", error.Message, error.Description, error.Remark);
            }
            else if (error.Severity == Severity.Warning)
            {
                _logger.LogWarning("PLC Error: {0}. {1}. {2}", error.Message, error.Description, error.Remark);
            }
            else if (error.Severity == Severity.Error)
            {
                _logger.LogError("PLC Error: {0}. {1}. {2}",  error.Message, error.Description,error.Remark );
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

        private void PLCInterface_ErrorEvent(object sender, ErrorModel error)
        {
            LogError(error);
            if (ErrorEvent != null)
            {
                ErrorEvent(sender, error);
            }
        }
        private void PLCInterface_ResponseReceivedEvent(object sender, uint tagId, object value)
        {
            PLCTagModel plcTag = dicPLCTags[tagId];
            PLCData data = (PLCData)value;
            if (plcTag != null)
            {
                
                if (plcTag.AlgoNo == 0)//Linear scale
                {
                    int minValue = 0;
                    UInt32 maxValue = (plcTag.DataLength == 1) ? UInt16.MaxValue : UInt32.MaxValue;
                    double fVal=System.Convert.ToDouble(data.Value);
                    plcTag.Value = plcTag.Offset+ ((fVal - minValue) / (maxValue - minValue))*plcTag.Span;
                }                
                else if (plcTag.AlgoNo == 1)// FP
                {
                    // Example Algorithm: Scale value by a factor of 10
                    //Byte[] bytes = BitConverter.GetBytes(System.Convert.ToInt32(data.Value));
                    //plcTag.Value = ModbusBytesToFloat(bytes, false);
                    plcTag.Value = data.Value;
                }
                else if (plcTag.AlgoNo == 2)// String
                {
                    // Example Algorithm: Scale value by a factor of 10
                    //Byte[] bytes = BitConverter.GetBytes(System.Convert.ToInt32(value));
                    //plcTag.Value = ModbusBytesToString(bytes);
                    plcTag.Value = data.Value;
                }
                else plcTag.Value = "Error";
            }
        }

        public  float ModbusBytesToFloat(byte[] data, bool swapBytes = true)
        {
            if (data == null || data.Length != 4)
                throw new ArgumentException("Data must be exactly 4 bytes.");

            // Modbus often sends data in Big-Endian order, so we may need to swap
            if (swapBytes)
            {
                Array.Reverse(data);
            }

            return BitConverter.ToSingle(data, 0);
        }

        public string ModbusBytesToString(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty.");

            // Convert bytes to string using ASCII encoding (common for Modbus text data)
            return System.Text.Encoding.ASCII.GetString(data);
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
                    RaiseError(new ErrorModel(0, Severity.Warning, "PLC Engine", "Error accepting TCP client.", ErrorModel.GetErrorExceptionDetail(ex)));
                    continue;
                }

                _logger.LogInformation("TCP client connected.");

                _ = Task.Run(async () =>
                {
                    
                    try
                    {
                        using var stream = client.GetStream();
                        while (!ShudownInitiated && !token.IsCancellationRequested)
                        {
                            var buffer = new byte[1024];
                            int bytesRead = 0;
                            try
                            {
                                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                            }
                            catch (OperationCanceledException)
                            {
                                //_logger.LogInformation("Read canceled for client.");
                                RaiseError(new ErrorModel(0, Severity.Warning, "PLC Engine", "Read canceled for client.", ""));
                                return;
                            }

                            if (bytesRead <= 0)
                            {
                                //_logger.LogInformation("TCP client closed connection without sending data.");
                                RaiseError(new ErrorModel(0, Severity.Warning, "PLC Engine", "Client disconnected", ""));
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

                                //_logger.LogWarning(ex, "Failed to deserialize incoming TCP payload to RequestPackage. Payload as text: {payload}", requestText);
                                RaiseError(new ErrorModel(0, Severity.Warning, "PLC Engine", $"Failed to deserialize incoming TCP payload to RequestPackage. Payload as text: {requestText}", ""));

                            }

                            if (requestPackage != null)
                            {
                                // Successful parse - log minimal info. Replace 'ToString' with specific properties if available.
                                //_logger.LogInformation("Received RequestPackage: {package}", JsonSerializer.Serialize(requestPackage));
                                RaiseError(new ErrorModel(0, Severity.Warning, "PLC Engine", $"Received RequestPackage: {JsonSerializer.Serialize(requestPackage)}", ""));

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
                                        PLCInterface plcInterface = dicPLCInterface[tag.PLCNo];
                                        plcInterface.WriteTag(tag, kvp.Value);
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
                                    RaiseError(new ErrorModel(0, Severity.Error, "PLC Engine", "Failed to serialize ResponsePackage.", ErrorModel.GetErrorExceptionDetail(ex)));
                                    return;
                                }

                                // Send response back to client
                                try
                                {
                                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length, token);
                                    await stream.FlushAsync(token);
                                    RaiseError(new ErrorModel(0, Severity.Warning, "PLC Engine", $"Sent ResponsePackage (RequestId: {responsePackage.RequestId}) to client. Bytes: {responseBytes.Length}", ""));
                                }
                                catch (OperationCanceledException)
                                {
                                    RaiseError(new ErrorModel(0, Severity.Warning, "PLC Engine", "Write canceled while sending response to client.", ""));
                                }
                                catch (Exception ex)
                                {
                                    RaiseError(new ErrorModel(0, Severity.Warning, "PLC Engine", "Failed to send response to TCP client.", ErrorModel.GetErrorExceptionDetail(ex)));
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
                                RaiseError(new ErrorModel(0, Severity.Warning, "PLC Engine", $"Received TCP request (raw): {requestText}", ""));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        RaiseError(new ErrorModel(0, Severity.Error, "PLC Engine", "Unhandled exception while processing TCP client.", ErrorModel.GetErrorExceptionDetail(ex)));
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
