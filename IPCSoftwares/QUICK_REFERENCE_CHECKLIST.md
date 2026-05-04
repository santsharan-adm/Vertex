# QUICK REFERENCE CHECKLIST
## PLC Communication Module 2 - Immediate Verification Guide

---

## ? PRE-STARTUP VERIFICATION

### Configuration Files Exist?
```
? Check: C:\ProgramData\IPCSoftware\Data\DeviceInterfaces.csv
  ?? Must have: Header + at least 1 device row
  ?? Verify columns: Id,DeviceNo,DeviceName,UnitNo,Name,ComProtocol,IPAddress,PortNo,...

? Check: C:\ProgramData\IPCSoftware\Data\PLCTags.csv
  ?? Must have: Header + at least 1 tag row
  ?? Verify columns: Id,TagNo,Name,PLCNo,ModbusAddress,Length,AlgoNo,DataType,...

? Check: appsettings.json
  ?? Must contain:
     {
       "Config": {
         "DataFolder": "C:\\ProgramData\\IPCSoftware\\Data",
         "DeviceFileName": "DeviceInterfaces.csv",
         "PlcTagsFileName": "PLCTags.csv"
       }
     }
```

### PLC Devices Accessible?
```
? Ping 192.168.1.100 (Main PLC)
  ?? $ ping 192.168.1.100
  ?? Expected: Response time < 50ms

? Ping 192.168.1.101 (Safety PLC)
?? $ ping 192.168.1.101
  ?? Expected: Response time < 50ms

? Check Modbus Port Accessibility
  ?? $ telnet 192.168.1.100 502
  ?? Expected: Connection successful (port open)
```

---

## ?? STARTUP PHASE VERIFICATION

### Stage 1: Configuration Loading
```
? EXPECTED CONSOLE OUTPUT (first 5-10 seconds):

  [CoreService] Config base path: C:\...
  Loaded 3 PLC devices.
  Loaded 104 Modbus tags.
  
? VERIFY:
  ?? Device count matches CSV
  ?? Tag count matches CSV
  ?? No exception messages
  ?? Config path is correct
```

### Stage 2: Thread Creation
```
? EXPECTED CONSOLE OUTPUT (seconds 10-15):

  PLC[MainPLC] [INFO] Polling Task Starting.
  PLC[SafetyPLC] [INFO] Polling Task Starting.
  PLC[StoragePLC] [INFO] Polling Task Starting.

? VERIFY:
  ?? One "Polling Task Starting" per device
  ?? Thread count increased (Check Task Manager)
  ?? Device names match configuration
```

### Stage 3: Connection Attempts
```
? EXPECTED CONSOLE OUTPUT (seconds 15-20):

  PLC[MainPLC] [ATTEMPT] ? Attempting connection to 192.168.1.100:502
  PLC[SafetyPLC] [ATTEMPT] ? Attempting connection to 192.168.1.101:502
  PLC[StoragePLC] [ATTEMPT] ? Attempting connection to 192.168.1.102:502

? VERIFY:
  ?? One attempt per device
  ?? IP addresses are correct
  ?? Port is 502 (Modbus default)
```

### Stage 4: Successful Connection
```
? EXPECTED CONSOLE OUTPUT (seconds 20-25):

  PLC[MainPLC] [SUCCESS] ? CONNECTED.
  PLC[SafetyPLC] [SUCCESS] ? CONNECTED.
  PLC[StoragePLC] [SUCCESS] ? CONNECTED.

? VERIFY:
  ?? All devices show SUCCESS within 20 seconds
  ?? No CONNECT ERROR messages
  ?? Each device shows exactly once (on startup)
```

---

## ?? NORMAL OPERATION VERIFICATION

### Polling Active?
```
? EXPECTED CONSOLE OUTPUT (continuous, every 90ms per device):

  PLC[MainPLC] [DATA] Polling successful. Dispatched 104 raw register groups.
  PLC[SafetyPLC] [DATA] Polling successful. Dispatched 85 raw register groups.
  PLC[StoragePLC] [DATA] Polling successful. Dispatched 30 raw register groups.
  PLC[MainPLC] [DATA] Polling successful. Dispatched 104 raw register groups.
  ...

? VERIFY:
  ?? DATA messages appear continuously
  ?? No time gaps > 500ms between polls
  ?? "Dispatched X raw register groups" where X = unique addresses
  ?? Device names rotate (not all from one device)
  
?? RED FLAGS:
  ?? Polling stops (no DATA messages for > 5 seconds)
  ?? Only one device polling (others silent)
  ?? Dispatched count = 0 or 1 (should be 20-100+)
  ?? Repeated ATTEMPT/ERROR messages
```

### CPU & Memory Usage?
```
? CHECK TASK MANAGER:

  Process: IPCSoftware.CoreService.exe
  CPU: Should be < 5% (idle)
  Memory: Should be < 200MB
  Threads: Should be NumDevices + main threads
    ?? Example: 3 devices = ?7 threads
  
? VERIFY:
  ?? Stable CPU usage (not spiking)
  ?? Memory gradually increases then plateaus
  ?? Thread count stable
  
?? RED FLAGS:
  ?? CPU constantly > 50%
  ?? Memory growing unbounded
  ?? Thread count continuously increasing
  ?? Process in "Not Responding" state
```

