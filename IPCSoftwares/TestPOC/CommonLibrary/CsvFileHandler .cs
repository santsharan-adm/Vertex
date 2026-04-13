using CommonLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary
{
    public class CsvFileHandler : IFileHandler
    {
        string filePath;
        public CsvFileHandler(string filePath)
        {
            this.filePath = filePath;
        }
        public Dictionary<int, string[]> ReadFile()
        {
            // Implementation for loading CSV files

            Dictionary<int, string[]> csvData = new Dictionary<int, string[]>();
            
            using (var reader = new StreamReader(filePath))
            {
                int i = 0;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (i > 0)// skip header
                    {                        
                        var values = line.Split(','); // Use ',' or your delimiter
                        csvData.Add(i, values);
                    }
                    i++;
                }
            }

            return csvData;

        }


        public void WriteCsv(Dictionary<int, string[]> data, char delimiter = ',')
        {
            using (var writer = new StreamWriter(filePath))
            {
                foreach (var row in data)
                {
                    writer.WriteLine(string.Join(delimiter, row.Value));
                }
            }
        }

    }
}
