using FlatRedBall.Glue.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.PostProcessingPlugin
{
    public class PostProcessSave
    {
        // The .cs file containing the class that implements IPostProcess
        public string CodeFile;
        // The .fx files - Glue will track these references
        public List<string> FxFiles = new List<string>();
        // The variables - these will be assigned on the IPostProcess class, and will show up in the editor
        public List<VariableDefinition> Variables = new List<VariableDefinition>();
    }
}