### Data Flow to Dashboard?
```
? CHECK DATA RECEPTION:

  Monitor file: C:\ProgramData\IPCSoftware\Logs\ProductionLog.csv
  
? VERIFY:
  ?? File is being written to
  ?? New entries appear every 2-5 seconds
  ?? Entries contain data from all devices
  ?? No zero values for all tags
  
?? RED FLAGS:
  ?? File not created or not updated
  ?? All values are zero
  ?? Only data from one device
```

---

## ?? ERROR SCENARIO VERIFICATION

### Scenario 1: Network Disconnected

**Simulate:** Unplug network cable from PLC

```
? EXPECTED BEHAVIOR (first 10 seconds after disconnect):

  PLC[MainPLC] [ERROR] Polling Cycle FAILED. Retrying in 3s.
  Exception: Unable to read from transport connection...
  
  [Wait 3 seconds]
  
  PLC[MainPLC] [ATTEMPT] ? Attempting connection to 192.168.1.100:502
  
  [Wait 5 seconds - connection timeout]
  
  PLC[MainPLC] [ERROR] ? CONNECT ERROR. Retrying in 3s.
  Message: Connection attempt timed out after 5000ms.
  
  [Repeats every 3 seconds...]

? VERIFY:
  ?? Other devices (Safety PLC, Storage PLC) CONTINUE polling
  ?? No cross-device failure
  ?? Error logged, not crashed
  ?? Automatic retry started
  
?? RED FLAGS:
  ?? ALL devices stop polling (cascade failure)
  ?? Service crashes
  ?? CPU spikes to 100%
  ?? Memory grows rapidly
```

**Recovery:** Reconnect cable

```
? EXPECTED BEHAVIOR (after reconnection):

  PLC[MainPLC] [SUCCESS] ? CONNECTED.
  PLC[MainPLC] [DATA] Polling successful. Dispatched 104 raw register groups.
  
? VERIFY:
?? Connection re-established automatically
?? Data flowing again within 20 seconds
  ?? No manual restart needed
  ?? Other devices unaffected during recovery
```

### Scenario 2: Modbus Slave Offline

**Simulate:** Restart PLC (goes offline for 30 seconds)

```
? EXPECTED BEHAVIOR:

  [First 5-10 seconds: Normal polling]
  
  PLC[MainPLC] [ERROR] Polling Cycle FAILED...
  PLC[MainPLC] [ATTEMPT] ? Attempting connection...
  PLC[MainPLC] [ERROR] ? CONNECT ERROR. Retrying in 3s.
  
  [Retries continue every 3 seconds]
  
  [PLC comes back online]
  
  PLC[MainPLC] [SUCCESS] ? CONNECTED.
  PLC[MainPLC] [DATA] Polling successful...

? VERIFY:
  ?? Automatic recovery when PLC comes back
  ?? No data loss after recovery
  ?? All 104 tags still present
```

### Scenario 3: Invalid Tag Configuration

**Scenario:** PLCTags.csv has a tag with invalid data type (e.g., "99")

```
? EXPECTED BEHAVIOR:

  [Startup proceeds normally]
  [Polling happens]
  
  But when that specific tag is processed:
  
  PLC[MainPLC] [ERROR] Failed to convert data for Tag BadTag...

? VERIFY:
  ?? Other tags still process correctly
  ?? Service doesn't crash
  ?? Error logged
  ?? Polling continues

?? RED FLAGS:
  ?? Service crashes on startup
?? No tags load at all
```

---

## ?? DETAILED INSPECTION POINTS

### Device Configuration Inspection

```
? Check DeviceInterfaces.csv Format:

  1. Open file in Notepad
  2. Verify header row:
  Id,DeviceNo,DeviceName,UnitNo,Name,ComProtocol,IPAddress,PortNo,Gateway,Description,Remark,Enabled

  3. Inspect each device row:
     1,1,MainPLC,1,PLC1_TCP,Modbus TCP,192.168.1.100,502,192.168.1.1,Main Production,Primary,true
     ?? DeviceNo: MUST be unique (1, 2, 3, ...)
   ?? IPAddress: MUST be valid IP format
     ?? PortNo: MUST be 502 (Modbus) or your configured port
     ?? Enabled: "true" or "false" (case-sensitive)
     ?? ComProtocol: Currently supports "Modbus TCP"
```

### Tag Configuration Inspection

