using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.CreatedClass;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.ViewModels
{
    public class CustomClassViewModel : ViewModel
    {
        public void HandleUseThisClassClick(CustomClassSave classToUse)
        {
            ReferencedFileSave currentReferencedFile = GlueState.Self.CurrentReferencedFileSave;

            if (GlueState.Self.CurrentReferencedFileSave != null)
            {
                if (classToUse != null)
                {
                    CustomClassController.Self.SetCsvRfsToUseCustomClass(currentReferencedFile, classToUse, force:false);
                }

            }
        }


    }
}
