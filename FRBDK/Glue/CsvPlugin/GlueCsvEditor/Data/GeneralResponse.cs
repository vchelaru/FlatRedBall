using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlueCsvEditor.Data
{
    public enum Severity
    {
        Warning,
        Error
    }


    public class GeneralResponse
    {
        public Severity Severity { get; set; }

        public bool Succeeded { get; set; }

        public string Message { get; set; }
    }
}