```
? Check PLCTags.csv Format:

  1. Open file in Notepad
  2. Verify header row:
     Id,TagNo,Name,PLCNo,ModbusAddress,Length,AlgoNo,DataType,BitNo,Offset,Span,...

  3. Inspect sample tags:
     1,1,Temperature_1,1,40001,1,1,4,0,0,100,...
     ?? PLCNo: MUST match a DeviceNo from DeviceInterfaces.csv
     ?? ModbusAddress: MUST be numeric (e.g., 40001)
 ?? Length: 1 for 16-bit, 2 for 32-bit
     ?? DataType: 1-7 (Int16, Word32, Bit, Float, String, UInt16, UInt32)
     ?? AlgoNo: 0 (Raw) or 1 (LinearScale)
     
  4. Count total tags:
     ?? Sum up all tags (should be around 100-104)
     ?? Verify each has unique Id
```

### Thread Inspection

```
? Check Active Threads (Windows Task Manager):

  1. Start CoreService
  2. Wait for successful connection (20-30 seconds)
  3. Open Task Manager ? Processes tab
  4. Find "IPCSoftware.CoreService.exe"
  5. Right-click ? Details tab
  6. Look for "Threads" column
  
  Expected:
  ?? 1 main thread
  ?? 1 backup loop thread
  ?? 1 UI listener thread
  ?? 1 dashboard thread
  ?? N polling threads (1 per PLC device)
  
  Total: ~5 + N threads
  
  Example for 3 devices:
  ?? 5 utility threads
  ?? 3 PLC polling threads
  ?? Total: ?8 threads
  
? VERIFY:
  ?? Thread count is stable (not growing)
  ?? Matches expected count
```

### Memory Growth Inspection

```
? Check Memory Stability (Windows Task Manager):

  1. Start CoreService
  2. Record memory usage immediately: X MB
  3. Let run for 5 minutes
  4. Record memory usage: Y MB
  5. Difference should be small (< 50 MB)
  
  Example:
  ?? T=0: 120 MB
  ?? T=5min: 140 MB
  ?? Growth: 20 MB (acceptable)
  
  ?? RED FLAGS:
  ?? T=0: 120 MB
  ?? T=5min: 250 MB
  ?? Growth: 130 MB (memory leak!)
```

---

## ?? DAILY HEALTH CHECK

Run this every morning to verify system health:

```
? MORNING STARTUP CHECK (Complete in 5 minutes):

  1. Service Status
  ? Is CoreService.exe running? (Task Manager)
     ? No errors in Event Viewer?
  
  2. Network Connectivity
  ? Can ping all PLC devices?
     ? Are PLCs accessible on port 502?
  
  3. Polling Activity
     ? Console shows continuous "[DATA]" messages?
  ? Rate is consistent (every 90ms ?11/sec)?
     ? All 3+ devices polling?
  
  4. File Logging
     ? Is ProductionLog.csv being written?
     ? Timestamp updated within last 5 seconds?
  
  5. Resource Usage
     ? CPU < 10%?
     ? Memory < 300 MB?
 ? No disk errors?
  
  6. Recent Errors
   ? Check last 100 lines of log
     ? Any repeated connection failures?
     ? Any conversion errors?

? ALL GREEN = System healthy
? ANY RED = Investigate immediately
```

---

## ??? TROUBLESHOOTING QUICK REFERENCE

| Issue | Cause | Solution |
|-------|-------|----------|
| No devices load | CSV file missing | Create DeviceInterfaces.csv in Data folder |
| "0 PLC devices" | File empty or header only | Add device rows to CSV |
| "CONNECT ERROR" | PLC unreachable | Check PLC IP, ping test, port 502 |
| All devices retry | Network down | Check network connectivity |
| Only 1 device polls | PLCNo mismatch in tags | Verify PLCNo matches DeviceNo |
| No data in result | Tags have no Modbus address | Check ModbusAddress field |
| Memory grows | Memory leak | Restart service |
| Service crashes | Invalid config | Check CSV format, data types |
| High CPU usage | Excessive polling | Check polling rate (should be 90ms) |

---

## ?? VALIDATION CHECKLIST - FINAL SIGN-OFF

Before declaring system ready:

```
? CONFIGURATION
  ? DeviceInterfaces.csv: 3+ valid devices, all enabled
  ? PLCTags.csv: 100+ valid tags, correct PLCNo associations
  ? All PLCs online and reachable

? STARTUP
  ? Devices load message appears
  ? Tags load message appears
  ? Thread creation logs appear (one per device)
  ? Connection attempts appear
  ? All connections successful

? OPERATION
  ? Polling active (DATA messages every 90ms)
  ? All devices polling simultaneously
  ? Data flowing to dashboard
  ? Logs being written

? ERROR HANDLING
  ? Network disconnect: Device retries, others unaffected
  ? PLC offline: Automatic recovery when PLC returns
  ? Invalid tag: Skipped without stopping other tags
  ? Service maintains stability

? PERFORMANCE
  ? CPU usage < 5% idle
  ? Memory stable (< 200 MB)
  ? Thread count stable
  ? Response time acceptable

? SIGN-OFF
  ? All checks passed
  ? System declared READY FOR PRODUCTION
  ? Date: _______________
  ? Verified by: _______________
```

---

**Quick Reference Version**: 1.0
**Last Updated**: 2024
**Ready for**: Immediate Use & Daily Verification

