/******************************************************************************
 * Project      : IPCSoftware-AOI /Bending
 * Module       : File Handling Interface
 * File Name    : IFileHandler.cs
 * Author       : Rishabh
 * Organization : Motherson Technology Service Limited
 * Created Date : 2026-04-25
 *
 * Description  :
 * Interface contract for file handling operations (CSV, JSON, XML, etc.)
 * Enables loose coupling between loaders and file format implementations.
 * Allows different file format handlers to be swapped at runtime through
 * dependency injection, supporting future extensions without modifying
 * existing loader classes.
 *
 * Change History:
 * ---------------------------------------------------------------------------
 * Date        Author        Version     Description
 * ---------------------------------------------------------------------------
 * 2026-04-25  Rishabh       1.0         Initial creation
 *                                       Defines core file handling contract
 *
 ******************************************************************************/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPCSoftware.Core.Interfaces
{
    public interface IFileHandler
    {
       public string Getversion(string filepath);
       public List<string[]> Read(string filePath);
       //public string[] SplitCsvLine(string line);
       public string GetHeader(string filepath);
       public string EscapeCsv(string value);

       public Task WriteCsv(string filepath, string content);        //Added by Rishabh - date - 25/04/2026//

        public bool IsFileExists(string file);                        //Added by Rishabh - date - 25/04/2026//

    }
}
