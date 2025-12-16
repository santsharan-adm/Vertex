using System.Text.Json;

namespace IPCSoftware.App.Helpers
{
    /// Provides safe extension methods for extracting typed values 
    /// (double, int, string) from a JsonElement without throwing exceptions.
    public static class JsonElementExtensions
    {
        /// Safely gets a double value from a JsonElement.
        /// Handles both numeric and string types (e.g. "12.5" or 12.5).
        /// Returns 0 if parsing fails or value type is unsupported.
        public static double GetDoubleSafe(this JsonElement el)
        {
            return el.ValueKind switch
            {
                JsonValueKind.Number => el.GetDouble(),                 // Direct number type
                JsonValueKind.String => double.TryParse(el.GetString(), out var d) ? d : 0,
                _ => 0                                                 // Default if not a valid type
            };
        }
        /// Safely gets an integer value from a JsonElement.
        /// Handles both numeric and string types (e.g. "25" or 25).
        /// Returns 0 if parsing fails or value type is unsupported.
        public static int GetIntSafe(this JsonElement el)
        {
            return el.ValueKind switch
            {
                JsonValueKind.Number => el.GetInt32(),                 // Direct number type   
                JsonValueKind.String => int.TryParse(el.GetString(), out var i) ? i : 0,
                _ => 0                                                // Default if not a valid type
            };
        }

        public static string GetStringSafe(this JsonElement el)
        {
            return el.ValueKind == JsonValueKind.String
                ? el.GetString()                                    // Direct string type
                : el.ToString();                                    // Fallback for other JSON types (like numbers or bools)
        }
    }
    }
}
