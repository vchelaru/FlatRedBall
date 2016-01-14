using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlueViewOfficialPlugins.Scripting
{
    public class StateSettingException : Exception
    {
        public StateSettingException()
            : base()
        {

        }

        public StateSettingException(string message)
            : base(message)
        {

        }
    }
}
