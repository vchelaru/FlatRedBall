using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Events;
using System.Windows.Forms;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using GlueFormsCore.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.SetVariable
{
    class ScreenSaveSetVariableLogic
    {
        internal void ReactToScreenChangedValue(string changedMember, object oldValue)
        {
            ScreenSave screenSave = GlueState.Self.CurrentScreenSave;

            ReactToScreenPropertyChanged(screenSave, changedMember, oldValue);
        }


        public void ReactToScreenPropertyChanged(ScreenSave screenSave, string propertyName, object oldValue)
        {
            #region Name

            if (propertyName == "ClassName")
            {
                ReactToChangedClassName(oldValue, screenSave);
            }

            #endregion

            #region BaseScreen

            else if (propertyName == "BaseScreen")
            {
                InheritanceManager.ReactToChangedBaseScreen(oldValue, screenSave);
            }

            #endregion

            // Jan 5, 2023
            // Vic asks - should this call plugin code? Need to trace the code when setting properties
            // through the property grid to see what it does, and re-route it through this function.

            EventResponseSave eventSave = screenSave.GetEvent(propertyName);

            if (eventSave != null)
            {
                //if (!string.IsNullOrEmpty(eventSave.InstanceMethod) && EditorLogic.CurrentElement != null)
                //{
                //    InsertMethodCallInElementIfNecessary(EditorLogic.CurrentScreenSave, eventSave.InstanceMethod);
                //}
            }
        }

        private static void ReactToChangedClassName(object oldValue, ScreenSave screenSave)
        {

        }


    }
}
