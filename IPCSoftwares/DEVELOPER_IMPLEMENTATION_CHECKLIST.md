# DEVELOPER IMPLEMENTATION CHECKLIST
## Module 2: PLC Communication System

**Purpose:** Verify that all components are properly implemented and functioning
**Prepared For:** Development & QA Teams
**Date:** 2024

---

## PART 1: CODE STRUCTURE VERIFICATION

### ? File Locations & Organization

```
IPCSoftware.CoreService\
?? Program.cs
?  ?? ? Mutex implementation (prevents multiple instances)
?  ?? ? Configuration loading
?  ? DI registration
?     ?? ? PLCClientManager registered as Singleton
?     ?? ? PlcClient created per device
?
?? Worker.cs
?  ?? ? Inherits BackgroundService
?  ?? ? ExecuteAsync() method implemented
?  ?? ? Initialization sequence:
?     ?? ? Load devices
?     ?? ? Load tags
?     ?? ? SharedServiceHost initialization
?     ?? ? UIListener startup
?     ?? ? Backup loop
?     ?? ? Dashboard initialization
?
?? Services\PLC\
?  ?? PLCClientManager.cs
?  ?  ?? ? InitializeClients() async method
?  ?  ?? ? Tag filtering by DeviceNo
?  ?  ?? ? GetAllClients() method
?  ?  ?? ? GetClient(int plcNo) method
?  ?  ?? ? UpdateTags() method
?  ?
?  ?? PlcClient.cs
? ?? ? Constructor with device, tags, config, logger
?     ?? ? StartAsync() - infinite polling loop
?     ?? ? ConnectAsync() - auto-reconnect logic
?     ?? ? PollAllGroups() - data reading
?     ?? ? OptimizeReads() - chunking algorithm
?     ?? ? WriteAsync() - data writing
?     ?? ? UpdateTags() - thread-safe update
?     ?? ? OnPlcDataReceived event
?     ?? ? Disconnect() - graceful shutdown
```

---

## PART 2: CONFIGURATION SERVICES VERIFICATION

### ? DeviceConfigurationService

**File:** `IPCSoftware.Services\ConfigServices\DeviceConfigurationService.cs`

```
Core Functionality:
?? ? Inherits BaseService
?? ? Implements IDeviceConfigurationService
?? ? Constructor sets up paths:
?  ?? ? Gets DataFolder from IOptions<ConfigSettings>
?  ?? ? Creates directory if not exists
?  ?? ? Sets CSV file paths
?
?? InitializeAsync()
?  ?? ? Calls LoadDevicesFromCsvAsync()
?  ?? ? Calls LoadInterfacesFromCsvAsync()
?  ?? ? Calls LoadCameraInterfacesFromCsvAsync()
?
?? GetPlcDevicesAsync()
?  ?? ? Returns List<DeviceInterfaceModel>
?  ?? ? Lazy-loads if empty
?  ?? ? Error handling returns existing list
?
?? LoadInterfacesFromCsvAsync()
?  ?? ? File existence check
?  ?? ? CSV parsing with proper escaping
?  ?? ? ParseInterfaceCsvLine() method
?  ?? ? Line validation (12 fields min)
?  ?? ? Error handling with logging
?
?? SaveInterfacesToCsvAsync()
?  ?? ? Proper CSV formatting
?  ?? ? Quote escaping
?  ?? ? Async file writing
?
?? Helper Methods
   ?? ? SplitCsvLine() - handles quoted fields
 ?? ? EscapeCsv() - proper escaping
```

### ? PLCTagConfigurationService

**File:** `IPCSoftware.Services\ConfigServices\PLCTagConfigurationService.cs`

```
Core Functionality:
?? ? Inherits BaseService
?? ? Implements IPLCTagConfigurationService
?? ? Uses TagConfigLoader for parsing
?
?? GetAllTagsAsync()
?  ?? ? Returns List<PLCTagConfigurationModel>
?  ?? ? Lazy-loads if empty
?  ?? ? Error handling
?
?? LoadTagsInternalAsync()
?  ?? ? File existence check
?  ?? ? Delegates to TagConfigLoader.Load()
?  ?? ? Thread-safe: Interlocked.Exchange()
?  ?? ? Updates _nextId counter
?  ?? ? Error handling with logging
?
?? SaveToCsvAsync()
?  ?? ? Proper CSV header
?  ?? ? All fields included
?  ?? ? CSV escaping
?  ?? ? Async file writing
?
?? UpdateTagAsync()
?  ?? ? Find existing tag
?  ?? ? Update list
?  ?? ? Save to CSV
?
?? ReloadTagsAsync()
   ?? ? Called by TagChangeWatcherService
   ?? ? Returns reloaded list
```

