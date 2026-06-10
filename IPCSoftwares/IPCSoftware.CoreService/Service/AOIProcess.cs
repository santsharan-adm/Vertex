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
    public class AOIProcessService: IProcessLogic
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
        public IReadOnlyList<BatchModel> GetActiveBatches() => _fifoQueue.AsReadOnly();



        public AOIProcessService(IAppLogger logger, IProductionDataLogger prodLogger, PLCClientManager plcManager, AlgorithmAnalysisService algo)
        {
            _logger = logger;
            _prodLogger = prodLogger;
            _plcManager = plcManager;
            _algo = algo;

            // Initialize all previous trigger states to TRUE (prevents spurious edges at startup per Req 11.4)
            
        }

        /// <summary>
        /// Main entry point — called every 500ms from the processing loop.
        /// Single-trigger conveyor logic: RobotPickDone advances ALL batches + creates new one.
        /// </summary>
        public void Process(Dictionary<int, object> latestValues)
        {
            
        }
        

        /// <summary>
        /// Records/snapshots process data when a batch arrives at a data-recording stage.
        /// </summary>
       
    }
}
