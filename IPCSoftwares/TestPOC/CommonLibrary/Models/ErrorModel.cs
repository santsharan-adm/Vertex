using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Models
{
   
    public class ErrorModel
    {
        public int Id { get; set; }

        public Severity Severity { get; set; }
        public string Message { get; set; }
        public string Description { get; set; }
        public string Remark { get; set; }

        public ErrorModel(int id, Severity severity, string message, string description, string remark) 
        {
            Id = id;
            Severity = severity;
            Message = message;
            Description = description;
            Remark = remark;
        }

        public static string GetErrorExceptionDetail(Exception exception)
        {
            string detail = string.Empty;
            if (exception != null)
            {
                detail = exception.Message + GetErrorExceptionDetail(exception.InnerException);
            }

            return detail;
                
        }
    }

    public enum Severity
    {
        Verbose,
        Information,
        Warning,
        Error,
    }

    public class Errors :ObservableCollection<ErrorModel> { }


}
