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

namespace FlatRedBall.Gum
{
    public partial class GumIdb : IDrawableBatch, RenderingLibrary.Graphics.IVisible
    {
        #region Fields

        static SystemManagers mManagers;

        //The project file can be common across multiple instances of the IDB.
        static string mProjectFileName;

        static Dictionary<FlatRedBall.Graphics.Layer, List<RenderingLibrary.Graphics.Layer>> mFrbToGumLayers =
            new Dictionary<FlatRedBall.Graphics.Layer, List<RenderingLibrary.Graphics.Layer>>();

        GraphicalUiElement element;

        #endregion

        #region Properties

        /// <summary>
        /// Makes the Gum IDB skip its rendering code. This can be used to isolate rendering performance bottlenecks.
        /// </summary>
        public static bool DisableDrawing
        {
            get;
            set;
        }

        public bool Visible
        {
            get
            {
                return element.Visible;
            }
            set
            {
                element.Visible = value;
            }
        }

        public string Name { get; set; }

        public GraphicalUiElement Element
        {
            get
            {
                return element;
            }
        }

        public bool UpdateEveryFrame
        {
            get { return true; }
        }

        public float X
        {
            get
            {
                return 0;
            }
            set
            {
                throw new InvalidOperationException();
            }
        }

        public float Y
        {
            get
            {
                return 0;
            }
            set
            {
                throw new InvalidOperationException();
            }
        }

        public float Z
        {
            get;
            set;
        }

        #endregion

        #region Public Functions

        public GumIdb()
        {

        }

        public void AssignReferences()
        {
            this.element.AssignReferences();
        }

        public static void StaticInitialize(string projectFileName)
        {
            if (mManagers == null)
            {
                mManagers = new SystemManagers();
                mManagers.Initialize(FlatRedBallServices.GraphicsDevice);
                mManagers.Renderer.Camera.AbsoluteLeft = 0;
                mManagers.Renderer.Camera.AbsoluteTop = 0;

                UpdateDisplayToMainFrbCamera();

                // Need to do the zoom here in response to the FRB camera vs. the Gum camera
                mManagers.Renderer.Camera.Zoom = mManagers.Renderer.GraphicsDevice.Viewport.Height / (float)GraphicalUiElement.CanvasHeight;
                mManagers.Renderer.Camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;
                mManagers.Renderer.Camera.X = 0;
                mManagers.Renderer.Camera.Y = 0;

                SystemManagers.Default = mManagers;
                FlatRedBallServices.AddManager(RenderingLibrary.SystemManagers.Default);

                RenderingLibrary.Graphics.Text.RenderBoundaryDefault = false;
                // FlatRedBall uses premult alpha.
                RenderingLibrary.Graphics.Renderer.NormalBlendState = Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend;


                var idb = new GumIdb();
                // We don't want the UI to be at Z=0 because it will render 
                // at the same Z along with FRB entities and environments so UI might 
                // be hidden. The proper way to solve this is to use Layers, but
                // this shouldn't be creating new Layers, that's up to the user.
                // Let's make it have a positive Z so it draws in more things at once 
                idb.Z = 10;

                // This could be called on a secondary thread, like if called by GlobalContent, so we
                // want this to happen on the primary thread:
                Action primaryThreadAction = () =>
                {
                    FlatRedBall.SpriteManager.AddDrawableBatch(idb);
                    FlatRedBall.Screens.ScreenManager.PersistentDrawableBatches.Add(idb);
                };

                var instruction = new FlatRedBall.Instructions.DelegateInstruction(primaryThreadAction);
                FlatRedBall.Instructions.InstructionManager.Add(instruction);

            }

            if (projectFileName == null)
            {
                throw new Exception("The GumIDB must be initialized with a valid (non-null) project file.");
            }

            string errors;
            mProjectFileName = projectFileName;

            if (FlatRedBall.IO.FileManager.IsRelative(mProjectFileName))
            {
                mProjectFileName = FlatRedBall.IO.FileManager.RelativeDirectory + mProjectFileName;
            }

            // First let's set the relative directory to the file manager's relative directory so we can load
            // the file normally...
            ToolsUtilities.FileManager.RelativeDirectory = FlatRedBall.IO.FileManager.RelativeDirectory;

            GumLoadResult result;

            ObjectFinder.Self.GumProjectSave = GumProjectSave.Load(mProjectFileName, out result);

#if DEBUG
            if(ObjectFinder.Self.GumProjectSave == null)
            {
                throw new Exception("Could not find Gum project at " + mProjectFileName);
            }

            if(!string.IsNullOrEmpty(result.ErrorMessage))
            {
                throw new Exception(result.ErrorMessage);

            }

            if(result.MissingFiles.Count != 0)
            {
                throw new Exception("Missing files starting with " + result.MissingFiles[0]);
            }
#endif

            // Now we can set the directory to Gum's root:
            ToolsUtilities.FileManager.RelativeDirectory = ToolsUtilities.FileManager.GetDirectory(mProjectFileName);

            // The Gum tool does a lot more init than this, but we're going to only do a subset 
            //of initialization for performance
            // reasons:
            foreach (var item in ObjectFinder.Self.GumProjectSave.Screens)
            {
                // Only initialize using the default state
                if (item.DefaultState != null)
                {
                    item.Initialize(item.DefaultState);
                }
            }
            foreach (var item in ObjectFinder.Self.GumProjectSave.Components)
            {
                // Only initialize using the default state
                if (item.DefaultState != null)
                {
                    item.Initialize(item.DefaultState);
                }
            }
            foreach (var item in ObjectFinder.Self.GumProjectSave.StandardElements)
            {
                // Only initialize using the default state
                if (item.DefaultState != null)
                {
                    item.Initialize(item.DefaultState);
                }
                //for atlased colored rectangles
                if (item.Name == "ColoredRectangle")
                    RenderingLibrary.Graphics.SolidRectangle.AtlasedTextureName = "..\\Graphics\\Misc\\ColoredRectangle.png";
            }

            StandardElementsManager.Self.Initialize();
        }

