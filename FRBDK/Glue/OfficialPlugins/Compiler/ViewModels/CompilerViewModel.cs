using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.Compiler.ViewModels
{
    public class CompilerViewModel : ViewModel
    {
        public bool AutoBuildContent { get; set; }

        public string Configuration { get; set; }
    }
}
