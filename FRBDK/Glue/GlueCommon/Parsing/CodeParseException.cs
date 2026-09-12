using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Parsing
{
    public class CodeParseException : Exception
    {
        public CodeParseException()
            : base()
        {

        }

        public CodeParseException(string message)
            : base(message)
        {

        }
    }
}
