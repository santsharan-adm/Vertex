using System;
using System.Collections.Generic;
using System.Linq;

namespace IPCSoftware.Shared.Models.Bending
{
    public class BendingProcessDashboardModel : ObservableObjectVM
    {
        private List<BatchModel> _activeBatches = new();

        /// <summary>
        /// Active batches sorted descending by Stage (Stage 9 first, Stage 1 last).
        /// </summary>
        public List<BatchModel> ActiveBatches
        {
            get => _activeBatches;
            set => SetProperty(ref _activeBatches, value);
        }

        /// <summary>
        /// Re-sorts the provided FIFO queue by Stage descending and notifies the UI.
        /// </summary>
        public void UpdateFrom(IReadOnlyList<BatchModel> fifoQueue)
        {
            if (fifoQueue == null || fifoQueue.Count == 0)
            {
                ActiveBatches = new List<BatchModel>();
                return;
            }

            ActiveBatches = fifoQueue
                .OrderByDescending(b => b.Stage)
                .ToList();
        }
    }
}
