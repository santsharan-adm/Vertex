using System;
using System.Collections.Generic;
using System.Linq;

namespace IPCSoftware.Shared.Models.Bending
{
    public class BendingProcessDashboardModel : ObservableObjectVM
    {
        private IReadOnlyList<BatchModel> _activeBatches = Array.Empty<BatchModel>();

        /// <summary>
        /// Active batches sorted descending by Stage (Stage 9 first, Stage 1 last).
        /// </summary>
        public IReadOnlyList<BatchModel> ActiveBatches
        {
            get => _activeBatches;
            private set => SetProperty(ref _activeBatches, value);
        }

        /// <summary>
        /// Re-sorts the provided FIFO queue by Stage descending and notifies the UI.
        /// </summary>
        /// <param name="fifoQueue">Current list of active batches from the FIFO queue.</param>
        public void UpdateFrom(IReadOnlyList<BatchModel> fifoQueue)
        {
            if (fifoQueue == null || fifoQueue.Count == 0)
            {
                ActiveBatches = Array.Empty<BatchModel>();
                return;
            }

            ActiveBatches = fifoQueue
                .OrderByDescending(b => b.Stage)
                .ToList()
                .AsReadOnly();
        }
    }
}
