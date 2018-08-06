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
using FlatRedBall.Glue.RuntimeObjects.File;

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
        List<object> mAddedRfses = new List<object>();

        List<ShapeCollection> mLoadedShapeCollections = new List<ShapeCollection>();
        List<NodeNetwork> mLoadedNodeNetworks = new List<NodeNetwork>();
        List<EmitterList> mLoadedEmitterLists = new List<EmitterList>();

        public static List<IRuntimeFileManager> FileManagers
        { get; private set; } = new List<IRuntimeFileManager>();


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

        public IReadOnlyCollection<Scene> AddedScenes
        {
            get
            {
                return mAddedRfses
                    .Where(item => item is Scene)
                    .Select(item => (Scene)item)
                    .ToArray();
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

        static ReferencedFileRuntimeList()
        {
            FileManagers.Add(new SceneRuntimeFileManager());
        }


        public void Add(object objectToAdd)
        {
            if (objectToAdd is NodeNetwork)
            {
                LoadedNodeNetworks.Add(objectToAdd as NodeNetwork);
            }
            else if(objectToAdd is EmitterList)
            {
                LoadedEmitterLists.Add(objectToAdd as EmitterList);
            }
            else if(objectToAdd is ShapeCollection)
            {
                LoadedShapeCollections.Add(objectToAdd as ShapeCollection);
            }

            mAddedRfses.Add(objectToAdd);
        }


        public void Activity()
        {
            foreach(var manager in FileManagers)
            {
                manager.Activity(LoadedRfses.Values);
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
            foreach (var manager in FileManagers)
            {
                manager.Destroy(mAddedRfses);
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

            // todo - move this to a file manager
            foreach(var item in mAddedRfses.Where(item =>item is SplineList)
                .Select(item =>item as SplineList))
            {
                item.RemoveFromManagers();
            }


            mLoadedShapeCollections.Clear();
            mLoadedNodeNetworks.Clear();
            mLoadedEmitterLists.Clear();

            mAddedRfses.Clear();
            mLoadedRfses.Clear();
        }

        public void Destroy(string name)
        {
            name = name.ToLower();

            if (mLoadedRfses.ContainsKey(name))
            {
                object loadedObject = mLoadedRfses[name];

                foreach(var manager in FileManagers)
                {
                    if(manager.TryDestroy(loadedObject, mAddedRfses))
                    {
                        break;
                    }
                }

                if (loadedObject is ShapeCollection)
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
                mAddedRfses.Remove(name);
            }
        }

        private object LoadRfsAndAddToLists(ReferencedFileSave r, IElement container)
        {
            object runtimeObject = null;

            foreach(var manager in FileManagers)
            {
                runtimeObject = manager.TryCreateFile(r, container);
                if (runtimeObject != null)
                {
                    if ((!r.IsSharedStatic || container is ScreenSave) && runtimeObject != null)
                    {
                        LoadedRfses.Add(r.Name, runtimeObject);
                    }
                    break;
                }
            }

            if(runtimeObject == null)
            {
                string extension = FileManager.GetExtension(r.Name).ToLower();

                switch (extension)
                {
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

            var shouldLoad = isBeingAccessed || r.IsSharedStatic;

            if(shouldLoad)
            {

                object runtimeObject = LoadRfsAndAddToLists(r, container);

                if (isBeingAccessed || runtimeObject != null)
                {
                    mAddedRfses.Add(runtimeObject);
                    mLoadedRfses.Add(r.Name.ToLower(), runtimeObject);

                    return mLoadedRfses[r.Name.ToLower()];
                }
                else
                {
                    // It's null, but not being accessed so that's okay
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        internal void RefreshFiles(string fileName)
        {
            foreach(var manager in FileManagers)
            {
                if(manager.TryHandleRefreshFile(fileName, mAddedRfses))
                {
                    break;
                }
            }
        }
    }
}