        public static void UpdateDisplayToMainFrbCamera()
        {
            var viewport = mManagers.Renderer.GraphicsDevice.Viewport;
            viewport.Width = FlatRedBall.Math.MathFunctions.RoundToInt(FlatRedBall.Camera.Main.DestinationRectangle.Width);
            viewport.Height = FlatRedBall.Math.MathFunctions.RoundToInt(FlatRedBall.Camera.Main.DestinationRectangle.Height);
            mManagers.Renderer.GraphicsDevice.Viewport = viewport;

            if (FlatRedBall.Camera.Main.Orthogonal)
            {
                GraphicalUiElement.CanvasHeight = FlatRedBall.Camera.Main.OrthogonalHeight;
                GraphicalUiElement.CanvasWidth = FlatRedBall.Camera.Main.OrthogonalWidth;
            }
            else
            {
                GraphicalUiElement.CanvasHeight = FlatRedBall.Camera.Main.DestinationRectangle.Height;
                GraphicalUiElement.CanvasWidth = FlatRedBall.Camera.Main.DestinationRectangle.Width;
            }
        }




        public void LoadFromFile(string fileName)
        {
            var section = FlatRedBall.Performance.Measurement.Section.GetAndStartContextAndTime("LoadFromFile " + fileName);
            {

                var sectionA = FlatRedBall.Performance.Measurement.Section.GetAndStartContextAndTime("GetElement");

                if (mProjectFileName == null || ObjectFinder.Self.GumProjectSave == null)
                {
                    throw new Exception("The GumIDB must be initialized with a project file before loading any components/screens.  Make sure you have a .gumx project file in your global content, or call StaticInitialize in code first.");
                }

                string oldDir = ToolsUtilities.FileManager.RelativeDirectory;
                string oldFrbDir = FlatRedBall.IO.FileManager.RelativeDirectory;

                ToolsUtilities.FileManager.RelativeDirectory = ToolsUtilities.FileManager.GetDirectory(mProjectFileName);

                ElementSave elementSave = null;
                string extension = ToolsUtilities.FileManager.GetExtension(fileName);

                string strippedName = FlatRedBall.IO.FileManager.RemoveExtension(FlatRedBall.IO.FileManager.RemovePath(fileName));

                if (extension == GumProjectSave.ComponentExtension)
                {
                    elementSave = ObjectFinder.Self.GumProjectSave.Components.FirstOrDefault(item => item.Name.Equals(strippedName, StringComparison.OrdinalIgnoreCase));
                }
                else if (extension == GumProjectSave.ScreenExtension)
                {

                    elementSave = ObjectFinder.Self.GumProjectSave.Screens.FirstOrDefault(item => item.Name.Equals(strippedName, StringComparison.OrdinalIgnoreCase));
                }
                else if (extension == GumProjectSave.StandardExtension)
                {
                    elementSave = ObjectFinder.Self.GumProjectSave.StandardElements.FirstOrDefault(item => item.Name.Equals(strippedName, StringComparison.OrdinalIgnoreCase));
                }

                // Set this *after* the deserialization happens.  The fileName is relative to FRB's relative directory but
                // contained file references won't be.
                FlatRedBall.IO.FileManager.RelativeDirectory = ToolsUtilities.FileManager.GetDirectory(mProjectFileName);

                sectionA.EndTimeAndContext();

                var sectionB = FlatRedBall.Performance.Measurement.Section.GetAndStartContextAndTime("Set ParentContainer");

                // We used to only init the uncategorized states
                // but every state should be initialized
                //foreach (var state in elementSave.States)
                foreach (var state in elementSave.AllStates)
                {
                    state.Initialize();
                    state.ParentContainer = elementSave;
                }

                foreach (var instance in elementSave.Instances)
                {
                    instance.ParentContainer = elementSave;
                }

                sectionB.EndTimeAndContext();

                var sectionC = FlatRedBall.Performance.Measurement.Section.GetAndStartContextAndTime("ToGraphicalUiElement");


                element = elementSave.ToGraphicalUiElement(mManagers, false);

                sectionC.EndTimeAndContext();

                var sectionD = FlatRedBall.Performance.Measurement.Section.GetAndStartContextAndTime("Wrap up");



                element.ExplicitIVisibleParent = this;

                //Set the relative directory back once we are done
                ToolsUtilities.FileManager.RelativeDirectory = oldDir;
                FlatRedBall.IO.FileManager.RelativeDirectory = oldFrbDir;
                sectionD.EndTimeAndContext();
            }

            section.EndTimeAndContext();


        }

