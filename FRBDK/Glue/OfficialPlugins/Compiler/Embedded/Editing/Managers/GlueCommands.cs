using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GlueControl.Dtos;


namespace GlueControl.Managers
{
    internal class GlueCommands : GlueCommandsStateBase
    {
        #region Fields/properties

        public static GlueCommands Self { get; }

        public GluxCommands GluxCommands { get; private set; }

        public GenerateCodeCommands GenerateCodeCommands { get; private set; }

        #endregion

        #region  Constructors

        static GlueCommands() => Self = new GlueCommands();

        public GlueCommands()
        {
            GluxCommands = new GluxCommands();
            GenerateCodeCommands = new GenerateCodeCommands();
        }

        #endregion

        public void PrintOutput(string output)
        {
            SendMethodCallToGame(nameof(PrintOutput), output);
        }

        private Task<object> SendMethodCallToGame(string caller = null, params object[] parameters)
        {
            return base.SendMethodCallToGame(new GlueCommandDto(), caller, parameters);
        }
    }
}
