using System.Collections.Generic;
using System.IO;

namespace IPCSoftware.CoreService.Services
{
    public static class CsvReader
    {
        public static List<string[]> Read(string filePath)
        {
            var rows = new List<string[]>();

            using (var reader = new StreamReader(filePath))
            {
                bool isHeader = true;

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (isHeader)
                    {
                        isHeader = false;
                        continue; // skip header row
                    }

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    rows.Add(line.Split(','));
                }
            }

            return rows;
        }
    }
}
