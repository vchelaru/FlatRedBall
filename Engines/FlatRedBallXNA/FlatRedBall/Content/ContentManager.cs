#if XBOX360 || SILVERLIGHT || WINDOWS_PHONE || ANDROID || WINDOWS_8 || IOS
#define USES_DOT_SLASH_ABOLUTE_FILES
#endif
using System;
using System.Collections.Generic;
using System.Globalization;

using System.Text;
using FileManager = FlatRedBall.IO.FileManager;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Texture;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Content.Particle;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Content.AI.Pathfinding;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Math;
using FlatRedBall.Content.Polygon;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Gui;
using FlatRedBall.Content.Saves;
using FlatRedBall.Math.Splines;
using FlatRedBall.Content.Math.Splines;
using System.Threading;

#if MONOGAME
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Audio;
#endif


#if !XBOX360 && !SILVERLIGHT && !WINDOWS_PHONE && !MONODROID && !MONOGAME
#if !XNA4
using CoreGraphics;
#endif
using Image = System.Drawing.Image;
using FlatRedBall.IO.Gif;
using FlatRedBall.IO; // For Image
#endif

#if XBOX && XNA4
using FlatRedBall.IO.Gif;
using FlatRedBall.IO;
#endif

#if !SILVERLIGHT

using FlatRedBall.IO.Csv;

#endif



#if FRB_XNA || SILVERLIGHT || WINDOWS_PHONE
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;


#elif FRB_MDX


#endif

#if XNA4
using Color = Microsoft.Xna.Framework.Color;
using Microsoft.Xna.Framework.Media;
using FlatRedBall.Content.ContentLoaders;
#endif

namespace FlatRedBall.Content
{
    //public delegate void UnloadMethod();

