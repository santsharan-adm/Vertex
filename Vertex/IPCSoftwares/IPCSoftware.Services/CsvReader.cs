using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace IPCSoftware.Services
{

public static class CsvReader
    {
        public static List<string[]> Read(string filePath)
        {
            var rows = new List<string[]>();

            if (!File.Exists(filePath)) return rows;

            using (var reader = new StreamReader(filePath))
            {
                bool isHeader = true;

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (isHeader)
                    {
                        isHeader = false;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // FIX: Don't use line.Split(',')
                    // Use Regex to split only on commas that are NOT inside quotes
                    string[] parts = SplitCsvLine(line);

                    rows.Add(parts);
                }
            }

            return rows;
        }

        private static string[] SplitCsvLine(string line)
        {
            // This Regex finds commas that are followed by an even number of quotes (meaning outside of a string)
            string pattern = ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)";

            string[] rawParts = Regex.Split(line, pattern);

            // Clean up the quotes from the results
            for (int i = 0; i < rawParts.Length; i++)
            {
                string part = rawParts[i].Trim();

                // If it starts and ends with quotes, remove them and unescape double quotes
                if (part.StartsWith("\"") && part.EndsWith("\"") && part.Length >= 2)
                {
                    part = part.Substring(1, part.Length - 2).Replace("\"\"", "\"");
                }
                rawParts[i] = part;
            }

            return rawParts;
        }
    }

}
