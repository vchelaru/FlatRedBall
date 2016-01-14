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

		// This can turn off generating mipmaps which can save memory and solves some rendering bugs on Samsung devices.
		public static bool CreateMipMaps = true;

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

#if !SILVERLIGHT
			if (FileManager.IsRelative(assetName))
			{
				// get the absolute path using the current relative directory
				assetName = FileManager.RelativeDirectory + assetName;
			}

#endif



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

				#region Texture2D
				if (typeof(T) == typeof(Texture2D) || typeof(T) == typeof(Microsoft.Xna.Framework.Graphics.Texture2D))
				{

					loadedAsset = LoadTexture2D(assetName, extension, loadedAsset);
				}
				#endregion

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

				#region Effect
#if !FRB_MDX && !SILVERLIGHT && !XNA4 && !WINDOWS_8
#if !XBOX360
				else if (typeof(T) == typeof(Effect))
				{
					lock (Renderer.GraphicsDevice)
					{
						CompiledEffect compiledEffect =
							Effect.CompileEffectFromFile(assetName,
							null, null,
#if DEBUG
 CompilerOptions.Debug,
#else
		CompilerOptions.None,
#endif
#if XBOX360
							TargetPlatform.Xbox360
#else
 TargetPlatform.Windows
#endif
);
						Effect effect = new Effect(
							FlatRedBallServices.GraphicsDevice,
							compiledEffect.GetEffectCode(),
#if DEBUG
 CompilerOptions.Debug,
#else
 CompilerOptions.None,
#endif
 Renderer.mEffectPool);



						mFirstEffect = effect;
						mFirstEffectCache = new EffectCache(effect, true);


						loadedAsset = effect;
					}
				}
#endif
#endif// !FRB_MDX
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
#if WINDOWS_8
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

				#region GuiSkin

                // Need to eventually make this 360 supported

#if SUPPORTS_FRB_DRAWN_GUI
				else if (typeof(T) == typeof(GuiSkin))
				{
					return (T)((object)GuiSkinSave.FromFile(assetName).ToGuiSkin(mName));
				}
#endif

                #endregion

#if FRB_XNA && !MONOGAME
                #region Model

#if !XNA4

				else if (typeof(T) == typeof(Microsoft.Xna.Framework.Graphics.Model))
				{
					if (extension.ToLowerInvariant() == "x")
					{

						//throw new NotImplementedException();

						object objModel =
							FlatRedBall.Content.Model.XLoader.XLoader.LoadFile(assetName, FlatRedBallServices.GraphicsDevice, this);

						return (T)objModel;
					}
					else if (extension.ToLowerInvariant() == "fbx")
					{
						object objModel =
							FlatRedBall.Content.Model.FBXLoader.FBXLoader.LoadModel(assetName, this);

						return (T)objModel;
					}
				}
#endif

				#endregion

