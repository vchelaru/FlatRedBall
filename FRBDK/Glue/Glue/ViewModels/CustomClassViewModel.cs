using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.CreatedClass;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.ViewModels
{
    public class CustomClassViewModel : ViewModel
    {
        public void HandleUseThisClassClick(CustomClassSave classToUse)
        {
            ReferencedFileSave currentReferencedFile = EditorLogic.CurrentReferencedFile;

            if (EditorLogic.CurrentReferencedFile != null)
            {
                if (classToUse != null)
                {
                    CustomClassController.Self.SetCsvRfsToUseCustomClass(currentReferencedFile, classToUse);
                }

            }
        }


    }
}
