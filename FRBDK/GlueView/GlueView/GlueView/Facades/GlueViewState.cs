using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Facades;

namespace GlueView.Facades
{
    public class GlueViewState : IGlueState, IProjectValues
    {
        static GlueViewState mSelf;

        public static GlueViewState Self
        {
            get 
            {
                if (mSelf == null)
                {
                    mSelf = new GlueViewState();
                }
                return mSelf; 
            }
        }

        public CursorState CursorState
        {
            get;
            private set;
        }


        private GlueViewState()
        {
            CursorState = new CursorState();
        }


        public IElement CurrentElement
        {
            get;
            internal set;
        }

        public ElementRuntime CurrentElementRuntime
        {
            get
            {
                return GluxManager.CurrentElement;
            }

        }

        public ElementRuntime HighlightedElementRuntime
        {
            get
            {
                return GluxManager.CurrentElementHighlighted;
            }
            set
            {
                if (value != null)
                {
                    GluxManager.ElementToHighlight = value.Name;
                }
                else
                {
                    GluxManager.ElementToHighlight = null;
                }
            }
        }

        public GlueProjectSave CurrentGlueProject
        {
            get
            {
                return GluxManager.GlueProjectSave;
            }
        }

        public string CurrentGlueProjectFile
        {
            get
            {
                return GluxManager.CurrentGlueFile;
            }
        }


        public System.Windows.Forms.TreeNode CurrentTreeNode
        {
            get { return null; }
        }

        public EntitySave CurrentEntitySave
        {
            get { return CurrentElement as EntitySave; }
        }

        public ScreenSave CurrentScreenSave
        {
            get { return CurrentElement as ScreenSave; }
        }

        public NamedObjectSave CurrentNamedObjectSave
        {
            get 
            {
                if (HighlightedElementRuntime != null && HighlightedElementRuntime.AssociatedNamedObjectSave != null)
                {
                    return HighlightedElementRuntime.AssociatedNamedObjectSave;
                }
                return null;
            }
        }

        public ReferencedFileSave CurrentReferencedFileSave
        {
            get
            {
                // I don't think we can have this, so return null?
                return null;
            }
        }

        public FlatRedBall.Glue.Events.EventResponseSave CurrentEventResponseSave
        {
            get { return null; }
        }

        public CustomVariable CurrentCustomVariable
        {
            get { return null; }
        }

        public StateSave CurrentStateSave
        {
            get { return null; }
        }

        public StateSaveCategory CurrentStateSaveCategory
        {
            get { return null; }
        }

        public IElement GetElement(string name)
        {
            return ObjectFinder.Self.GetIElement(name);
        }

        public NamedObjectSave GetNamedObjectSave(string containerName, string name)
        {
            throw new NotImplementedException();
        }

        public CustomVariable GetCustomVariable(string containerName, string name)
        {
            throw new NotImplementedException();
        }

        public StateSave GetState(string containerName, string name)
        {
            throw new NotImplementedException();
        }

        public StateSaveCategory GetStateCategory(string containerName, string name)
        {
            throw new NotImplementedException();
        }

        public List<FlatRedBall.Glue.VSHelpers.Projects.ProjectBase> GetProjects()
        {
            throw new NotImplementedException();
        }

        public string ContentDirectory
        {
            get 
            {
                if (!string.IsNullOrEmpty(GluxManager.AlternativeContentDirectory))
                {
                    return GluxManager.AlternativeContentDirectory;
                }
                else
                {

                    return GluxManager.ContentDirectory;
                }
            
            }
        }


        public FlatRedBall.Glue.VSHelpers.Projects.ProjectBase CurrentMainProject
        {
            get { throw new NotImplementedException(); }
        }

        public FlatRedBall.Glue.VSHelpers.Projects.ProjectBase CurrentMainContentProject
        {
            get { throw new NotImplementedException(); }
        }


        public string ProjectNamespace
        {
            get { throw new NotImplementedException(); }
        }
    }
}
