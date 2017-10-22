using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueTestProject.GumRuntimes
{
    public partial class ComponentWithCustomInitializeRuntime
    {
        partial void CustomInitialize()
        {
            this.Width = 20;
        }
    }
}
