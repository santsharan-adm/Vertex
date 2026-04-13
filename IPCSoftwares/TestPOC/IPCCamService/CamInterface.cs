using AttachMetaDataToFile.Common;
using AttachMetaDataToFile.Model;
using CommonLibrary.Models;
using FileWatcherService;
using IPCCamService.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IPCCamService
{
    //public delegate void RaiseErrorDelegate(object sender, ErrorModel error);


    public class CamInterface 
    {
        

        bool ShudownInitiated;
        public event RaiseErrorDelegate? ErrorEvent;
        public event RaiseCamResponseReceivedDelegate? RaiseCamResponseReceivedEvent;
        public CameraModel Camera { get; set; }

        /// <summary>
        /// //////////////////////////////
        private readonly ConcurrentDictionary<string, DateTime> _fileEventTimes = new();
        private readonly IConfiguration _config;
        private FileSystemWatcher _watcher;
        private readonly ConcurrentQueue<string> _fileQueue = new();
        private readonly SemaphoreSlim _signal = new(0);
        private readonly int _maxDegreeOfParallelism = 4;
        //private readonly string _watchFolder = _config["AppSettings:WatchFolder"]; //@"C:\Ekram\FileWatchFolder"; // Folder to watch
        //private readonly string _destinationRootFolder = ConfigurationManager.AppSettings["ProcessedFileFolder"]; //@"C:\Ekram\FileWatchFolder\ProcessedFiles"; // Final destination folder

        private readonly string _watchFolder;
        private readonly string _destinationRootFolder;


        private readonly string _ip;
        private readonly int _port;
        private TcpListener _listener;
        private readonly ConcurrentQueue<ResponsePackage> _TcpIPQueue = new ConcurrentQueue<ResponsePackage>();
        ///

        public CamInterface(IConfiguration config)
        {
            _config = config;

            _watchFolder = _config["AppSettings:WatchFolder"];
            _destinationRootFolder = _config["AppSettings:ProcessedFileFolder"];


            _ip = _config["AppSettings:TCPIp"];
            _port = Convert.ToInt32(_config["AppSettings:Port"]);
            //_TcpIPQueue = queue;
        }

        void RaiseError(ErrorModel error)
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

        public void Start(object? item)
        {
            if (item is CameraModel)
            {
                Camera = (CameraModel)item;

                RaiseError(new ErrorModel(0, Severity.Verbose, "Camera Interface",
                    $"Starting Camera Interface. Camera Name is {Camera.Name}, Camera No is {Camera.Id}, Address is {Camera.IPAddress}, PortNo is {Camera.PortNo}", ""));

                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();
                RaiseError(new ErrorModel(0, Severity.Verbose, "Camera Interface",
                    $"_logger.LogInformation(\"TCP listener started on {{_ip}}:{{_port}}", ""));

                Run();
                Stop();
                

            }

        }


        //TcpClient client;
        //NetworkStream stream;
        public void Run()
        {
            if (Camera != null)
            {
                while (!ShudownInitiated)
                {
                    try
                    {
                        _watcher = new FileSystemWatcher(_watchFolder)
                        {
                            EnableRaisingEvents = true,
                            IncludeSubdirectories = false,
                            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
                        };

                        _watcher.Created += OnCreated;
                        //_watcher.Changed += OnChanged;
                        //_watcher.Renamed += OnRenamed;
                        //_watcher.Deleted += OnDeleted;

                        /*create listner here*/
                        _ = Task.Run(async () =>
                        {
                            TcpClient client = _listener.AcceptTcpClient();
                            RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                $"Accepted connection from {{client.Client.RemoteEndPoint}}", ""));
                            // Fire-and-forget handling of the client; exceptions are observed and logged inside.
                            _ = HandleClientAsync(client);
                        });

                        for (int i = 0; i < _maxDegreeOfParallelism; i++)
                        {
                            _ = Task.Run(() => ProcessQueueAsync());
                        }
                    }
                    catch (Exception ex)
                    {
                        RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                $"Failed to open communication with Camera. Pls refer error details.", ""));

                        string err = ErrorModel.GetErrorExceptionDetail(ex);
                        RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                $"Communication Failed. Error - {err}", ""));

                    }
                    Thread.Sleep(5000);

                }
            }
        }

        public void Stop()
        {
            try
            {
                _listener?.Stop();
                RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                $"TCP listener stopped", ""));
                _watcher?.Dispose();
            }
            catch (Exception ex)
            {
                RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                $"Error while stopping listener", ErrorModel.GetErrorExceptionDetail(ex)));
            }


        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                var stream = client.GetStream();
                var reader = new StreamReader(stream, Encoding.UTF8);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };


                try
                {
                    // Read line-by-line. Each line is one JSON message.
                    while (!ShudownInitiated)
                    {
                        string line =  reader.ReadLine();
                        if (line == null)
                        {
                            // remote closed connection
                            RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                               $"Connection closed by remote: {{client.Client.RemoteEndPoint}}", ""));

                            break;
                        }


                        if (string.IsNullOrWhiteSpace(line))
                            continue;


                        try
                        {
                            var pkg = JsonSerializer.Deserialize<ResponsePackage>(line, options);
                            if (pkg != null)
                            {
                                // write to the channel for downstream processing
                                //pkg.RequestId

                                _TcpIPQueue.Enqueue(pkg);

                                RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                    $"Enqueued package RequestId={{pkg.RequestId}} with {{pkg.Parameters?.Count ?? 0}} parameters", ""));
                            }
                            else
                            {
                                RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                    $"Received JSON that didn't deserialize to ResponsePackage: {{line}}", ""));
                            }
                        }
                        catch (JsonException jex)
                        {
                            RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                   $"Invalid JSON received: {{line}}", ErrorModel.GetErrorExceptionDetail(jex)));
                        }
                    }
                }
                catch (OperationCanceledException) when (ShudownInitiated)
                {
                    // shutting down
                }
                catch (Exception ex)
                {
                    RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                   $"Error handling client {{Client.RemoteEndPoint}}", ErrorModel.GetErrorExceptionDetail(ex)));
                }
            }
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                   $"File created: {{e.FullPath}}", ""));
            EnqueueFileIfNotDuplicate(e.FullPath);
        }

        //private void OnChanged(object sender, FileSystemEventArgs e)
        //{
        //    _logger.LogInformation("File changed: {FilePath}", e.FullPath);
        //    EnqueueFileIfNotDuplicate(e.FullPath);
        //}

        //private void OnRenamed(object sender, RenamedEventArgs e)
        //{
        //    _logger.LogInformation("File renamed from {OldPath} to {NewPath}", e.OldFullPath, e.FullPath);
        //    EnqueueFileIfNotDuplicate(e.FullPath);
        //}

        //private void OnDeleted(object sender, FileSystemEventArgs e)
        //{
        //    _logger.LogWarning("File deleted: {FilePath}", e.FullPath);
        //    // Optional: handle deleted files
        //}

        private void EnqueueFileIfNotDuplicate(string filePath)
        {
            var now = DateTime.UtcNow;

            if (_fileEventTimes.TryGetValue(filePath, out var lastEventTime))
            {
                if ((now - lastEventTime).TotalSeconds < 3)
                {
                    // Skip duplicate event within 3 seconds
                    return;
                }
            }

            _fileEventTimes[filePath] = now;

            _fileQueue.Enqueue(filePath);
            _signal.Release();
        }

        private async Task ProcessQueueAsync()
        {
            while (!ShudownInitiated)
            {
                try
                {
                    _signal.Wait();

                    if (_fileQueue.TryDequeue(out var filePath))
                    {
                        RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                   $"Processing file: {{filePath}}", ""));

                        bool success = await TryProcessFileAsync(filePath);

                        if (!success)
                        {
                            RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                   $"Failed to process file after retries: {{filePath}}", ""));
                            // Optionally, move to an error folder
                        }
                    }
                }
                catch (OperationCanceledException oce)
                {
                    
                    RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                   $"Cancellation requested, stopping processing.", ErrorModel.GetErrorExceptionDetail(oce)));
                    break;
                }
                catch (Exception ex)
                {
                    RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                   $"Unexpected error in processing queue.", ErrorModel.GetErrorExceptionDetail(ex)));
                }
            }
        }

        private async Task<bool> TryProcessFileAsync(string filePath)
        {
            const int maxRetries = 5;
            const int delayMs = 1000;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                if (ShudownInitiated) break;

                if (await WaitForFileReadyAsync(filePath, TimeSpan.FromSeconds(5)))
                {
                    try
                    {
                        //string fileName = Path.GetFileName(filePath);
                        //string dateFolder = DateTime.Now.ToString("yyyy-MM-dd");

                        //string targetFolder = Path.Combine(_destinationRootFolder, dateFolder);
                        //if (!Directory.Exists(targetFolder))
                        //{
                        //    Directory.CreateDirectory(targetFolder);
                        //    _logger.LogInformation("Created directory: {Directory}", targetFolder);
                        //}

                        //string targetFilePath = Path.Combine(targetFolder, fileName);

                        //////If file already exists, version it
                        //////if (File.Exists(targetFilePath))
                        //////{
                        //////    string versionedName = GenerateVersionedFileName(targetFolder, fileName);
                        //////    string backupPath = Path.Combine(targetFolder, versionedName);

                        //////    File.Move(targetFilePath, backupPath); // Rename existing file
                        //////    _logger.LogInformation("Existing file backed up as: {BackupPath}", backupPath);
                        //////}

                        //File.Move(filePath, targetFilePath);
                        //_logger.LogInformation("Moved file to {TargetFilePath}", targetFilePath);


                        //// 1) ATTACH METADATA TO SAME IMAGE  
                        //_logger.LogInformation("Attaching metadata to {FilePath}", filePath);
                        //var writer = new ImageMetadataWriter();

                        //// Build Metadata (you can move this to a separate function later)
                        //Dictionary<string, string> metadata = BuildMetadata();
                        //string MetaDataStyle = ConfigurationManager.AppSettings["MetaDataStyle"];
                        //// Apply metadata on same file (input == output)
                        //writer.AttachMetadataToImage(filePath, filePath, metadata, MetaDataStyle);

                        //=================

                        string fileName = Path.GetFileName(filePath);
                        string dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
                        string fileNameOnly = Path.GetFileNameWithoutExtension(filePath);

                        // Target folder
                        string targetFolder = Path.Combine(_destinationRootFolder, dateFolder);
                        if (!Directory.Exists(targetFolder))
                        {
                            Directory.CreateDirectory(targetFolder);
                            RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                   $"Created directory: {{targetFolder}}", ""));
                        }

                        string targetFilePath = Path.Combine(targetFolder, fileName);

                        // -----------------------------------------
                        // 1) Copy original file to a TEMP location
                        // -----------------------------------------
                        string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + Path.GetExtension(filePath));
                        File.Copy(filePath, tempFile, overwrite: true);

                       
                        RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                   $"Copied original file to temp: {{tempFile}}", ""));

                        // -----------------------------------------
                        // 2) Attach metadata to the TEMP file
                        // -----------------------------------------
                        
                        RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                   $"Attaching metadata to temp file: {{tempFile}}", ""));

                        var writer = new ImageMetadataWriter();
                        //string MetaDataStyle = ConfigurationManager.AppSettings["MetaDataStyle"];
                        string MetaDataStyle = _config["AppSettings:MetaDataStyle"];

                        List<KeyValuePair<string, string>> metadataApple = new List<KeyValuePair<string, string>>();
                        List<KeyValuePair<string, string>> metadataVendor = new List<KeyValuePair<string, string>>();

                        /*Attach Meta Data to Image file*/
                        ResAttachMeta result = new ResAttachMeta();
                        if (MetaDataStyle == "METADATASTYLE001" || MetaDataStyle == "METADATASTYLE002")
                        {
                            metadataApple = BuildMetadata(MetaDataStyle, fileNameOnly);
                            result = writer.AttachMetadataToImage(tempFile, tempFile, metadataApple, MetaDataStyle);
                        }
                        if (MetaDataStyle == "METADATASTYLE003")
                        {
                            metadataApple = BuildMetadata("METADATASTYLE001", fileNameOnly);
                            metadataVendor = BuildMetadata("METADATASTYLE002", fileNameOnly);
                            result = writer.AttachMetadataToImageForStyle3(tempFile, tempFile, metadataApple, metadataVendor, MetaDataStyle);
                        }

                        /*Calculate MD5Hash Code using file data*/
                        Utility.CalculateMD5HashCode(result.output, result.imgPath, result.metadataSize);

                        // -----------------------------------------
                        // 3) Move modified file to destination folder
                        // -----------------------------------------
                        File.Move(tempFile, targetFilePath, overwrite: true);

                       // _logger.LogInformation("Moved modified file to: {TargetFilePath}", targetFilePath);
                        RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                   $"Moved modified file to: {{targetFilePath}}", ""));

                        // -----------------------------------------
                        // 4) ORIGINAL FILE stays unchanged
                        // -----------------------------------------
                        //_logger.LogInformation("Original file untouched at: {OriginalFilePath}", filePath);
                        RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                   $"Original file untouched at: {{filePath}}", ""));

                        return true;
                    }
                    catch (IOException ioEx)
                    {
                        //_logger.LogWarning(ioEx, "IO exception on attempt {Attempt} while moving file: {FilePath}", attempt, filePath);
                        RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                   $"IO exception on attempt {{attempt}} while moving file: {{filePath}}", ""));
                    }
                    catch (Exception ex)
                    {
                        //_logger.LogError(ex, "Unexpected error while processing file: {FilePath}", filePath);
                        RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                   $"Unexpected error while processing file: {{filePath}}", ""));
                        return false;
                    }
                }
                else
                {
                    //_logger.LogWarning("File not ready on attempt {Attempt}: {FilePath}", attempt, filePath);
                    RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                   $"File not ready on attempt {{attempt}}: {{filePath}}", ""));
                }

                Task.Delay(delayMs).Wait();
            }

            return false;
        }

        private string GenerateVersionedFileName(string folder, string originalFileName)
        {
            string nameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
            string ext = Path.GetExtension(originalFileName);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            string versionedName = $"{nameWithoutExt}_{timestamp}{ext}";
            return versionedName;
        }

        private async Task<bool> WaitForFileReadyAsync(string filePath, TimeSpan timeout)
        {
            var startTime = DateTime.UtcNow;

            while (DateTime.UtcNow - startTime < timeout)
            {
                try
                {
                    using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        return true;
                    }
                }
                catch (IOException)
                {
                    await Task.Delay(500);
                }
            }

            return false;
        }

        //public override void Dispose()
        //{
        //    _watcher?.Dispose();
        //    base.Dispose();
        //}


        private List<KeyValuePair<string, string>> BuildMetadata(string Style, string imgFileName)
        {
            string Version = string.Empty;
            string Date = string.Empty;
            string Time = string.Empty;
            string VisionVendor = string.Empty;
            string StationID = string.Empty;
            string StationNickname = string.Empty;
            string DUTSerialNumber = string.Empty;
            string ProcessCommand = string.Empty;
            string CameraNumber = string.Empty;
            string XPixelSizeMM = string.Empty;
            string YPixelSizeMM = string.Empty;
            string CameraGain = string.Empty;
            string CameraExposure = string.Empty;
            string ofLightSettings = string.Empty;
            string LightSetting1 = string.Empty;
            string LightSettingN = string.Empty;
            string DUTColor = string.Empty;
            string ImageNickname = string.Empty;
            string ASCII0x07 = ((char)0x07).ToString();
            string MetaDataStyle = string.Empty;

            var writer = new ImageMetadataWriter();


            /*Pass Meta data values*/
            if (Style == "METADATASTYLE001")
            {
                //NameValueCollection data = (NameValueCollection)ConfigurationManager.GetSection("AppleMetaDataParams");

                var data = _config.GetSection("AppleMetaDataParams");

                Version = "0002";
                Date = DateTime.Now.ToString("yyyy_MM_dd");  //"2025_11_25"
                Time = DateTime.Now.ToString("HH:mm:ss,fff");  //"14:36:37,666";
                VisionVendor = "KEYENCE";
                StationID = "FXZZ-B11-0XXX-XXX0-XXX-XX";
                StationNickname = "AOI";
                //StationNickname = "";
                //DUTSerialNumber = "G6TDG0003O3DY";
                ProcessCommand = "T1,1,23,5,0,7";
                CameraNumber = "1";
                //XPixelSizeMM = "0.0267";
                //YPixelSizeMM = "0.0265";
                CameraGain = "1.0";
                CameraExposure = "0.030";
                ofLightSettings = "2";
                LightSetting1 = "L01_C01_I255";
                LightSettingN = "L01_C02_I010";
                DUTColor = "Black";
                ImageNickname = "T1-1_Insp_1";

                /*Fetch data from TCP IP Queue*/

                if (TryGetDataBySerial(imgFileName, out DUTSerialNumber, out XPixelSizeMM, out YPixelSizeMM))
                {
                   // _logger.LogInformation("Match found! Scan={Scan}, X={X}, Y={Y}", DUTSerialNumber, XPixelSizeMM, YPixelSizeMM);
                    RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                  $"Match found! Scan={{DUTSerialNumber}}, X={{XPixelSizeMM}}, Y={{YPixelSizeMM}}", ""));
                }
                else
                {
                    //_logger.LogWarning("No TCP metadata found for serial {Serial}", DUTSerialNumber);
                    RaiseError(new ErrorModel(0, Severity.Error, "Camera Interface",
                                  $"No TCP metadata found for serial {{DUTSerialNumber}}", ""));
                }



                /*check and Manipulate parameters data*/
                Version = string.IsNullOrEmpty(Version) ? " ," : Version;
                Date = string.IsNullOrEmpty(Date) ? " ," : Date;
                Time = string.IsNullOrEmpty(Time) ? " ," : Time;
                VisionVendor = string.IsNullOrEmpty(VisionVendor) ? " ," : VisionVendor;
                StationID = string.IsNullOrEmpty(StationID) ? " ," : StationID;
                StationNickname = string.IsNullOrEmpty(StationNickname) ? " ," : StationNickname;
                DUTSerialNumber = string.IsNullOrEmpty(DUTSerialNumber) ? " ," : DUTSerialNumber;
                ProcessCommand = string.IsNullOrEmpty(ProcessCommand) ? " ," : ProcessCommand.Replace(',', (char)0x07);   // Replace ASCII 0x2C with ASCII 0x07
                CameraNumber = string.IsNullOrEmpty(Version) ? " ," : Version;
                XPixelSizeMM = string.IsNullOrEmpty(XPixelSizeMM) ? "1.0" : XPixelSizeMM;
                YPixelSizeMM = string.IsNullOrEmpty(YPixelSizeMM) ? "1.0" : YPixelSizeMM;
                CameraGain = string.IsNullOrEmpty(Version) ? " ," : Version;
                CameraExposure = string.IsNullOrEmpty(Version) ? " ," : Version;
                ofLightSettings = string.IsNullOrEmpty(Version) ? " ," : Version;
                LightSetting1 = string.IsNullOrEmpty(Version) ? " ," : Version;
                LightSettingN = string.IsNullOrEmpty(Version) ? " ," : Version;
                DUTColor = string.IsNullOrEmpty(DUTColor) ? "AllColors" : DUTColor;
                ImageNickname = !string.IsNullOrEmpty(ImageNickname) ? Utility.ValidateImageNickname(ImageNickname) : ImageNickname;


                //Dictionary<string, string> metadata = new Dictionary<string, string>();
                List<KeyValuePair<string, string>> metadata = new List<KeyValuePair<string, string>>();

                // Helper function to add only applicable metadata parameters
                void AddIfApplicable(string key, string value, string isApplicable)
                {
                    if (isApplicable == "1")
                    {
                        metadata.Add(new KeyValuePair<string, string>(key, value));
                    }
                    else
                    {
                        metadata.Add(new KeyValuePair<string, string>("", ""));
                    }
                }

                AddIfApplicable("Version", Version, data["Version"]);
                AddIfApplicable("Date", Date, data["Date"]);
                AddIfApplicable("Time", Time, data["Time"]);
                AddIfApplicable("Vision Vendor", VisionVendor, data["VisionVendor"]);
                AddIfApplicable("Station ID", StationID, data["StationID"]);
                AddIfApplicable("Station Nickname", StationNickname, data["StationNickname"]);
                AddIfApplicable("DUT Serial Number", DUTSerialNumber, data["DUTSerialNumber"]);
                AddIfApplicable("Process Command", ProcessCommand, data["ProcessCommand"]);
                AddIfApplicable("Camera Number", CameraNumber, data["CameraNumber"]);
                AddIfApplicable("X Pixel Size (mm)", XPixelSizeMM, data["XPixelSizeMM"]);
                AddIfApplicable("Y Pixel Size (mm)", YPixelSizeMM, data["YPixelSizeMM"]);
                AddIfApplicable("Camera Gain", CameraGain, data["CameraGain"]);
                AddIfApplicable("Camera Exposure (s)", CameraExposure, data["CameraExposure"]);
                AddIfApplicable("# of Light Settings (N)", ofLightSettings, data["#ofLightSettings"]);
                AddIfApplicable("LightSetting 1", LightSetting1, data["LightSetting1"]);
                AddIfApplicable("LightSetting N", LightSettingN, data["LightSettingN"]);
                AddIfApplicable("DUT Color", DUTColor, data["DUTColor"]);
                AddIfApplicable("Image Nickname", ImageNickname, data["ImageNickname"]);

                return metadata;

            }

            if (Style == "METADATASTYLE002")
            {
                //NameValueCollection data = (NameValueCollection)ConfigurationManager.GetSection("VendorMetaDataParams");

                var data = _config.GetSection("VendorMetaDataParams");

                Version = "0002";
                Date = DateTime.Now.ToString("yyyy_MM_dd");  //"2025_11_25"
                Time = DateTime.Now.ToString("HH:mm:ss,fff");  //"14:36:37,666";
                VisionVendor = "KEYENCE Vendor";
                StationID = "FXZZ-B11-0XXX-XXX0-XXX-XX";
                StationNickname = "AOI";
                //StationNickname = "";
                DUTSerialNumber = "G6TDG0003O3DY";
                ProcessCommand = "T1,1,23,5,0,7";
                CameraNumber = "1";
                XPixelSizeMM = "0.0267";
                YPixelSizeMM = "0.0265";
                CameraGain = "1.0";
                CameraExposure = "0.030";
                ofLightSettings = "2";
                LightSetting1 = "L01_C01_I255";
                LightSettingN = "L01_C02_I010";
                DUTColor = "Black";
                ImageNickname = "T1-1_Insp_1";

                /*check and Manipulate parameters data*/
                Version = string.IsNullOrEmpty(Version) ? " ," : Version;
                Date = string.IsNullOrEmpty(Date) ? " ," : Date;
                Time = string.IsNullOrEmpty(Time) ? " ," : Time;
                VisionVendor = string.IsNullOrEmpty(VisionVendor) ? " ," : VisionVendor;
                StationID = string.IsNullOrEmpty(StationID) ? " ," : StationID;
                StationNickname = string.IsNullOrEmpty(StationNickname) ? " ," : StationNickname;
                DUTSerialNumber = string.IsNullOrEmpty(DUTSerialNumber) ? " ," : DUTSerialNumber;
                ProcessCommand = string.IsNullOrEmpty(ProcessCommand) ? " ," : ProcessCommand.Replace(',', (char)0x07);   // Replace ASCII 0x2C with ASCII 0x07
                CameraNumber = string.IsNullOrEmpty(Version) ? " ," : Version;
                XPixelSizeMM = string.IsNullOrEmpty(XPixelSizeMM) ? "1.0" : XPixelSizeMM;
                YPixelSizeMM = string.IsNullOrEmpty(YPixelSizeMM) ? "1.0" : YPixelSizeMM;
                CameraGain = string.IsNullOrEmpty(Version) ? " ," : Version;
                CameraExposure = string.IsNullOrEmpty(Version) ? " ," : Version;
                ofLightSettings = string.IsNullOrEmpty(Version) ? " ," : Version;
                LightSetting1 = string.IsNullOrEmpty(Version) ? " ," : Version;
                LightSettingN = string.IsNullOrEmpty(Version) ? " ," : Version;
                DUTColor = string.IsNullOrEmpty(DUTColor) ? "AllColors" : DUTColor;
                ImageNickname = !string.IsNullOrEmpty(ImageNickname) ? Utility.ValidateImageNickname(ImageNickname) : ImageNickname;

                List<KeyValuePair<string, string>> metadata = new List<KeyValuePair<string, string>>();

                // Helper function to add only applicable metadata parameters
                void AddIfApplicable(string key, string value, string isApplicable)
                {
                    if (isApplicable == "1")
                    {
                        metadata.Add(new KeyValuePair<string, string>(key, value));
                    }
                    else
                    {
                        metadata.Add(new KeyValuePair<string, string>("", ""));
                    }
                }

                AddIfApplicable("Vendor_Version", Version, data["Version"]);
                AddIfApplicable("Vendor_Date", Date, data["Date"]);
                AddIfApplicable("Vendor_Time", Time, data["Time"]);
                AddIfApplicable("Vendor_Vision Vendor", VisionVendor, data["VisionVendor"]);
                AddIfApplicable("Vendor_Station ID", StationID, data["StationID"]);
                AddIfApplicable("Vendor_Station Nickname", StationNickname, data["StationNickname"]);
                AddIfApplicable("Vendor_DUT Serial Number", DUTSerialNumber, data["DUTSerialNumber"]);
                AddIfApplicable("Vendor_Process Command", ProcessCommand, data["ProcessCommand"]);
                AddIfApplicable("Vendor_Camera Number", CameraNumber, data["CameraNumber"]);
                AddIfApplicable("Vendor_X Pixel Size (mm)", XPixelSizeMM, data["XPixelSizeMM"]);
                AddIfApplicable("Vendor_Y Pixel Size (mm)", YPixelSizeMM, data["YPixelSizeMM"]);
                AddIfApplicable("Vendor_Camera Gain", CameraGain, data["CameraGain"]);
                AddIfApplicable("Vendor_Camera Exposure (s)", CameraExposure, data["CameraExposure"]);
                AddIfApplicable("Vendor_# of Light Settings (N)", ofLightSettings, data["#ofLightSettings"]);
                AddIfApplicable("Vendor_LightSetting 1", LightSetting1, data["LightSetting1"]);
                AddIfApplicable("Vendor_LightSetting N", LightSettingN, data["LightSettingN"]);
                AddIfApplicable("Vendor_DUT Color", DUTColor, data["DUTColor"]);
                AddIfApplicable("Vendor_Image Nickname", ImageNickname, data["ImageNickname"]);

                return metadata;
            }

            return null;
        }


        public bool TryGetDataBySerial(string targetSerial,
                                   out string Serialno,
                                   out string xData,
                                   out string yData)
        {
            Serialno = "";
            xData = "";
            yData = "";

            List<ResponsePackage> buffer = new();  // to requeue non-matching items

            bool found = false;

            while (_TcpIPQueue.TryDequeue(out ResponsePackage pkg))
            {
                // Check if this package contains the serial in parameter key=1
                if (pkg.Parameters != null && pkg.RequestId == 3 &&
                    pkg.Parameters.TryGetValue(1, out object serialObj) &&
                    serialObj?.ToString() == targetSerial)
                {
                    // MATCH FOUND — Extract values
                    Serialno = serialObj.ToString();

                    if (pkg.Parameters.TryGetValue(2, out object xVal))
                        xData = Convert.ToString(xVal);

                    if (pkg.Parameters.TryGetValue(3, out object yVal))
                        yData = Convert.ToString(yVal);

                    found = true;
                    break;
                }

                // Not a match → keep it for re-adding to queue
                buffer.Add(pkg);
            }

            // Restore all other dequeued items so queue remains intact
            foreach (var item in buffer)
                _TcpIPQueue.Enqueue(item);

            return found;
        }

    }
}
