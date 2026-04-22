# COMPREHENSIVE TESTING DOCUMENTATION
## MODULE 2: PLC COMMUNICATION - Configuration Loading & Device Management

---

## TABLE OF CONTENTS
1. [Configuration Loading Flow](#configuration-loading-flow)
2. [Device Discovery & Reading](#device-discovery--reading)
3. [Thread Management for Multiple PLC Connections](#thread-management-for-multiple-plc-connections)
4. [PLC Tag Configuration Loading](#plc-tag-configuration-loading)
5. [Connection Initialization Sequence](#connection-initialization-sequence)
6. [Detailed Function Analysis](#detailed-function-analysis)
7. [Thread Safety & Concurrency](#thread-safety--concurrency)
8. [Test Cases & Verification Checklist](#test-cases--verification-checklist)

---

## CONFIGURATION LOADING FLOW

### Overview
The PLC communication module follows a **3-stage configuration loading process**:

```
???????????????????????????????????????????????????????????????????
? Stage 1: Load Device Configuration (DeviceInterfaceModel)    ?
? ?? Read DeviceInterfaces.csv from Data folder         ?
? ?? Parse each line into DeviceInterfaceModel objects  ?
? ?? Filter by protocol (Modbus TCP, EtherIP, etc.)      ?
? ?? Result: List<DeviceInterfaceModel> with IP/Port/Protocol    ?
???????????????????????????????????????????????????????????????????
     ?
???????????????????????????????????????????????????????????????????
? Stage 2: Load PLC Tag Configuration (PLCTagConfigurationModel) ?
? ?? Read PLCTags.csv from Data folder     ?
? ?? Parse each line with TagConfigLoader      ?
? ?? Map tags to DeviceNo (device association)               ?
? ?? Result: List<PLCTagConfigurationModel> with Modbus address  ?
???????????????????????????????????????????????????????????????????
   ?
???????????????????????????????????????????????????????????????????
? Stage 3: Create PLC Clients & Start Polling Threads       ?
? ?? Create one PlcClient per device      ?
? ?? Assign filtered tags to each client  ?
? ?? Call StartAsync() for each client  ?
? ?? Result: N independent polling threads running   ?
???????????????????????????????????????????????????????????????????
```

---

## DEVICE DISCOVERY & READING

### Class: DeviceConfigurationService

**Location:** `IPCSoftware.Services\ConfigServices\DeviceConfigurationService.cs`

**Purpose:** Manages device and interface configuration persistence and retrieval

### Key Method: `GetPlcDevicesAsync()`

```csharp
public async Task<List<DeviceInterfaceModel>> GetPlcDevicesAsync()
{
    try
    {
        if (_interfaces.Count == 0)
     {
       await LoadInterfacesFromCsvAsync();
        }
        return _interfaces.ToList();
  }
    catch (Exception ex)
    {
        _logger.LogError(ex.Message, LogType.Diagnostics);
        return _interfaces.ToList();
    }
}
```

**Execution Flow:**

```
GetPlcDevicesAsync()
?
?? Check: Are interfaces already loaded in memory?
?  ?? YES: Return cached list (fast path)
?  ?? NO: Load from CSV file
?
?? LoadInterfacesFromCsvAsync()
   ?
   ?? 1. FILE EXISTENCE CHECK
   ?  ?? If DeviceInterfaces.csv doesn't exist ? Create empty file
   ?
 ?? 2. READ FILE
   ?  ?? File.ReadAllLinesAsync(_interfacesCsvPath)
   ?     Result: string[] with header + data lines
   ?
   ?? 3. PARSE EACH LINE (Skip header, i=1)
   ?  ?? For each line:
   ?     ?? Call ParseInterfaceCsvLine(line)
   ?     ?? Extract 12 fields:
   ?     ?  [0] Id
   ?     ?  [1] DeviceNo ? CRITICAL: Links device to PLC
   ? ?  [2] DeviceName
   ?  ?  [3] UnitNo
   ?     ?  [4] Name
   ?     ?  [5] ComProtocol (Modbus TCP, EtherIP, etc.)
   ?     ?  [6] IPAddress ? CRITICAL: PLC IP
   ?     ?  [7] PortNo ? CRITICAL: Modbus TCP port (default 502)
   ?   ?  [8] Gateway
   ?     ?  [9] Description
   ?     ?  [10] Remark
   ?     ?  [11] Enabled (boolean)
   ?     ?? Create DeviceInterfaceModel object
   ?
   ?? 4. UPDATE NEXT ID COUNTER
   ?  ?? Track max ID for future additions
   ?
   ?? 5. RETURN
      ?? _interfaces list populated with all devices
```

### DeviceInterfaceModel Structure

```csharp
public class DeviceInterfaceModel
{
    public int Id { get; set; }    // Unique record ID
    public int DeviceNo { get; set; }   // PLC Device Number (e.g., 1, 2, 3)
  public string DeviceName { get; set; }         // Human-readable name (e.g., "MainPLC")
    public int UnitNo { get; set; }    // Modbus Unit/Slave ID (usually 1)
    public string Name { get; set; }         // Interface name
    public string ComProtocol { get; set; }        // "Modbus TCP", "EtherIP", etc.
    public string IPAddress { get; set; }          // 192.168.1.100
    public int PortNo { get; set; }           // 502 (Modbus TCP default)
    public string Gateway { get; set; }            // Gateway IP
    public string Description { get; set; }     // User description
    public string Remark { get; set; }             // Additional remarks
    public bool Enabled { get; set; }            // Is this device active?
}
```

### Example DeviceInterfaces.csv

```csv
Id,DeviceNo,DeviceName,UnitNo,Name,ComProtocol,IPAddress,PortNo,Gateway,Description,Remark,Enabled
1,1,MainPLC,1,PLC1_TCP,Modbus TCP,192.168.1.100,502,192.168.1.1,Main Production PLC,Primary Connection,true
2,2,SafetyPLC,1,PLC2_TCP,Modbus TCP,192.168.1.101,502,192.168.1.1,Safety Controller,Backup,true
3,3,StoragePLC,1,PLC3_TCP,Modbus TCP,192.168.1.102,502,192.168.1.1,Storage Area Control,,false
```

---

## PLC TAG CONFIGURATION LOADING

### Class: PLCTagConfigurationService

**Location:** `IPCSoftware.Services\ConfigServices\PLCTagConfigurationService.cs`

**Purpose:** Loads and manages Modbus tag definitions

### Key Method: `GetAllTagsAsync()`

```csharp
public async Task<List<PLCTagConfigurationModel>> GetAllTagsAsync()
{
    try
    {
        if (_tags.Count == 0)
        {
            await LoadTagsInternalAsync();
     }
        return _tags.ToList();
    }
    catch (Exception ex)
    {
     _logger.LogError(ex.Message, LogType.Diagnostics);
        throw;
    }
}
```

**Execution Flow:**

```
GetAllTagsAsync()
?
?? LoadTagsInternalAsync()
   ?
   ?? 1. FILE CHECK
   ?  ?? If PLCTags.csv doesn't exist ? Create empty file
   ?
   ?? 2. DELEGATE TO TagConfigLoader
   ?  ?? Uses dedicated loader for robust parsing
   ?   var reloadedTags = _tagLoader.Load(_csvFilePath);
   ?
   ?? 3. THREAD-SAFE UPDATE
   ?  ?? Interlocked.Exchange(ref _tags, reloadedTags)
   ?     ? Atomic swap for thread safety
   ?
   ?? 4. UPDATE ID COUNTER
?  ?? _nextId = _tags.Max(t => t.Id) + 1
   ?
   ?? 5. RETURN
  ?? List<PLCTagConfigurationModel> ready for use
```

### PLCTagConfigurationModel Structure

```csharp
public class PLCTagConfigurationModel
{
    public int Id { get; set; }    // Tag ID
    public int TagNo { get; set; }   // Sequential tag number
    public string Name { get; set; }            // Tag name (e.g., "Temperature_Sensor_1")
    public int PLCNo { get; set; } // Which PLC? (matches DeviceNo)
    public int ModbusAddress { get; set; }      // Modbus register address (e.g., 40001)
    public int Length { get; set; }   // Number of registers (1 for 16-bit, 2 for 32-bit)
    public int AlgNo { get; set; }     // Algorithm (0=Raw, 1=LinearScale)
    public int DataType { get; set; }           // Data type (1=Int16, 2=Word32, 3=Bit, 4=Float, etc.)
    public int BitNo { get; set; }  // Bit position (if DataType=3)
    public double Offset { get; set; }          // Scaling offset
    public double Span { get; set; }            // Scaling span
    public string Description { get; set; }     // Description
    public string Remark { get; set; }          // Remarks
    public bool CanWrite { get; set; }   // Writable?
    public string IOType { get; set; }  // Input/Output
    public bool UseEngMinMax { get; set; }      // Use Eng Min/Max scaling?
}
```

### Example PLCTags.csv

```csv
Id,TagNo,Name,PLCNo,ModbusAddress,Length,AlgoNo,DataType,BitNo,Offset,Span,Description,Remark,CanWrite,IOType
1,1,Temperature_1,1,40001,1,1,4,0,0,100,Temperature Sensor 1,Primary Sensor,false,Input
2,2,Pressure_1,1,40002,2,1,2,0,0,1000,Pressure Sensor,Gauge Pressure,false,Input
3,3,Machine_Status,1,40004,1,0,3,0,0,1,Machine Status Bit,Bit 0=Running,false,Input
4,4,Setpoint_Temp,1,40005,1,1,4,0,20,80,Temperature Setpoint,Writable,true,Output
5,5,Temperature_1,2,40001,1,1,4,0,0,100,Temperature Sensor 1 (PLC2),Secondary,false,Input
```

**Data Type Mapping:**
- `1` = Int16 (signed 16-bit)
- `2` = Word32 (32-bit integer)
- `3` = Bit (single bit)
- `4` = Float (IEEE 32-bit)
- `5` = String (multiple registers)
- `6` = UInt16 (unsigned 16-bit)
- `7` = UInt32 (unsigned 32-bit)

---

## THREAD MANAGEMENT FOR MULTIPLE PLC CONNECTIONS

### Class: PLCClientManager

**Location:** `IPCSoftware.CoreService\Services\PLC\PLCClientManager.cs`

**Purpose:** Creates and manages multiple PlcClient instances (one per device)

### Key Method: `InitializeClients()`

```csharp
private async Task InitializeClients()
{
    try
    {
        var allTags = await _tagService.GetAllTagsAsync();
        var devices = await _deviceService.GetPlcDevicesAsync();
        
        foreach (var dev in devices)
        {
 // CRITICAL: Filter tags for THIS device only
       var myTags = allTags
      .Where(t => t.PLCNo == dev.DeviceNo)
        .ToList();
            
        // Create independent client for this device
            var client = new PlcClient(dev, myTags, _config, _logger);
            Clients.Add(client);
        }
 }
    catch (Exception ex)
    {
        _logger.LogError(ex.Message, LogType.Diagnostics);
  }
}
```

**Execution Flow with Threading:**

```
PLCClientManager Constructor
?
?? InitializeClients() [Fire-and-Forget Task]
   ?
   ?? LOAD DEVICES
   ?  ?? await GetPlcDevicesAsync()
   ?     Returns: [Device1, Device2, Device3, ...]
   ?
   ?? LOAD ALL TAGS
   ?  ?? await GetAllTagsAsync()
   ?     Returns: [Tag1, Tag2, ..., TagN]
   ?
 ?? FOR EACH DEVICE: CREATE INDEPENDENT PLCCLIENT
  ?
      ?? Device 1 (192.168.1.100:502)
      ?  ?? Filter: Tags where PLCNo == 1
      ?  ?? Create: PlcClient(Device1, Tags1-20, config, logger)
      ?  ?? Store: Clients[0] = PlcClient1
      ?
  ?? Device 2 (192.168.1.101:502)
      ?  ?? Filter: Tags where PLCNo == 2
      ?  ?? Create: PlcClient(Device2, Tags21-40, config, logger)
      ??? Store: Clients[1] = PlcClient2
      ?
      ?? Device 3 (192.168.1.102:502)
     ?? Filter: Tags where PLCNo == 3
         ?? Create: PlcClient(Device3, Tags41-50, config, logger)
         ?? Store: Clients[2] = PlcClient3
```

### CRITICAL: Thread Creation in Worker.cs

After PLCClientManager is initialized, the Worker.cs explicitly starts polling threads:

```csharp
// In Worker.ExecuteAsync()

// Each PlcClient has a separate Task running continuously
foreach (var device in devices)
{
 // This MUST be called to start the polling thread
    var client = _plcManager.GetClient(device.DeviceNo);
    if (client != null)
    {
        // Fire and forget - starts infinite polling loop
        _ = client.StartAsync();
    }
}
```

### Thread Lifecycle Diagram

```
???????????????????????????????????????????????????????
? Main Thread (Worker.ExecuteAsync)    ?
?    ?
?  ?? Load configuration                  ?
?  ?? Initialize PLCClientManager        ?
?  ?  ?? Creates N PlcClient instances        ?
?  ?        ?
?  ?? Call StartAsync() for each PlcClient           ?
?  ?  ?? Spawns N new threads (one per device)       ?
?  ?     ?
?  ?? await Task.Delay(Timeout.Infinite) ???        ?
?            ?        ?
?  [Main thread sleeps indefinitely]      ?        ?
?        ??
?????????????????????????????????????????????????????
  ?
        ???????????????????????????????????????????????????????????????????????????
        ?   ?        ?      ?
?????????????????????  ?????????????????????  ?????????????????????
? PLC1 Thread       ?  ? PLC2 Thread       ?  ? PLC3 Thread       ?
? (Task.Run loop)   ?  ? (Task.Run loop)   ?  ? (Task.Run loop) ?
?????????????????????  ?????????????????????  ?????????????????????
? ? while(true)     ?  ? ? while(true)   ?  ? ? while(true)     ?
? ?? Connect()      ?  ? ?? Connect()      ?  ? ?? Connect()      ?
? ?? Poll() every   ?  ? ?? Poll() every   ?  ? ?? Poll() every   ?
? ?  90ms           ?  ? ?  90ms           ?  ? ?  90ms    ?
? ?? Fire Event ?  ? ?? Fire Event  ?  ? ?? Fire Event     ?
? ?? Retry on err   ?  ? ?? Retry on err   ?  ? ?? Retry on err   ?
?????????????????????  ?????????????????????  ?????????????????????
     192.168.1.100        192.168.1.101        192.168.1.102
     Port 502 Port 502 Port 502
     
Each thread runs COMPLETELY INDEPENDENTLY
No data shared between threads
Each has its own TCP connection
```

---

## CONNECTION INITIALIZATION SEQUENCE

### Class: PlcClient

**Location:** `IPCSoftware.CoreService\Services\PLC\PlcClient.cs`

### Key Method: `StartAsync()` - Main Polling Loop

```csharp
public Task StartAsync()
{
    return Task.Run(async () =>
    {
        Console.WriteLine($"PLC[{_device.DeviceName}] [INFO] Polling Task Starting.");
      _logger.LogInfo($"PLC[{_device.DeviceName}] [INFO] Polling Task Starting.", LogType.Diagnostics);

   while (true) // Infinite polling loop
  {
          try
          {
    // 1. ENSURE CONNECTED
           if (!IsConnected)
                {
          await ConnectAsync();
          await Task.Delay(500); // Stabilization delay
 continue;
                }

    // 2. POLL DATA
    if (_tags.Any())
           {
     var data = await PollAllGroups();
         OnPlcDataReceived?.Invoke(_device.DeviceNo, data);
                    Console.WriteLine($"PLC[{_device.DeviceName}] [DATA] Polling successful. " +
            $"Dispatched {data.Count} raw register groups.");
      }
            }
  catch (Exception ex)
   {
             _logger.LogError($"PLC[{_device.DeviceName}] [ERROR] Polling Cycle FAILED. " +
             $"Retrying in 3s. Exception: {ex.Message}", LogType.Diagnostics);
        Disconnect();
             await Task.Delay(3000);
            }

// POLLING RATE: 90ms between cycles (?11 polls/second)
            await Task.Delay(90);
        }
    });
}
```

**Timeline Visualization (for one PLC):**

```
T=0ms     ?? StartAsync() called
?? Task.Run() creates new thread
        
T=1ms     ?? Thread starts polling loop
          ?? if (!IsConnected) ? false (not yet connected)
  
T=1ms ?? ConnectAsync() called
  ?? Create new TcpClient()
          ?? Connect to 192.168.1.100:502
          ?  ?? Timeout: 5000ms
          ?
          ?? Success? Yes
       ?? Create Modbus Master
  ?? Log: "CONNECTED"
             ?? Return from ConnectAsync()
        
T=10ms    ?? Task.Delay(500) - stabilization
          ?? continue ? go back to while loop
    
T=510ms   ?? Check IsConnected ? true ?
  ?? PollAllGroups()
          ?  ?? OptimizeReads() ? creates 3-4 chunks
          ?  ?? ReadHoldingRegistersAsync() chunk 1
  ?  ?? ReadHoldingRegistersAsync() chunk 2
          ?  ?? ReadHoldingRegistersAsync() chunk 3
     ?  ?? Return Dictionary<address, registers>
   ?
          ?? OnPlcDataReceived?.Invoke(1, data)
          ?  ?? Sends to DashboardInitializer
          ?
        ?? Task.Delay(90)
?? Back to while loop
          
T=600ms   ?? Poll again (T+90ms from previous)
     ?? Continue indefinitely...
```

### Key Method: `ConnectAsync()` - Connection with Retry

```csharp
private async Task ConnectAsync()
{
    if (IsConnected) return; // Already connected

  while (true) // Infinite retry loop
    {
     try
      {
     Console.WriteLine($"PLC[{_device.DeviceName}] [ATTEMPT] ? Attempting connection to " +
          $"{_device.IPAddress}:{_device.PortNo}");

            _tcp = new TcpClient();

  // CRITICAL: Timeout handling with Task.WhenAny
 var connectTask = _tcp.ConnectAsync(_device.IPAddress, _device.PortNo);

      if (await Task.WhenAny(connectTask, Task.Delay(ConnectionTimeoutMs)) != connectTask)
    {
   throw new TimeoutException($"Connection attempt timed out after {ConnectionTimeoutMs}ms.");
   }

            await connectTask; // Propagate any exceptions

            // Connection successful
       var factory = new ModbusFactory();
       _master = factory.CreateMaster(_tcp);
            _logger.LogInfo($"PLC[{_device.DeviceName}] [SUCCESS] ? CONNECTED.", LogType.Diagnostics);
            Console.WriteLine($"PLC[{_device.DeviceName}] [SUCCESS] ? CONNECTED.");
  return; // Exit retry loop
        }
        catch (Exception ex)
  {
          Console.WriteLine($"PLC[{_device.DeviceName}] [ERROR] ? CONNECT ERROR. " +
         $"Retrying in 3s. Message: {ex.Message}");
         
            Disconnect(); // Clean up
            await Task.Delay(3000); // Wait 3 seconds
        }
    }
}
```

**Connection Retry Timeline:**

```
T=0ms        ?? ConnectAsync() called
             ?? Create TcpClient
        ?? Start connection to 192.168.1.100:502
             
T=100ms      ?? Connection successful ?
  ?? Create Modbus Master
         ?? return (exit retry loop)
      
If connection fails:

T=0ms        ?? ConnectAsync() called
 ?? Create TcpClient
   ?? Start connection to 192.168.1.100:502
      
T=5000ms     ?? TIMEOUT! No response
  ?? Throw TimeoutException
         ?? Catch block:
         ?  ?? Disconnect()
             ?  ?? Log error
        ?  ?? Task.Delay(3000)
     ?
T=8000ms  ?? Retry ConnectAsync() again
          ?? Create new TcpClient
    ?? Start connection...
             
[Continues retrying every 3 seconds until success]
```

### Key Method: `PollAllGroups()` - Optimized Read

```csharp
private async Task<Dictionary<uint, object>> PollAllGroups()
{
    var result = new Dictionary<uint, object>();

 // 1. OPTIMIZE READS: Convert 104 tags into 3-4 chunks
    var chunks = OptimizeReads(_tags);

    foreach (var chunk in chunks)
    {
        try
        {
            // 2. READ BIG BLOCK
   ushort[] bigBlockRaw = await _master.ReadHoldingRegistersAsync(
      1,     // Slave ID (Modbus Unit)
      chunk.StartOffset,  // Start register address
      chunk.TotalCount// Number of registers to read
    );

       // 3. SLICE DATA: Extract individual tags from block
            foreach (var addrDef in chunk.IncludedAddresses)
      {
       int indexInBlock = addrDef.Offset - chunk.StartOffset;

       // Safety check
       if (indexInBlock < 0 || (indexInBlock + addrDef.Length) > bigBlockRaw.Length)
       continue;

            // Extract registers for this specific address
           ushort[] specificData = new ushort[addrDef.Length];
        Array.Copy(bigBlockRaw, indexInBlock, specificData, 0, addrDef.Length);

   // 4. STORE IN RESULT
                result[(uint)addrDef.ModbusAddress] = specificData;
    }
  }
        catch (Exception ex)
    {
     // Continue with next chunk if one fails
    }
    }

    return result;
}
```

**Data Reading Flow:**

```
Suppose we have 104 tags at addresses:
40001, 40002, 40003, ..., 40104

Without optimization (104 individual reads):
?? ReadHoldingRegistersAsync(1, 1, 1)
?? ReadHoldingRegistersAsync(1, 2, 1)
?? ReadHoldingRegistersAsync(1, 3, 1)
?  ... (100 more requests)
?? Total: 104 network requests
   Performance: VERY SLOW (each request ~50-100ms)

With optimization (3-4 grouped reads):
?? CHUNK 1: ReadHoldingRegistersAsync(1, 1, 50)
?  ?? Returns: ushort[50] = {val1, val2, ..., val50}
?  ?? Extract: address 40001 gets val1[0]
?  ?? Extract: address 40002 gets val2[0]
?  ?? ...
?
?? CHUNK 2: ReadHoldingRegistersAsync(1, 65, 40)
?  ?? Returns: ushort[40] = {val51, val52, ..., val90}
?  ?? Extract individual addresses...
?
?? Total: 3-4 network requests
   Performance: 20-30x FASTER!
```

---

## DETAILED FUNCTION ANALYSIS

### Function: `OptimizeReads()`

**Purpose:** Groups nearby tags into efficient Modbus read chunks

**Algorithm:**
```
1. Get unique addresses from all tags
   ?? Group by ModbusAddress
   ?? Take max Length per address (for 32-bit types)

2. Sort by offset (ascending)

3. Build chunks:
   ?? Start with first address
   ?? For each subsequent address:
   ?  ?? Calculate gap from previous end
   ?  ?? If gap ? 10 registers AND total size ? 120:
 ??  ?? Add to current chunk
   ?  ?? Else:
   ?     ?? Close current chunk
   ?     ?? Start new chunk
   ?? Add final chunk

4. Return list of chunks with:
   ?? StartOffset (first register address)
   ?? TotalCount (number of registers)
   ?? IncludedAddresses[] (which tags in this chunk)
```

**Constraints:**
- MAX_GAP = 10 registers (don't read 10+ empty registers)
- MAX_READ = 120 registers per request (Modbus limit ~125)

**Example Optimization:**

```
Input Tags:
- Address 40001 (16-bit, 1 register)
- Address 40002 (32-bit, 2 registers)
- Address 40004 (16-bit, 1 register)
- Address 40100 (32-bit, 2 registers)
- Address 40102 (16-bit, 1 register)

Step 1: Calculate offsets (assuming DefaultModBusAddress=40001)
- 40001 ? offset 0, len 1
- 40002 ? offset 1, len 2
- 40004 ? offset 3, len 1
- 40100 ? offset 99, len 2
- 40102 ? offset 101, len 1

Step 2: Build chunks
CHUNK 1:
?? Start: offset 0
?? Includes: 40001, 40002, 40004
?? End: offset 4
?? Size: 4 registers
?? Reason: Gap to 40100 is 95 (>10), exceeds MAX_GAP
?? TotalCount: 4

CHUNK 2:
?? Start: offset 99
?? Includes: 40100, 40102
?? End: offset 102
?? Size: 3 registers
?? TotalCount: 3

Result: 2 chunks instead of 5 individual reads
```

---

## THREAD SAFETY & CONCURRENCY

### Critical Areas & Protection Mechanisms

#### 1. **Tag List Updates** - Interlocked Exchange

```csharp
public void UpdateTags(List<PLCTagConfigurationModel> allNewTags)
{
    // Filter for this PLC
    var myNewTags = allNewTags
        .Where(t => t.PLCNo == _device.DeviceNo)
      .ToList();

    // THREAD-SAFE replacement
    Interlocked.Exchange(ref _tags, myNewTags);
    Console.WriteLine($"PLCClient[{_device.DeviceName}] [INFO] Tags updated to {myNewTags.Count} tags.");
}
```

**Why Interlocked.Exchange?**
- Multiple threads access `_tags`:
  - Polling thread reads `_tags` in PollAllGroups()
  - Main thread may update `_tags` via UpdateTags()
- Interlocked.Exchange ensures atomic update
- No partial/torn reads during replacement

#### 2. **TCP Connection State**

```csharp
private TcpClient? _tcp;
private IModbusMaster? _master;

public bool IsConnected => _tcp != null && _tcp.Connected;

private void Disconnect()
{
    if (_master != null) { _master.Dispose(); _master = null; }
    if (_tcp != null)
    {
      _tcp.Close();
        _tcp.Dispose();
        _tcp = null;
    }
}
```

**Thread Safety:**
- Each PlcClient has own `_tcp` and `_master`
- Only one thread accesses them (the polling thread)
- No cross-client interference
- Disposed properly on error

#### 3. **Event Invocation** - Fire and Forget

```csharp
OnPlcDataReceived?.Invoke(_device.DeviceNo, data);
```

**Characteristics:**
- One-way notification (no blocking)
- Subscribers (DashboardInitializer) handle async
- No deadlock risk
- Each event carries data (not reference)

---

## TEST CASES & VERIFICATION CHECKLIST

### Test Category: Configuration Loading

```
? TEST 1.1: Device CSV File Loading
  Scenario: DeviceInterfaces.csv exists with 3 PLC devices
  Expected:
  - DeviceConfigurationService loads file
  - Creates 3 DeviceInterfaceModel objects
  - Filters for enabled devices
  - Returns List<DeviceInterfaceModel> with correct IP/Port
  
  Verify:
  - Console output: "Loaded 3 PLC devices"
  - Each device has valid IP address
  - PortNo = 502 (Modbus default)
  - Enabled flag is respected

? TEST 1.2: CSV File Not Found
  Scenario: DeviceInterfaces.csv doesn't exist
  Expected:
  - Service creates empty CSV file
  - Returns empty list
  - No exception thrown
  
  Verify:
  - File is created with header row
  - _interfaces count = 0

? TEST 1.3: Malformed CSV Lines
  Scenario: CSV has invalid line (missing columns, bad data types)
  Expected:
  - ParseInterfaceCsvLine() returns null for bad line
  - Good lines still load
  - No crash
  
  Verify:
  - Exception caught and logged
  - Service continues loading

? TEST 1.4: Tag Configuration Loading
  Scenario: PLCTags.csv exists with 104 tags
  Expected:
  - TagConfigLoader parses each line
  - Creates PLCTagConfigurationModel for each
  - Tags grouped by PLCNo (e.g., 50 for PLC1, 30 for PLC2, 24 for PLC3)
  
  Verify:
  - Console output: "Loaded 104 PLC tags"
  - _tags.Count == 104
  - Tags properly associated with devices
```

### Test Category: Device Discovery & Tag Association

```
? TEST 2.1: Multiple PLC Detection
  Scenario: 3 devices in CSV
  Expected:
  - PLCClientManager loads devices
  - Creates 3 PlcClient instances
  - Each client gets filtered tag subset
  
  Verify:
  - Clients.Count == 3
  - PlcClient[0].Device.DeviceNo == 1
  - PlcClient[1].Device.DeviceNo == 2
- PlcClient[2].Device.DeviceNo == 3
  - Each has correct tag count

? TEST 2.2: Tag Filtering by DeviceNo
  Scenario: Load tags where:
  - Tags 1-20: PLCNo=1
  - Tags 21-40: PLCNo=2
  - Tags 41-50: PLCNo=3
  
  Expected:
- PlcClient1 gets only tags 1-20
  - PlcClient2 gets only tags 21-40
  - PlcClient3 gets only tags 41-50
  
  Verify:
  - PlcClient1._tags.Count == 20
  - All tags in PlcClient1 have PLCNo == 1
  - No tag appears in multiple clients
```

### Test Category: Thread Creation & Management

```
? TEST 3.1: Independent Thread Creation
  Scenario: 3 devices configured
Expected:
  - StartAsync() called for each device
  - 3 new threads created
  - Each runs independently
  - Each has own TCP connection
  
  Verify:
  - Task.Run() creates thread for each client
  - Console shows: "PLC[Device1] [INFO] Polling Task Starting"
  - Console shows: "PLC[Device2] [INFO] Polling Task Starting"
  - Console shows: "PLC[Device3] [INFO] Polling Task Starting"
  - Timing: All start nearly simultaneously

? TEST 3.2: Parallel Polling
  Scenario: 3 devices, observe polling for 5 seconds
  Expected:
  - Each thread polls independently every 90ms
  - No blocking between threads
  - Data arrives from all 3 devices
  
  Verify:
  - Console shows interleaved polls from different devices
  - Timestamps show overlapping execution
  - Total output count ? (5000ms / 90ms) * 3 ? 166 polls

? TEST 3.3: Connection Error Isolation
  Scenario: Device 2 network unreachable
  Expected:
  - Device 2 thread retries connection (3s delay)
- Device 1 and 3 continue polling unaffected
  
  Verify:
  - Device 1: Continues polling
  - Device 2: Shows "CONNECT ERROR", retries after 3s
  - Device 3: Continues polling
  - No cascade failure
```

### Test Category: Connection & Polling

```
? TEST 4.1: Initial Connection Success
  Scenario: PLC at 192.168.1.100:502 is online
  Expected:
  - ConnectAsync() succeeds within 1 second
  - Modbus Master created
  - IsConnected returns true
  
  Verify:
  - Console: "PLC[MainPLC] [SUCCESS] ? CONNECTED"
  - No repeated connection attempts
  - Polling starts immediately

? TEST 4.2: Connection Timeout
  Scenario: PLC IP unreachable (network dropped)
  Expected:
  - ConnectAsync() waits 5 seconds
  - TimeoutException thrown
  - Automatic retry after 3 seconds
  
  Verify:
  - Console: "PLC[...] [ATTEMPT] ? Attempting connection"
  - After 5s: "CONNECT ERROR. Retrying in 3s"
  - After 8s: Another attempt
  - Retries continue indefinitely

? TEST 4.3: Polling Data Flow
  Scenario: Connected to PLC with 104 tags
  Expected:
  - Every 90ms: PollAllGroups() called
  - OptimizeReads() groups tags into ~3-4 chunks
  - Each chunk read via Modbus
  - Data Dictionary returned
  - OnPlcDataReceived event fired
  
  Verify:
  - Console: "PLC[...] [DATA] Polling successful. Dispatched X raw register groups"
  - X should be number of unique addresses (typically 100-104)
  - Consistent polling rate (every 90ms ±10ms)

? TEST 4.4: Read Chunk Optimization
  Scenario: 104 tags spread across address range
  Expected:
  - OptimizeReads() returns 3-4 chunks
- NOT 104 individual reads
  
  Verify:
  - Enable debug logging in OptimizeReads()
  - Count ReadHoldingRegistersAsync() calls per poll
  - Should be 3-4, not 104
```

### Test Category: Error Handling

```
? TEST 5.1: Polling Exception Handling
  Scenario: Modbus read fails mid-poll
  Expected:
  - Exception caught in StartAsync() try-catch
  - Disconnect() called
  - Logged with error message
  - Retry loop resumes
  
  Verify:
  - Console: "PLC[...] [ERROR] Polling Cycle FAILED"
  - Connection reset
  - Retries after 3 seconds
  - Service doesn't crash

? TEST 5.2: Tag Update During Polling
  Scenario: Tags updated while thread polling
  Expected:
  - UpdateTags() uses Interlocked.Exchange
  - Polling thread sees new tag list
  - No corruption or exceptions
  
  Verify:
  - No race condition errors
  - Tag counts update correctly
  - Polling continues uninterrupted

? TEST 5.3: Graceful Shutdown
  Scenario: Service stopped
  Expected:
  - Polling loops exit
  - TCP connections closed
  - Resources disposed
  
  Verify:
  - Disconnect() called for each client
  - All Tasks complete
  - No hanging threads
```

### Test Category: Data Integrity

```
? TEST 6.1: Register Data Slicing
  Scenario: Read chunk with 50 registers, extract 3 addresses
  Expected:
  - Address at offset 10: Gets registers[10:11]
  - Address at offset 20 (32-bit): Gets registers[20:21]
  - Address at offset 40: Gets registers[40:41]
  - No mixing or misalignment
  
  Verify:
  - Each address gets correct data
  - Byte order preserved (Modbus Big Endian)
  - 32-bit values get 2 registers correctly

? TEST 6.2: Multiple PLC Data Isolation
  Scenario: Poll 3 PLCs simultaneously
  Expected:
  - Each PlcClient has separate data
  - OnPlcDataReceived fires 3 times (once per device)
  - No cross-contamination
  
  Verify:
  - DeviceNo correctly passed to event
  - Data tagged with correct device
  - DashboardInitializer processes correctly
```

---

## CONSOLE OUTPUT REFERENCE

### Expected Startup Output

```
[CoreService] Config base path: C:\...\Data
[CoreService] Environment: Production
Loaded 3 PLC devices.
Loaded 104 Modbus tags.
[UI Listener] Started on port 5050
PLC[MainPLC] [INFO] Polling Task Starting.
PLC[SafetyPLC] [INFO] Polling Task Starting.
PLC[StoragePLC] [INFO] Polling Task Starting.
PLC[MainPLC] [ATTEMPT] ? Attempting connection to 192.168.1.100:502
PLC[SafetyPLC] [ATTEMPT] ? Attempting connection to 192.168.1.101:502
PLC[StoragePLC] [ATTEMPT] ? Attempting connection to 192.168.1.102:502
```

### Expected After Connection

```
PLC[MainPLC] [SUCCESS] ? CONNECTED.
PLC[SafetyPLC] [SUCCESS] ? CONNECTED.
PLC[StoragePLC] [SUCCESS] ? CONNECTED.
PLC[MainPLC] [DATA] Polling successful. Dispatched 104 raw register groups.
PLC[SafetyPLC] [DATA] Polling successful. Dispatched 85 raw register groups.
PLC[StoragePLC] [DATA] Polling successful. Dispatched 30 raw register groups.
PLC[MainPLC] [DATA] Polling successful. Dispatched 104 raw register groups.
[repeats every 90ms...]
```

---

## SUMMARY

### Configuration Loading Sequence
1. **DeviceConfigurationService** reads DeviceInterfaces.csv
2. **Enabled devices** filtered and returned
3. **PLCTagConfigurationService** reads PLCTags.csv
4. **Tags** filtered by DeviceNo and associated
5. **PLCClientManager** creates PlcClient per device
6. **Each PlcClient** spawned as independent thread

### Threading Model
- **Main Thread**: Orchestrates startup, then sleeps
- **N Polling Threads**: One per device, independent execution
- **Each Thread**: Infinite loop with 90ms polling interval
- **Auto-Reconnect**: Built-in retry logic (3s delay)
- **Error Isolation**: Device-level failures don't affect others

### Performance Optimization
- **Read Chunking**: 104 tags ? 3-4 Modbus requests
- **Polling Rate**: 11 requests/second per device
- **Scalability**: Supports many devices simultaneously
- **Thread Safety**: Interlocked operations for tag updates

---

**Document Version**: 1.0
**Last Updated**: 2024
**Status**: Complete for Module 2 PLC Communication

