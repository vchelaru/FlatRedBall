using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue;
using System.Dynamic;
using GlueView.Facades;
using FlatRedBall.Glue.IO;

namespace GlueView.Plugin
{
    public abstract class GlueViewPlugin : IPlugin
    {
        #region Events to += by derived classes

        public event EventHandler MouseMove;
        public event EventHandler Push;
        public event EventHandler Drag;
        public event EventHandler Click;
        public event EventHandler RightClick;
		public event EventHandler MiddleScroll;
		public event EventHandler Update;

        public event EventHandler ElementLoaded;
        public event EventHandler ElementHiglight;
        public event Action ElementRemoved;
        public event EventHandler<VariableSetArgs> BeforeVariableSet;
        public event EventHandler<VariableSetArgs> AfterVariableSet;

        public event Action ResolutionChange;

        #endregion

        #region Call methods for raising events


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

        public void CallBeforeElementRemoved()
        {
            ElementRemoved?.Invoke();
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

        #endregion

        public abstract string FriendlyName
        {
            get;
        }

        protected dynamic PersistentData = new ExpandoObject();

        Dictionary<string, ExpandoObject> elementSpecificPersistentData = null;
        protected dynamic PersistentDataForCurrentElement
        {
            get
            {
                if(elementSpecificPersistentData == null &&
                    GlueViewState.Self.CurrentGlueProject != null)
                {
                    LoadAllElementPersistentData();
                }
                if(GlueViewState.Self.CurrentElement == null)
                {
                    return null;
                }
                else
                {
                    var name = GlueViewState.Self.CurrentElement.Name;

                    if(!elementSpecificPersistentData.ContainsKey(name))
                    {
                        dynamic expando = new ExpandoObject();

                        elementSpecificPersistentData[name] = expando;


                    }
                    return elementSpecificPersistentData[name];
                }
            }
        }

        protected bool DataHasMember(ExpandoObject expandoObject, string member)
        {
            return ((IDictionary<String, object>)expandoObject).ContainsKey(member);
        }

        protected void SavePersistentDataForElements()
        {
            if(elementSpecificPersistentData != null)
            {
                var persistentDataFile = GetPersistentDataFile();

                string text = Newtonsoft.Json.JsonConvert.SerializeObject(elementSpecificPersistentData);

                System.IO.Directory.CreateDirectory(persistentDataFile.GetDirectoryContainingThis().Standardized);

                System.IO.File.WriteAllText(persistentDataFile.Standardized, text);
            }

        }

        private void LoadAllElementPersistentData()
        {
            var persistentDataFile = GetPersistentDataFile();

            if (System.IO.File.Exists(persistentDataFile.Standardized))
            {
                var allText = System.IO.File.ReadAllText(persistentDataFile.Standardized);
                elementSpecificPersistentData =
                    Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, ExpandoObject>>(allText);
            }
            else
            {
                elementSpecificPersistentData = new Dictionary<string, ExpandoObject>();
            }
        }

        private FilePath GetPersistentDataFile()
        {
            if (GlueViewState.Self.CurrentGlueProject == null)
            {
                throw new InvalidOperationException("Cannot determine persistent data file - no project is loaded.");
            }

            var gluxFileName = new FilePath(GlueViewState.Self.CurrentGlueProjectFile);

            var gluxDirectory = gluxFileName.GetDirectoryContainingThis();

            var persistentDataFile = gluxDirectory +
                $"GlueSettings/{this.GetType().FullName}.Settings.json";
            return persistentDataFile;
        }

        public abstract Version Version
        {
            get;
        }

        public abstract void StartUp();

        public abstract bool ShutDown(PluginShutDownReason shutDownReason);
    }
}
