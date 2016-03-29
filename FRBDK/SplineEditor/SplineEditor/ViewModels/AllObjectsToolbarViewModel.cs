using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SplineEditor.ViewModels
{
    public class AllObjectsToolbarViewModel : ViewModel
    {
        bool showDeselectedSplines = true; 
        public bool ShowDeselectedSplines
        {
            get
            {
                return showDeselectedSplines;
            }
            set
            {
                base.ChangeAndNotify(ref showDeselectedSplines, value, nameof(ShowDeselectedSplines));
            }
        }
    }
}
