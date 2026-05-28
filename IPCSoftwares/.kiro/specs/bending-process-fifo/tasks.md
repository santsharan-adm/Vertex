# Implementation Plan: Bending Process FIFO

## Overview

This plan implements a FIFO-based batch process tracking system for the 9-stage bending machine. The implementation creates a new `BendingProcessService` class that integrates into the existing 500ms processing loop, a `BatchModel` for data tracking, PLC tag configuration via `BendingProcessTags`, and a dashboard model for UI binding. All code is C# targeting .NET 8.0.

## Tasks

- [x] 1. Create data models and tag configuration
  - [x] 1.1 Create `BatchModel` class in `IPCSoftware.Shared\Models\Bending\BatchModel.cs`
    - Inherit from `ObservableObjectVM`
    - Add properties: `BatchNumber` (string), `Stage` (int), `QrCode1`–`QrCode4` (string), `Bending1Temperatures` (float[4]), `Bending1Loads` (float[4]), `Bending2Temperatures` (float[4]), `Bending2Loads` (float[4]), `Bending3Temperatures` (float[4]), `Bending3Loads` (float[4]), `Bending3X` (float[4]), `Bending3Y` (float[4]), `Bending3Z` (float[4]), `Bending3W` (float[4]), `TearingTemperatures` (float[4]), `FlippingForces` (float[4]), `InspectionResults` (bool[4])
    - Add metadata: `CreatedAt` (DateTime), `ArrivedAtStage9` (DateTime?), `InspectionComplete` (bool), `DataLogged` (bool)
    - Initialize all numeric arrays to `new float[4]`, booleans to false, strings to `string.Empty`, Stage to 1
    - Use `SetProperty` pattern for observable properties matching existing models
    - _Requirements: 9.1, 9.2, 9.5_

  - [x] 1.2 Create `BendingProcessDashboardModel` class in `IPCSoftware.Shared\Models\Bending\BendingProcessDashboardModel.cs`
    - Inherit from `ObservableObjectVM`
    - Expose `IReadOnlyList<BatchModel> ActiveBatches` property sorted descending by Stage (Stage 9 first)
    - Provide `UpdateFrom(IReadOnlyList<BatchModel> fifoQueue)` method that re-sorts and notifies UI
    - _Requirements: 8.1, 8.2, 8.5_

  - [x] 1.3 Add `BendingProcessTags` class to `IPCSoftware.Shared\Models\TagMappingSettings.cs`
    - Add all trigger signal tag IDs: `RobotPickDone`, `TT1IndexComplete`, `TransferDone`, `TransferActive`, `TT2Start`, `Robo2Done`, `CameraInspectionComplete`, `InspectionStartWrite`
    - Add bending completion signals: `CD_B1_AllDataReadComp`, `CD_B2_AllDataReadComp`, `CD_B3_AllDataReadComp`, `TearingComplete`, `FlippingComplete`
    - Add QR code tags: `QrCode1`–`QrCode4`
    - Add all process data tags: B1 temps/loads (8), B2 temps/loads (8), B3 temps/loads/X/Y/Z/W (24), Tearing temps (4), Flipping forces (4), Inspection results (4)
    - Add `BendingProcess` property of type `BendingProcessTags` to `TagMappingSettings` class
    - _Requirements: 10.1, 10.2, 10.3_

  - [x] 1.4 Add `BendingProcess` section to `appsettings.json` under `TagMapping` with all tag IDs set to 0 (placeholder)
    - Follow existing pattern of `Dashboard2`, `OEE`, etc.
    - Include all trigger, data, and QR code tag IDs
    - _Requirements: 10.1, 10.3_

  - [x] 1.5 Extend `ConstantValues` class to load `BendingProcessTags` from configuration
    - Add static fields for all bending process tag IDs
    - Populate them in the `Initialize(ConfigSettings)` method from the `TagMapping.BendingProcess` section
    - _Requirements: 10.1, 10.2_

- [x] 2. Checkpoint - Ensure models compile
  - Ensure all tests pass, ask the user if questions arise.

