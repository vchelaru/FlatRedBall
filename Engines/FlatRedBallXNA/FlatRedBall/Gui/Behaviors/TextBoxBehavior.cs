using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui.Controls;

namespace FlatRedBall.Gui.Behaviors
{
    public class TextBoxBehavior : BehaviorBase
    {
        const string Enabled = nameof(Enabled);
        const string Disabled = nameof(Disabled);

        const string Highlighted = nameof(Highlighted);

        const string Selected = nameof(Selected);

        const string HighlightedSelected = nameof(HighlightedSelected);

        public override void ApplyTo(IControl control)
        {
            control.RollOn += HandleRollOn;
            control.RollOff += HandleRollOff;
            control.Push += HandlePush;
            //control.Click += HandleClick;
            control.EnabledChange += HandleEnabledChange;

            var asSelectable = control as ISelectable;

#if DEBUG
            if(asSelectable == null)
            {
                throw new System.Exception("Argument control needs to implement ISelectable");
            }
#endif
            asSelectable.IsSelectedChanged += HandleSelectionChanged;
        }

        void HandleRollOn(IWindow window)
        {
            if (window.Enabled)
            {
                var asControl = ((IControl)window);
                var asSelectable = ((ISelectable)window);

                if(asSelectable.IsSelected)
                {
                    asControl.SetState(HighlightedSelected);
                }
                else
                {
                    asControl.SetState(Highlighted);
                }
            }
        }

        void HandleRollOff(IWindow window)
        {
            if (window.Enabled)
            {
                var asControl = ((IControl)window);

                var asSelectable = ((ISelectable)window);

                if (asSelectable.IsSelected)
                {
                    asControl.SetState(Selected);
                }
                else
                {
                    asControl.SetState(Enabled);
                }
            }
        }

        void HandlePush(IWindow window)
        {
            if (window.Enabled)
            {
                var asSelectable = ((ISelectable)window);

                // this raises the selection change event
                asSelectable.IsSelected = true;
            }
        }

        void HandleSelectionChanged(object sender, EventArgs args)
        {
            // need to consider enabled vs. disabled here?
            var selectable = (ISelectable)sender;
            var control = (IControl)sender;
            bool isHighlighted = control.HasCursorOver(GuiManager.Cursor);
            if (selectable.IsSelected)
            {
                if (isHighlighted)
                {
                    control.SetState(HighlightedSelected);
                }
                else
                {
                    control.SetState(Selected);
                }
            }
            else // not selected
            {
                if(isHighlighted)
                {
                    control.SetState(Highlighted);
                }
                else
                {
                    control.SetState(Enabled);
                }
            }
        }

        void HandleEnabledChange(IWindow window)
        {
            var asControl = window as IControl;
            if (asControl.Enabled)
            {
                asControl.SetState(Enabled);
            }
            else
            {
                asControl.SetState(Disabled);
            }
        }
    }
}
