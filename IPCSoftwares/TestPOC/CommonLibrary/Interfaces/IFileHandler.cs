using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Interfaces
{
    public interface IFileHandler
    {
        Dictionary<int, string[]> ReadFile();
        void WriteCsv( Dictionary<int, string[]> data, char delimiter = ',');
    }
}
