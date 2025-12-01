using System.Text.Json;

namespace IPCSoftware.App.Helpers
{
    public static class JsonElementExtensions
    {
        public static double GetDoubleSafe(this JsonElement el)
        {
            return el.ValueKind switch
            {
                JsonValueKind.Number => el.GetDouble(),
                JsonValueKind.String => double.TryParse(el.GetString(), out var d) ? d : 0,
                _ => 0
            };
        }

        public static int GetIntSafe(this JsonElement el)
        {
            return el.ValueKind switch
            {
                JsonValueKind.Number => el.GetInt32(),
                JsonValueKind.String => int.TryParse(el.GetString(), out var i) ? i : 0,
                _ => 0
            };
        }

        public static string GetStringSafe(this JsonElement el)
        {
            return el.ValueKind == JsonValueKind.String
                ? el.GetString()
                : el.ToString();
        }
    }
}
