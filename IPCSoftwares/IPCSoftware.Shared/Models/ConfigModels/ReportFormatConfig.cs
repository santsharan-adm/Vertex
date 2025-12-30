using System.Collections.Generic;

namespace IPCSoftware.Shared.Models
{
    public class ReportFormatConfig
    {
        public string Name { get; set; }
        public List<string> SelectedColumns { get; set; } = new();
    }
}
