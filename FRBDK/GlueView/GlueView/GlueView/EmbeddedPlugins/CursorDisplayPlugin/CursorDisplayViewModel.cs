using GlueView.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueView.EmbeddedPlugins.CursorDisplayPlugin
{
    class CursorDisplayViewModel : ViewModel
    {
        float zValue;
        public float ZValue
        {
            get { return zValue; }
            set { base.SetProperty(ref zValue, value); }
        }

        string cursorText;
        public string CursorText
        {
            get { return cursorText; }
            set { base.SetProperty(ref cursorText, value); }
        }


    }
}
