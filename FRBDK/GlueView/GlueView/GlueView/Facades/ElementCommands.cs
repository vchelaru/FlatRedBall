using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue;

namespace GlueView.Facades
{
    public class ElementCommands
    {
        public void ShowState(StateSave stateSave)
        {
            if (GluxManager.CurrentElement != null)
            {
                // We used to reset positioned values 
                // because states would get set, undo, then
                // a different state would get set.  Now all
                // current states are set, including initial values
                // by the State plugin, therefore we shouldn't reset
                // position values unless there is no state.
                GluxManager.CurrentElement.SetState(stateSave, stateSave == null, GluxManager.CurrentElement.AssociatedIElement);
            }

        }

        public void ShowState(string stateSave)
        {
            if (GluxManager.CurrentElement != null)
            {
                GluxManager.CurrentElement.SetState(stateSave, false);
            }

        }

        public void ReloadCurrentElement()
        {
            if (GluxManager.CurrentElement != null)
            {
                GluxManager.ShowElement(GluxManager.CurrentElement.Name);
            }
            else
            {
                GluxManager.ClearEngine();

            }
        }
    }
}
