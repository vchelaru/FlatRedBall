using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.GlueView
{
    [Description("SelectionInterface")]
    public class SelectionInterfaceClient : MarshalByRefObject, ISelectionInterface
    {
        public void ExecuteScript(string script)
        {
            throw new NotImplementedException();
        }

        public void HighlightElement(string name)
        {
            throw new NotImplementedException();
        }

        public bool IsConnected()
        {
            throw new NotImplementedException();
        }

        public void LoadGluxFile(string file)
        {
            throw new NotImplementedException();
        }

        public void RefreshCurrentElement()
        {
            throw new NotImplementedException();
        }

        public void RefreshFile(string fileName)
        {
            throw new NotImplementedException();
        }

        public void RefreshGlueProject()
        {
            throw new NotImplementedException();
        }

        public void RefreshVariables()
        {
            throw new NotImplementedException();
        }

        public void SetState(string name)
        {
            throw new NotImplementedException();
        }

        public void ShowElement(string name)
        {
            throw new NotImplementedException();
        }

        public void UnloadGlux()
        {
            throw new NotImplementedException();
        }
    }
}
