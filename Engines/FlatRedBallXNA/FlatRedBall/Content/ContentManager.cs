#if ANDROID || IOS
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

using Microsoft.Xna.Framework.Media;
#if MONOGAME || FNA
using Microsoft.Xna.Framework.Audio;
#endif


#if !MONOGAME && !FNA
using Image = System.Drawing.Image;
using FlatRedBall.IO.Gif;
using FlatRedBall.IO; // For Image
#endif


using FlatRedBall.IO.Csv;



using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

using Color = Microsoft.Xna.Framework.Color;
using FlatRedBall.Content.ContentLoaders;
using FlatRedBall.Performance.Measurement;
using FlatRedBall.IO;

#if NET6_0_OR_GREATER
using FlatRedBall.Content.Aseprite;
#endif

namespace FlatRedBall.Content
{
    //public delegate void UnloadMethod();

	public partial class ContentManager : Microsoft.Xna.Framework.Content.ContentManager
	{
        #region Fields

        TextureContentLoader textureContentLoader = new TextureContentLoader();

		//internal Dictionary<string, Type> mAssetTypeAssociation;
		internal Dictionary<string, object> mAssets = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

		internal Dictionary<string, IDisposable> mDisposableDictionary = new Dictionary<string, IDisposable>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, object> mNonDisposableDictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

		Dictionary<string, Action> mUnloadMethods = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// If true FlatRedBall will look for cached content in the Global content manager even if
        /// the ContentManager passed to the Load function is not Global. This defaults to true.
        /// </summary>
        public static bool LoadFromGlobalIfExists = true;

        public static Dictionary<FilePath, FilePath> FileAliases { get; private set; } = new Dictionary<FilePath, FilePath>();
		
#if FRB_XNA && !MONOGAME
		internal Effect mFirstEffect = null;
		internal EffectCache mFirstEffectCache = null;
#endif

#if PROFILE
		static List<ContentLoadHistory> mHistory = new List<ContentLoadHistory>();
#endif

		string mName;

		#endregion

        #region Default Content

        public static Texture2D GetDefaultFontTexture(GraphicsDevice graphicsDevice)
        {
#if ANDROID
            var activity = FlatRedBallServices.Game.Services.GetService<Android.App.Activity>();

            if(activity == null)
            {
                string message =
                    "As of July 2017, FlatRedBall Android performs a much faster loading of the default font. This requires a change to the Activity1.cs file. You can look at a brand-new Android template to see the required changes.";

                throw new NullReferenceException(message);
            }


            Android.Content.Res.AssetManager androidAssetManager = activity.Assets;
            Texture2D texture;

            try
            {

				var fileName = "Content/defaultfonttexture.png";

#if !NET8_0
				fileName = "content/defaultfonttexture.png";

#endif

                using (var stream = androidAssetManager.Open(fileName))
                {
                    texture = Texture2D.FromStream(graphicsDevice, stream);
                }
            }
            catch
            {
                throw new Exception("The file defaultfonttexture.png in the game's content folder is missing. If you are missing this file, look at the default Android template to see where it should be added (in Assets/content/)");
            }

#else


				var colors = DefaultFontDataColors.GetColorArray();

            Texture2D texture = new Texture2D(graphicsDevice, 256, 128);

			texture.SetData<Microsoft.Xna.Framework.Color>(colors);

#endif

            return texture;
        }

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
			: base(serviceProvider)
		{
			mName = name;
			//    mAssetTypeAssociation = new Dictionary<string, Type>();
			ManualResetEventList = new List<ManualResetEvent>();
		}

		public ContentManager(string name, IServiceProvider serviceProvider, string rootDictionary)
			: base(serviceProvider, rootDictionary)
		{
			mName = name;
			//    mAssetTypeAssociation = new Dictionary<string, Type>();
			ManualResetEventList = new List<ManualResetEvent>();
		}

#endregion

		#region Public Methods

        /// <summary>
        /// Adds the argument disposable object to the content manager, to be disposed when the ContentManager Unload method is eventually called.
        /// </summary>
        /// <remarks>
        /// This method is used for objects which either need to be cached and obtained later (such as custom from-file content) or which
        /// is not usually referenced by its key but which does noeed to be disposed later (such as a RenderTarget2D).
        /// </remarks>
        /// <param name="disposableName">The name of the disposable, so that it can be retrieved later if needed.</param>
        /// <param name="disposable">The disposable object to be added for disposal upon Unload.</param>
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

            bool shouldAdd = true;

            if(mNonDisposableDictionary.ContainsKey(modifiedName))
            {
                var existing = objectToAdd;
                if(existing != objectToAdd)
                {
                    throw new InvalidOperationException($"The name {objectName} is already taken by {objectToAdd}");
                }
                else
                {
                    shouldAdd = false;
                }
            }