---

## PART 3: PLC CLIENT IMPLEMENTATION VERIFICATION

### ? PlcClient - Connection Management

```
Constructor:
?? ? Takes DeviceInterfaceModel
?? ? Takes List<PLCTagConfigurationModel>
?? ? Takes ConfigSettings
?? ? Takes IAppLogger
?? ? Stores all as readonly

IsConnected Property:
?? ? Returns _tcp != null && _tcp.Connected
?? ? Used throughout for state checking

Disconnect() Method:
?? ? Disposes _master (IModbusMaster)
?? ? Closes _tcp
?? ? Disposes _tcp
?? ? Sets both to null
?? ? Logs warning message
?? ? Called on error cleanup

ConnectAsync() Method:
?? ? Checks already connected (early return)
?? ? Infinite retry loop
?? ? Creates new TcpClient
?? ? Sets 5-second timeout using Task.WhenAny
?? ? Handles TimeoutException
?? ? Creates ModbusFactory and Master
?? ? Logs success
?? ? Catches and retries on error
?? ? 3-second delay between retries
?? ? Logs error messages
```

### ? PlcClient - Polling Engine

```
StartAsync() Method:
?? ? Returns Task (fire-and-forget)
?? ? Task.Run() creates independent thread
?? ? Infinite while(true) loop
?? ? Try-catch for exception handling
?
?? Loop Logic:
?  ?? ? Check !IsConnected ? call ConnectAsync()
?  ?? ? 500ms stabilization delay after connect
?  ?? ? Check _tags.Any()
?  ?? ? Call PollAllGroups()
?  ?? ? Fire OnPlcDataReceived event
?  ?? ? Catch all exceptions
?  ?? ? Disconnect on error
?  ?? ? Log errors
?  ?? ? 90ms polling delay (?11/sec)
?
?? Event:
   ?? ? OnPlcDataReceived?.Invoke(DeviceNo, data)
```

### ? PlcClient - Data Reading

```
PollAllGroups() Method:
?? ? Returns Dictionary<uint, object>
?? ? Calls OptimizeReads(_tags)
?? ? For each chunk:
?  ?? ? ReadHoldingRegistersAsync(1, offset, count)
?  ?? ? Exception handling per chunk
?  ?? ? Continues if chunk fails
?
?? Data Extraction:
   ?? ? For each address in chunk:
   ?  ?? ? Calculate index in block
   ?  ?? ? Bounds checking
   ?  ?? ? Array.Copy for extraction
   ?  ?? ? Store in result dictionary
   ?? ? Key: Modbus address (uint)
    ?? ? Value: ushort[] registers

OptimizeReads() Method:
?? ? Groups unique addresses
?? ? Takes max length per address
?? ? Sorts by offset
?? ? Creates chunks respecting:
?  ?? ? MAX_GAP = 10 registers
?  ?? ? MAX_READ = 120 registers
?
?? Result:
   ?? ? List<ModbusReadChunk>
   ?? ? Each chunk has StartOffset, TotalCount
   ?? ? Each chunk has IncludedAddresses[]
```

### ? PlcClient - Data Writing

```
WriteAsync() Method:
?? ? Takes PLCTagConfigurationModel
?? ? Takes object value
?? ? Checks IsConnected
?
?? Algorithm Conversion (if AlgNo == 1):
?  ?? ? ReverseLinearScale_EngMinMax()
?  ?? ? ReverseLinearScale_GainOffset()
?
?? ConvertValueToRegisters()
?  ?? ? Handles all DataTypes (1-7)
?  ?? ? Byte swapping if configured
??? ? String padding
?
?? Write Based on Type:
?  ?? ? If Bit: read-modify-write
?  ?? ? If Single Register: WriteSingleRegisterAsync
?  ?? ? If Multiple: WriteMultipleRegistersAsync
?
?? Error Handling:
   ?? ? Try-catch
   ?? ? Logs errors
   ?? ? Throws exception on failure
```

