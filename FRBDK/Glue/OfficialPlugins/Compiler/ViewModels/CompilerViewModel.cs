using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OfficialPlugins.Compiler.ViewModels
{
    public class CompilerViewModel : ViewModel
    {

        public bool AutoBuildContent { get; set; }

        public string Configuration { get; set; }

        Visibility compileContentButtonVisibility;
        public Visibility CompileContentButtonVisibility
        {
            get { return compileContentButtonVisibility; }
            set { base.ChangeAndNotify(ref compileContentButtonVisibility, value); }
        }
    }
}
