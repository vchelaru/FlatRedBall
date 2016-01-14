using System;
using System.Windows.Forms;

namespace MasterInstaller.Components
{
    public class EnableButtonEventArgs : EventArgs
    {
        public bool ButtonEnabled { get; set; }
        public string ButtonText { get; set; }
    }

    public class MoveToEventArgs : EventArgs
    {
        public ComponentBase Component { get; set; }
    }

    public abstract class ComponentBase
    {
        protected Control Control;

        public Control MainControl { get { return Control; } }

        public abstract ComponentBase PreviousComponent { get; }
        public abstract ComponentBase NextComponent { get; }

        public event EventHandler<EnableButtonEventArgs> PreviousChanged;
        public event EventHandler<EnableButtonEventArgs> NextChanged;

        public event EventHandler MoveToNext;
        protected void OnMoveToNext()
        {
            MoveToNext(this, null);
        }

        public event EventHandler<MoveToEventArgs> MoveTo;
        protected void OnMoveTo(ComponentBase component)
        {
            MoveTo(this, new MoveToEventArgs{Component = component});
        }

        protected virtual bool PreviousButtonEnabledByDefault { get { return false; } }
        protected virtual bool NextButtonEnabledByDefault { get { return false; } }
        protected virtual string PreviousButtonString { get { return null; } }
        protected virtual string NextButtonString { get { return null; } }

        public virtual void MovedToComponent()
        {
            PreviousChanged(this, new EnableButtonEventArgs { ButtonEnabled = PreviousButtonEnabledByDefault, ButtonText = PreviousButtonString });
            NextChanged(this, new EnableButtonEventArgs { ButtonEnabled = NextButtonEnabledByDefault, ButtonText = NextButtonString });
        }

        public virtual bool MovingBackFromComponent()
        {
            return true;
        }

        public virtual bool MovingNextFromComponent()
        {
            return true;
        }
    }
}