	public partial class ContentManager
#if FRB_XNA || SILVERLIGHT || WINDOWS_PHONE
 : Microsoft.Xna.Framework.Content.ContentManager
#endif
	{
        #region Fields

        TextureContentLoader textureContentLoader = new TextureContentLoader();

		//internal Dictionary<string, Type> mAssetTypeAssociation;
		internal Dictionary<string, object> mAssets;
		internal Dictionary<string, IDisposable> mDisposableDictionary = new Dictionary<string, IDisposable>();
		Dictionary<string, object> mNonDisposableDictionary = new Dictionary<string, object>();

		Dictionary<string, Action> mUnloadMethods = new Dictionary<string, Action>();

        /// <summary>
        /// If true FlatRedBall will look for cached content in the Global content manager even if
        /// the ContentManager passed to the Load function is not Global. This defaults to true.
        /// </summary>
        public static bool LoadFromGlobalIfExists = true;

		
#if FRB_XNA && !WINDOWS_PHONE && !MONOGAME
		internal Effect mFirstEffect = null;
		internal EffectCache mFirstEffectCache = null;
#endif

#if PROFILE
		static List<ContentLoadHistory> mHistory = new List<ContentLoadHistory>();
#endif

		string mName;

		#endregion

        #region Default Content

        #region Default Font Texture Data
#if !FRB_MDX && !SILVERLIGHT
        public static Texture2D GetDefaultFontTexture(GraphicsDevice graphicsDevice)
        {
            var colors = DefaultFontDataColors.GetColorArray();

            Texture2D texture = new Texture2D(graphicsDevice, 256, 128);
#if XNA3
            texture.SetData<Microsoft.Xna.Framework.Graphics.Color>(colors);
#else
			texture.SetData<Microsoft.Xna.Framework.Color>(colors);
#endif
            return texture;
        }
#endif
        #endregion



        #endregion

        #region Properties

        public IEnumerable<IDisposable> DisposableObjects
        {
            get
            {
                return mDisposableDictionary.Values;
            }
        }

        public List<ManualResetEvent> ManualResetEventList
		{
			get;
			private set;
		}

		public bool IsWaitingOnAsyncLoadsToFinish
		{
			get
			{
				foreach (ManualResetEvent mre in ManualResetEventList)
				{
					if (mre.WaitOne(0) == false)
					{
						return true;
					}
				}
				return false;
			}
		}

		public string Name
		{
			get { return mName; }
		}

#if PROFILE
		public static List<ContentLoadHistory> LoadingHistory
		{
			get { return mHistory; }
		}
#endif

#if DEBUG
        public static bool ThrowExceptionOnGlobalContentLoadedInNonGlobal
        {
            get;
            set;
        }
#endif

		#endregion

		#region Methods

		#region Constructors

		public ContentManager(string name, IServiceProvider serviceProvider)
#if FRB_XNA || SILVERLIGHT
			: base(serviceProvider)
#endif
		{
			mName = name;
			//    mAssetTypeAssociation = new Dictionary<string, Type>();
			mAssets = new Dictionary<string, object>();
			ManualResetEventList = new List<ManualResetEvent>();
		}

		public ContentManager(string name, IServiceProvider serviceProvider, string rootDictionary)
#if FRB_XNA || SILVERLIGHT
			: base(serviceProvider, rootDictionary)
#endif
		{
			mName = name;
			//    mAssetTypeAssociation = new Dictionary<string, Type>();
			mAssets = new Dictionary<string, object>();
			ManualResetEventList = new List<ManualResetEvent>();
		}

		#endregion

		#region Public Methods

		public void AddDisposable(string disposableName, IDisposable disposable)
		{
			if (FileManager.IsRelative(disposableName))
			{
				disposableName = FileManager.MakeAbsolute(disposableName);
			}

			disposableName = FileManager.Standardize(disposableName);

			string modifiedName = disposableName + disposable.GetType().Name;



			lock (mDisposableDictionary)
			{
				mDisposableDictionary.Add(modifiedName, disposable);
			}
		}

		public void AddNonDisposable(string objectName, object objectToAdd)
		{
			if (FileManager.IsRelative(objectName))
			{
				objectName = FileManager.MakeAbsolute(objectName);
			}

			string modifiedName = objectName + objectToAdd.GetType().Name;

			mNonDisposableDictionary.Add(modifiedName, objectToAdd);
		}

		public void AddUnloadMethod(string uniqueID, Action unloadMethod)
		{
            if (!mUnloadMethods.ContainsKey(uniqueID))
            {
                mUnloadMethods.Add(uniqueID, unloadMethod);
            }
		}

        public T GetDisposable<T>(string objectName)
        {
            if (FileManager.IsRelative(objectName))
            {
                objectName = FileManager.MakeAbsolute(objectName);
            }

            objectName += typeof(T).Name;

            return (T)mDisposableDictionary[objectName];
        }

		// This used to be internal - making it public since users use this
		public T GetNonDisposable<T>(string objectName)
		{
			if (FileManager.IsRelative(objectName))
			{
				objectName = FileManager.MakeAbsolute(objectName);
			}

			objectName += typeof(T).Name;

			return (T)mNonDisposableDictionary[objectName];
		}

		public bool IsAssetLoadedByName<T>(string assetName)
		{
			if (FileManager.IsRelative(assetName))
			{
				assetName = FileManager.MakeAbsolute(assetName);
			}

			assetName = FileManager.Standardize(assetName);

			string combinedName = assetName + typeof(T).Name;

			if (mDisposableDictionary.ContainsKey(combinedName))
			{
				return true;
			}
			else if (mNonDisposableDictionary.ContainsKey(combinedName))
			{
				return true;
			}

			else if (mAssets.ContainsKey(combinedName))
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool IsAssetLoadedByReference(object objectToCheck)
		{
			return mDisposableDictionary.ContainsValue(objectToCheck as IDisposable) ||
				mNonDisposableDictionary.ContainsValue(objectToCheck) ||
				mAssets.ContainsValue(objectToCheck);
		}


#if FRB_MDX
		public T Load<T>(string assetName)
#else
		// Ok, let's explain why this method uses the "new" keyword.
		// In the olden days (before September 2009) the user would call
		// FlatRedBallServices.Load<TypeToLoad> which would investigate whether
		// the user passed an assetName that had an extension or not.  Then the
		// FlatRedBallServices class would call LoadFromFile or LoadFromProject
		// depending on the presence of an extension.
		//
		// In an effort to reduce the responsibilities of the FlatRedBallServices
		// class, Vic decided to move the loading code (including the logic that branches
		// depending on whether an asset has an extension) into the ContentManager class.
		// To keep things simple, the FlatRedBallServices just has to call Load and the Load
		// method will do the branching inside the ContentManager class.  That all worked well,
		// except that the branching code also "standardizes" the name, which means it turns relative
		// paths into absolute paths.  The reason this is a problem is IF the user loads an object (such
		// as a .X file) which references another file, then the Load method will be called again on the referenced
		// asset name.
		//
		// The FRB ContentManager doesn't use relative directories - instead, it makes all assets relative to the .exe
		// by calling Standardize.  However, if Load is called by XNA code (such as when loading a .X file)
		// then any files referenced by the X will come in already made relative to the ContentManager.  
		// This means that if a .X file was "Content\myModel" and it referenced myTexture.png which was in the same
		// folder as the .X file, then Load would get called with "Content\myTexture" as the argument.  That is, the .X loading
		// code would already prepend "Content\" before "myTexture.  But the FRB content manager wouldn't know this, and it'd
		// try to standardize it as well, making the file "Content\Content\myTexture."
		//
		// So the solution?  We make Load a "new" method.  That means that if Load is called by XNA, then it'll call the
		// Load of XNA's ContentManager.  But if FRB calls it, it'll call the "new" version.  Problem solved.
		public new T Load<T>(string assetName)
#endif
		{
			// Assets can be loaded either from file or from assets referenced
			// in the project.
			string extension = FileManager.GetExtension(assetName);

			#region If there is an extension, loading from file or returning an already-loaded asset
			assetName = FileManager.Standardize(assetName);

			if (extension != String.Empty)
			{
				return LoadFromFile<T>(assetName);
			}

			#endregion
#if !FRB_MDX

			#region Else there is no extension, so the file is already part of the project.  Use a ContentManager
			else
			{
#if PROFILE
				bool exists = false;

				exists = IsAssetLoadedByName<T>(assetName);
				
				if (exists)
				{
					mHistory.Add(new ContentLoadHistory(
						TimeManager.CurrentTime, typeof(T).Name, assetName, ContentLoadDetail.Cached));
				}
				else
				{
					mHistory.Add(new ContentLoadHistory(
						TimeManager.CurrentTime, typeof(T).Name, assetName, ContentLoadDetail.HddFromContentPipeline));
				}
#endif



				return LoadFromProject<T>(assetName);
			}
			#endregion

#else
			return default(T); // This gets reached only in FRB_MDX
#endif
		}


#if FRB_XNA || SILVERLIGHT || WINDOWS_PHONE
		public T LoadFromProject<T>(string assetName)
		{
			string oldRelativePath = FileManager.RelativeDirectory;
			FlatRedBall.IO.FileManager.RelativeDirectory = FileManager.GetDirectory(assetName);




#if DEBUG && !SILVERLIGHT && !WINDOWS_PHONE

			bool shouldCheckForXnb = true;

		#if ANDROID 
			if(typeof(T) == typeof(Song))
			{
				shouldCheckForXnb = false;
			}
		#endif


			string fileToCheckFor = assetName + ".xnb";


			if (shouldCheckForXnb && !FileManager.FileExists(fileToCheckFor))
			{
				string errorString = "Could not find the file " + fileToCheckFor + "\n";

#if !WINDOWS_8
				List<string> filesInDirectory = FileManager.GetAllFilesInDirectory(FileManager.GetDirectory(assetName), null, 0);

				errorString += "Found the following files:\n\n";

				foreach (string s in filesInDirectory)
				{

					errorString += FileManager.RemovePath(s) + "\n";
				}

#endif
				throw new FileNotFoundException(errorString);
			}
#endif

#if XNA4 && !XBOX360 && !WINDOWS_PHONE && !MONOGAME
			if (!FileManager.IsRelative(assetName))
			{
				assetName = FileManager.MakeRelative(
					assetName, System.Windows.Forms.Application.StartupPath + "/");
			}
#endif

#if USES_DOT_SLASH_ABOLUTE_FILES

            T asset;

			if (assetName.StartsWith(@".\") || assetName.StartsWith(@"./"))
			{
				asset = base.Load<T>(assetName.Substring(2));
			}
			else
			{
				asset = base.Load<T>(assetName);

			}
#else
			T asset = base.Load<T>(assetName);
#endif
			if (!mAssets.ContainsKey(assetName))
			{
				mAssets.Add(assetName, asset);
			}

			FileManager.RelativeDirectory = oldRelativePath;

			return AdjustNewAsset(asset, assetName);
		}
#endif

		public T LoadFromFile<T>(string assetName)
		{
			string extension = FileManager.GetExtension(assetName);

			if (FileManager.IsRelative(assetName))
			{
				// get the absolute path using the current relative directory
				assetName = FileManager.RelativeDirectory + assetName;
			}



			string fullNameWithType = assetName + typeof(T).Name;


			// get the dictionary by the contentManagerName.  If it doesn't exist, GetDisposableDictionaryByName
			// will create it.

			if (mDisposableDictionary.ContainsKey(fullNameWithType))
			{

#if PROFILE
				mHistory.Add(new ContentLoadHistory(
					TimeManager.CurrentTime, typeof(T).Name, fullNameWithType, ContentLoadDetail.Cached));
#endif

				return ((T)mDisposableDictionary[fullNameWithType]);
			}
			else if (mNonDisposableDictionary.ContainsKey(fullNameWithType))
			{
				return ((T)mNonDisposableDictionary[fullNameWithType]);
			}
			else
			{
#if PROFILE
				mHistory.Add(new ContentLoadHistory(
					TimeManager.CurrentTime, 
					typeof(T).Name, 
					fullNameWithType, 
					ContentLoadDetail.HddFromFile));
#endif
#if DEBUG
				// The ThrowExceptionIfFileDoesntExist
				// call used to be done before the checks
				// in the dictionaries.  But whatever is held
				// in there may not really be a file so let's check
				// if the file exists after we check the dictionaries.
				FileManager.ThrowExceptionIfFileDoesntExist(assetName);
#endif

				IDisposable loadedAsset = null;

				if (typeof(T) == typeof(Texture2D) || typeof(T) == typeof(Microsoft.Xna.Framework.Graphics.Texture2D))
				{
                    // for now we'll create it here, eventually have it in a dictionary:
					loadedAsset = textureContentLoader.Load(assetName);
				}

				#region Scene

				else if (typeof(T) == typeof(FlatRedBall.Scene))
				{
					FlatRedBall.Scene scene = FlatRedBall.Content.Scene.SceneSave.FromFile(assetName).ToScene(mName);

					object sceneAsObject = scene;

					lock (mNonDisposableDictionary)
					{
						if (!mNonDisposableDictionary.ContainsKey(fullNameWithType))
						{
							mNonDisposableDictionary.Add(fullNameWithType, scene);
						}
					}
					return (T)sceneAsObject;
				}

				#endregion

				#region EmitterList

				else if (typeof(T) == typeof(EmitterList))
				{
					EmitterList emitterList = EmitterSaveList.FromFile(assetName).ToEmitterList(mName);


					mNonDisposableDictionary.Add(fullNameWithType, emitterList);


					return (T)((object)emitterList);

				}

				#endregion

				#region Image
#if !XBOX360 && !SILVERLIGHT && !WINDOWS_PHONE && !MONOGAME
				else if (typeof(T) == typeof(Image))
				{

                    switch (extension.ToLowerInvariant())
					{
						case "gif":
							Image image = Image.FromFile(assetName);
							loadedAsset = image;
							break;
					}

				}
#endif
				#endregion

				#region BitmapList
#if !XBOX360 && !SILVERLIGHT && !WINDOWS_PHONE && !MONOGAME

				else if (typeof(T) == typeof(BitmapList))
				{
					loadedAsset = BitmapList.FromFile(assetName);

				}
#endif

				#endregion

				#region NodeNetwork
				else if (typeof(T) == typeof(NodeNetwork))
				{
					NodeNetwork nodeNetwork = NodeNetworkSave.FromFile(assetName).ToNodeNetwork();

					mNonDisposableDictionary.Add(fullNameWithType, nodeNetwork);

					return (T)((object)nodeNetwork);
				}
				#endregion

				#region ShapeCollection

				else if (typeof(T) == typeof(ShapeCollection))
				{
					ShapeCollection shapeCollection =
						ShapeCollectionSave.FromFile(assetName).ToShapeCollection();

					mNonDisposableDictionary.Add(fullNameWithType, shapeCollection);

					return (T)((object)shapeCollection);
				}
				#endregion

				#region PositionedObjectList<Polygon>

				else if (typeof(T) == typeof(PositionedObjectList<FlatRedBall.Math.Geometry.Polygon>))
				{
					PositionedObjectList<FlatRedBall.Math.Geometry.Polygon> polygons =
						PolygonSaveList.FromFile(assetName).ToPolygonList();
					mNonDisposableDictionary.Add(fullNameWithType, polygons);
					return (T)((object)polygons);
				}

				#endregion

				#region AnimationChainList

				else if (typeof(T) == typeof(AnimationChainList))
				{

					if (assetName.EndsWith("gif"))
					{
#if WINDOWS_8 || UWP
                        throw new NotImplementedException();
#else
						AnimationChainList acl = new AnimationChainList();
						acl.Add(FlatRedBall.Graphics.Animation.AnimationChain.FromGif(assetName, this.mName));
						acl[0].ParentGifFileName = assetName;
						loadedAsset = acl;
#endif
					}
					else
					{
						loadedAsset =
							AnimationChainListSave.FromFile(assetName).ToAnimationChainList(mName);


					}

					mNonDisposableDictionary.Add(fullNameWithType, loadedAsset);
				}

				#endregion

				else if(typeof(T) == typeof(Song))
				{
                    var loader = new SongLoader();
                    return (T)(object) loader.Load(assetName);
				}
#if MONOGAME

                else if (typeof(T) == typeof(SoundEffect))
                {
                    T soundEffect;

                    if (assetName.StartsWith(@".\") || assetName.StartsWith(@"./"))
                    {
                        soundEffect = base.Load<T>(assetName.Substring(2));
                    }
                    else
                    {
                        soundEffect = base.Load<T>(assetName);

                    }

                    return soundEffect;
                }
#endif

                #region RuntimeCsvRepresentation

#if !SILVERLIGHT
                else if (typeof(T) == typeof(RuntimeCsvRepresentation))
                {
#if XBOX360
					throw new NotImplementedException("Can't load CSV from file.  Try instead to use the content pipeline.");
#else

                    return (T)((object)CsvFileManager.CsvDeserializeToRuntime(assetName));
#endif
                }
#endif


                #endregion

                #region SplineList

                else if (typeof(T) == typeof(List<Spline>))
                {
                    List<Spline> splineList = SplineSaveList.FromFile(assetName).ToSplineList();
                    mNonDisposableDictionary.Add(fullNameWithType, splineList);
                    object asObject = splineList;

                    return (T)asObject;

                }

                else if (typeof(T) == typeof(SplineList))
                {
                    SplineList splineList = SplineSaveList.FromFile(assetName).ToSplineList();
                    mNonDisposableDictionary.Add(fullNameWithType, splineList);
                    object asObject = splineList;

                    return (T)asObject;
                }

                #endregion

                #region BitmapFont

                else if (typeof(T) == typeof(BitmapFont))
                {
                    // We used to assume the texture is named the same as the font file
                    // But now FRB understands the .fnt file and gets the PNG from the font file
                    //string pngFile = FileManager.RemoveExtension(assetName) + ".png";
                    string fntFile = FileManager.RemoveExtension(assetName) + ".fnt";

                    BitmapFont bitmapFont = new BitmapFont(fntFile, this.mName);

                    object bitmapFontAsObject = bitmapFont;

                    return (T)bitmapFontAsObject;
                }

                #endregion


                #region Text

                else if (typeof(T) == typeof(string))
                {
                    return (T)((object)FileManager.FromFileText(assetName));
                }

                #endregion

                #region Catch mistakes

#if DEBUG
                else if (typeof(T) == typeof(Spline))
                {
                    throw new Exception("Cannot load Splines.  Try using the List<Spline> type instead.");

                }
                else if (typeof(T) == typeof(Emitter))
                {
                    throw new Exception("Cannot load Emitters.  Try using the EmitterList type instead.");

                }
#endif

                #endregion

                #region else, exception!

                else
                {
                    throw new NotImplementedException("Cannot load content of type " +
                        typeof(T).AssemblyQualifiedName + " from file.  If you are loading " +
                        "through the content pipeline be sure to remove the extension of the file " +
                        "name.");
                }

				#endregion

				if (loadedAsset != null)
				{
					lock (mDisposableDictionary)
					{
						// Multiple threads could try to load this content simultaneously
						if (!mDisposableDictionary.ContainsKey(fullNameWithType))
						{
							mDisposableDictionary.Add(fullNameWithType, loadedAsset);
						}
					}
				}

				return ((T)loadedAsset);
			}
		}

        /// <summary>
        /// Removes an IDisposable from the ContentManager.  This method does not call Dispose on the argument Disposable.  It 
        /// must be disposed 
        /// </summary>
        /// <param name="disposable">The IDisposable to be removed</param>
        public void RemoveDisposable(IDisposable disposable)
        {
            KeyValuePair<string, IDisposable>? found = null;
            foreach(var kvp in mDisposableDictionary)
            {
                if(kvp.Value == disposable)
                {
                    found = kvp;
                    break;
                }
            }
            if(found != null)
            {
                mDisposableDictionary.Remove(found.Value.Key);
            }
        }




		public void UnloadAsset<T>(T assetToUnload)
		{
			#region Remove from non-disposables if the non-disposables containes the assetToUnload
			if (this.mNonDisposableDictionary.ContainsValue(assetToUnload))
			{
				string assetName = "";

				foreach (KeyValuePair<string, object> kvp in mNonDisposableDictionary)
				{
					if (kvp.Value == assetToUnload as object)
					{
						assetName = kvp.Key;
						break;
					}
				}

				mNonDisposableDictionary.Remove(assetName);

			}
			#endregion

			#region If it's an IDisposable, then remove it from the disposable dictionary
			if (assetToUnload is IDisposable)
			{
				IDisposable asDisposable = assetToUnload as IDisposable;

				if (this.mDisposableDictionary.ContainsValue(asDisposable))
				{
					asDisposable.Dispose();

					string assetName = "";

					foreach (KeyValuePair<string, IDisposable> kvp in mDisposableDictionary)
					{
						if (kvp.Value == ((IDisposable)assetToUnload))
						{
							assetName = kvp.Key;
							break;
						}
					}

					mDisposableDictionary.Remove(assetName);
				}
				else
				{
					throw new ArgumentException("The content manager " + mName + " does not contain the argument " +
						"assetToUnload.  Check the " +
						"contentManagerName and verify that it is a contentManager that has loaded this asset.  Assets which have been " +
						"loaded from the project must be loaded by unloading an entire Content Manager");
				}
			}
			#endregion
		}

		public new void Unload()
		{
			if (IsWaitingOnAsyncLoadsToFinish)
			{
				FlatRedBallServices.MoveContentManagerToWaitingToUnloadList(this);
			}
			else
			{
#if FRB_XNA || SILVERLIGHT || WINDOWS_PHONE
				base.Unload();
#endif

				this.mAssets.Clear();
				this.mNonDisposableDictionary.Clear();

				foreach (KeyValuePair<string, IDisposable> kvp in mDisposableDictionary)
				{
					kvp.Value.Dispose();
				}

				foreach (Action unloadMethod in mUnloadMethods.Values)
				{
					unloadMethod();
				}
				mUnloadMethods.Clear();

				mDisposableDictionary.Clear();
			}
		}

#if PROFILE
		public static void RecordEvent(string eventName)
		{
			ContentLoadHistory history = new ContentLoadHistory();
			history.SpecialEvent = eventName;

			mHistory.Add(history);
		}

		public static void SaveContentLoadingHistory(string fileToSaveTo)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (ContentLoadHistory clh in mHistory)
			{
				if (!string.IsNullOrEmpty(clh.SpecialEvent))
				{
					stringBuilder.AppendLine("========================================");
					stringBuilder.AppendLine(clh.SpecialEvent);
					stringBuilder.AppendLine("========================================");
					stringBuilder.AppendLine();
				}
				else
				{
					stringBuilder.AppendLine("Time:\t\t" + clh.Time);
					stringBuilder.AppendLine("Content Type:\t" + clh.Type);
					stringBuilder.AppendLine("Content Name:\t" + clh.ContentName);
					stringBuilder.AppendLine("Detail:\t\t" + clh.ContentLoadDetail);
					stringBuilder.AppendLine();
				}
			}

			FileManager.SaveText(stringBuilder.ToString(), fileToSaveTo);
		}

#endif

		#endregion

		#region Internal Methods

		// Vic says: I don't think we need this anymore
		internal void RefreshTextureOnDeviceLost()
		{
			//List<string> texturesToReload = new List<string>();

			//foreach(KeyValuePair<string, IDisposable> kvp in mDisposableDictionary)
			//{
			//    if (kvp.Value is Texture2D)
			//    {
			//        texturesToReload.Add(kvp.Key);
			//        kvp.Value.Dispose();

			//        contentManagers.Add(kvp.Key);
			//        fileNames.Add(subKvp.Key);
			//    }
			//}

			//kvp.Value.Clear();
		}

		#endregion

		#region Private Methods

		private T AdjustNewAsset<T>(T asset, string assetName)
		{
#if FRB_XNA

			if (asset is Microsoft.Xna.Framework.Graphics.Texture)
			{
				(asset as Microsoft.Xna.Framework.Graphics.Texture).Name = assetName;
			}

#if WINDOWS_PHONE || WINDOWS_8 || MONOGAME
			// do nothing???
#else

			else if (mFirstEffect == null && asset is Effect)
			{
				lock (Renderer.GraphicsDevice)
				{
					mFirstEffect = asset as Effect;
					mFirstEffectCache = new EffectCache(mFirstEffect, true);
				}
			}
#endif
#elif SILVERLIGHT

			if (asset is Texture2D)
			{
				(asset as Texture2D).Name = assetName;
			}

#endif

			if (asset is FlatRedBall.Scene)
			{
				return (T)(object)((asset as FlatRedBall.Scene).Clone());
			}
			else if (asset is FlatRedBall.Graphics.Particle.EmitterList)
			{
				return (T)(object)(asset as FlatRedBall.Graphics.Particle.EmitterList).Clone();
			}
			else
			{
				return asset;
			}

		}

		#endregion

		#endregion
	}
}