- [x] 3. Implement core `BendingProcessService`
  - [x] 3.1 Create `BendingProcessService` class in `IPCSoftware.CoreService.Bending\Service\Bending_Process.cs`
    - Constructor accepts `IAppLogger` and `IProductionDataLogger`
    - Internal state: `List<BatchModel> _fifoQueue`, `Dictionary<string, bool> _previousTriggerStates`, `int _dailyCounter`, `DateTime _lastCounterDate`, `Dictionary<string, int> _retryCounters`
    - Public `void Process(Dictionary<int, object> latestValues)` entry point
    - Public `IReadOnlyList<BatchModel> GetActiveBatches()` method
    - Public `BendingProcessDashboardModel GetDashboardModel()` method
    - Initialize all previous trigger states to `true` on construction (prevent spurious edges at startup)
    - _Requirements: 9.3, 9.4, 2.6, 11.4_

  - [x] 3.2 Implement safe tag reading helpers in `BendingProcessService`
    - `bool ReadBool(Dictionary<int, object> values, int tagId)` — returns false if tag ID is 0, key missing, or unparseable
    - `float ReadFloat(Dictionary<int, object> values, int tagId)` — returns 0f if tag ID is 0, key missing, or unparseable
    - `string ReadString(Dictionary<int, object> values, int tagId)` — returns empty string if tag ID is 0, key missing, or null
    - Log diagnostic when non-zero tag ID is missing from dictionary
    - Skip silently when tag ID is 0
    - _Requirements: 10.3, 10.4, 10.5_

  - [x] 3.3 Implement rising edge detection in `BendingProcessService`
    - Compare current boolean values against `_previousTriggerStates`
    - Detect false→true transitions for all trigger signals
    - Update `_previousTriggerStates` at end of each `Process()` call
    - Return a set of signal names that had rising edges this cycle
    - _Requirements: 11.1, 11.2, 11.3, 11.4_

  - [x] 3.4 Implement batch number generation in `BendingProcessService`
    - Format: `BN-YYYYMMDD-NNN` with zero-padded daily counter
    - Reset counter to 1 when `_lastCounterDate` differs from current date
    - Allow counter to exceed 999 without rejection
    - _Requirements: 1.2, 1.6_

  - [ ]* 3.5 Write property test for batch number format and daily reset
    - **Property 4: Batch Number Format and Daily Reset**
    - **Validates: Requirements 1.2, 1.6**

  - [ ]* 3.6 Write property test for rising edge detection contract
    - **Property 8: Rising Edge Detection Contract**
    - **Validates: Requirements 11.1, 11.2, 11.4, 11.5**

  - [ ]* 3.7 Write property test for safe tag reading with defaults
    - **Property 11: Safe Tag Reading with Defaults**
    - **Validates: Requirements 10.2, 10.3, 10.4, 10.5**

- [x] 4. Implement FIFO queue management and batch creation
  - [x] 4.1 Implement batch creation logic in `BendingProcessService`
    - On `Robot_Pick_Done` rising edge: validate all 4 QR codes are non-empty/non-null
    - Reject if queue already has 9 batches (log warning)
    - Reject if any QR code is invalid (log warning with position 1-4)
    - Generate batch number, create `BatchModel`, enqueue at tail with Stage = 1
    - _Requirements: 1.1, 1.3, 1.4, 1.5_

  - [x] 4.2 Implement FIFO queue operations in `BendingProcessService`
    - Maintain ordered list with max capacity 9
    - Enforce unique stage constraint (no two batches at same stage)
    - Preserve insertion order (oldest at head)
    - Dequeue from head on Stage 9 completion
    - _Requirements: 2.1, 2.2, 2.4, 2.5_

  - [ ]* 4.3 Write property test for FIFO ordering invariant
    - **Property 1: FIFO Ordering Invariant**
    - **Validates: Requirements 1.3, 2.5**

  - [ ]* 4.4 Write property test for queue capacity and stage uniqueness
    - **Property 2: Queue Capacity and Stage Uniqueness Invariant**
    - **Validates: Requirements 1.4, 2.1**

  - [ ]* 4.5 Write property test for batch creation with validation
    - **Property 3: Batch Creation with Validation**
    - **Validates: Requirements 1.1, 1.4, 1.5**

