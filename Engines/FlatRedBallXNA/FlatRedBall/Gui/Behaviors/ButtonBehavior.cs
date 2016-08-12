using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui.Controls;

namespace FlatRedBall.Gui.Behaviors
{
    public class ButtonBehavior : BehaviorBase
    {
        const string Enabled = nameof(Enabled);
        const string Disabled = nameof(Disabled);
        const string Highlighted = nameof(Highlighted);
        const string Pushed = nameof(Pushed);

        public override void ApplyTo(IControl control)
        {
            control.RollOn += HandleRollOn;
            control.RollOff += HandleRollOff;
            control.Push += HandlePush;
            control.Click += HandleClick;
            control.EnabledChange += HandleEnabledChange;
        }

        void HandleRollOn(IWindow window)
        {
            if(window.Enabled)
            {
                var asControl = window as IControl;
                asControl.SetState(Highlighted);
            }
        }

        void HandleRollOff(IWindow window)
        {
            if (window.Enabled)
            {
                var asControl = window as IControl;
                asControl.SetState(Enabled);
            }
        }

        void HandlePush(IWindow window)
        {
            if(window.Enabled)
            {
                var asControl = window as IControl;
                asControl.SetState(Pushed);
            }
        }

        void HandleClick(IWindow window)
        {
            var asControl = window as IControl;
            if (asControl.Enabled)
            {
                if(window.HasCursorOver(GuiManager.Cursor))
                {
                    asControl.SetState(Highlighted);
                }
                else
                {
                    asControl.SetState(Enabled);
                }
            }
            else
            {
                asControl.SetState(Disabled);
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