            if(shouldAdd)
            {
                mNonDisposableDictionary.Add(modifiedName, objectToAdd);
            }
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

			var assetNameNoExtension = FileManager.RemoveExtension(assetName);

			string combinedName = assetName + typeof(T).Name;

			if (mDisposableDictionary.ContainsKey(combinedName))
			{
				return true;
			}
			else if (mNonDisposableDictionary.ContainsKey(combinedName))
			{
				return true;
			}

			else if (mAssets.ContainsKey(assetNameNoExtension))
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
		{
            var standardized = FileManager.Standardize(assetName);

            if(FileAliases.ContainsKey(standardized))
            {
                assetName = FileAliases[standardized].FullPath;
            }

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

		}


		public T LoadFromProject<T>(string assetName)
		{
			string oldRelativePath = FileManager.RelativeDirectory;
			FlatRedBall.IO.FileManager.RelativeDirectory = FileManager.GetDirectory(assetName);




#if DEBUG

			bool shouldCheckForXnb = true;

			string fileToCheckFor = assetName + ".xnb";


			if (shouldCheckForXnb && !FileManager.FileExists(fileToCheckFor))
			{
				// Restore the old RelativeDirectory just in case the user intentionally 
				// catches the exception and RelativeDirectory is left with an invalid path.
				// This invalid path could make the next content load fail.
				FileManager.RelativeDirectory = oldRelativePath;

				string errorString = "Could not find the file " + fileToCheckFor + "\n";

				throw new FileNotFoundException(errorString);
			}
#endif

#if !MONOGAME && !FNA
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

		public T LoadFromFile<T>(string assetName)
		{
			string extension = FileManager.GetExtension(assetName);

			if (FileManager.IsRelative(assetName))
			{
				// get the absolute path using the current relative directory
				assetName = FileManager.RelativeDirectory + assetName;
			}



			string fullNameWithType = assetName + typeof(T).Name;
			string fullNameStandardizeWithType = FileManager.Standardize(assetName) + typeof(T).Name;

			// get the dictionary by the contentManagerName.  If it doesn't exist, GetDisposableDictionaryByName
			// will create it.

			if (mDisposableDictionary.ContainsKey(fullNameStandardizeWithType))
			{

#if PROFILE
				mHistory.Add(new ContentLoadHistory(
					TimeManager.CurrentTime, typeof(T).Name, fullNameWithType, ContentLoadDetail.Cached));
#endif

				return ((T)mDisposableDictionary[fullNameStandardizeWithType]);
			}
			else if (mNonDisposableDictionary.ContainsKey(fullNameStandardizeWithType))
			{
				return ((T)mNonDisposableDictionary[fullNameStandardizeWithType]);
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
#if !WEB
// can't check if a file exists (for now) on web, so just skip throwing an exception...
				FileManager.ThrowExceptionIfFileDoesntExist(assetName);
#endif
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
#if !MONOGAME && !FNA
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
#if !MONOGAME && !FNA

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
                        throw new NotImplementedException();

						// We used to support gif => AnimationChain but this is being
						// dropped. It could be added in the future if needed.
						//AnimationChainList acl = new AnimationChainList();
						//acl.Add(FlatRedBall.Graphics.Animation.AnimationChain.FromGif(assetName, this.mName));
						//acl[0].ParentGifFileName = assetName;
						//loadedAsset = acl;
					}
#if NET6_0_OR_GREATER
                    else if(assetName.EndsWith("aseprite") || assetName.EndsWith("ase"))
					{
						loadedAsset = AsepriteFileLoader.Load(assetName).ToAnimationChainList();
					}
#endif
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
#if MONOGAME || FNA

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

				else if(typeof(T) == typeof(Effect))
				{
					return base.Load<T>(assetName);
				}

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
						if (!mDisposableDictionary.ContainsKey(fullNameStandardizeWithType))
						{
							mDisposableDictionary.Add(fullNameStandardizeWithType, loadedAsset);
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

        public override string ToString()
        {
			return $"{Name} with {mDisposableDictionary.Count} disposables, {mNonDisposableDictionary.Count} non disposables, {mAssets.Count} assets";
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
						"assetToUnload, or the file has been loaded from XNB and cannot be unloaded without disposing the content manager.  " +
						"Check the " +
						"contentManagerName and verify that it is a contentManager that has loaded this asset.");
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
				base.Unload();

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
			if (asset is Microsoft.Xna.Framework.Graphics.Texture)
			{
				(asset as Microsoft.Xna.Framework.Graphics.Texture).Name = assetName;
			}
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