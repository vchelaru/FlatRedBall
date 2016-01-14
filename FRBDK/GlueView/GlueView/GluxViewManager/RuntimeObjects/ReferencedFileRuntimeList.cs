using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Graphics;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Localization;
using FlatRedBall.IO;
using FlatRedBall.IO.Csv;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Math.Splines;

namespace FlatRedBall.Glue.RuntimeObjects
{
    public class ReferencedFileRuntimeList
    {
        #region Fields

        /// <summary>
        /// A dictionary containing cached objects loaded from-file and their names as represented as RFSs.  
        /// </summary>
        /// <remarks>
        /// The key is not absolute.  It's relative to the project
        /// </remarks>
        Dictionary<string, object> mLoadedRfses = new Dictionary<string, object>();

        List<Scene> mLoadedScenes = new List<Scene>();
        List<ShapeCollection> mLoadedShapeCollections = new List<ShapeCollection>();
        List<NodeNetwork> mLoadedNodeNetworks = new List<NodeNetwork>();
        List<EmitterList> mLoadedEmitterLists = new List<EmitterList>();

        #endregion

        public Dictionary<string, object> LoadedRfses
        {
            get
            {
                return mLoadedRfses;
            }
        }

        public List<NodeNetwork> LoadedNodeNetworks
        {
            get { return mLoadedNodeNetworks; }
        }

        public List<Scene> LoadedScenes
        {
            get
            {
                return mLoadedScenes;
            }
        }

        public List<EmitterList> LoadedEmitterLists
        {
            get
            {
                return mLoadedEmitterLists;
            }
        }

        public List<ShapeCollection> LoadedShapeCollections
        {
            get
            {
                return mLoadedShapeCollections;
            }
        }

        public void Add(object objectToAdd)
        {
            if (objectToAdd is NodeNetwork)
            {
                LoadedNodeNetworks.Add(objectToAdd as NodeNetwork);
            }
            else if (objectToAdd is Scene)
            {
                LoadedScenes.Add(objectToAdd as Scene);
            }
            else if(objectToAdd is EmitterList)
            {
                LoadedEmitterLists.Add(objectToAdd as EmitterList);
            }
            else if(objectToAdd is ShapeCollection)
            {
                LoadedShapeCollections.Add(objectToAdd as ShapeCollection);
            }
        }


        public void Activity()
        {

            foreach (Scene scene in mLoadedScenes)
            {
                scene.ManageAll();
            }

            foreach (NodeNetwork nodeNetwork in mLoadedNodeNetworks)
            {
                if (nodeNetwork.Visible)
                {
                    nodeNetwork.UpdateShapes();
                }
            }
        }

        public void Destroy()
        {
            foreach (Scene s in mLoadedScenes)
            {
                s.RemoveFromManagers();
            }

            foreach (ShapeCollection s in mLoadedShapeCollections)
            {
                s.RemoveFromManagers();
            }

            foreach (EmitterList emitterList in mLoadedEmitterLists)
            {
                emitterList.RemoveFromManagers();
            }

            foreach (NodeNetwork nodeNetwork in mLoadedNodeNetworks)
            {
                // Setting its visible to false is the same as removing it shapes form managers
                nodeNetwork.Visible = false;
            }


            mLoadedScenes.Clear();
            mLoadedShapeCollections.Clear();
            mLoadedNodeNetworks.Clear();
            mLoadedEmitterLists.Clear();

            mLoadedRfses.Clear();
        }

        public void Destroy(string name)
        {
            name = name.ToLower();

            if (mLoadedRfses.ContainsKey(name))
            {
                object loadedObject = mLoadedRfses[name];

                if (loadedObject is Scene)
                {
                    ((Scene)loadedObject).RemoveFromManagers();
                    mLoadedScenes.Remove((Scene)loadedObject);
                }
                else if (loadedObject is ShapeCollection)
                {
                    ((ShapeCollection)loadedObject).RemoveFromManagers();
                    mLoadedShapeCollections.Remove((ShapeCollection)loadedObject);
                }
                else if (loadedObject is NodeNetwork)
                {
                    ((NodeNetwork)loadedObject).Visible = false;
                    mLoadedNodeNetworks.Remove((NodeNetwork)loadedObject);
                }
                else if (loadedObject is EmitterList)
                {
                    ((EmitterList)loadedObject).RemoveFromManagers();
                    mLoadedEmitterLists.Remove((EmitterList)loadedObject);
                }
                mLoadedRfses.Remove(name);
            }
        }

        private object LoadRfsAndAddToLists(ReferencedFileSave r, bool isBeingAccessed, IElement container)
        {

            string extension = FileManager.GetExtension(r.Name).ToLower();

            object runtimeObject = null;