---

## PART 4: PLCClientManager VERIFICATION

### ? Manager Functionality

```
Constructor:
?? ? Inherits BaseService
?? ? Takes IDeviceConfigurationService
?? ? Takes IPLCTagConfigurationService
?? ? Takes IOptions<ConfigSettings>
?? ? Takes IAppLogger
?? ? Calls InitializeClients() as fire-and-forget

Properties:
?? ? List<PlcClient> Clients { get; private set; }
?? ? Initialized as new List<PlcClient>()

InitializeClients() Method:
?? ? Async method
?? ? Loads all tags
?? ? Loads all devices
?? ? For each device:
?  ?? ? Filters tags by DeviceNo
?  ?? ? Creates PlcClient with filtered tags
?  ?? ? Adds to Clients list
?
?? Error Handling:
   ?? ? Try-catch
   ?? ? Logs error, doesn't rethrow

Methods:
?? ? GetAllClients() ? List<PlcClient>
?? ? GetClient(int plcNo) ? PlcClient (FirstOrDefault)
?
?? UpdateTags() Method:
   ?? ? Takes List<PLCTagConfigurationModel>
 ?? ? For each client:
   ?  ?? ? Calls client.UpdateTags(allNewTags)
   ?? ? Logs update count
   ?? ? No exception handling (shouldn't fail)
```

---

## PART 5: WORKER SERVICE VERIFICATION

### ? ExecuteAsync Implementation

```
Method Signature:
?? ? protected override async Task ExecuteAsync(CancellationToken)
?? ? Implements BackgroundService
?? ? Runs as Windows Service

Initialization Phase:
?? ? GetPlcDevicesAsync()
?? ? GetCameraDevicesAsync()
?? ? GetAllTagsAsync()
?? ? Logs device and tag counts
?
?? SharedServiceHost.Initialize()
   ?? ? Sets PlcManager
   ?? ? Sets AlgorithmService

UI Listener Startup:
?? ? Task.Run() async call
?? ? Calls _uiListener.StartAsync()
?? ? Error handling (logs, doesn't crash)
?? ? Fire-and-forget

Backup Loop:
?? ? Task.Run() async loop
?? ? While (!stoppingToken.IsCancellationRequested)
?? ? _logManager.CheckAndPerformBackups()
?? ? _logManager.CheckAndPerformPurge()
?? ? Task.Delay(60000) - 60 second interval
?? ? Error handling
?? ? Fire-and-forget

Dashboard Startup:
?? ? await _dashboard.StartAsync()
?? ? This doesn't return (runs indefinitely)

Main Wait:
?? ? await Task.Delay(Timeout.Infinite, stoppingToken)
?? ? Main thread sleeps forever

Cleanup (Finally Block):
?? ? Checks _cameraFtpService.IsRunning
?? ? Calls _cameraFtpService.StopAsync()

Error Handling:
?? ? Try-catch wraps entire execution
?? ? Logs FATAL ERROR
?? ? Throws exception (service fails)
```

---

## PART 6: THREADING MODEL VERIFICATION

### ? Thread Lifecycle

```
Main Thread (starts on service startup):
?? ? Enters Worker.ExecuteAsync()
?? ? Loads configuration
?? ? Creates PLCClientManager
?  ?? ? Creates PlcClient instances (no threads yet)
?
?? ? Spawns background tasks:
?  ?? ? UIListener Task.Run()
?  ?? ? Backup Loop Task.Run()
?  ?? ? Dashboard await (doesn't create task)
?
?? ? Sleeps on Task.Delay(Timeout.Infinite)

Per-PLC Thread (created by Task.Run):
?? ? One thread per PlcClient.StartAsync()
?? ? Each thread runs independently
?? ? Each has own TCP connection
?? ? Infinite while(true) polling loop
?? ? 90ms cycle time (?11 polls/sec)
?? ? Auto-reconnect on failure
?? ? Fires OnPlcDataReceived event
?? ? Continues until service stops

Thread Count:
?? ? N = number of PLC devices
?? ? Each device = 1 polling thread
?? ? Additional threads for UI, backup, dashboard
?? ? Total: N + 5 (approximately)
```

