using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Datalogger;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.Bending;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IPCSoftware.CoreService.Bending.Service
{
    /// <summary>
    /// FIFO-based batch process tracking service for the 9-stage bending machine.
    /// Called every 500ms from DashboardInitializerBending with the latest PLC tag values.
    /// </summary>
    public class BendingProcessService: IProcessLogic
    {
        private readonly IAppLogger _logger;
        private readonly IProductionDataLogger _prodLogger;
        private readonly PLCClientManager _plcManager;
        private readonly AlgorithmAnalysisService _algo;

        // FIFO queue: ordered list of active batches, max 9
        private readonly List<BatchModel> _fifoQueue = new();

        // Previous trigger states for rising edge detection (initialized to true to prevent spurious edges at startup)
        private readonly Dictionary<string, bool> _previousTriggerStates = new();

        // Batch number generation
        private int _dailyCounter = 0;
        private DateTime _lastCounterDate = DateTime.MinValue;

        // Persistence retry tracking per batch number
        private readonly Dictionary<int, int> _retryCounters = new();

        // Track B1/B2 data completion per batch (needed for Stage 3→4 precondition)
        private readonly HashSet<int> _b1DataRecorded = new();
        private readonly HashSet<int> _b2DataRecorded = new();
        // Track B3 data completion per batch (needed for Stage 4→5 precondition)
        private readonly HashSet<int> _b3DataRecorded = new();

        // Dashboard model for UI binding
        private readonly BendingProcessDashboardModel _dashboardModel = new();
        private int _cycleCount = 0;

        public BendingProcessService(IAppLogger logger, IProductionDataLogger prodLogger, PLCClientManager plcManager, AlgorithmAnalysisService algo)
        {
            _logger = logger;
            _prodLogger = prodLogger;
            _plcManager = plcManager;
            _algo = algo;

            // Initialize all previous trigger states to TRUE (prevents spurious edges at startup per Req 11.4)
            InitializePreviousTriggerStates();

            _logger.LogInfo("[BendingProcess] Service initialized. FIFO queue empty, awaiting batches.", LogType.Diagnostics);
        }

        /// <summary>
        /// Main entry point — called every 500ms from the processing loop.
        /// Single-trigger conveyor logic: RobotPickDone advances ALL batches + creates new one.
        /// </summary>
        public void Process(Dictionary<int, object> latestValues)
        {
            // Debug: confirm method is called
            if (latestValues.Count > 0 && _fifoQueue.Count == 0)
            {
                // Only print once when we have data but no batches yet
                Console.WriteLine($"[FIFO] Process() running. Tags={latestValues.Count}. Waiting for trigger (Tag 1334)...");
            }

            // 1. Detect rising edges on trigger signal
            var risingEdges = DetectRisingEdges(latestValues);

            // Debug: show tag 1334 value
            if (latestValues.TryGetValue(1334, out var triggerVal))
            {
                // Only log when value changes or first time
            }
            else if (latestValues.Count > 0 && _cycleCount % 20 == 0)
            {
                Console.WriteLine($"[FIFO] WARNING: Tag 1334 (RobotPickDone) NOT in latestValues! Dict has {latestValues.Count} keys.");
            }
            _cycleCount++;

            // 2. On RobotPickDone rising edge: advance all + create new batch
            if (risingEdges.Contains("RobotPickDone"))
            {
                Console.WriteLine("[FIFO] *** TRIGGER: RobotPickDone — advancing all batches ***");

                // Step A: If batch at Stage 9 has inspection complete, dequeue it
                var batchAt9 = GetBatchAtStage(9);
                if (batchAt9 != null && batchAt9.InspectionComplete)
                {
                    _fifoQueue.Remove(batchAt9);
                    Console.WriteLine($"[FIFO] {batchAt9.BatchNumber} EXIT from Stage 9 (inspection complete)");
                }

                // Step B: Advance ALL existing batches by one stage (highest first)
                // SHIFT REGISTER: only advance if next stage is unoccupied
                var batchesSorted = _fifoQueue.OrderByDescending(b => b.Stage).ToList();
                foreach (var batch in batchesSorted)
                {
                    if (batch.Stage < 9)
                    {
                        int nextStage = batch.Stage + 1;
                        // Only advance if next stage is empty (shift register — no collisions)
                        if (!_fifoQueue.Any(b => b != batch && b.Stage == nextStage))
                        {
                            int oldStage = batch.Stage;
                            batch.Stage = nextStage;

                            // Step C: Record data on arrival at specific stages
                            RecordDataOnArrival(batch, batch.Stage, latestValues);

                            Console.WriteLine($"[FIFO] {batch.BatchNumber} Stage {oldStage}→{batch.Stage}");
                        }
                        else
                        {
                            Console.WriteLine($"[FIFO] {batch.BatchNumber} BLOCKED at Stage {batch.Stage} (Stage {nextStage} occupied)");
                        }
                    }
                }

                // Step D: Create new batch at Stage 1
                TryCreateBatch(latestValues);

                // Print queue status
                Console.WriteLine($"[FIFO] Queue={_fifoQueue.Count} | {string.Join(", ", _fifoQueue.Select(b => $"{b.BatchNumber}@S{b.Stage}"))}");
            }

            // 3. Handle Stage 9 inspection (CameraInspectionComplete signal — separate from main trigger)
            if (risingEdges.Contains("CameraInspectionComplete"))
            {
                var batchAt9 = GetBatchAtStage(9);
                if (batchAt9 != null && !batchAt9.InspectionComplete)
                {
                    batchAt9.InspectionResults[0] = ReadBool(latestValues, ConstantValues.BP_InspResult1, "InspResult1");
                    batchAt9.InspectionResults[1] = ReadBool(latestValues, ConstantValues.BP_InspResult2, "InspResult2");
                    batchAt9.InspectionResults[2] = ReadBool(latestValues, ConstantValues.BP_InspResult3, "InspResult3");
                    batchAt9.InspectionResults[3] = ReadBool(latestValues, ConstantValues.BP_InspResult4, "InspResult4");
                    batchAt9.InspectionComplete = true;
                    Console.WriteLine($"[FIFO] {batchAt9.BatchNumber} Inspection complete: [{batchAt9.InspectionResults[0]},{batchAt9.InspectionResults[1]},{batchAt9.InspectionResults[2]},{batchAt9.InspectionResults[3]}]");
                }
            }

            // Update dashboard model
            _dashboardModel.UpdateFrom(_fifoQueue);
        }

        /// <summary>
        /// Records/snapshots process data when a batch arrives at a data-recording stage.
        /// </summary>
        private void RecordDataOnArrival(BatchModel batch, int newStage, Dictionary<int, object> latestValues)
        {
            switch (newStage)
            {
                case 3: // Bending-1 & 2 — snapshot temps and loads
                    batch.Bending1Temperatures[0] = ReadFloat(latestValues, ConstantValues.BP_B1_Temp1, "B1_Temp1");
                    batch.Bending1Temperatures[1] = ReadFloat(latestValues, ConstantValues.BP_B1_Temp2, "B1_Temp2");
                    batch.Bending1Temperatures[2] = ReadFloat(latestValues, ConstantValues.BP_B1_Temp3, "B1_Temp3");
                    batch.Bending1Temperatures[3] = ReadFloat(latestValues, ConstantValues.BP_B1_Temp4, "B1_Temp4");
                    batch.Bending1Loads[0] = ReadFloat(latestValues, ConstantValues.BP_B1_Load1, "B1_Load1");
                    batch.Bending1Loads[1] = ReadFloat(latestValues, ConstantValues.BP_B1_Load2, "B1_Load2");
                    batch.Bending1Loads[2] = ReadFloat(latestValues, ConstantValues.BP_B1_Load3, "B1_Load3");
                    batch.Bending1Loads[3] = ReadFloat(latestValues, ConstantValues.BP_B1_Load4, "B1_Load4");
                    batch.Bending2Temperatures[0] = ReadFloat(latestValues, ConstantValues.BP_B2_Temp1, "B2_Temp1");
                    batch.Bending2Temperatures[1] = ReadFloat(latestValues, ConstantValues.BP_B2_Temp2, "B2_Temp2");
                    batch.Bending2Temperatures[2] = ReadFloat(latestValues, ConstantValues.BP_B2_Temp3, "B2_Temp3");
                    batch.Bending2Temperatures[3] = ReadFloat(latestValues, ConstantValues.BP_B2_Temp4, "B2_Temp4");
                    batch.Bending2Loads[0] = ReadFloat(latestValues, ConstantValues.BP_B2_Load1, "B2_Load1");
                    batch.Bending2Loads[1] = ReadFloat(latestValues, ConstantValues.BP_B2_Load2, "B2_Load2");
                    batch.Bending2Loads[2] = ReadFloat(latestValues, ConstantValues.BP_B2_Load3, "B2_Load3");
                    batch.Bending2Loads[3] = ReadFloat(latestValues, ConstantValues.BP_B2_Load4, "B2_Load4");
                    break;

                case 4: // Bending-3 — snapshot temps, loads, X, Y, Z, W
                    batch.Bending3Temperatures[0] = ReadFloat(latestValues, ConstantValues.BP_B3_Temp1, "B3_Temp1");
                    batch.Bending3Temperatures[1] = ReadFloat(latestValues, ConstantValues.BP_B3_Temp2, "B3_Temp2");
                    batch.Bending3Temperatures[2] = ReadFloat(latestValues, ConstantValues.BP_B3_Temp3, "B3_Temp3");
                    batch.Bending3Temperatures[3] = ReadFloat(latestValues, ConstantValues.BP_B3_Temp4, "B3_Temp4");
                    batch.Bending3Loads[0] = ReadFloat(latestValues, ConstantValues.BP_B3_Load1, "B3_Load1");
                    batch.Bending3Loads[1] = ReadFloat(latestValues, ConstantValues.BP_B3_Load2, "B3_Load2");
                    batch.Bending3Loads[2] = ReadFloat(latestValues, ConstantValues.BP_B3_Load3, "B3_Load3");
                    batch.Bending3Loads[3] = ReadFloat(latestValues, ConstantValues.BP_B3_Load4, "B3_Load4");
                    batch.Bending3X[0] = ReadFloat(latestValues, ConstantValues.BP_B3_X1, "B3_X1");
                    batch.Bending3X[1] = ReadFloat(latestValues, ConstantValues.BP_B3_X2, "B3_X2");
                    batch.Bending3X[2] = ReadFloat(latestValues, ConstantValues.BP_B3_X3, "B3_X3");
                    batch.Bending3X[3] = ReadFloat(latestValues, ConstantValues.BP_B3_X4, "B3_X4");
                    batch.Bending3Y[0] = ReadFloat(latestValues, ConstantValues.BP_B3_Y1, "B3_Y1");
                    batch.Bending3Y[1] = ReadFloat(latestValues, ConstantValues.BP_B3_Y2, "B3_Y2");
                    batch.Bending3Y[2] = ReadFloat(latestValues, ConstantValues.BP_B3_Y3, "B3_Y3");
                    batch.Bending3Y[3] = ReadFloat(latestValues, ConstantValues.BP_B3_Y4, "B3_Y4");
                    batch.Bending3Z[0] = ReadFloat(latestValues, ConstantValues.BP_B3_Z1, "B3_Z1");
                    batch.Bending3Z[1] = ReadFloat(latestValues, ConstantValues.BP_B3_Z2, "B3_Z2");
                    batch.Bending3Z[2] = ReadFloat(latestValues, ConstantValues.BP_B3_Z3, "B3_Z3");
                    batch.Bending3Z[3] = ReadFloat(latestValues, ConstantValues.BP_B3_Z4, "B3_Z4");
                    batch.Bending3W[0] = ReadFloat(latestValues, ConstantValues.BP_B3_W1, "B3_W1");
                    batch.Bending3W[1] = ReadFloat(latestValues, ConstantValues.BP_B3_W2, "B3_W2");
                    batch.Bending3W[2] = ReadFloat(latestValues, ConstantValues.BP_B3_W3, "B3_W3");
                    batch.Bending3W[3] = ReadFloat(latestValues, ConstantValues.BP_B3_W4, "B3_W4");
                    break;

                case 6: // Tearing — record status
                    batch.TearingStatus[0] = ReadBool(latestValues, ConstantValues.BP_Tear_Temp1, "Tear_Status1");
                    batch.TearingStatus[1] = ReadBool(latestValues, ConstantValues.BP_Tear_Temp2, "Tear_Status2");
                    batch.TearingStatus[2] = ReadBool(latestValues, ConstantValues.BP_Tear_Temp3, "Tear_Status3");
                    batch.TearingStatus[3] = ReadBool(latestValues, ConstantValues.BP_Tear_Temp4, "Tear_Status4");
                    break;

                case 7: // Flipping — record status
                    batch.FlippingStatus[0] = ReadBool(latestValues, ConstantValues.BP_Flip_Force1, "Flip_Status1");
                    batch.FlippingStatus[1] = ReadBool(latestValues, ConstantValues.BP_Flip_Force2, "Flip_Status2");
                    batch.FlippingStatus[2] = ReadBool(latestValues, ConstantValues.BP_Flip_Force3, "Flip_Status3");
                    batch.FlippingStatus[3] = ReadBool(latestValues, ConstantValues.BP_Flip_Force4, "Flip_Status4");
                    break;

                case 9: // Inspection position — write start signal
                    batch.ArrivedAtStage9 = DateTime.Now;
                    WriteInspectionStartSignal();
                    break;
            }
        }

        /// <summary>
        /// Returns the current list of active batches (read-only).
        /// </summary>
        public IReadOnlyList<BatchModel> GetActiveBatches() => _fifoQueue.AsReadOnly();

        /// <summary>
        /// Returns the dashboard model (sorted descending by Stage for UI display).
        /// </summary>
        public BendingProcessDashboardModel GetDashboardModel() => _dashboardModel;

        #region Safe Tag Reading Helpers (Task 3.2)

        /// <summary>
        /// Reads a boolean value from the PLC tag dictionary.
        /// Returns false if tag ID is 0, key missing, or unparseable.
        /// </summary>
        private bool ReadBool(Dictionary<int, object> values, int tagId, string signalName = "")
        {
            // Skip silently when tag ID is 0 (placeholder)
            if (tagId == 0) return false;

            if (!values.TryGetValue(tagId, out var val))
            {
                // Non-zero tag ID missing from dictionary — skip silently (too noisy for every cycle)
                return false;
            }

            if (val == null) return false;

            // Try direct bool
            if (val is bool b) return b;

            // Try parse from string
            if (bool.TryParse(val.ToString(), out var parsed)) return parsed;

            // Try numeric: 1 = true, 0 = false
            if (int.TryParse(val.ToString(), out var intVal)) return intVal != 0;
            if (float.TryParse(val.ToString(), out var floatVal)) return floatVal != 0f;

            return false;
        }

        /// <summary>
        /// Reads a float value from the PLC tag dictionary.
        /// Returns 0f if tag ID is 0, key missing, or unparseable.
        /// </summary>
        private float ReadFloat(Dictionary<int, object> values, int tagId, string signalName = "")
        {
            // Skip silently when tag ID is 0 (placeholder)
            if (tagId == 0) return 0f;

            if (!values.TryGetValue(tagId, out var val))
            {
                // Non-zero tag ID missing from dictionary — skip silently
                return 0f;
            }

            if (val == null) return 0f;

            if (val is float f) return f;
            if (val is double d) return (float)d;
            if (val is int i) return i;

            if (float.TryParse(val.ToString(), out var parsed)) return parsed;

            return 0f;
        }

        /// <summary>
        /// Reads a string value from the PLC tag dictionary.
        /// Returns empty string if tag ID is 0, key missing, or null.
        /// </summary>
        private string ReadString(Dictionary<int, object> values, int tagId, string signalName = "")
        {
            // Skip silently when tag ID is 0 (placeholder)
            if (tagId == 0) return string.Empty;

            if (!values.TryGetValue(tagId, out var val))
            {
                // Non-zero tag ID missing from dictionary — skip silently
                return string.Empty;
            }

            return val?.ToString() ?? string.Empty;
        }

        #endregion

        #region Batch Creation and FIFO Queue (Tasks 4.1, 4.2)

        /// <summary>
        /// Attempts to create a new batch on RobotPickDone rising edge.
        /// Validates QR codes and queue capacity before enqueuing.
        /// </summary>
        private void TryCreateBatch(Dictionary<int, object> latestValues)
        {
            // Check queue capacity (max 9 batches)
            if (_fifoQueue.Count >= 9)
            {
                _logger.LogWarning("[BendingProcess] Queue full (9 batches). Rejecting new batch.", LogType.Diagnostics);
                return;
            }

            // Read and validate all 4 QR codes
            var qr1 = ReadString(latestValues, ConstantValues.BP_QrCode1, "QrCode1");
            var qr2 = ReadString(latestValues, ConstantValues.BP_QrCode2, "QrCode2");
            var qr3 = ReadString(latestValues, ConstantValues.BP_QrCode3, "QrCode3");
            var qr4 = ReadString(latestValues, ConstantValues.BP_QrCode4, "QrCode4");

            Console.WriteLine($"[FIFO] QR values: QR1='{qr1}' QR2='{qr2}' QR3='{qr3}' QR4='{qr4}'");
            Console.WriteLine($"[FIFO] Tag IDs: QR1={ConstantValues.BP_QrCode1} QR2={ConstantValues.BP_QrCode2} QR3={ConstantValues.BP_QrCode3} QR4={ConstantValues.BP_QrCode4}");

            // Validate QR codes — reject if any is empty or null
            if (string.IsNullOrEmpty(qr1))
            {
                _logger.LogWarning("[BendingProcess] QR code position 1 is empty/null. Rejecting batch.", LogType.Diagnostics);
                return;
            }
            if (string.IsNullOrEmpty(qr2))
            {
                _logger.LogWarning("[BendingProcess] QR code position 2 is empty/null. Rejecting batch.", LogType.Diagnostics);
                return;
            }
            if (string.IsNullOrEmpty(qr3))
            {
                _logger.LogWarning("[BendingProcess] QR code position 3 is empty/null. Rejecting batch.", LogType.Diagnostics);
                return;
            }
            if (string.IsNullOrEmpty(qr4))
            {
                _logger.LogWarning("[BendingProcess] QR code position 4 is empty/null. Rejecting batch.", LogType.Diagnostics);
                return;
            }

            // Generate batch number and create batch
            var batchNumber = GenerateBatchNumber();
            var batch = new BatchModel
            {
                BatchNumber = batchNumber,
                Stage = 1,
                QrCode1 = qr1,
                QrCode2 = qr2,
                QrCode3 = qr3,
                QrCode4 = qr4,
                CreatedAt = DateTime.Now
            };

            // Enqueue at tail
            _fifoQueue.Add(batch);

            Console.WriteLine($"[FIFO] *** BATCH CREATED: {batchNumber} | Stage=1 | Queue={_fifoQueue.Count} ***");
            _logger.LogInfo(
                $"[BendingProcess] Batch created: {batchNumber} | QR1={qr1}, QR2={qr2}, QR3={qr3}, QR4={qr4} | Stage=1 | Queue size={_fifoQueue.Count}",
                LogType.Diagnostics);
        }

        /// <summary>
        /// Gets the batch currently at the specified stage, or null if unoccupied.
        /// </summary>
        private BatchModel? GetBatchAtStage(int stage)
        {
            return _fifoQueue.FirstOrDefault(b => b.Stage == stage);
        }

        /// <summary>
        /// Checks if a stage is currently unoccupied.
        /// </summary>
        private bool IsStageEmpty(int stage)
        {
            return !_fifoQueue.Any(b => b.Stage == stage);
        }

        /// <summary>
        /// Dequeues the batch at the head of the FIFO queue (oldest batch).
        /// </summary>
        private void DequeueHead()
        {
            if (_fifoQueue.Count > 0)
            {
                var removed = _fifoQueue[0];
                _fifoQueue.RemoveAt(0);
                _logger.LogInfo($"[BendingProcess] Dequeued batch {removed.BatchNumber} from head. Queue size={_fifoQueue.Count}", LogType.Diagnostics);
            }
        }

        #endregion

        #region Stage Transitions (Task 6.1 - TT1)

        /// <summary>
        /// Processes all stage transitions from highest stage to lowest (per Req 2.3).
        /// </summary>
        private void ProcessStageTransitions(HashSet<string> risingEdges, Dictionary<int, object> latestValues)
        {
            // Process from highest stage to lowest to prevent position conflicts

            // Stage 8→9: Robo2_Done (Robot-2 pick)
            ProcessStage8To9(risingEdges);

            // Stages 6→7, 7→8: TT2_Start (shared signal)
            ProcessTT2Transitions(risingEdges);

            // Stage 5→6: Transfer_Done
            ProcessStage5To6(risingEdges);

            // Stage 4→5: TT1_Index_Complete + B3 data recorded + Transfer_Active is false
            ProcessStage4To5(risingEdges, latestValues);

            // Stage 3→4: TT1_Index_Complete + B1 AND B2 data recorded
            ProcessStage3To4(risingEdges);

            // Stage 2→3: TT1_Index_Complete + Stage 3 empty
            ProcessStage2To3(risingEdges);

            // Stage 1→2: Robot_Pick_Done + Stage 2 empty
            ProcessStage1To2(risingEdges);
        }

        /// <summary>
        /// Stage 1→2: On Robot_Pick_Done rising edge, advance if Stage 2 is unoccupied.
        /// </summary>
        private void ProcessStage1To2(HashSet<string> risingEdges)
        {
            if (!risingEdges.Contains("RobotPickDone")) return;

            var batch = GetBatchAtStage(1);
            if (batch == null) return;

            if (IsStageEmpty(2))
            {
                batch.Stage = 2;
                _logger.LogInfo($"[BendingProcess] {batch.BatchNumber} advanced Stage 1→2", LogType.Diagnostics);
            }
            else
            {
                _logger.LogInfo($"[BendingProcess] {batch.BatchNumber} blocked at Stage 1 (Stage 2 occupied)", LogType.Diagnostics);
            }
        }

        /// <summary>
        /// Stage 2→3: On TT1_Index_Complete rising edge, advance if Stage 3 is unoccupied.
        /// </summary>
        private void ProcessStage2To3(HashSet<string> risingEdges)
        {
            if (!risingEdges.Contains("TT1IndexComplete")) return;

            var batch = GetBatchAtStage(2);
            if (batch == null) return;

            if (IsStageEmpty(3))
            {
                batch.Stage = 3;
                _logger.LogInfo($"[BendingProcess] {batch.BatchNumber} advanced Stage 2→3", LogType.Diagnostics);
            }
            else
            {
                _logger.LogInfo($"[BendingProcess] {batch.BatchNumber} blocked at Stage 2 (Stage 3 occupied)", LogType.Diagnostics);
            }
        }

        /// <summary>
        /// Stage 3→4: On TT1_Index_Complete rising edge, advance if Stage 4 is unoccupied
        /// AND both B1+B2 data have been recorded.
        /// </summary>
        private void ProcessStage3To4(HashSet<string> risingEdges)
        {
            if (!risingEdges.Contains("TT1IndexComplete")) return;

            var batch = GetBatchAtStage(3);
            if (batch == null) return;

            // Check precondition: both B1 and B2 data must be recorded
            bool b1Done = _b1DataRecorded.Contains(batch.BatchNumber);
            bool b2Done = _b2DataRecorded.Contains(batch.BatchNumber);

            if (!b1Done || !b2Done)
            {
                _logger.LogInfo($"[BendingProcess] {batch.BatchNumber} blocked at Stage 3 (B1={b1Done}, B2={b2Done} — waiting for data)", LogType.Diagnostics);
                return;
            }

            if (IsStageEmpty(4))
            {
                batch.Stage = 4;
                _logger.LogInfo($"[BendingProcess] {batch.BatchNumber} advanced Stage 3→4", LogType.Diagnostics);
            }
            else
            {
                _logger.LogInfo($"[BendingProcess] {batch.BatchNumber} blocked at Stage 3 (Stage 4 occupied)", LogType.Diagnostics);
            }
        }

        /// <summary>
        /// Stage 4→5: On TT1_Index_Complete rising edge, advance if Stage 5 is unoccupied
        /// AND B3 data recorded AND Transfer_Active is false.
        /// </summary>
        private void ProcessStage4To5(HashSet<string> risingEdges, Dictionary<int, object> latestValues)
        {
            if (!risingEdges.Contains("TT1IndexComplete")) return;

            var batch = GetBatchAtStage(4);
            if (batch == null) return;

            // Check precondition: B3 data must be recorded
            bool b3Done = _b3DataRecorded.Contains(batch.BatchNumber);
            if (!b3Done)
            {
                _logger.LogInfo($"[BendingProcess] {batch.BatchNumber} blocked at Stage 4 (B3 data not recorded)", LogType.Diagnostics);
                return;
            }

            // Check precondition: Transfer_Active must be false (Req 4.2)
            bool transferActive = ReadBool(latestValues, ConstantValues.BP_TransferActive, "TransferActive");
            if (transferActive)
            {
                _logger.LogInfo($"[BendingProcess] {batch.BatchNumber} blocked at Stage 4 (Transfer_Active=true)", LogType.Diagnostics);
                return;
            }

            if (IsStageEmpty(5))
            {
                batch.Stage = 5;
                _logger.LogInfo($"[BendingProcess] {batch.BatchNumber} advanced Stage 4→5", LogType.Diagnostics);
            }
            else
            {
                _logger.LogInfo($"[BendingProcess] {batch.BatchNumber} blocked at Stage 4 (Stage 5 occupied)", LogType.Diagnostics);
            }
        }

        #endregion

        #region Transfer Module Transition (Task 6.2)

        /// <summary>
        /// Stage 5→6: On Transfer_Done rising edge, advance if Stage 6 is unoccupied.
        /// </summary>
        private void ProcessStage5To6(HashSet<string> risingEdges)
        {
            if (!risingEdges.Contains("TransferDone")) return;

            var batch = GetBatchAtStage(5);
            if (batch == null) return;

            if (IsStageEmpty(6))
            {
                batch.Stage = 6;
                _logger.LogInfo($"[BendingProcess] {batch.BatchNumber} advanced Stage 5→6 (Transfer complete)", LogType.Diagnostics);
            }
            else
            {
                _logger.LogWarning($"[BendingProcess] {batch.BatchNumber} blocked at Stage 5 (Stage 6 occupied)", LogType.Diagnostics);
            }
        }

        #endregion

        #region TT2 Shared Signal Transitions (Task 6.3)

        /// <summary>
        /// On TT2_Start rising edge: advance all TT2 batches by one position (7→8 before 6→7).
        /// Blocks all TT2 advancement if Stage 8 is occupied by an unpicked batch.
        /// </summary>
        private void ProcessTT2Transitions(HashSet<string> risingEdges)
        {
            if (!risingEdges.Contains("TT2Start")) return;

            // Check blocking condition: Stage 8 occupied by unpicked batch
            var batchAtStage8 = GetBatchAtStage(8);
            if (batchAtStage8 != null)
            {
                _logger.LogWarning($"[BendingProcess] TT2 blocked — Stage 8 occupied by {batchAtStage8.BatchNumber} (not yet picked by Robot-2)", LogType.Diagnostics);
                return;
            }

            // Process highest stage first: 7→8 before 6→7
            var batchAt7 = GetBatchAtStage(7);
            if (batchAt7 != null)
            {
                batchAt7.Stage = 8;
                _logger.LogInfo($"[BendingProcess] {batchAt7.BatchNumber} advanced Stage 7→8 (TT2 rotation)", LogType.Diagnostics);
            }

            var batchAt6 = GetBatchAtStage(6);
            if (batchAt6 != null)
            {
                batchAt6.Stage = 7;
                _logger.LogInfo($"[BendingProcess] {batchAt6.BatchNumber} advanced Stage 6→7 (TT2 rotation)", LogType.Diagnostics);
            }
        }

        #endregion

        #region Robot-2 Pick Transition (Task 6.4)

        /// <summary>
        /// Stage 8→9: On Robo2_Done rising edge, advance if Stage 9 is unoccupied.
        /// On arrival at Stage 9, write inspection start signal to PLC.
        /// </summary>
        private void ProcessStage8To9(HashSet<string> risingEdges)
        {
            if (!risingEdges.Contains("Robo2Done")) return;

            var batch = GetBatchAtStage(8);
            if (batch == null)
            {
                _logger.LogInfo("[BendingProcess] Robo2_Done fired but no batch at Stage 8 — ignoring", LogType.Diagnostics);
                return;
            }

            if (!IsStageEmpty(9))
            {
                _logger.LogWarning($"[BendingProcess] {batch.BatchNumber} blocked at Stage 8 (Stage 9 occupied)", LogType.Diagnostics);
                return;
            }

            batch.Stage = 9;
            batch.ArrivedAtStage9 = DateTime.Now;
            _logger.LogInfo($"[BendingProcess] {batch.BatchNumber} advanced Stage 8→9 (Robot-2 pick complete)", LogType.Diagnostics);

            // Write inspection start signal to PLC
            WriteInspectionStartSignal();
        }

        /// <summary>
        /// Writes the inspection start signal to the PLC to initiate camera inspection.
        /// </summary>
        private async void WriteInspectionStartSignal()
        {
            try
            {
                int tagId = ConstantValues.BP_InspectionStartWrite;
                if (tagId == 0)
                {
                    _logger.LogInfo("[BendingProcess] InspectionStartWrite tag ID is 0 (placeholder) — skipping write", LogType.Diagnostics);
                    return;
                }

                // Look up tag config from algo service (same pattern as DashboardInitializerBase)
                var tagConfig = _algo.Tags?.FirstOrDefault(t => t.Id == tagId);

                if (tagConfig == null)
                {
                    _logger.LogWarning($"[BendingProcess] Tag config not found for InspectionStartWrite (ID={tagId})", LogType.Diagnostics);
                    return;
                }

                var plcClient = _plcManager.GetClient(tagConfig.PLCNo);
                if (plcClient != null && plcClient.IsConnected)
                {
                    await plcClient.WriteAsync(tagConfig, true);
                    _logger.LogInfo($"[BendingProcess] Inspection start signal written to PLC (Tag {tagId})", LogType.Diagnostics);
                }
                else
                {
                    _logger.LogWarning("[BendingProcess] PLC client not connected — cannot write inspection start", LogType.Diagnostics);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[BendingProcess] Error writing inspection start signal: {ex.Message}", LogType.Diagnostics);
            }
        }

        #endregion

        #region Process Data Recording (Tasks 7.1-7.4)

        /// <summary>
        /// Records process data at relevant stages when completion signals fire.
        /// </summary>
        private void RecordProcessData(HashSet<string> risingEdges, Dictionary<int, object> latestValues)
        {
            // Bending-1 data at Stage 3
            RecordBending1Data(risingEdges, latestValues);

            // Bending-2 data at Stage 3
            RecordBending2Data(risingEdges, latestValues);

            // Bending-3 data at Stage 4
            RecordBending3Data(risingEdges, latestValues);

            // Tearing data at Stage 6
            RecordTearingData(risingEdges, latestValues);

            // Flipping data at Stage 7
            RecordFlippingData(risingEdges, latestValues);
        }

        /// <summary>
        /// On CD_B1_AllDataReadComp rising edge at Stage 3: read 4 temperatures and 4 loads.
        /// </summary>
        private void RecordBending1Data(HashSet<string> risingEdges, Dictionary<int, object> latestValues)
        {
            if (!risingEdges.Contains("CD_B1_AllDataReadComp")) return;

            var batch = GetBatchAtStage(3);
            if (batch == null) return;

            batch.Bending1Temperatures[0] = ReadFloat(latestValues, ConstantValues.BP_B1_Temp1, "B1_Temp1");
            batch.Bending1Temperatures[1] = ReadFloat(latestValues, ConstantValues.BP_B1_Temp2, "B1_Temp2");
            batch.Bending1Temperatures[2] = ReadFloat(latestValues, ConstantValues.BP_B1_Temp3, "B1_Temp3");
            batch.Bending1Temperatures[3] = ReadFloat(latestValues, ConstantValues.BP_B1_Temp4, "B1_Temp4");

            batch.Bending1Loads[0] = ReadFloat(latestValues, ConstantValues.BP_B1_Load1, "B1_Load1");
            batch.Bending1Loads[1] = ReadFloat(latestValues, ConstantValues.BP_B1_Load2, "B1_Load2");
            batch.Bending1Loads[2] = ReadFloat(latestValues, ConstantValues.BP_B1_Load3, "B1_Load3");
            batch.Bending1Loads[3] = ReadFloat(latestValues, ConstantValues.BP_B1_Load4, "B1_Load4");

            _b1DataRecorded.Add(batch.BatchNumber);
            _logger.LogInfo($"[BendingProcess] {batch.BatchNumber} B1 data recorded at Stage 3", LogType.Diagnostics);
        }

        /// <summary>
        /// On CD_B2_AllDataReadComp rising edge at Stage 3: read 4 temperatures and 4 loads.
        /// </summary>
        private void RecordBending2Data(HashSet<string> risingEdges, Dictionary<int, object> latestValues)
        {
            if (!risingEdges.Contains("CD_B2_AllDataReadComp")) return;

            var batch = GetBatchAtStage(3);
            if (batch == null) return;

            batch.Bending2Temperatures[0] = ReadFloat(latestValues, ConstantValues.BP_B2_Temp1, "B2_Temp1");
            batch.Bending2Temperatures[1] = ReadFloat(latestValues, ConstantValues.BP_B2_Temp2, "B2_Temp2");
            batch.Bending2Temperatures[2] = ReadFloat(latestValues, ConstantValues.BP_B2_Temp3, "B2_Temp3");
            batch.Bending2Temperatures[3] = ReadFloat(latestValues, ConstantValues.BP_B2_Temp4, "B2_Temp4");

            batch.Bending2Loads[0] = ReadFloat(latestValues, ConstantValues.BP_B2_Load1, "B2_Load1");
            batch.Bending2Loads[1] = ReadFloat(latestValues, ConstantValues.BP_B2_Load2, "B2_Load2");
            batch.Bending2Loads[2] = ReadFloat(latestValues, ConstantValues.BP_B2_Load3, "B2_Load3");
            batch.Bending2Loads[3] = ReadFloat(latestValues, ConstantValues.BP_B2_Load4, "B2_Load4");

            _b2DataRecorded.Add(batch.BatchNumber);
            _logger.LogInfo($"[BendingProcess] {batch.BatchNumber} B2 data recorded at Stage 3", LogType.Diagnostics);
        }

        /// <summary>
        /// On CD_B3_AllDataReadComp rising edge at Stage 4: read temps, loads, X, Y, Z, W.
        /// </summary>
        private void RecordBending3Data(HashSet<string> risingEdges, Dictionary<int, object> latestValues)
        {
            if (!risingEdges.Contains("CD_B3_AllDataReadComp")) return;

            var batch = GetBatchAtStage(4);
            if (batch == null) return;

            batch.Bending3Temperatures[0] = ReadFloat(latestValues, ConstantValues.BP_B3_Temp1, "B3_Temp1");
            batch.Bending3Temperatures[1] = ReadFloat(latestValues, ConstantValues.BP_B3_Temp2, "B3_Temp2");
            batch.Bending3Temperatures[2] = ReadFloat(latestValues, ConstantValues.BP_B3_Temp3, "B3_Temp3");
            batch.Bending3Temperatures[3] = ReadFloat(latestValues, ConstantValues.BP_B3_Temp4, "B3_Temp4");

            batch.Bending3Loads[0] = ReadFloat(latestValues, ConstantValues.BP_B3_Load1, "B3_Load1");
            batch.Bending3Loads[1] = ReadFloat(latestValues, ConstantValues.BP_B3_Load2, "B3_Load2");
            batch.Bending3Loads[2] = ReadFloat(latestValues, ConstantValues.BP_B3_Load3, "B3_Load3");
            batch.Bending3Loads[3] = ReadFloat(latestValues, ConstantValues.BP_B3_Load4, "B3_Load4");

            batch.Bending3X[0] = ReadFloat(latestValues, ConstantValues.BP_B3_X1, "B3_X1");
            batch.Bending3X[1] = ReadFloat(latestValues, ConstantValues.BP_B3_X2, "B3_X2");
            batch.Bending3X[2] = ReadFloat(latestValues, ConstantValues.BP_B3_X3, "B3_X3");
            batch.Bending3X[3] = ReadFloat(latestValues, ConstantValues.BP_B3_X4, "B3_X4");

            batch.Bending3Y[0] = ReadFloat(latestValues, ConstantValues.BP_B3_Y1, "B3_Y1");
            batch.Bending3Y[1] = ReadFloat(latestValues, ConstantValues.BP_B3_Y2, "B3_Y2");
            batch.Bending3Y[2] = ReadFloat(latestValues, ConstantValues.BP_B3_Y3, "B3_Y3");
            batch.Bending3Y[3] = ReadFloat(latestValues, ConstantValues.BP_B3_Y4, "B3_Y4");

            batch.Bending3Z[0] = ReadFloat(latestValues, ConstantValues.BP_B3_Z1, "B3_Z1");
            batch.Bending3Z[1] = ReadFloat(latestValues, ConstantValues.BP_B3_Z2, "B3_Z2");
            batch.Bending3Z[2] = ReadFloat(latestValues, ConstantValues.BP_B3_Z3, "B3_Z3");
            batch.Bending3Z[3] = ReadFloat(latestValues, ConstantValues.BP_B3_Z4, "B3_Z4");

            batch.Bending3W[0] = ReadFloat(latestValues, ConstantValues.BP_B3_W1, "B3_W1");
            batch.Bending3W[1] = ReadFloat(latestValues, ConstantValues.BP_B3_W2, "B3_W2");
            batch.Bending3W[2] = ReadFloat(latestValues, ConstantValues.BP_B3_W3, "B3_W3");
            batch.Bending3W[3] = ReadFloat(latestValues, ConstantValues.BP_B3_W4, "B3_W4");

            _b3DataRecorded.Add(batch.BatchNumber);
            _logger.LogInfo($"[BendingProcess] {batch.BatchNumber} B3 data recorded at Stage 4", LogType.Diagnostics);
        }

        /// <summary>
        /// On TearingComplete rising edge while batch at Stage 6: read 4 status values (OK/NG).
        /// Does NOT block Stage 6→7 advancement.
        /// </summary>
        private void RecordTearingData(HashSet<string> risingEdges, Dictionary<int, object> latestValues)
        {
            if (!risingEdges.Contains("TearingComplete")) return;

            var batch = GetBatchAtStage(6);
            if (batch == null) return;

            batch.TearingStatus[0] = ReadBool(latestValues, ConstantValues.BP_Tear_Temp1, "Tear_Status1");
            batch.TearingStatus[1] = ReadBool(latestValues, ConstantValues.BP_Tear_Temp2, "Tear_Status2");
            batch.TearingStatus[2] = ReadBool(latestValues, ConstantValues.BP_Tear_Temp3, "Tear_Status3");
            batch.TearingStatus[3] = ReadBool(latestValues, ConstantValues.BP_Tear_Temp4, "Tear_Status4");

            _logger.LogInfo($"[BendingProcess] {batch.BatchNumber} Tearing status recorded at Stage 6: [{batch.TearingStatus[0]},{batch.TearingStatus[1]},{batch.TearingStatus[2]},{batch.TearingStatus[3]}]", LogType.Diagnostics);
        }

        /// <summary>
        /// On FlippingComplete rising edge while batch at Stage 7: read 4 status values (OK/NG).
        /// Does NOT block Stage 7→8 advancement.
        /// </summary>
        private void RecordFlippingData(HashSet<string> risingEdges, Dictionary<int, object> latestValues)
        {
            if (!risingEdges.Contains("FlippingComplete")) return;

            var batch = GetBatchAtStage(7);
            if (batch == null) return;

            batch.FlippingStatus[0] = ReadBool(latestValues, ConstantValues.BP_Flip_Force1, "Flip_Status1");
            batch.FlippingStatus[1] = ReadBool(latestValues, ConstantValues.BP_Flip_Force2, "Flip_Status2");
            batch.FlippingStatus[2] = ReadBool(latestValues, ConstantValues.BP_Flip_Force3, "Flip_Status3");
            batch.FlippingStatus[3] = ReadBool(latestValues, ConstantValues.BP_Flip_Force4, "Flip_Status4");

            _logger.LogInfo($"[BendingProcess] {batch.BatchNumber} Flipping status recorded at Stage 7: [{batch.FlippingStatus[0]},{batch.FlippingStatus[1]},{batch.FlippingStatus[2]},{batch.FlippingStatus[3]}]", LogType.Diagnostics);
        }

        #endregion

        #region Stage 9 Completion (Tasks 8.1, 8.2)

        /// <summary>
        /// Handles Stage 9: camera inspection reading, data persistence, and dequeue.
        /// </summary>
        private void ProcessStage9Completion(HashSet<string> risingEdges, Dictionary<int, object> latestValues)
        {
            var batch = GetBatchAtStage(9);
            if (batch == null) return;

            // 8.1: Read camera inspection results on CameraInspectionComplete rising edge
            if (risingEdges.Contains("CameraInspectionComplete") && !batch.InspectionComplete)
            {
                batch.InspectionResults[0] = ReadBool(latestValues, ConstantValues.BP_InspResult1, "InspResult1");
                batch.InspectionResults[1] = ReadBool(latestValues, ConstantValues.BP_InspResult2, "InspResult2");
                batch.InspectionResults[2] = ReadBool(latestValues, ConstantValues.BP_InspResult3, "InspResult3");
                batch.InspectionResults[3] = ReadBool(latestValues, ConstantValues.BP_InspResult4, "InspResult4");

                batch.InspectionComplete = true;
                _logger.LogInfo(
                    $"[BendingProcess] {batch.BatchNumber} Inspection complete at Stage 9 — Results: [{batch.InspectionResults[0]}, {batch.InspectionResults[1]}, {batch.InspectionResults[2]}, {batch.InspectionResults[3]}]",
                    LogType.Diagnostics);
            }

            // Check inspection timeout warning (>10 seconds at Stage 9 without results)
            if (!batch.InspectionComplete && batch.ArrivedAtStage9.HasValue)
            {
                var elapsed = DateTime.Now - batch.ArrivedAtStage9.Value;
                if (elapsed.TotalSeconds > 10)
                {
                    // Log warning once (use a simple check — only log every ~10 seconds)
                    if ((int)elapsed.TotalSeconds % 10 == 0)
                    {
                        _logger.LogWarning(
                            $"[BendingProcess] {batch.BatchNumber} waiting for inspection at Stage 9 for {elapsed.TotalSeconds:F0}s",
                            LogType.Diagnostics);
                    }
                }
            }

            // 8.2: Persist data and dequeue when inspection is complete
            if (batch.InspectionComplete && !batch.DataLogged)
            {
                TryPersistAndDequeue(batch);
            }
        }

        /// <summary>
        /// Attempts to persist batch data. On success: dequeue. On failure: retry next cycle (max 5).
        /// </summary>
        private void TryPersistAndDequeue(BatchModel batch)
        {
            // Check retry count
            if (!_retryCounters.ContainsKey(batch.BatchNumber))
            {
                _retryCounters[batch.BatchNumber] = 0;
            }

            try
            {
                // Persist via IProductionDataLogger
                // For now, log the batch data as a production record
                // TODO: Map BatchModel fields to ProductionDataRecord format when integrating with actual logger
                _logger.LogInfo(
                    $"[BendingProcess] Persisting batch {batch.BatchNumber} — " +
                    $"QR=[{batch.QrCode1},{batch.QrCode2},{batch.QrCode3},{batch.QrCode4}] " +
                    $"B1Temps=[{string.Join(",", batch.Bending1Temperatures)}] " +
                    $"Inspection=[{string.Join(",", batch.InspectionResults)}]",
                    LogType.Diagnostics);

                // Mark as logged
                batch.DataLogged = true;

                // Dequeue from head
                _fifoQueue.Remove(batch);

                // Clean up tracking data for this batch
                _b1DataRecorded.Remove(batch.BatchNumber);
                _b2DataRecorded.Remove(batch.BatchNumber);
                _b3DataRecorded.Remove(batch.BatchNumber);
                _retryCounters.Remove(batch.BatchNumber);

                _logger.LogInfo(
                    $"[BendingProcess] Batch {batch.BatchNumber} completed and dequeued. Queue size={_fifoQueue.Count}",
                    LogType.Diagnostics);
            }
            catch (Exception ex)
            {
                _retryCounters[batch.BatchNumber]++;
                var attempts = _retryCounters[batch.BatchNumber];

                if (attempts >= 5)
                {
                    // Max retries exceeded — log error and remove batch
                    _logger.LogError(
                        $"[BendingProcess] Persistence failed after 5 attempts for {batch.BatchNumber}. Removing from queue. Error: {ex.Message}",
                        LogType.Diagnostics);

                    _fifoQueue.Remove(batch);
                    _b1DataRecorded.Remove(batch.BatchNumber);
                    _b2DataRecorded.Remove(batch.BatchNumber);
                    _b3DataRecorded.Remove(batch.BatchNumber);
                    _retryCounters.Remove(batch.BatchNumber);
                }
                else
                {
                    _logger.LogWarning(
                        $"[BendingProcess] Persistence failed for {batch.BatchNumber} (attempt {attempts}/5). Will retry. Error: {ex.Message}",
                        LogType.Diagnostics);
                }
            }
        }

        #endregion

        #region Rising Edge Detection (Task 3.3)

        /// <summary>
        /// Compares current trigger values against previous states.
        /// Returns a set of signal names that had false→true transitions this cycle.
        /// Updates _previousTriggerStates at the end.
        /// </summary>
        private HashSet<string> DetectRisingEdges(Dictionary<int, object> latestValues)
        {
            var risingEdges = new HashSet<string>();

            // Read current values for all trigger signals
            var currentStates = new Dictionary<string, bool>
            {
                ["RobotPickDone"] = ReadBool(latestValues, ConstantValues.BP_RobotPickDone, "RobotPickDone"),
                ["TT1IndexComplete"] = ReadBool(latestValues, ConstantValues.BP_TT1IndexComplete, "TT1IndexComplete"),
                ["TransferDone"] = ReadBool(latestValues, ConstantValues.BP_TransferDone, "TransferDone"),
                ["TransferActive"] = ReadBool(latestValues, ConstantValues.BP_TransferActive, "TransferActive"),
                ["TT2Start"] = ReadBool(latestValues, ConstantValues.BP_TT2Start, "TT2Start"),
                ["Robo2Done"] = ReadBool(latestValues, ConstantValues.BP_Robo2Done, "Robo2Done"),
                ["CameraInspectionComplete"] = ReadBool(latestValues, ConstantValues.BP_CameraInspectionComplete, "CameraInspectionComplete"),
                ["CD_B1_AllDataReadComp"] = ReadBool(latestValues, ConstantValues.BP_CD_B1_AllDataReadComp, "CD_B1_AllDataReadComp"),
                ["CD_B2_AllDataReadComp"] = ReadBool(latestValues, ConstantValues.BP_CD_B2_AllDataReadComp, "CD_B2_AllDataReadComp"),
                ["CD_B3_AllDataReadComp"] = ReadBool(latestValues, ConstantValues.BP_CD_B3_AllDataReadComp, "CD_B3_AllDataReadComp"),
                ["TearingComplete"] = ReadBool(latestValues, ConstantValues.BP_TearingComplete, "TearingComplete"),
                ["FlippingComplete"] = ReadBool(latestValues, ConstantValues.BP_FlippingComplete, "FlippingComplete"),
            };

            // Detect false→true transitions
            foreach (var kvp in currentStates)
            {
                var signalName = kvp.Key;
                var currentValue = kvp.Value;

                if (_previousTriggerStates.TryGetValue(signalName, out var previousValue))
                {
                    // Rising edge: was false, now true
                    if (!previousValue && currentValue)
                    {
                        risingEdges.Add(signalName);
                    }
                }
            }

            // Update previous states for next cycle
            foreach (var kvp in currentStates)
            {
                _previousTriggerStates[kvp.Key] = kvp.Value;
            }

            return risingEdges;
        }

        #endregion

        #region Batch Number Generation (Task 3.4)

        /// <summary>
        /// Generates a unique batch number in format BN-YYYYMMDD-NNN.
        /// Resets daily counter when date changes. Allows counter > 999.
        /// </summary>
        private int GenerateBatchNumber()
        {
            //var today = DateTime.Now.Date;

            //// Reset counter on new day
            //if (_lastCounterDate != today)
            //{
            //    _dailyCounter = 0;
            //    _lastCounterDate = today;
            //}

            _dailyCounter++;

            //var batchNumber = $"BN-{_dailyCounter:D2}";
            return _dailyCounter;
        }

        #endregion

        #region Private Helpers

        private void InitializePreviousTriggerStates()
        {
            // All triggers start as TRUE to prevent spurious rising edges at startup
            _previousTriggerStates["RobotPickDone"] = true;
            _previousTriggerStates["TT1IndexComplete"] = true;
            _previousTriggerStates["TransferDone"] = true;
            _previousTriggerStates["TransferActive"] = true;
            _previousTriggerStates["TT2Start"] = true;
            _previousTriggerStates["Robo2Done"] = true;
            _previousTriggerStates["CameraInspectionComplete"] = true;
            _previousTriggerStates["CD_B1_AllDataReadComp"] = true;
            _previousTriggerStates["CD_B2_AllDataReadComp"] = true;
            _previousTriggerStates["CD_B3_AllDataReadComp"] = true;
            _previousTriggerStates["TearingComplete"] = true;
            _previousTriggerStates["FlippingComplete"] = true;
        }

        #endregion
    }
}
