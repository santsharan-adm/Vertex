using CommonLibrary.Interfaces;
using CommonLibrary.Models;
using IPCCamService.Events;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace IPCCamService
{
    public class CamEngine : BackgroundService
    {
        private readonly IConfiguration _config;
        IFileHandler _camConfigurationHandler;
        private readonly ILogger<CamEngine> _logger;
        private TcpListener _tcpListener;

        public event RaiseErrorDelegate? ErrorEvent;
        public event RaiseCamResponseReceivedDelegate? RaiseCamResponseReceivedEvent;

        Dictionary<uint, CamInterface> dicCamInterface = new System.Collections.Generic.Dictionary<uint, CamInterface>();
        Dictionary<uint, Thread> dicCamThreads = new System.Collections.Generic.Dictionary<uint, Thread>();
        Cameras Cameras { get; set; }
        bool ShudownInitiated;

        public CamEngine(ILogger<CamEngine> logger, IConfiguration config, IFileHandler camLoader)
        {
            _logger = logger;
            _config = config;
            _camConfigurationHandler = camLoader;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Loading configuration...");
                LoadConfiguration();
                _logger.LogInformation("Run Cameras...");
                RunCameras();
                _logger.LogInformation("Camera Polling Service started at: {time}", DateTimeOffset.Now);
                StartListener(stoppingToken);

                _logger.LogInformation("Camera Server Started");



                while (!stoppingToken.IsCancellationRequested)
                {
                    foreach (KeyValuePair<uint, Thread> camThread in dicCamThreads)
                    {
                        if ((!camThread.Value.IsAlive) && (!ShudownInitiated))
                        {
                            RaiseError(new ErrorModel(0, Severity.Error, "Cam Engine", $"CAm Interface Thread for PLC-{camThread.Key} is not alive. Restarting thread.", ""));
                            CamInterface camInterface = dicCamInterface[(uint)camThread.Key];
                            Thread thread = new Thread(new ParameterizedThreadStart(camInterface.Start));
                            thread.Start(Cameras.GetCameraById(camThread.Key));
                            dicCamThreads[(uint)camThread.Key] = thread;
                        }
                    }
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                RaiseError(new ErrorModel(0, Severity.Error, "Cam Engine", "Exception in Cam Main ExecuteAsync.", ErrorModel.GetErrorExceptionDetail(ex)));
            }
            finally
            {
                ShudownInitiated = true;
                foreach (KeyValuePair<uint, CamInterface> plcinterface in dicCamInterface)
                {
                    plcinterface.Value.Shutdown();
                }
                _logger.LogInformation("Cam Polling Service is stopping at: {time}", DateTimeOffset.Now);
            }
        }

        void LoadConfiguration()
        {
            // Load configuration settings 
            Dictionary<int, string[]> items = _camConfigurationHandler.ReadFile();
            foreach (var item in items)
            {
                try
                {
                    CameraModel cam = new CameraModel();
                    cam.LoadFromStringArray(item.Value);
                    Cameras.Add(cam);
                }
                catch (Exception ex)
                {
                    RaiseError(new ErrorModel(0, Severity.Error, "PLC Engine", "Exception in LoadConfiguration while loading PLC configuration.", ErrorModel.GetErrorExceptionDetail(ex)));
                }
            }
        }
        void RunCameras()
        {
            foreach (CameraModel cam in Cameras)
            {
                CamInterface CamInterface = new CamInterface(_config);
                CamInterface.ErrorEvent += CamInterface_ErrorEvent;
                CamInterface.RaiseCamResponseReceivedEvent += CamInterface_RaiseCamResponseReceivedEvent;
                Thread thread = new Thread(new ParameterizedThreadStart(CamInterface.Start));

                RaiseError(new ErrorModel(0, Severity.Verbose, "PLC Engine", $"Starting interface thread for Camera -{cam.Id}, Name is - {cam.Name}.", ""));

                thread.Start(cam);

                dicCamInterface.Add((uint)cam.Id, CamInterface);
                dicCamThreads.Add((uint)cam.Id, thread);
            }


        }

        void RaiseError(ErrorModel error)
        {
            if (ErrorEvent != null)
            {
                ErrorEvent(this, error);
            }
        }

        private void CamInterface_ErrorEvent(object sender, ErrorModel error)
        {
            if (ErrorEvent != null)
            {
                ErrorEvent(sender, error);
            }
        }
        private void CamInterface_RaiseCamResponseReceivedEvent(object sender, uint unitno, uint modbusRegister, ushort value)
        {
            if (RaiseCamResponseReceivedEvent != null)
            {
                RaiseCamResponseReceivedEvent(sender, unitno, modbusRegister, value);
            }
        }

        private Task<string> PollCamDataAsync()
        {
            // Replace with actual PLC SDK logic (e.g., Keyence API)
            return Task.FromResult($"DummyData-{DateTime.Now}");
        }
        #region TCP Listener
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
                var client = await _tcpListener.AcceptTcpClientAsync();
                _logger.LogInformation("TCP client connected.");

                _ = Task.Run(async () =>
                {
                    using var stream = client.GetStream();
                    var buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                    var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    _logger.LogInformation($"Received TCP request: {request}");

                    //var response = PlcDataService.GetLatestPlcData();
                    //var responseBytes = Encoding.UTF8.GetBytes(response);
                    //await stream.WriteAsync(responseBytes, 0, responseBytes.Length, token);

                    client.Close();
                });
            }
        }
        #endregion TCP Listener
    }
}