#endif

				else if(typeof(T) == typeof(Song))
				{
					return (T)(object)LoadSong(assetName);
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


#if FRB_MDX
                #region ModelData

				else if (typeof(T) == typeof(ModelData))
				{
					loadedAsset = ModelData.FromFile(assetName, this.mName, true);
				}

                #endregion
#endif

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

		private Song LoadSong(string assetName)
		{
			Song song;

#if ANDROID
	if(assetName.StartsWith("./"))
	{
		assetName = assetName.Substring(2);
	}
#endif

            var uri = new Uri(assetName, UriKind.Relative);

            song = Song.FromUri(assetName, uri);

#if ANDROID
            var songType = song.GetType();

            var fields = songType.GetField("assetUri",
                                        System.Reflection.BindingFlags.NonPublic |
                                        System.Reflection.BindingFlags.Instance);
			Android.Net.Uri androidUri = Android.Net.Uri.Parse(assetName);
            fields.SetValue(song, androidUri);
#endif
            return song;

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
#if XNA4
		static internal void ClearPremultipliedAlphaImageData()
		{
			lock (mPremultipliedAlphaImageData)
			{
				mPremultipliedAlphaImageData.SetDataDimensions(1, 1);
			}
		}
#endif
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

		private IDisposable LoadTexture2D(string assetName, string extension, IDisposable loadedAsset)
		{
#if FRB_MDX
			if (Renderer.GraphicsDevice == null)
#else
			if (Renderer.Graphics == null)
#endif
			{
				throw new NullReferenceException("The Renderer's Graphics is null.  Call FlatRedBallServices.Initialize before attempting to load any textures");
			}



#if FRB_MDX

            Microsoft.Xna.Framework.Graphics.Texture2D tempTexture = new Microsoft.Xna.Framework.Graphics.Texture2D();

			switch (extension.ToLowerInvariant())
			{
				case "gif":

					ImageDataList imageDataList = GifLoader.GetImageDataList(assetName);

					int numberOfFrames = imageDataList.Count;

					if (imageDataList.Count == 0)
					{
						throw new InvalidOperationException("The gif file " + assetName + " has no frames");
					}
					else
					{
						tempTexture = imageDataList[0].ToTexture2D();
						tempTexture.Name = assetName + "0";
						loadedAsset = tempTexture;
					}

					break;

				default:
					uint textureLoadingColorKey = 0;
					// if dealing with a png or tga use the alpha in the image
					if (extension == "bmp")
						textureLoadingColorKey = FlatRedBallServices.GraphicsOptions.TextureLoadingColorKey;

					Microsoft.DirectX.Direct3D.ImageInformation imageInfo =
						Microsoft.DirectX.Direct3D.TextureLoader.ImageInformationFromFile(assetName);


					if (System.IO.File.Exists(assetName))
					{
						int desiredWidth = imageInfo.Width;
						int desiredHeight = imageInfo.Height;

						// Vic says:  If we leave the desired width/height to 0, then DX will pick a power-of-2 
						// dimension.  to force that this doesn't happen, let's set the values manually:


						tempTexture.texture =
							Microsoft.DirectX.Direct3D.TextureLoader.FromFile(
								Renderer.GraphicsDevice, assetName,
								desiredWidth,
								desiredHeight,
								0, 0,
								Microsoft.DirectX.Direct3D.Format.Unknown,
								Microsoft.DirectX.Direct3D.Pool.Managed,
								Microsoft.DirectX.Direct3D.Filter.Linear,
								Microsoft.DirectX.Direct3D.Filter.Linear,
								(int)textureLoadingColorKey, ref imageInfo);

					}
					else
					{
						throw new System.ArgumentException("Cannot find the file " + assetName);
					}

					tempTexture.Width = imageInfo.Width;
					tempTexture.Height = imageInfo.Height;

					tempTexture.mBitsPerPixel = GraphicalEnumerations.BitsPerPixelInFormat(imageInfo.Format);
					tempTexture.Name = assetName;  


					break;
			}
			tempTexture.mMemoryHeight = tempTexture.texture.GetLevelDescription(0).Height;
			tempTexture.mMemoryWidth = tempTexture.texture.GetLevelDescription(0).Width;
		  
			loadedAsset = tempTexture;


#elif XBOX360 && !XNA4
				 

						switch (extension.ToLowerInvariant())
						{
							case "png":
							{
								ImageData png = FlatRedBall.IO.PngLoader.GetPixelData(assetName);
							   // Texture2D texture = new Texture2D(FlatRedBallServices.GraphicsDevice, png.Width , png.Height, 0, TextureUsage.None, SurfaceFormat.Color);
							   // texture.SetData(test);
							   // texture.Name = assetName;
								loadedAsset = png.ToTexture2D();
								//loadedAsset = texture;
								break;
							}
							case "bmp":
							{
								ImageData bmp = FlatRedBall.IO.BmpLoader.GetPixelData(assetName);
								Texture2D texture = new Texture2D(FlatRedBallServices.GraphicsDevice, bmp.Width , bmp.Height, 0, TextureUsage.None, SurfaceFormat.Color);
								texture.SetData<Color>(bmp.Data);
								texture.Name = assetName;
								loadedAsset = texture; 
								break;
							}
							default:
							{
								throw new ArgumentException("FlatRedBall only supports loading png files on the 360. Please convert the file to png format.");
							}
						}
#elif SILVERLIGHT


					switch (extension.ToLowerInvariant())
					{
						//these are the only allowed file types for graphics in SL
						case "jpg":
						case "png":
						case "gif":
							{
								string modifiedName = assetName.Replace("." + FlatRedBallServices.GetExtension(assetName), "");

								Texture2D texture =
									LoadFromProject<Texture2D>(modifiedName);
								loadedAsset = texture as IDisposable;


								break;
							}
						default:
							throw new ArgumentException("FlatRedBall does not support the following type for Texture2D:\n\n" + extension + 
								"\n\nCurrently only jpg, png, and gif are supported.  " +
								"Try converting " + assetName + " to one of these formats." );
						//break;
					}

#else
            switch (extension.ToLowerInvariant())
			{
				case "bmp":
					{
						//bool useCustomBmpLoader = true;

						//if (useCustomBmpLoader)
						//{
						//    ImageData bmp = FlatRedBall.IO.BmpLoader.GetPixelData(assetName);
						//    Texture2D texture = bmp.ToTexture2D();
						//    //Texture2D texture = new Texture2D(FlatRedBallServices.GraphicsDevice, bmp.Width, bmp.Height, 0, TextureUsage.None, SurfaceFormat.Color);
						//    //texture.SetData<Microsoft.Xna.Framework.Graphics.Color>(bmp.Data);
						//    texture.Name = assetName;
						//    loadedAsset = texture;
						//    break;
						//}
						//else
						{

#if XNA4 || WINDOWS_8

							ImageData bmp = FlatRedBall.IO.BmpLoader.GetPixelData(assetName);

							bmp.Replace(FlatRedBallServices.GraphicsOptions.TextureLoadingColorKey, Color.Transparent);

							Texture2D texture = bmp.ToTexture2D();
							//new Texture2D(FlatRedBallServices.GraphicsDevice, bmp.Width, bmp.Height, 0, TextureUsage.None, SurfaceFormat.Color);
							//texture.SetData<Color>(bmp.Data);
							texture.Name = assetName;
							loadedAsset = texture;
							break;
#else
							TextureCreationParameters parameters = TextureCreationParameters.Default;
							parameters.ColorKey = FlatRedBallServices.GraphicsOptions.TextureLoadingColorKey;
							Texture2D texture = Texture2D.FromFile(Renderer.Graphics.GraphicsDevice, assetName, parameters);
							texture.Name = assetName;
							loadedAsset = texture;
							break;
#endif
						}

						//break;
					}
				case "png":
				case "jpg":
					bool useFrbPngLoader = false;

					if (useFrbPngLoader)
					{
#if WINDOWS_PHONE || WINDOWS_8
						throw new NotImplementedException();
#else
						ImageData png = FlatRedBall.IO.PngLoader.GetPixelData(assetName);

						Texture2D texture = png.ToTexture2D();

						texture.Name = assetName;
						loadedAsset = texture;
#endif
					}
					else
					{
                        TextureLoadingStyle loadingStyle;

                        bool useImageData = FlatRedBallServices.IsThreadPrimary() == false;

#if WINDOWS
                        // Since the graphics device can get lost, we don't want to use render targets, so we'll fall
                        // back to using ImageData:
                        useImageData = true;
#endif

                        if (useImageData)
                        {
                            // Victor Chelaru
                            // April 22, 2015
                            // The purpose of this
                            // code is to support loading
                            // textures from file (as opposed
                            // to using the content pipeline) and
                            // having them contain premultiplied alpha.
                            // I believe at one point using render targets
                            // may not have worked on a secondary thread. I 
                            // switched the code to using render targets (not
                            // image data), and ran the automated tests which do
                            // background texture loading on PC and all seemed to
                            // work okay. Render targets will be much faster than ImageData
                            // and imagedata seems to throw errors on iOS, so I'm going to try
                            // render targets and see what happens.

                            loadingStyle = TextureLoadingStyle.ImageData;
                        }
                        else
                        {
                            loadingStyle = TextureLoadingStyle.RenderTarget;
                        }


						Texture2D texture = LoadTextureFromFile(assetName, loadingStyle);
						texture.Name = assetName;
						loadedAsset = texture;
					}

					break;

				case "dds":
				case "dib":
				case "hdr":
				case "pfm":
				case "ppm":
				case "tga":
					{
#if XNA4 || WINDOWS_8
						throw new NotImplementedException("The following texture format is not supported" +
							extension + ".  We recommend using the .png format");
#else
						Texture2D texture = Texture2D.FromFile(Renderer.Graphics.GraphicsDevice, assetName, TextureCreationParameters.Default);
						texture.Name = assetName;
						loadedAsset = texture;
						break;
#endif
						//break;
					}
				case "gif":
					{
#if WINDOWS_PHONE || MONOGAME
						throw new NotImplementedException();
#else
						ImageDataList imageDataList = GifLoader.GetImageDataList(assetName);

						int numberOfFrames = imageDataList.Count;

						if (imageDataList.Count == 0)
						{
							throw new InvalidOperationException("The gif file " + assetName + " has no frames");
						}
						else
						{
							Texture2D texture2D = imageDataList[0].ToTexture2D();
							texture2D.Name = assetName;
							loadedAsset = texture2D;
						}
						break;
#endif

					}

					//break;
				default:
					throw new ArgumentException("FlatRedBall does not support the " + extension + " file type passed for loading a Texture2D");
				//break;
			}
#endif
			return loadedAsset;
		}



#if XNA4 || WINDOWS_8
		static ImageData mPremultipliedAlphaImageData = new ImageData(256, 256);

        enum TextureLoadingStyle
        {
            ImageData,
            RenderTarget

        }


		private Texture2D LoadTextureFromFile(string loc, TextureLoadingStyle loadingStyle)
		{
            bool canLoadNow = true;

#if ANDROID
							canLoadNow &= FlatRedBallServices.IsThreadPrimary();
#endif

            if (!canLoadNow)
            {
#if ANDROID
				AddTexturesToLoadOnPrimaryThread(loc);
#endif
                return null;
            }
            else
            {


                Texture2D file = null;

                using (Stream titleStream = FileManager.GetStreamForFile(loc))
                {
                    file = Texture2D.FromStream(Renderer.GraphicsDevice, titleStream);
                }




                if (loadingStyle == TextureLoadingStyle.ImageData)
                {
                    lock (mPremultipliedAlphaImageData)
                    {
                        mPremultipliedAlphaImageData.ExpandIfNecessary(file.Width, file.Height);

                        mPremultipliedAlphaImageData.CopyFrom(file);

                        mPremultipliedAlphaImageData.MakePremultiplied(file.Width * file.Height);

                        mPremultipliedAlphaImageData.ToTexture2D(file);
                    }
                    return file;
                }
                else if (loadingStyle == TextureLoadingStyle.RenderTarget)
                {
                    RenderTarget2D result = null;
                    lock (Renderer.Graphics.GraphicsDevice)
                    {
                        if (MathFunctions.IsPowerOfTwo(file.Width) &&
                            MathFunctions.IsPowerOfTwo(file.Height))
                        {
                            //Setup a render target to hold our final texture which will have premulitplied alpha values
                            result = new RenderTarget2D(Renderer.GraphicsDevice, file.Width, file.Height, CreateMipMaps, SurfaceFormat.Color, DepthFormat.None);
                        }
                        else
                        {
                            result = new RenderTarget2D(Renderer.GraphicsDevice, file.Width, file.Height);
                        }


                        Renderer.GraphicsDevice.SetRenderTarget(result);
                        Renderer.GraphicsDevice.Clear(Color.Black);

                        //Multiply each color by the source alpha, and write in just the color values into the final texture
                        BlendState blendColor = new BlendState();
                        blendColor.ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue;

                        blendColor.AlphaDestinationBlend = Blend.Zero;
                        blendColor.ColorDestinationBlend = Blend.Zero;

                        blendColor.AlphaSourceBlend = Blend.SourceAlpha;
                        blendColor.ColorSourceBlend = Blend.SourceAlpha;

                        SpriteBatch spriteBatch = new SpriteBatch(Renderer.GraphicsDevice);
                        spriteBatch.Begin(SpriteSortMode.Immediate, blendColor);
                        spriteBatch.Draw(file, file.Bounds, Color.White);
                        spriteBatch.End();

                        //Now copy over the alpha values from the PNG source texture to the final one, without multiplying them
                        BlendState blendAlpha = new BlendState();
                        blendAlpha.ColorWriteChannels = ColorWriteChannels.Alpha;

                        blendAlpha.AlphaDestinationBlend = Blend.Zero;
                        blendAlpha.ColorDestinationBlend = Blend.Zero;

                        blendAlpha.AlphaSourceBlend = Blend.One;
                        blendAlpha.ColorSourceBlend = Blend.One;

                        spriteBatch.Begin(SpriteSortMode.Immediate, blendAlpha);
                        spriteBatch.Draw(file, file.Bounds, Color.White);
                        spriteBatch.End();

                        //Release the GPU back to drawing to the screen
                        Renderer.GraphicsDevice.SetRenderTarget(null);
                    }

                    Renderer.ForceSetBlendOperation();
                    if (Renderer.mLastColorOperationSet != ColorOperation.None)
                    {
                        Renderer.ForceSetColorOperation(Renderer.mLastColorOperationSet);
                    }

                    return result as Texture2D;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
		}
#endif


		#endregion

		#endregion
	}
}