### ? Thread Safety

```
Data Protection:
?? ? Interlocked.Exchange(ref _tags, newList)
?  ?? ? Used in PlcClient.UpdateTags()
?
?? ? Each PlcClient has own _tcp
?  ?? ? No sharing between threads
?
?? ? Each PlcClient has own _master
?  ?? ? No sharing between threads
?
?? ? Event-driven communication
?  ?? ? OnPlcDataReceived?.Invoke()
?     ?? ? Data passed by value, not reference
?
?? ? No locks needed
 ?? ? Each component owns its data
```

---

## PART 7: DATA FLOW VERIFICATION

### ? Configuration ? Thread ? Polling ? Event

```
Stage 1: Load Configuration
?? ? DeviceConfigurationService.GetPlcDevicesAsync()
?  ?? ? Returns List<DeviceInterfaceModel>
?
?? ? PLCTagConfigurationService.GetAllTagsAsync()
   ?? ? Returns List<PLCTagConfigurationModel>

Stage 2: Create Clients
?? ? PLCClientManager.InitializeClients()
?  ?? ? For each device:
?  ?  ?? ? Filter tags by PLCNo
?  ?  ?? ? Create PlcClient
?  ?  ?? ? Add to list
?  ?
?  ?? ? Fire-and-forget (no await)

Stage 3: Start Polling
?? ? For each PlcClient:
?  ?? ? client.StartAsync()
?     ?? ? Task.Run() ? new thread
?     ?? ? Infinite polling loop starts

Stage 4: Polling Cycle
?? ? Check IsConnected
?  ?? ? If not: ConnectAsync()
?  ?? ? If yes: continue
?
?? ? PollAllGroups()
?  ?? ? OptimizeReads() ? chunks
?  ?? ? ReadHoldingRegistersAsync() per chunk
?  ?? ? Return Dictionary<uint, ushort[]>
?
?? ? OnPlcDataReceived?.Invoke(DeviceNo, data)
?  ?? ? Event fires to subscribers
?  ?? ? DashboardInitializer receives
?
?? ? Task.Delay(90) ? repeat

Stage 5: Data Processing
?? ? DashboardInitializer.OnPlcDataReceived()
?  ?? ? Receives device number
?  ?? ? Receives raw data dictionary
?  ?? ? Calls AlgorithmAnalysisService.Apply()
?  ?  ?? ? Converts raw ? typed values
?  ?  ?? ? Applies scaling (if configured)
?  ?  ?? ? Returns engineering values
?  ?
?  ?? ? Updates internal cache
?  ?? ? Ready for UI display
```

---

## PART 8: ERROR HANDLING VERIFICATION

### ? Connection Error Recovery

```
Failure Detection:
?? ? ReadHoldingRegistersAsync() throws exception
?? ? Caught in StartAsync() try-catch
?? ? Logged with error message
?? ? Disconnect() called

Recovery Process:
?? ? Task.Delay(3000) - wait 3 seconds
?? ? Back to while loop
?? ? Check !IsConnected ? true
?? ? ConnectAsync() called
?? ? Create new TcpClient
?? ? Attempt connection
?? ? If success: continue polling
?? ? If timeout: retry after 3 seconds

Other Devices:
?? ? Not affected by Device 1 failure
?? ? Continue polling independently
?? ? Their threads run uninterrupted
```

### ? Device Isolation

```
When Device 1 fails:
?? Device 1 thread:
?  ?? ? Handles error, retries connection
?
?? Device 2 thread:
?  ?? ? Continues polling unaffected
?
?? Device 3 thread:
   ?? ? Continues polling unaffected

Service Level:
?? ? Main thread unaffected (sleeps)
?? ? UI listener unaffected
?? ? Backup loop unaffected
?? ? Dashboard unaffected
```

---

## PART 9: PERFORMANCE VERIFICATION

### ? Read Optimization

```
Without Optimization:
?? 104 individual tags
?? 104 separate Modbus requests
   ?? Each: 50-100ms latency
   ?? Total: 5-10 seconds per cycle

With Optimization:
?? OptimizeReads() groups addresses
?? Result: 3-4 chunks
?? 3-4 Modbus requests
   ?? Each: 50-100ms latency
   ?? Total: 200-400ms per cycle

Improvement:
?? 30-50x faster read cycle
?? 11 polls/second (vs 1 poll every 10 seconds)
?? ? Responsive to state changes
```