            if (!r.LoadedOnlyWhenReferenced || isBeingAccessed)
            {
                switch (extension)
                {
                    case "scnx":
                        runtimeObject = LoadScnx(r, container);
                        if ((!r.IsSharedStatic || container is ScreenSave) && runtimeObject != null)
                        {
                            LoadedScenes.Add(runtimeObject as Scene);
                        }
                        break;
                    case "shcx":
                        runtimeObject = LoadShcx(r, container);
                        if ((!r.IsSharedStatic || container is ScreenSave) && runtimeObject != null)
                        {
                            LoadedShapeCollections.Add(runtimeObject as ShapeCollection);
                        }
                        break;
                    case "nntx":
                        runtimeObject = LoadNntx(r, container);
                        if ((!r.IsSharedStatic || container is ScreenSave) && runtimeObject != null)
                        {
                            LoadedNodeNetworks.Add(runtimeObject as NodeNetwork);
                        }
                        break;
                    case "emix":
                        runtimeObject = LoadEmix(r);
                        if ((!r.IsSharedStatic || container is ScreenSave) && runtimeObject != null)
                        {
                            LoadedEmitterLists.Add(runtimeObject as EmitterList);
                        }
                        break;
                    case "achx":
                        runtimeObject = LoadAchx(r);
                        break;
                    case "png":
                    case "bmp":
                    case "dds":
                    case "tga":
                        runtimeObject = LoadTexture2D(r);
                        break;
                    case "csv":
                        runtimeObject = LoadCsv(r);
                        break;
                    case "fnt":
                        runtimeObject = LoadFnt(r);
                        break;
                    case "splx":
                        runtimeObject = LoadSplx(r);
                        break;
                }
            }


            return runtimeObject;
        }

        private object LoadSplx(ReferencedFileSave r)
        {
            var toReturn = FlatRedBallServices.Load<SplineList>(ElementRuntime.ContentDirectory + r.Name, GluxManager.ContentManagerName);
            return toReturn;
        }

        static RuntimeCsvRepresentation LoadCsv(ReferencedFileSave r)
        {
            RuntimeCsvRepresentation rcr = FlatRedBallServices.Load<RuntimeCsvRepresentation>(ElementRuntime.ContentDirectory + r.Name, 
                GluxManager.ContentManagerName);

            return rcr;
        }

        static Texture2D LoadTexture2D(ReferencedFileSave r)
        {
            Texture2D texture;

            texture = FlatRedBallServices.Load<Texture2D>(ElementRuntime.ContentDirectory + r.Name, GluxManager.ContentManagerName);

            return texture;
        }

        static BitmapFont LoadFnt(ReferencedFileSave r)
        {
            BitmapFont bitmapFont;
            bitmapFont = FlatRedBallServices.Load<BitmapFont>(ElementRuntime.ContentDirectory + r.Name, GluxManager.ContentManagerName);
            return bitmapFont;
        }

        static NodeNetwork LoadNntx(ReferencedFileSave r, IElement container)
        {
            NodeNetwork nodeNetwork;
            nodeNetwork = FlatRedBallServices.Load<NodeNetwork>(ElementRuntime.ContentDirectory + r.Name, GluxManager.ContentManagerName);

            if (!r.IsSharedStatic || container is ScreenSave)
            {
                nodeNetwork.Visible = true;
            }
            return nodeNetwork;
        }

        static EmitterList LoadEmix(ReferencedFileSave r)
        {
            return FlatRedBallServices.Load<EmitterList>(ElementRuntime.ContentDirectory + r.Name, GluxManager.ContentManagerName);
        }

        static ShapeCollection LoadShcx(ReferencedFileSave r, IElement container)
        {
            ShapeCollection newShapeCollection;
            newShapeCollection = FlatRedBallServices.Load<ShapeCollection>(ElementRuntime.ContentDirectory + r.Name, GluxManager.ContentManagerName);

            if (!r.IsSharedStatic || container is ScreenSave)
            {
                newShapeCollection.AddToManagers();
            }
            return newShapeCollection;
        }

        static AnimationChainList LoadAchx(ReferencedFileSave r)
        {
            AnimationChainList newAnimationChainList;
            string fileToLoad = ElementRuntime.ContentDirectory + r.Name;
            newAnimationChainList = FlatRedBallServices.Load<AnimationChainList>(fileToLoad, GluxManager.ContentManagerName);
            return newAnimationChainList;
        }

        static Scene LoadScnx(ReferencedFileSave r, IElement container)
        {
            Scene newScene = null;
            try
            {
                newScene = FlatRedBallServices.Load<Scene>(ElementRuntime.ContentDirectory + r.Name, GluxManager.ContentManagerName);

                foreach (Text text in newScene.Texts)
                {
                    text.AdjustPositionForPixelPerfectDrawing = true;
                    if (ObjectFinder.Self.GlueProject.UsesTranslation)
                    {
                        text.DisplayText = LocalizationManager.Translate(text.DisplayText);
                    }
                }

                if (!r.IsSharedStatic || container is ScreenSave)
                {
                    newScene.AddToManagers();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error loading Scene file " + ElementRuntime.ContentDirectory + r.Name + e.ToString());
            }
            return newScene;
        }

        public object LoadReferencedFileSave(ReferencedFileSave r, IElement container)
        {
            return LoadReferencedFileSave(r, false, container);
        }

        public object LoadReferencedFileSave(ReferencedFileSave r, bool isBeingAccessed, IElement container)
        {
            //////////////////EARLY OUT///////////////////
            if (r.LoadedAtRuntime == false)
            {
                return null;
            }

            if (mLoadedRfses.ContainsKey(r.Name.ToLower()))
            {
                // do nothing, it's already been loaded)
                return mLoadedRfses[r.Name.ToLower()];
            }
            /////////////END EARLY OUT//////////////////////

            object runtimeObject = LoadRfsAndAddToLists(r, isBeingAccessed, container);

            if (isBeingAccessed || runtimeObject != null)
            {
                mLoadedRfses.Add(r.Name.ToLower(), runtimeObject);

                return mLoadedRfses[r.Name.ToLower()];
            }
            else
            {
                // It's null, but not being accessed so that's okay
                return null;
            }
        }
    }
}
