using System.Text.Json;

namespace IPCSoftware.App.Helpers
{
    /// Extension methods for JsonElement to safely extract values
    /// without throwing exceptions.
    public static class JsonElementExtensions
    {
        /// Safely gets a double value from a JsonElement.
        /// Supports both Number and String JSON types.
        public static double GetDoubleSafe(this JsonElement el)
        {
            return el.ValueKind switch
            {
                // If JSON value is a number, return it directly
                JsonValueKind.Number => el.GetDouble(),

                // If JSON value is a string, try to parse it as double
                JsonValueKind.String => double.TryParse(el.GetString(), out var d) ? d : 0,
                // For all other JSON types (Null, Object, Array, etc.)
                _ => 0
            };
        }
        /// Safely gets an integer value from a JsonElement.
        /// Supports both Number and String JSON types.

        public static int GetIntSafe(this JsonElement el)
        {
            return el.ValueKind switch
            {
                // If JSON value is a number, return it directly
                JsonValueKind.Number => el.GetInt32(),
                // If JSON value is a string, try to parse it as integer
                JsonValueKind.String => int.TryParse(el.GetString(), out var i) ? i : 0,
                // For all other JSON types
                _ => 0
            };
        }

        /// Safely gets a string value from a JsonElement.
       

        /// String value if JSON type is String,
        /// otherwise the JSON value converted to string
        public static string GetStringSafe(this JsonElement el)
        {
            // Return string directly if JSON type is String
            return el.ValueKind == JsonValueKind.String
                ? el.GetString()
                : el.ToString();
        }
    }
}
