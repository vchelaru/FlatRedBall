using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using Gum.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumPlugin.CodeGeneration
{
    class CustomCodeGenerator : Singleton<CustomCodeGenerator>
    {
        public string GetCustomCodeTemplateCode(ElementSave element)
        {
            // Example:
            /*
using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopGlForms.GumRuntimes.DefaultForms
{
    public partial class ButtonRuntime
    {
        partial void CustomInitialize()
        {

        }
    }
}

             */

            var codeBlockBase = new CodeBlockBaseNoIndent(null);
            ICodeBlock codeBlock = codeBlockBase;
            
            var toReturn = codeBlock;
            codeBlock.Line("using System;");
            codeBlock.Line("using System.Collections.Generic;");
            codeBlock.Line("using System.Linq;");
            codeBlock.Line();
            codeBlock = codeBlock.Namespace(
                GueDerivingClassCodeGenerator.Self.GetFullRuntimeNamespaceFor(element));
            {
                string runtimeClassName =
                    GueDerivingClassCodeGenerator.Self.GetUnqualifiedRuntimeTypeFor(element);

                codeBlock = codeBlock.Class("public partial", runtimeClassName);
                {
                    codeBlock = codeBlock.Function("partial void", "CustomInitialize");
                }
            }
            return toReturn.ToString();
        }

    }
}