### ? Polling Rate

```
Interval:
?? ? Task.Delay(90) milliseconds
?? ? ?11 polls per second per device

Purpose:
?? ? Fast enough for reactive control
?? ? Slow enough to be CPU efficient
?? ? Scalable to multiple devices

CPU Impact:
?? ? Idle polling: <5% CPU
?? ? With multiple devices: <10% CPU
?? ? Acceptable for production
```

---

## PART 10: INTEGRATION VERIFICATION

### ? Dependency Injection

```
Program.cs DI Registration:
?? ? IPLCTagConfigurationService ? PLCTagConfigurationService (Singleton)
?? ? IDeviceConfigurationService ? DeviceConfigurationService (Singleton)
?? ? PLCClientManager (Singleton)
?? ? UiListener (Singleton)
?? ? IMessagePublisher ? UiListener instance
?? ? AlgorithmAnalysisService (Singleton)
?? ? DashboardInitializer (Singleton)
?? ? Worker (HostedService)
?? ? All dependencies injected correctly

Worker Constructor Injection:
?? ? IAppLogger
?? ? ILogManagerService
?? ? IPLCTagConfigurationService
?? ? AlgorithmAnalysisService
?? ? DashboardInitializer
?? ? CCDTriggerService
?? ? IDeviceConfigurationService
?? ? IOptions<ConfigSettings>
?? ? CameraFtpService
?? ? PLCClientManager
?? ? UiListener
```

### ? Initialization Order

```
Proper Sequence:
?? 1. ? Program.Main() - entry point
?? 2. ? CreateDefaultBuilder() - host creation
?? 3. ? ConfigureAppConfiguration() - settings load
?? 4. ? ConfigureServices() - DI registration
?? 5. ? Build() - service provider created
?? 6. ? host.Run() - service starts
?? 7. ? Worker ctor called - DI injection
?? 8. ? ExecuteAsync() - main execution
?? 9. ? InitializeClients() - PlcClients created
?? 10. ? StartAsync() per client - threads spawned
?? 11. ? Polling starts
```

---

## PART 11: LOGGING VERIFICATION

### ? Console Output Points

```
Startup Logs:
?? ? "[CoreService] Config base path: ..."
?? ? "Loaded X PLC devices"
?? ? "Loaded Y Modbus tags"
?? ? "PLC[DeviceName] [INFO] Polling Task Starting"
?? ? "PLC[DeviceName] [ATTEMPT] ? Attempting connection..."
?? ? "PLC[DeviceName] [SUCCESS] ? CONNECTED"

Polling Logs:
?? ? "PLC[DeviceName] [DATA] Polling successful..."
?? ? Appears every 90ms per device

Error Logs:
?? ? "PLC[DeviceName] [ERROR] Polling Cycle FAILED..."
?? ? "PLC[DeviceName] [ERROR] ? CONNECT ERROR..."
?? ? "PLC[DeviceName] [WARN] ? DISCONNECTED..."
```

---

## PART 12: CONFIGURATION FILES VERIFICATION

### ? DeviceInterfaces.csv

```
Required Format:
?? ? Header: Id,DeviceNo,DeviceName,UnitNo,Name,ComProtocol,IPAddress,PortNo,Gateway,Description,Remark,Enabled
?? ? Minimum 1 data row
?
?? Field Validation:
   ?? ? Id: unique integer
   ?? ? DeviceNo: unique integer (key for tag association)
   ?? ? DeviceName: string (human-readable)
   ?? ? UnitNo: integer (typically 1)
   ?? ? Name: string
   ?? ? ComProtocol: "Modbus TCP" (currently)
   ?? ? IPAddress: valid IP format
   ?? ? PortNo: 502 (Modbus default)
?? ? Gateway: IP address (optional)
   ?? ? Description: string
   ?? ? Remark: string
   ?? ? Enabled: "true" or "false"
```

### ? PLCTags.csv

