using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Events;
using System.Windows.Forms;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.SetVariable
{
    class ScreenSaveSetVariableLogic
    {
        internal void ReactToScreenChangedValue(string changedMember, object oldValue)
        {
            ScreenSave screenSave = EditorLogic.CurrentScreenSave;

            #region Name

            if (changedMember == "ClassName")
            {
                ReactToChangedClassName(oldValue, screenSave);
            }

            #endregion

            #region BaseScreen

            else if (changedMember == "BaseScreen")
            {
                if (ProjectManager.VerifyInheritanceGraph(screenSave) == ProjectManager.CheckResult.Failed)
                {
                    screenSave.BaseScreen = (string)oldValue;
                }
                else
                {
                    screenSave.UpdateFromBaseType();
                }
            }

            #endregion


            EventResponseSave eventSave = screenSave.GetEvent(changedMember);

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
