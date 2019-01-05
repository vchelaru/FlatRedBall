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
using FlatRedBall.Glue.IO;

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
        List<LoadedFile> mLoadedRfses = new List<LoadedFile>();
        List<LoadedFile> mAddedRfses = new List<LoadedFile>();

        List<ShapeCollection> mLoadedShapeCollections = new List<ShapeCollection>();
        List<NodeNetwork> mLoadedNodeNetworks = new List<NodeNetwork>();
        List<EmitterList> mLoadedEmitterLists = new List<EmitterList>();

        public static List<RuntimeFileManager> FileManagers
        { get; private set; } = new List<RuntimeFileManager>();


        #endregion

        public List<LoadedFile> LoadedRfses
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
                    .Where(item => item.RuntimeObject is Scene)
                    .Select(item => (Scene)item.RuntimeObject)
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
            FileManagers.Add(new EmitterListRuntimeFileManager());
            FileManagers.Add(new NodeNetworkRuntimeFileManager());
            FileManagers.Add(new SceneRuntimeFileManager());
            FileManagers.Add(new ShapeCollectionRuntimeFileManager());
            FileManagers.Add(new SplineListRuntimeFileManager());
        }


        public void Add(LoadedFile loadedFile)
        {
            if (loadedFile.RuntimeObject is NodeNetwork)
            {
                LoadedNodeNetworks.Add(loadedFile.RuntimeObject as NodeNetwork);
            }
            else if(loadedFile.RuntimeObject is EmitterList)
            {
                LoadedEmitterLists.Add(loadedFile.RuntimeObject as EmitterList);
            }
            else if(loadedFile.RuntimeObject is ShapeCollection)
            {
                LoadedShapeCollections.Add(loadedFile.RuntimeObject as ShapeCollection);
            }

            mAddedRfses.Add(loadedFile);
        }


        public void Activity()
        {
            foreach(var manager in FileManagers)
            {
                manager.Activity(LoadedRfses);
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
                manager.RemoveFromManagers(mAddedRfses);
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
            foreach(var item in mAddedRfses.Where(item =>item.RuntimeObject is SplineList)
                .Select(item =>item.RuntimeObject as SplineList))
            {
                item.RemoveFromManagers();
            }


            mLoadedShapeCollections.Clear();
            mLoadedNodeNetworks.Clear();
            mLoadedEmitterLists.Clear();

            mAddedRfses.Clear();
            mLoadedRfses.Clear();
        }

        public void Destroy(FilePath filePath)
        {
            foreach(var loadedObject in mLoadedRfses.Where(item => item.FilePath == filePath))
            {
                foreach(var manager in FileManagers)
                {
                    if(manager.TryDestroy(loadedObject, mAddedRfses))
                    {
                        break;
                    }
                }

                if (loadedObject.RuntimeObject is ShapeCollection)
                {
                    ((ShapeCollection)loadedObject.RuntimeObject).RemoveFromManagers();
                    mLoadedShapeCollections.Remove((ShapeCollection)loadedObject.RuntimeObject);
                }
                else if (loadedObject.RuntimeObject is NodeNetwork)
                {
                    ((NodeNetwork)loadedObject.RuntimeObject).Visible = false;
                    mLoadedNodeNetworks.Remove((NodeNetwork)loadedObject.RuntimeObject);
                }
                else if (loadedObject.RuntimeObject is EmitterList)
                {
                    ((EmitterList)loadedObject.RuntimeObject).RemoveFromManagers();
                    mLoadedEmitterLists.Remove((EmitterList)loadedObject.RuntimeObject);
                }
                mAddedRfses.Remove(loadedObject);
            }
        }

        public void DestroyRuntime(object runtime)
        {
            foreach (var manager in FileManagers)
            {
                if(manager.DestroyRuntimeObject(runtime))
                {
                    break;
                }
            }
        }

        private object LoadRfsAndAddToLists(ReferencedFileSave r, IElement container)
        {
            LoadedFile loadedFile = null;
            bool isAlreadyAdded = false;

            FilePath filePath =
                ElementRuntime.ContentDirectory + r.Name;

            loadedFile = mLoadedRfses.FirstOrDefault(item => item.FilePath == filePath);
            if (loadedFile != null)
            {
                if(mAddedRfses.Any(item =>item.FilePath == filePath) == false &&
                    (r.IsSharedStatic == false || container is ScreenSave))
                {
                    foreach(var manager in FileManagers)
                    {
                        if(manager.AddToManagers(loadedFile))
                        {
                            mAddedRfses.Add(loadedFile);
                            break;
                        }
                    }
                }
            }
            else
            {
                foreach(var manager in FileManagers)
                {
                    loadedFile = manager.TryCreateFile(r, container);
                    if (loadedFile != null)
                    {
                        if ((!r.IsSharedStatic || container is ScreenSave) && loadedFile != null)
                        {
                            LoadedRfses.Add(loadedFile);

                            manager.AddToManagers(loadedFile);
                            mAddedRfses.Add(loadedFile);
                        }
                        break;
                    }
                }
            }




            if(loadedFile == null)
            {
                string extension = FileManager.GetExtension(r.Name).ToLower();
                object runtimeObject = null;

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

                if(runtimeObject != null)
                {
                    loadedFile = new LoadedFile();
                    loadedFile.RuntimeObject = runtimeObject;
                    loadedFile.ReferencedFileSave = r;
                    loadedFile.FilePath = ElementRuntime.ContentDirectory + r.Name;

                    LoadedRfses.Add(loadedFile);
                }
            }


            return loadedFile?.RuntimeObject;
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

        public LoadedFile LoadReferencedFileSave(ReferencedFileSave r, bool isBeingAccessed, IElement container)
        {
            //////////////////EARLY OUT///////////////////
            if (r.LoadedAtRuntime == false)
            {
                return null;
            }


            FilePath fileToLoad = ElementRuntime.ContentDirectory + r.Name;

            var alreadyLoaded = mLoadedRfses.FirstOrDefault(item => item.FilePath == fileToLoad);
            if (alreadyLoaded != null)
            {
                // do nothing, it's already been loaded)
                return alreadyLoaded;
            }
            /////////////END EARLY OUT//////////////////////

            var shouldLoad = isBeingAccessed || r.IsSharedStatic;

            if(shouldLoad)
            {

                object runtimeObject = LoadRfsAndAddToLists(r, container);

                if (isBeingAccessed || runtimeObject != null)
                {
                    var loadedFile = new LoadedFile();
                    loadedFile.FilePath = fileToLoad;
                    loadedFile.ReferencedFileSave = r;
                    loadedFile.RuntimeObject = runtimeObject;

                    mAddedRfses.Add(loadedFile);
                    mLoadedRfses.Add(loadedFile);

                    return loadedFile;
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

        public object CreateAndAddEmptyCloneOf(object originalObject)
        {
            object cloneToReturn = null;
            foreach (var manager in FileManagers)
            {
                cloneToReturn = manager.CreateEmptyObjectMatchingArgumentType(originalObject);

                if(cloneToReturn != null)
                {
                    var loadedFile = new LoadedFile();
                    loadedFile.FilePath = null;
                    loadedFile.ReferencedFileSave = null;
                    loadedFile.RuntimeObject = cloneToReturn;

                    mLoadedRfses.Add(loadedFile);
                    mAddedRfses.Add(loadedFile);
                    break;
                }
            }
            return cloneToReturn;
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