- [x] 5. Checkpoint - Ensure core logic compiles and unit tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Implement stage transition logic
  - [x] 6.1 Implement TT1 stage transitions (Stages 1→2, 2→3, 3→4, 4→5) in `BendingProcessService`
    - Stage 1→2: On `Robot_Pick_Done` rising edge, advance if Stage 2 is unoccupied
    - Stage 2→3: On `TT1_Index_Complete` rising edge, advance if Stage 3 is unoccupied
    - Stage 3→4: On `TT1_Index_Complete` rising edge, advance if Stage 4 is unoccupied AND both B1+B2 data recorded
    - Stage 4→5: On `TT1_Index_Complete` rising edge, advance if Stage 5 is unoccupied AND B3 data recorded AND `Transfer_Active` is false
    - Process transitions from highest stage to lowest within same cycle
    - Log diagnostic if target stage occupied (discard edge without retry)
    - _Requirements: 3.1, 3.2, 3.4, 3.6, 3.7, 3.8, 4.2, 11.5_

  - [x] 6.2 Implement Transfer Module transition (Stage 5→6) in `BendingProcessService`
    - On `Transfer_Done` rising edge, advance if Stage 6 is unoccupied
    - Block if Stage 6 occupied (log warning)
    - Prevent TT1 rotation to Stage 5 while `Transfer_Active` is true
    - _Requirements: 4.1, 4.2, 4.3_

  - [x] 6.3 Implement TT2 shared signal transitions (Stages 6→7, 7→8) in `BendingProcessService`
    - On `TT2_Start` rising edge: if Stage 8 is occupied by unpicked batch, block all TT2 advancement (log warning)
    - Otherwise advance all TT2 batches by one position, processing highest-stage first (7→8 before 6→7)
    - Use FIFO queue order to determine TT2 positions (no per-position PLC signals)
    - Allow advancement regardless of process data recording completion
    - _Requirements: 5.1, 5.2, 5.3, 5.6_

  - [x] 6.4 Implement Robot-2 pick transition (Stage 8→9) in `BendingProcessService`
    - On `Robo2_Done` rising edge: advance if Stage 9 is unoccupied and batch exists at Stage 8
    - Ignore if no batch at Stage 8 (log diagnostic)
    - Block if Stage 9 occupied (log warning)
    - On arrival at Stage 9: write inspection start signal to PLC via `PLCClientManager.GetClient().WriteAsync()`
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

  - [ ]* 6.5 Write property test for stage transition correctness
    - **Property 5: Stage Transition Correctness**
    - **Validates: Requirements 3.1, 3.2, 3.4, 3.6, 3.7, 4.1, 4.3, 6.1, 6.3**

  - [ ]* 6.6 Write property test for TT2 shared signal advancement
    - **Property 6: TT2 Shared Signal Advancement**
    - **Validates: Requirements 5.1, 5.2, 5.6**

  - [ ]* 6.7 Write property test for Transfer Active blocking
    - **Property 15: Transfer Active Blocking**
    - **Validates: Requirements 4.2**

  - [ ]* 6.8 Write property test for Stage 3→4 data precondition
    - **Property 14: Stage 3→4 Data Precondition**
    - **Validates: Requirements 3.8**

- [x] 7. Implement process data recording
  - [x] 7.1 Implement Bending-1 and Bending-2 data recording (Stage 3) in `BendingProcessService`
    - On `CD_B1_ALL_DATA_READ_COMP` rising edge at Stage 3: read 4 temperatures and 4 loads from configured tags into batch
    - On `CD_B2_ALL_DATA_READ_COMP` rising edge at Stage 3: read 4 temperatures and 4 loads from configured tags into batch
    - Track completion of both signals as precondition for Stage 3→4 advancement
    - _Requirements: 3.3, 3.8_

  - [x] 7.2 Implement Bending-3 data recording (Stage 4) in `BendingProcessService`
    - On `CD_B3_ALL_DATA_READ_COMP` rising edge at Stage 4: read 4 temperatures, 4 loads, 4 X, 4 Y, 4 Z, 4 W values
    - Track completion as precondition for Stage 4→5 advancement
    - _Requirements: 3.5_

  - [x] 7.3 Implement Tearing data recording (Stage 6) in `BendingProcessService`
    - On `TearingComplete` rising edge while batch at Stage 6: read 4 temperature values
    - Does NOT block Stage 6→7 advancement
    - _Requirements: 5.4, 5.6_

  - [x] 7.4 Implement Flipping data recording (Stage 7) in `BendingProcessService`
    - On `FlippingComplete` rising edge while batch at Stage 7: read 4 force values
    - Does NOT block Stage 7→8 advancement
    - _Requirements: 5.5, 5.6_

  - [ ]* 7.5 Write property test for process data recording
    - **Property 7: Process Data Recording**
    - **Validates: Requirements 3.3, 3.5, 5.4, 5.5, 7.1**

