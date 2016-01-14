using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue;

namespace GlueView.Plugin
{
    public abstract class GlueViewPlugin : IPlugin
    {
        public event EventHandler MouseMove;
        public event EventHandler Push;
        public event EventHandler Drag;
        public event EventHandler Click;
        public event EventHandler RightClick;
		public event EventHandler MiddleScroll;
		public event EventHandler Update;

        public event EventHandler ElementLoaded;
        public event EventHandler ElementHiglight;
        public event EventHandler<VariableSetArgs> BeforeVariableSet;
        public event EventHandler<VariableSetArgs> AfterVariableSet;

        public event Action ResolutionChange;


        public void CallPush()
        {
            if (Push != null)
            {
                Push(null, null);
            }
        }

        public void CallDrag()
        {
            if (Drag != null)
            {
                Drag(null, null);
            }
        }

        public void CallMouseMove()
        {
            if (MouseMove != null)
            {
                MouseMove(null, null);
            }
        }

        public void CallClick()
        {
            if (Click != null)
            {
                Click(null, null);
            }
        }

        public void CallRightClick()
        {
            if (RightClick != null)
            {
                RightClick(null, null);
            }
        }

		public void CallMiddleScroll()
		{
			if (MiddleScroll != null)
			{
				MiddleScroll(null, null);
			}
		}

        public void CallElementLoaded()
        {
            if (ElementLoaded != null)
            {
                ElementLoaded(null, null);
            }
        }

        public void CallElementHiglight()
        {
            if (ElementHiglight != null)
            {
                ElementHiglight(null, null);
            }
        }

        public void CallResolutionChange()
        {
            if (ResolutionChange != null)
            {
                ResolutionChange();
            }
        }

		public void CallUpdate()
		{
			if (Update != null)
			{
				Update(null, null);
			}
		}

        public void CallBeforeVariableSet(object sender, VariableSetArgs args)
        {
            if (BeforeVariableSet != null)
            {
                BeforeVariableSet(sender, args);
            }
        }

        public void CallAfterVariableSet(object sender, VariableSetArgs args)
        {
            if (AfterVariableSet != null)
            {
                AfterVariableSet(sender, args);
            }
        }

        public abstract string FriendlyName
        {
            get;
        }

        public abstract Version Version
        {
            get;
        }

        public abstract void StartUp();

        public abstract bool ShutDown(PluginShutDownReason shutDownReason);
    }
}