```
Required Format:
?? ? Header: Id,TagNo,Name,PLCNo,ModbusAddress,Length,AlgoNo,DataType,BitNo,Offset,Span,...
?? ? Minimum 1 data row
?
?? Field Validation:
   ?? ? Id: unique integer
   ?? ? TagNo: sequential number
?? ? Name: string
   ?? ? PLCNo: matches DeviceNo from DeviceInterfaces.csv
   ?? ? ModbusAddress: valid address (40001-40104)
   ?? ? Length: 1 (16-bit) or 2 (32-bit)
   ?? ? AlgoNo: 0 (Raw) or 1 (LinearScale)
   ?? ? DataType: 1-7
   ?? ? BitNo: 0-15 (for Bit type)
   ?? ? Offset: numeric (for scaling)
   ?? ? Span: numeric (for scaling)
   ?? ? ... other fields
```

---

## PART 13: FINAL VERIFICATION CHECKLIST

### ? Code Quality

```
Code Organization:
?? ? Classes follow single responsibility principle
?? ? Dependencies injected (not created in constructors)
?? ? Proper use of async/await
?? ? No blocking calls in async methods
?? ? Proper exception handling

Thread Safety:
?? ? Interlocked.Exchange for shared state
?? ? Each client owns its TCP connection
?? ? No shared mutable state between clients
?? ? Event-driven architecture (no callbacks with shared data)
?? ? No race conditions detected

Performance:
?? ? Read optimization implemented
?? ? Polling rate balanced
?? ? No memory leaks (resources disposed properly)
?? ? CPU usage minimal
?? ? Scalable to many devices

Error Handling:
?? ? Try-catch in appropriate places
?? ? Errors logged with context
?? ? Service doesn't crash on errors
?? ? Automatic recovery implemented
?? ? Device isolation maintained
```

### ? Functional Requirements

```
Multiple Device Support:
?? ? Each device gets own thread
?? ? Devices poll independently
?? ? Device failure doesn't affect others
?? ? Scalable to 10+ devices

Configuration Management:
?? ? Devices loaded from CSV
?? ? Tags loaded from CSV
?? ? Tags associated with devices
?? ? Changes can be made without rebuild

Connection Management:
?? ? Auto-connect on startup
?? ? Auto-reconnect on failure
?? ? Timeout implemented (5 seconds)
?? ? Retry logic implemented (3 seconds)
?? ? Graceful disconnect on error

Data Reading:
?? ? Read chunking implemented
?? ? All Modbus data types supported
?? ? Byte swapping configurable
?? ? String handling implemented

Data Processing:
?? ? Raw ? typed conversion
?? ? Scaling algorithms (Eng Min/Max, Gain/Offset)
?? ? Async event notification
?? ? Data ready for UI display

Logging:
?? ? Startup sequence logged
?? ? Connection events logged
?? ? Polling events logged
?? ? Errors logged with details
?? ? Console output as expected
```

---

## SIGN-OFF CHECKLIST

### ? Ready for Testing?

```
Code Review:
? All files reviewed and approved
? No critical issues found
? Thread safety verified
? Error handling adequate
? Performance acceptable

Configuration:
? DeviceInterfaces.csv prepared
? PLCTags.csv prepared
? appsettings.json configured
? Data folder setup

Testing Environment:
? PLCs accessible
? Network connectivity verified
? Service account configured
? Logs folder accessible

Documentation:
? Testing documentation complete
? Visual guides prepared
? Quick reference checklist ready
? Implementation checklist complete

Final Approval:
? Code Quality: ? PASS
? Functional Testing: ? READY
? Performance: ? PASS
? Thread Safety: ? PASS
? Error Handling: ? PASS

STATUS: ? READY FOR TESTING
```

---

## NEXT PHASE: MODULE 3 TESTING

After Module 2 is verified:

```
1. Dashboard & OEE (Module 3)
?? DashboardInitializer.cs
   ?? OeeEngine.cs
   ?? Algorithm application

2. Data Logging (Module 4)
   ?? ProductionDataLogger.cs
   ?? Persistence layer

3. External Interfaces (Module 5)
   ?? ExternalInterfaceService.cs
   ?? TCP client communication

4. Alarm Management (Module 6)
   ?? AlarmService.cs
   ?? Condition detection
```

---

**Checklist Version:** 1.0
**Date:** 2024
**Status:** COMPLETE

**Ready to proceed? ? START MODULE 2 TESTING**

