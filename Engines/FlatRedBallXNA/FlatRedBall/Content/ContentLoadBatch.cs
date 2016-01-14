using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using FlatRedBall.Graphics.Particle;
using System.Threading;
using FlatRedBall.IO;

namespace FlatRedBall.Content
{
    /// <summary>
    /// ContentLoadBatch allows for batch loading of resources through the Content Pipeline as well as from file. Loading can be asynchronous if
    /// specified by using the LoadAsync method.
    /// </summary>
    public class ContentLoadBatch
    {

        public delegate void OnLoadFinished();

        #region Fields

        private Dictionary<string, object> mCloneables;
        private Dictionary<string, IDisposable> mDictionary;
        private Dictionary<string, Type> mBatchContents;

        private bool mThreaded;
        private Thread mLoadThread;
        private OnLoadFinished mOnFinish;
        private bool mHasFinished;
        private string mContentManagerName;

        #endregion

        #region Properties

        public bool IsLoading
        {
            get;
            set;
        }
        public bool HasFinishedLoading
        {
            get { return mHasFinished; }
        }

        #endregion

        #region Methods

        #region Constructors

        public ContentLoadBatch()
        {
            mHasFinished = false;
            IsLoading = false;
            mThreaded = false;
            mBatchContents = new Dictionary<string, Type>();
            mCloneables = new Dictionary<string, object>();

            mLoadThread = null;
        }

        #endregion

        #region Public

        public void Add<T>(string fileName)
        {
            if(FileManager.IsRelative(fileName))
                fileName = FileManager.RelativeDirectory + fileName;

            if (!HasFinishedLoading)
            {
                mBatchContents.Add(fileName, typeof(T));
            }
            else
            {
                throw new InvalidOperationException("Unable to add " + fileName + " because this batch has already been loaded.");
            }
        }

        public T Get<T>(string fileName)
        {
            T toReturn;
            string assetKey = FileManager.Standardize(FileManager.RelativeDirectory + fileName);
            assetKey = assetKey + typeof(T).Name;
           

            if (HasFinishedLoading)
            {
                if (mDictionary != null && mDictionary.ContainsKey(assetKey))
                {
                    toReturn =(T)mDictionary[assetKey];
                }

                else if (mCloneables.ContainsKey(assetKey))
                {
                    toReturn = CloneAsset<T>((T)mCloneables[assetKey]);
                }
                else if (!fileName.Contains("."))
                {
                    toReturn = FlatRedBallServices.Load<T>(fileName);
                }
                else
                {
                    throw new InvalidOperationException("Unable to return resource " + fileName + ". Content has not been loaded.");
                }
            }
            else
            {
                throw new InvalidOperationException("Unable to return resource " + fileName + ". Content has not been loaded.");
            }


            return toReturn;
        }

        public void Load(string contentManagerName)
        {
            mContentManagerName = contentManagerName;
            mThreaded = false;
            Load();
        }

        public void LoadAsync(string contentManagerName, OnLoadFinished onFinish)
        {
            if (!mHasFinished)
            {
                mContentManagerName = contentManagerName;
                mThreaded = true;
                mOnFinish = onFinish;
                ThreadStart start = new ThreadStart(Load);
                mLoadThread = new Thread(start);
                mLoadThread.Start();
            }
        }

        public void Unload()
        {

            mHasFinished = false;
            mCloneables = new Dictionary<string, object>();
            FlatRedBallServices.Unload(mContentManagerName);
        }

        #endregion

        #region Protected

        #endregion

        #region Private

        private T CloneAsset<T>(T objectToClone)
        {
            if(typeof(T) ==  typeof(FlatRedBall.Scene))
            {
                return (T)(object)(objectToClone as FlatRedBall.Scene).Clone();
            }

            else if (typeof(T) == typeof(FlatRedBall.Graphics.Particle.EmitterList))
            {
                return (T)(object)(objectToClone as FlatRedBall.Graphics.Particle.EmitterList).Clone();
            }
            else
            {
                return objectToClone;
            }

        }