- [x] 8. Implement Stage 9 completion and data persistence
  - [x] 8.1 Implement camera inspection result reading (Stage 9) in `BendingProcessService`
    - On `CameraInspectionComplete` rising edge at Stage 9: read 4 OK/NG results from configured tags
    - Set `InspectionComplete = true` on batch
    - If inspection not received within 10 seconds of arrival, log warning and continue waiting
    - _Requirements: 7.1, 7.5_

  - [x] 8.2 Implement data persistence and dequeue logic (Stage 9) in `BendingProcessService`
    - When inspection complete: persist full batch data via `IProductionDataLogger`
    - On success: set `DataLogged = true`, dequeue batch from FIFO head
    - On failure: retain batch, log diagnostic, retry next cycle (max 5 attempts)
    - After 5 failures: log error, remove batch from queue
    - _Requirements: 7.2, 7.3, 7.4, 2.4_

  - [ ]* 8.3 Write property test for dequeue on successful completion
    - **Property 9: Dequeue on Successful Completion**
    - **Validates: Requirements 2.4, 7.3**

  - [ ]* 8.4 Write property test for persistence retry with bounded attempts
    - **Property 12: Persistence Retry with Bounded Attempts**
    - **Validates: Requirements 7.4**

- [x] 9. Checkpoint - Ensure all stage logic compiles and tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 10. Integrate into DashboardInitializerBending and DI
  - [x] 10.1 Register `BendingProcessService` in `Program.cs` DI container
    - Add `services.AddSingleton<BendingProcessService>()` following existing pattern
    - Ensure `IProductionDataLogger` and `IAppLogger` are resolved correctly
    - _Requirements: 9.3_

  - [x] 10.2 Inject `BendingProcessService` into `DashboardInitializerBending` constructor
    - Add `BendingProcessService bendingProcess` parameter
    - Store as `_bendingProcess` field
    - Call `_bendingProcess.Process(latestValueNew)` in the processing loop after existing service calls
    - _Requirements: 9.4_

  - [x] 10.3 Add `HandleUiRequest` case for bending process dashboard (RequestId 30)
    - Return `BendingProcessDashboardModel` via `ResponsePackage`
    - Follow existing pattern of RequestId 11–29
    - _Requirements: 8.1, 8.4_

  - [x] 10.4 Implement dashboard model update in `BendingProcessService`
    - After each `Process()` call, update `BendingProcessDashboardModel` with current queue state
    - Sort batches descending by Stage (Stage 9 at top)
    - Expose empty table when queue has zero batches
    - _Requirements: 8.2, 8.4, 8.5_

  - [ ]* 10.5 Write property test for dashboard sort order
    - **Property 10: Dashboard Sort Order**
    - **Validates: Requirements 8.2**

  - [ ]* 10.6 Write property test for batch initialization defaults
    - **Property 13: Batch Initialization Defaults**
    - **Validates: Requirements 9.5**

- [x] 11. Final checkpoint - Ensure full integration compiles and all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document using FsCheck
- Unit tests validate specific examples and edge cases
- The design uses C# (.NET 8.0) matching the existing project
- All PLC tag IDs start as placeholder (0) until PLC engineer provides actual values
- The service integrates via composition into the existing `DashboardInitializerBending` 500ms loop
- `PLCClientManager.GetClient().WriteAsync()` is needed for the inspection start signal at Stage 9 — inject `PLCClientManager` if write access is required

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3"] },
    { "id": 1, "tasks": ["1.4", "1.5"] },
    { "id": 2, "tasks": ["3.1", "3.2"] },
    { "id": 3, "tasks": ["3.3", "3.4"] },
    { "id": 4, "tasks": ["3.5", "3.6", "3.7", "4.1", "4.2"] },
    { "id": 5, "tasks": ["4.3", "4.4", "4.5", "6.1"] },
    { "id": 6, "tasks": ["6.2", "6.3", "6.4"] },
    { "id": 7, "tasks": ["6.5", "6.6", "6.7", "6.8", "7.1", "7.2"] },
    { "id": 8, "tasks": ["7.3", "7.4", "7.5"] },
    { "id": 9, "tasks": ["8.1", "8.2"] },
    { "id": 10, "tasks": ["8.3", "8.4"] },
    { "id": 11, "tasks": ["10.1", "10.2", "10.3", "10.4"] },
    { "id": 12, "tasks": ["10.5", "10.6"] }
  ]
}
```
