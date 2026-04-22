using System.Collections.ObjectModel;

namespace IPCSoftware.App.Bending.Models
{
    /// <summary>
    /// Tracks the current state of all TurnTable positions and process flow
    /// </summary>
    public class SystemStateModel
    {
        // TT-1 Positions (4 positions: Entry, Bending1, Bending2, Transfer)
        public string TT1_Pos0 { get; set; } // Entry
        public string TT1_Pos1 { get; set; } // Bending 1
        public string TT1_Pos2 { get; set; } // Bending 2
        public string TT1_Pos3 { get; set; } // Transfer

        // TT-2 Positions (3 positions: Receive, Bending3, Exit)
        public string TT2_Pos0 { get; set; } // Receive
        public string TT2_Pos1 { get; set; } // Bending 3
        public string TT2_Pos2 { get; set; } // Exit

        public string NextLotToEnter { get; set; }
        public ObservableCollection<string> CompletedLots { get; set; }
        public ObservableCollection<string> ProcessEvents { get; set; }

        public int CycleCounter { get; set; }
        public int TimeCounter { get; set; }

        public SystemStateModel()
        {
            CompletedLots = new ObservableCollection<string>();
            ProcessEvents = new ObservableCollection<string>();
            TT1_Pos0 = null;
            TT1_Pos1 = null;
            TT1_Pos2 = null;
            TT1_Pos3 = null;
            TT2_Pos0 = null;
            TT2_Pos1 = null;
            TT2_Pos2 = null;
            NextLotToEnter = "001";
            CycleCounter = 0;
            TimeCounter = 0;
        }
    }
}
