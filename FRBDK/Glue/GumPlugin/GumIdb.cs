using FlatRedBall;
using FlatRedBall.Graphics;
using Gum.DataTypes;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GumRuntime;
using Gum.Managers;
using Gum.DataTypes.Variables;

namespace FlatRedBall
{
    public class GumIdb : IDrawableBatch
    {
        #region Fields

        static SystemManagers mManagers;

        //The project file can be common across multiple instances of the IDB.
        static string mProjectFileName;

        static GraphicalUiElement element;

        #endregion

        #region Properties

        public bool UpdateEveryFrame
        {
            get { return true; }
        }

        public float X
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float Y
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float Z
        {
            get
            {
                return 0;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region Public Functions
        public GumIdb()
        {

        }

        public static void StaticInitialize(string projectFileName)
        {
            if (mManagers == null)
            {
                mManagers = new SystemManagers();
                mManagers.Initialize(FlatRedBallServices.GraphicsDevice);
                mManagers.Renderer.Camera.AbsoluteLeft = 0;
                mManagers.Renderer.Camera.AbsoluteTop = 0;
            }

            if (projectFileName == null)
            {
                throw new Exception("The GumIDB must be initialized with a valid (non-null) project file.");
            }

            string errors;
            mProjectFileName = projectFileName;
            ObjectFinder.Self.GumProjectSave = GumProjectSave.Load(projectFileName, out errors);
            StandardElementsManager.Self.Initialize();
        }

        public void LoadFromFile(string fileName)
        {
            if (mProjectFileName == null || ObjectFinder.Self.GumProjectSave == null)
            {
                throw new Exception("The GumIDB must be initialized with a project file before loading any components/screens.  Make sure you have a .gumx project file in your global content, or call StaticInitialize in code first.");
            }

            string oldDir = ToolsUtilities.FileManager.RelativeDirectory;
            ToolsUtilities.FileManager.RelativeDirectory = ToolsUtilities.FileManager.GetDirectory(mProjectFileName);

            ComponentSave elementSave = FlatRedBall.IO.FileManager.XmlDeserialize<ComponentSave>(fileName);
            foreach (var state in elementSave.States)
            {
                state.Initialize();
                state.ParentContainer = elementSave;
            }

            foreach (var instance in elementSave.Instances)
            {
                instance.ParentContainer = elementSave;
            }


            element = elementSave.ToGraphicalUiElement(mManagers, false);

            //Set the relative directory back once we are done
            ToolsUtilities.FileManager.RelativeDirectory = oldDir;

        }

        public void InstanceInitialize()
        {
            AddToManagers();
            SpriteManager.AddDrawableBatch(this);
        }

        public void InstanceDestroy()
        {
            SpriteManager.RemoveDrawableBatch(this);
        }

        IPositionedSizedObject FindByName(string name)
        {
            if (element.Name == name)
            {
                return element;
            }
            return element.GetChildByName(name);
        }
        #endregion

        #region Internal Functions
        protected void AddToManagers()
        {
            element.AddToManagers(mManagers, null);
        }
        #endregion

        #region IDrawableBatch
        public void Update()
        {
            mManagers.Activity(TimeManager.CurrentTime);
        }

        public void Draw(FlatRedBall.Camera camera)
        {
            mManagers.Draw();
        }

        public void Destroy()
        {
            element.RemoveFromManagers(mManagers);
        }
        #endregion

    }
}
