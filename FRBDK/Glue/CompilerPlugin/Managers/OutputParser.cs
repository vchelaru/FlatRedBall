using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompilerPlugin.Managers
{
    public enum OutputType
    {
        Info,
        Warning,
        Error
    }


    public class OutputParser
    {
        public OutputType GetOutputType(string text)
        {
            if(text.Contains("): warning CS"))
            {
                return OutputType.Warning;
            }
            else if(text.Contains(": error CS"))
            {
                return OutputType.Error;
            }
            else
            {
                return OutputType.Info;
            }
        }
    }
}
