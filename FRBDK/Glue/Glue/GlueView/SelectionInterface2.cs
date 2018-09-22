using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueView2.EmbeddedPlugins.SelectionFromGlue
{
    [Description("SelectionInterface2")]
    public class SelectionInterface2 : MarshalByRefObject
    {
        public void LoadGluxFile(string file) { }


        public void UnloadGlux()
        {
        }

        public void RefreshGlueProject()
        {
        }

        public void RefreshFile(string fileName)
        {
        }

        public void ShowElement(string name)
        {
        }

        public void HighlightElement(string name)
        {
        }

        public void SetState(string name)
        {
        }

        public void RefreshCurrentElement()
        {
        }

        public void RefreshVariables()
        {
        }

    }
}