        private void Load()
        {
            if (!mHasFinished)
            {
                IsLoading = true;
                if (mThreaded)
                {
#if XBOX360
                    Thread.CurrentThread.SetProcessorAffinity(5);
#endif
                }

                MethodInfo servicesLoadMethod = null;
                object[] args = null;
                object loadedObject;

                if (!string.IsNullOrEmpty(mContentManagerName))
                {

#if !XBOX360
                    servicesLoadMethod = typeof(FlatRedBallServices).GetMethod("Load", new Type[2] { typeof(String), typeof(String) });
#endif

                    args = new object[2];
                    args[1] = mContentManagerName;
                }

                else
                {
#if !XBOX360
                    servicesLoadMethod = typeof(FlatRedBallServices).GetMethod("Load", new Type[1] { typeof(String) });
#endif
                    args = new object[1];
                }

#if !XBOX360
                foreach (KeyValuePair<string, Type> pair in mBatchContents)
                {
                    args[0] = pair.Key;
                    loadedObject = servicesLoadMethod.MakeGenericMethod(pair.Value).Invoke(null, args);

                    if (pair.Value == typeof(FlatRedBall.Scene) || pair.Value == typeof(EmitterList))
                    {
                        mCloneables.Add(pair.Key + pair.Value.Name, loadedObject);
                    }
                }
#else
                foreach(KeyValuePair<string, Type> pair in mBatchContents)
                {
                    args[0] = pair.Key;
                    loadedObject = null;

                    if (pair.Value == typeof(FlatRedBall.Scene))
                    {
                        loadedObject = FlatRedBallServices.Load<FlatRedBall.Scene>((string)args[0], (string)args[1]);
                    }

                    else if (pair.Value == typeof(EmitterList))
                    {
                        loadedObject = FlatRedBallServices.Load<EmitterList>((string)args[0], (string)args[1]);
                    }

                    else if (pair.Value == typeof(Microsoft.Xna.Framework.Graphics.Texture2D))
                    {
                        loadedObject = FlatRedBallServices.Load<Microsoft.Xna.Framework.Graphics.Texture2D>((string)args[0], (string)args[1]);
                    }
/*
                    else if (pair.Value == typeof(System.Drawing.Image))
                    {
                        loadedObject = FlatRedBallServices.Load<System.Drawing.Image>((string)args[0], (string)args[1]);
                    }

                    else if (pair.Value == typeof(FlatRedBall.Graphics.Texture.BitmapList))
                    {
                        loadedObject = FlatRedBallServices.Load<FlatRedBall.Graphics.Texture.BitmapList>((string)args[0], (string)args[1]);
                    }
                    */
                    else if (pair.Value == typeof(Microsoft.Xna.Framework.Graphics.Effect))
                    {
                        loadedObject = FlatRedBallServices.Load<Microsoft.Xna.Framework.Graphics.Effect>((string)args[0], (string)args[1]);
                    }

                    else if (pair.Value == typeof(FlatRedBall.AI.Pathfinding.NodeNetwork))
                    {
                        loadedObject = FlatRedBallServices.Load<FlatRedBall.AI.Pathfinding.NodeNetwork>((string)args[0], (string)args[1]);
                    }

                    else if (pair.Value == typeof(FlatRedBall.Math.Geometry.ShapeCollection))
                    {
                        loadedObject = FlatRedBallServices.Load<FlatRedBall.Math.Geometry.ShapeCollection>((string)args[0], (string)args[1]);
                    }

                    else if (pair.Value == typeof(FlatRedBall.Math.PositionedObjectList<FlatRedBall.Math.Geometry.Polygon>))
                    {
                        loadedObject = FlatRedBallServices.Load<FlatRedBall.Math.PositionedObjectList<FlatRedBall.Math.Geometry.Polygon>>((string)args[0], (string)args[1]);
                    }

                    else if (pair.Value == typeof(FlatRedBall.Graphics.Animation.AnimationChainList))
                    {
                        loadedObject = FlatRedBallServices.Load<FlatRedBall.Graphics.Animation.AnimationChainList>((string)args[0], (string)args[1]);
                    }

                    else if (pair.Value == typeof(FlatRedBall.Gui.GuiSkin))
                    {
                        loadedObject = FlatRedBallServices.Load<FlatRedBall.Gui.GuiSkin>((string)args[0], (string)args[1]);
                    }
                
                    else if(pair.Value == typeof(FlatRedBall.IO.Csv.RuntimeCsvRepresentation)){
                        loadedObject = FlatRedBallServices.Load<FlatRedBall.IO.Csv.RuntimeCsvRepresentation>((string)args[0], (string)args[1]);
                    }

                    if (pair.Value == typeof(FlatRedBall.Scene) || pair.Value == typeof(EmitterList))
                    {
                        mCloneables.Add(pair.Key + pair.Value.Name, loadedObject);
                    }
                }
#endif


                mDictionary = FlatRedBallServices.GetDisposableDictionary(mContentManagerName);
                mHasFinished = true;
                IsLoading = false;
                if (mThreaded) 
                { 
                    mOnFinish();
                  //  mLoadThread.Abort();
                }
            }
        }

        #endregion

        #endregion

    }
}
