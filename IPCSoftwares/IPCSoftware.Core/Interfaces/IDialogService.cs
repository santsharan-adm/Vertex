using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Core.Interfaces
{
    public interface IDialogService
    {
        void ShowMessage(string message, string DialogBoxName = "Information");
        void ShowWarning(string message);

        bool ShowYesNo(string message, string title = "Confirm");

        UserInputResult UserInput(string field1title, string field1value, string field2title, string field2value);

    }

    public class UserInputResult
    {
        public bool Confirmed { get; set; }
        public string Field1Value { get; set; }
        public string Field2Value { get; set; }
    }
}
