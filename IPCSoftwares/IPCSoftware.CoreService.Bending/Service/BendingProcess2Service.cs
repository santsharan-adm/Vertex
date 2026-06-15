using IPCSoftware.Core.Interfaces;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Datalogger;
using IPCSoftware.Devices.PLC;
using IPCSoftware.Shared.Models;
using IPCSoftware.Shared.Models.Bending;
using IPCSoftware.Shared.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace IPCSoftware.CoreService.Bending.Service
{
    public class BendingProcess2Service : IProcessLogic
    {
        private readonly IAppLogger _logger;
        private readonly IProductionDataLogger _prodLogger;
        private readonly PLCClientManager _plcManager;
        private readonly AlgorithmAnalysisService _algo;

        // Simple station map: index 1..9 are stations, index 10 is OutputTray
        private readonly object _stateLock = new();
        private readonly BatchModel[] _stations = new BatchModel[11]; // ignore index 0
        private readonly Queue<BatchModel> _createdQueue = new();

        // Store previous trigger states to detect rising edges
        private readonly Dictionary<int, bool> _prevTriggerState = new();

        // Thread-safe counter for assigning sequential batch numbers
        private int _nextBatchNumber = 0;

        /// <summary>
        /// Returns the current list of active batches (read-only).
        /// </summary>
        public IReadOnlyList<BatchModel> GetActiveBatches() => _stations.AsReadOnly();

        // --- Trigger / Tag IDs (adjust to actual tag numbers in your system) ---
        //private const int TR_Robot1Intake1 = 1;
        //private const int TR_InputInspectionDone = 2;
        //private const int TR_Robot1Intake2 = 3;
        //private const int TR_TT1Rotate = 4;
        //private const int TR_TransferUnitRun = 5;
        //private const int TR_TT2Rotate = 6;
        //private const int TR_Robot2Intake1 = 7;
        //private const int TR_Robot2Intake2 = 8;

        // Tag that carries QR code value when input inspection done (adjust id as needed)

        // New: four separate QR tag IDs (adjust to actual IDs in your system)
        //private const int TAG_InputQRCode1 = 100;
        //private const int TAG_InputQRCode2 = 101;
        //private const int TAG_InputQRCode3 = 102;
        //private const int TAG_InputQRCode4 = 103;

        public BendingProcess2Service(IAppLogger logger, IProductionDataLogger prodLogger, PLCClientManager plcManager, AlgorithmAnalysisService algo)
        {
            _logger = logger;
            _prodLogger = prodLogger;
            _plcManager = plcManager;
            _algo = algo;
        }

        public void Process(Dictionary<int, object> latestValues)
        {
            if (latestValues == null) return;

            // Helper: read boolean value for a tag id
            bool ReadBool(int id) =>
                latestValues.TryGetValue(id, out var v) && v != null && Convert.ToBoolean(v);

            // Helper: read QR code string by tag id
            string ReadQrById(int id)
            {
                if (latestValues.TryGetValue(id, out var v) && v != null)
                    return v.ToString();
                return null;
            }

            // Process each trigger and act only on rising edge
            ProcessTrigger(ConstantValues.TR_Robot1Intake1, ReadBool(ConstantValues.TR_Robot1Intake1), HandleRobot1Intake1);
            // For InputInspectionDone read four separate QR tags and pass them to handler
            ProcessTrigger(ConstantValues.TR_InputInspectionDone, ReadBool(ConstantValues.TR_InputInspectionDone),
                () => HandleInputInspectionDone(
                    ReadQrById(ConstantValues.TAG_InputQRCode1),
                    ReadQrById(ConstantValues.TAG_InputQRCode2),
                    ReadQrById(ConstantValues.TAG_InputQRCode3),
                    ReadQrById(ConstantValues.TAG_InputQRCode4)
                ));
            ProcessTrigger(ConstantValues.TR_Robot1Intake2, ReadBool(ConstantValues.TR_Robot1Intake2), HandleRobot1Intake2);
            ProcessTrigger(ConstantValues.TR_TT1Rotate, ReadBool(ConstantValues.TR_TT1Rotate), HandleTT1Rotate);
            ProcessTrigger(ConstantValues.TR_TransferUnitRun, ReadBool(ConstantValues.TR_TransferUnitRun), HandleTransferUnitRun);
            ProcessTrigger(ConstantValues.TR_TT2Rotate, ReadBool(ConstantValues.TR_TT2Rotate), HandleTT2Rotate);
            ProcessTrigger(ConstantValues.TR_Robot2Intake1, ReadBool(ConstantValues.TR_Robot2Intake1), HandleRobot2Intake1);
            ProcessTrigger(ConstantValues.TR_Robot2Intake2, ReadBool(ConstantValues.TR_Robot2Intake2), HandleRobot2Intake2);
        }

        private void ProcessTrigger(int id, bool currentState, Action handler)
        {
            _prevTriggerState.TryGetValue(id, out var prev);
            // rising edge: false or missing -> true
            if (!prev && currentState)
            {
                try
                {
                    handler();
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Error handling trigger {id}: {ex.Message}", LogType.Error);
                }
            }
            _prevTriggerState[id] = currentState;
        }

        private void HandleRobot1Intake1()
        {
            lock (_stateLock)
            {
                var batch = CreateNewBatch();
                _createdQueue.Enqueue(batch);
                if (_stations[1] == null)
                {
                    _stations[1] = batch;
                    _logger?.LogInfo($"Robot1Intake1: Created batch placed into Station1 (Id: {GetBatchId(batch)})", LogType.Audit);
                }
                else
                {
                    _logger?.LogWarning("Robot1Intake1: Station1 occupied, new batch remains queued.", LogType.Audit);
                }
            }
        }

        // Modified to accept four QR code strings
        private void HandleInputInspectionDone(string qr1, string qr2, string qr3, string qr4)
        {
            lock (_stateLock)
            {
                var target = _stations[1];
                if (target == null)
                {
                    _logger?.LogWarning("inputInspectionDone: No batch at Station1 to update QR codes.", LogType.Audit);
                    return;
                }

                // If all QR values are missing, also check legacy single tag as fallback
                if (string.IsNullOrEmpty(qr1) && string.IsNullOrEmpty(qr2) && string.IsNullOrEmpty(qr3) && string.IsNullOrEmpty(qr4))
                {
                    // Try legacy single value if present on TAG_InputQRCode
                    // (latestValues not available here; upstream Process should map TAG_InputQRCode into one of the above ids,
                    // but we keep this message to indicate missing data)
                    _logger?.LogWarning("inputInspectionDone: No QR code values found in latest values.", LogType.Audit);
                    return;
                }

                try
                {
                    SetBatchQrCodes(target, qr1, qr2, qr3, qr4);

                    _logger?.LogInfo($"inputInspectionDone: Updated batch at Station1 with QR codes (Id: {GetBatchId(target)}) Q1:{TrimForLog(qr1)} Q2:{TrimForLog(qr2)} Q3:{TrimForLog(qr3)} Q4:{TrimForLog(qr4)}", LogType.Audit);
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"inputInspectionDone: Error setting QR codes on batch at Station1: {ex.Message}", LogType.Error);
                }
            }
        }

        private string TrimForLog(string s)
        {
            if (string.IsNullOrEmpty(s)) return "<null>";
            if (s.Length <= 64) return s;
            return s.Substring(0, 64) + "...";
        }

        private void HandleRobot1Intake2()
        {
            lock (_stateLock)
            {
                MoveIfPresent(1, 2, "Robot1Intake2");
            }
        }

        private void HandleTT1Rotate()
        {
            lock (_stateLock)
            {
                // Move in descending order to avoid overwrite
                MoveIfPresent(4, 5, "TT1Rotate");
                MoveIfPresent(3, 4, "TT1Rotate");
                MoveIfPresent(2, 3, "TT1Rotate");
            }
        }

        private void HandleTransferUnitRun()
        {
            lock (_stateLock)
            {
                MoveIfPresent(5, 6, "TransferUnitRun");
            }
        }

        private void HandleTT2Rotate()
        {
            lock (_stateLock)
            {
                MoveIfPresent(7, 8, "TT2Rotate");
                MoveIfPresent(6, 7, "TT2Rotate");
            }
        }

        private void HandleRobot2Intake1()
        {
            lock (_stateLock)
            {
                MoveIfPresent(8, 9, "Robot2Intake1");
            }
        }

        private void HandleRobot2Intake2()
        {
            lock (_stateLock)
            {
                // Move station 9 to output tray (index 10)
                if (_stations[9] == null)
                {
                    _logger?.LogWarning("Robot2Intake2: No batch at Station9 to move to OutputTray.", LogType.Audit);
                    return;
                }

                if (_stations[10] != null)
                {
                    // If output slot is occupied, try to flush it first or log and skip
                    _logger?.LogWarning("Robot2Intake2: Output tray occupied, cannot move batch from Station9.", LogType.Audit);
                    return;
                }

                var batch = _stations[9];
                _stations[9] = null;
                _stations[10] = batch;
                _logger?.LogInfo($"Robot2Intake2: Moved batch (Id: {GetBatchId(batch)}) from Station9 to OutputTray", LogType.Audit);

                // On arrival to output, create and append production record
                try
                {
                    var prodRecord = CreateProductionRecordFromBatch(batch);
                    if (prodRecord != null)
                    {
                        //Tobe changed to append to actual production data logger instead of just logging - BMK-11-June-2026
                        // _prodLogger?.AppendRecord(prodRecord);
                        _logger?.LogInfo($"Robot2Intake2: Appended production record for batch (Id: {GetBatchId(batch)}) QR:{prodRecord.TwoDCode}", LogType.Audit);
                    }
                    else
                    {
                        _logger?.LogWarning("Robot2Intake2: Could not build production record for batch.", LogType.Audit);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Robot2Intake2: Error appending production record: {ex.Message}", LogType.Error);
                }

                // Clear output slot after logging to keep array free (comment this out if you want to keep it)
                _stations[10] = null;
            }
        }

        // Helper utilities

        private BatchModel CreateNewBatch()
        {
            // Rely on project's BatchModel having parameterless ctor
            var batch = Activator.CreateInstance(typeof(BatchModel)) as BatchModel;
            if (batch == null) return null;
            batch.BatchNumber = GetNextBatchNumber();           

            return batch;
        }

        private int GetNextBatchNumber() => Interlocked.Increment(ref _nextBatchNumber);

        private void MoveIfPresent(int fromIndex, int toIndex, string reason)
        {
            if (fromIndex <= 0 || fromIndex >= _stations.Length || toIndex <= 0 || toIndex >= _stations.Length)
            {
                _logger?.LogWarning($"Invalid station indices in MoveIfPresent: {fromIndex}->{toIndex}", LogType.Error);
                return;
            }

            var src = _stations[fromIndex];
            if (src == null)
            {
                _logger?.LogInfo($"{reason}: No batch at Station{fromIndex} to move.", LogType.Audit);
                return;
            }

            if (_stations[toIndex] != null)
            {
                _logger?.LogWarning($"{reason}: Destination Station{toIndex} occupied; cannot move batch Id:{GetBatchId(src)}.", LogType.Audit);
                return;
            }

            _stations[toIndex] = src;
            _stations[fromIndex] = null;
            _logger?.LogInfo($"{reason}: Moved batch Id:{GetBatchId(src)} from Station{fromIndex} to Station{toIndex}", LogType.Audit);
        }

        // New: set four QR code fields/properties on the BatchModel
        private void SetBatchQrCodes(BatchModel batch, string qr1, string qr2, string qr3, string qr4)
        {
            if (batch == null) return;

            // Prefer direct typed access if possible
            try
            {
                // If the runtime type is the known BatchModel, set properties directly
                if (batch is BatchModel bm)
                {
                    if (!string.IsNullOrEmpty(qr1)) bm.QrCode1 = qr1;
                    if (!string.IsNullOrEmpty(qr2)) bm.QrCode2 = qr2;
                    if (!string.IsNullOrEmpty(qr3)) bm.QrCode3 = qr3;
                    if (!string.IsNullOrEmpty(qr4)) bm.QrCode4 = qr4;
                    return;
                }
            }
            catch
            {
                // fallback to reflection below
            }
        }

        private string GetBatchId(BatchModel batch)
        {
            if (batch == null) return "<null>";
            try
            {
                var t = batch.GetType();

                // Prefer BatchNumber if present
                var batchNumProp = t.GetProperty("BatchNumber", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (batchNumProp != null)
                {
                    var val = batchNumProp.GetValue(batch);
                    if (val != null) return $"Batch#{val}";
                }

                var idProp = t.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (idProp != null) return idProp.GetValue(batch)?.ToString() ?? "<no-id>";
                var nameProp = t.GetProperty("Name", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (nameProp != null) return nameProp.GetValue(batch)?.ToString() ?? "<no-name>";
            }
            catch { /* swallow */ }
            return batch.ToString();
        }

        private ProductionDataRecordBending CreateProductionRecordFromBatch(BatchModel batch)
        {
            if (batch == null) return null;
            var rec = new ProductionDataRecordBending();
            // Set TwoDCode using reflection (try same candidate names)
            string code = null;
            var t = batch.GetType();
            var candidates = new[] { "TwoDCode", "QRCode", "Code", "TwoD", "TwoD_CODE" };
            foreach (var n in candidates)
            {
                var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null && p.PropertyType == typeof(string))
                {
                    code = p.GetValue(batch) as string;
                    break;
                }
                var f = t.GetField(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (f != null && f.FieldType == typeof(string))
                {
                    code = f.GetValue(batch) as string;
                    break;
                }
            }

            rec.TwoDCode = code ?? string.Empty;
            // Stations array is optional; leave default or fill with nulls/empty as needed.
            return rec;
        }
    }
}
