using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui.Controls;

namespace FlatRedBall.Gui.Behaviors
{
    public class ToggleBehavior : BehaviorBase
    {
        const string EnabledOn = nameof(EnabledOn);
        const string EnabledOff = nameof(EnabledOff);

        const string DisabledOn = nameof(DisabledOn);
        const string DisabledOff = nameof(DisabledOff);

        const string HighlightedOn = nameof(HighlightedOn);
        const string HighlightedOff = nameof(HighlightedOff);

        const string PushedOn = nameof(PushedOn);
        const string PushedOff = nameof(PushedOff);

        public override void ApplyTo(IControl control)
        {
            var asToggle = control as IToggle;

#if DEBUG
            if(asToggle == null)
            {
                throw new System.Exception("Argument control needs to implement IToggle");
            }
#endif

            control.RollOn += HandleRollOn;
            control.RollOff += HandleRollOff;
            control.Push += HandlePush;
            control.Click += HandleClick;
            control.EnabledChange += (a) => Refresh(asToggle);

            asToggle.IsOnChanged += (a, b) => Refresh(asToggle);

            Refresh(asToggle);
        }

        void HandleRollOn(IWindow window)
        {
            if (window.Enabled)
            {
                var asToggle = window as IToggle;

                if(asToggle.IsOn)
                {
                    asToggle.SetState(HighlightedOn);
                }
                else
                {
                    asToggle.SetState(HighlightedOff);
                }
            }
        }

        void HandleRollOff(IWindow window)
        {
            if (window.Enabled)
            {
                var asToggle = window as IToggle;

                if (asToggle.IsOn)
                {
                    asToggle.SetState(EnabledOn);
                }
                else
                {
                    asToggle.SetState(EnabledOff);
                }
            }
        }

        void HandlePush(IWindow window)
        {
            if (window.Enabled)
            {
                var asToggle = window as IToggle;
                // mimic windows check box behavior, which is to preserve
                // the on/off state when a push occurs. Click swaps it
                if(asToggle.IsOn)
                {
                    asToggle.SetState(PushedOn);
                }
                else
                {
                    asToggle.SetState(PushedOff);
                }
            }
        }

        void HandleClick(IWindow window)
        {
            if (window.Enabled)
            {
                var asToggle = window as IToggle;

                asToggle.IsOn = !asToggle.IsOn;

                HandleIsOnChange(asToggle);
            }
        }

        void HandleIsOnChange(IToggle toggle)
        {
            if (toggle.HasCursorOver(GuiManager.Cursor))
            {
                HandleRollOn(toggle);
            }
            else
            {
                HandleRollOff(toggle);
            }
        }

        void Refresh(IToggle toggle)
        {
            if (toggle.Enabled)
            {
                if (toggle.HasCursorOver(GuiManager.Cursor))
                {
                    HandleRollOn(toggle);
                }
                else
                {
                    HandleRollOff(toggle);
                }
            }
            else
            {
                if(toggle.IsOn)
                {
                    toggle.SetState(DisabledOn);
                }
                else
                {
                    toggle.SetState(DisabledOff);
                }
            }
        }

    }
}