        public void InstanceInitialize()
        {
            AddToManagers();
        }

        IPositionedSizedObject FindByName(string name)
        {
            if (element.Name == name)
            {
                return element;
            }
            return element.GetChildByName(name);
        }

        public GraphicalUiElement GetGraphicalUiElementByName(string name)
        {
            if (name == "this")
            {
                return element;
            }
            else
            {
                return element.GetGraphicalUiElementByName(name);
            }
        }

        public void AddGumLayerToFrbLayer(RenderingLibrary.Graphics.Layer gumLayer, FlatRedBall.Graphics.Layer frbLayer)
        {
            if (mFrbToGumLayers.ContainsKey(frbLayer) == false)
            {
                mFrbToGumLayers[frbLayer] = new List<RenderingLibrary.Graphics.Layer>();
            }

            mFrbToGumLayers[frbLayer].Add(gumLayer);
            SpriteManager.AddToLayerAllowDuplicateAdds(this, frbLayer);
        }

        public IEnumerable<RenderingLibrary.Graphics.Layer> GumLayersOnFrbLayer(FlatRedBall.Graphics.Layer frbLayer)
        {
            if (frbLayer == null)
            {
                yield return SystemManagers.Default.Renderer.MainLayer;
            }
            else if (mFrbToGumLayers.ContainsKey(frbLayer))
            {
                foreach (var item in mFrbToGumLayers[frbLayer])
                {
                    yield return item;
                }
            }
            else
            {
                yield break;
            }
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

        /// <summary>
        /// Variable that stores the last draw call. It is used to determine if drawing a new frame.
        /// </summary>
        double mLastDrawCall = double.NaN;

        public void Draw(FlatRedBall.Camera camera)
        {
            ////////////////////////Early Out///////////////////////////////

            if (DisableDrawing)
            {
                return;
            }

            //////////////////////End Early Out/////////////////////////////


            // This is the first call of the frame, so reset this value:
            SystemManagers.Default.Renderer.ClearPerformanceRecordingVariables();

            if (FlatRedBall.Graphics.Renderer.CurrentLayer == null)
            {
                mManagers.TextManager.RenderTextTextures();
                mManagers.Draw(mManagers.Renderer.MainLayer);
            }
            else if (mFrbToGumLayers.ContainsKey(FlatRedBall.Graphics.Renderer.CurrentLayer))
            {
                mManagers.Draw(mFrbToGumLayers[FlatRedBall.Graphics.Renderer.CurrentLayer]);
            }

            var renderBreaks = FlatRedBall.Graphics.Renderer.LastFrameRenderBreakList;

            if (renderBreaks != null)
            {
#if DEBUG
                // This object handles its own render breaks
                if (renderBreaks.Count != 0)
                {
                    var last = renderBreaks.Last();

                    if (last.ObjectCausingBreak == this)
                    {
                        renderBreaks.RemoveAt(renderBreaks.Count - 1);
                    }
                }
#endif
                foreach (var item in mManagers.Renderer.SpriteRenderer.LastFrameDrawStates)
                {
                    foreach (var changeRecord in item.ChangeRecord)
                    {
                        var renderBreak = new RenderBreak(0,
                            changeRecord.Texture,
                            ColorOperation.Texture,
                            BlendOperation.Regular,
                            TextureAddressMode.Clamp);

#if DEBUG
                        renderBreak.ObjectCausingBreak = changeRecord.ObjectRequestingChange;
#endif
                        renderBreaks.Add(renderBreak);
                    }
                }
            }
            mManagers.Renderer.ForceEnd();
        }

        public void Destroy()
        {
            element.RemoveFromManagers();

            foreach (var kvp in mFrbToGumLayers)
            {
                var listOfGumLayers = kvp.Value;
                foreach (var gumLayer in listOfGumLayers)
                {
                    mManagers.Renderer.RemoveLayer(gumLayer);
                }
            }

            // Not sure if we need to do some work to only clear layers for the instance rather than all,
            // in case we're async loading
            mFrbToGumLayers.Clear();

        }
#endregion

#region RenderingLibrary.Graphics.IVisible

        public bool AbsoluteVisible
        {
            get
            {
                if (Parent == null)
                {
                    // Maybe update this if we support parent relationships between IDBs
                    return Visible;
                }
                else if (Parent is RenderingLibrary.Graphics.IVisible)
                {
                    return Visible && ((RenderingLibrary.Graphics.IVisible)Parent).AbsoluteVisible;
                }
                else if (Parent is FlatRedBall.Graphics.IVisible)
                {
                    return Visible && ((FlatRedBall.Graphics.IVisible)Parent).AbsoluteVisible;
                }
                else
                {
                    return Visible;
                }
            }
        }

        public PositionedObject Parent
        {
            get;
            set;
        }

        RenderingLibrary.Graphics.IVisible RenderingLibrary.Graphics.IVisible.Parent
        {
            get
            {
                return this.Parent as RenderingLibrary.Graphics.IVisible;
            }
        }

        // temporary to allow Glue to attach.  Eventually I'll change how Glue generates this code
        public void CopyAbsoluteToRelative()
        {
            // do nothing
        }

        public void AttachTo(PositionedObject newParent, bool throwaway)
        {
            Parent = newParent;
        }

#endregion
    }
}
