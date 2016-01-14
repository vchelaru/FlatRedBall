using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

using FlatRedBall;

using FlatRedBall.Content;
using FlatRedBall.Content.Model;

using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Lighting;
using FlatRedBall.Graphics.Model;

using FlatRedBall.Gui;

using FlatRedBall.Input;

using FlatRedBall.IO;


using FlatRedBall.Math;

using XmlObjectEditor.Gui;

using EditorObjects;
using System.Reflection;

namespace XmlObjectEditor
{
    public static class EditorData
    {
        #region Fields

        public const string SceneContentManager = "Scene ContentManager";

        static EditorLogic mEditorLogic;

        static List<Assembly> mAssemblies = new List<Assembly>();
        static ReadOnlyCollection<Assembly> mReadOnlyAssemblies;

        #endregion

        #region Properties

        public static ReadOnlyCollection<Assembly> Assemblies
        {
            get { return mReadOnlyAssemblies; }
        }

        public static EditorLogic EditorLogic
        {
            get { return mEditorLogic; }
        }

        #endregion

        #region Methods

        static EditorData()
        {
            mReadOnlyAssemblies = new ReadOnlyCollection<Assembly>(mAssemblies);
        }

        public static void Initialize()
        {
            SpriteManager.Camera.CameraCullMode = CameraCullMode.None;

            mEditorLogic = new EditorLogic();

            GuiData.Initialize();

        }


        public static void LoadAssembly(string fileName)
        {
            mAssemblies.Add(Assembly.LoadFile(fileName));

        }


        public static void Update()
        {
            GuiData.Update();

            // Control camera
            if (InputManager.ReceivingInput == null)
            {
                InputManager.Keyboard.ControlPositionedObject(SpriteManager.Camera, 16);

            }

            EditorObjects.CameraMethods.MouseCameraControl(SpriteManager.Camera);

            UndoManager.EndOfFrameActivity();
        }

        #endregion
    }
}
