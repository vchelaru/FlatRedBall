using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels; 
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Content;
using System.Windows.Forms;
using FlatRedBall.Localization;
using FlatRedBall.Utilities;
using FlatRedBall.Glue.RuntimeObjects;
using KellermanSoftware.CompareNetObjects;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Graphics;
using FlatRedBall.Glue.GuiDisplay.Facades;
using GluePropertyGridClasses.Interfaces;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Evaluation;

namespace FlatRedBall.Glue
{
    class VsProjectState : IVsProjectState

    {
        public string DefaultNamespace { get; set; }
    }

    public class GluxManager     
    {
        #region Fields
        
        static ElementRuntime mCurrentElement;
        private static ElementRuntimeHighlight mCurrentElementHighlight;

        static ReferencedFileRuntimeList mGlobalContentFilesRuntime = new ReferencedFileRuntimeList();

        static string mNextElement;
        static string mNextElementToHighlight;
        static bool mShouldRefreshHighlight = false;
        static bool mRefreshVariablesOnly = false; 
        static string mCurrentGlueFile = null;
        static bool mShouldRefreshGlux = false;
        static bool mShouldUnloadGlux = false;

        static string mNextState = "";
        static bool mUpdateState = false;

        static object mLockObject = new object();


        static int mReloadIgnores;

        static IObjectFinder mObjectFinder;

        #endregion


        #region Properties

        // I'm not sure what the AlternativeContentDirctory
        // is vs. the ContentDirectory but we seem to check the
        // Alternative first and if that doesn't exist, then we return
        // the regular.
        public static string AlternativeContentDirectory
        {
            get;
            private set;
        }

        public static string ContentDirectory
        {
            get;
            // Made public for tests
            set;
        }

        public static string CurrentElementName
        {
            get
            {
                if (CurrentElement == null)
                {
                    return "";
                }
                return CurrentElement.Name; 
            }
        }

        public static ElementRuntime CurrentElement
        {
            get { return mCurrentElement; }
            
            private set
            {
                mCurrentElement = value;
               
            }
        }

        public static string ElementToHighlight
        {

            set 
            {
                //if (mHighlightIgnores == 0)
                //{
                    bool shouldRefresh = CurrentElementHighlighted == null ||
                        value != CurrentElementHighlighted.Name;

                    if (shouldRefresh)
                    {
                        mShouldRefreshHighlight = true;

                        mNextElementToHighlight = value;
                    }
                //}
                //else
                //{
                //    mHighlightIgnores--;
                //}
            }
        }

        public static GlueProjectSave GlueProjectSave { get; set; }

        public static String ContentManagerName
        {
            get; 
            set;
        }

        public static ElementRuntime CurrentElementHighlighted
        {
            get { return mCurrentElementHighlight.CurrentElement; }
        }

        public static string CurrentGlueFile
        {
            get { return mCurrentGlueFile; }
        }

        public static ReferencedFileRuntimeList GlobalContentFilesRuntime
        {
            get
            {
                return mGlobalContentFilesRuntime;
            }
        }

        public static IObjectFinder ObjectFinder
        {
            get
            {
                if (mObjectFinder == null)
                {
                    return FlatRedBall.Glue.Elements.ObjectFinder.Self;
                }
                else
                {
                    return mObjectFinder;
                }
            }
            set
            {
                mObjectFinder = value;
            }
        }

        #endregion

        #region Events

        public static event Action GluxLoaded;
        public static event Action<IElement> ElementRemoved;
        public static event Action<ElementRuntime> ElementHighlighted;
        public static event Action<IElement> BeforeElementLoaded;
        public static event Action<IElement> AfterElementLoaded;

        public static event EventHandler<VariableSetArgs> BeforeVariableSet;
        public static event EventHandler<VariableSetArgs> AfterVariableSet;

        public static event Action<string> ScriptReceived;

        #endregion

        #region Constructor

        static GluxManager()
        {
            mCurrentElementHighlight = new ElementRuntimeHighlight();
            mReloadIgnores = 0;

        }

        #endregion

        public string DefaultNamespace
        {
            get;
            private set;
        }


        public static void IgnoreNextReload()
        {
            lock (mLockObject)
            {
                //mReloadIgnores++;
                // I used to ++ but we actually don't
                // get commands from the file system to
                // reload.  Instead, we get them from Glue.
                // We may save multiple times but Glue may only
                // respond to 1 of those.  In that case we'd accumulate
                // tons of ignores.
                mReloadIgnores = 1;

            }
        }

        //public static void IgnoreNextSelection()
        //{
        //    lock (mLockObject)
        //    {
        //        mHighlightIgnores++;
        //    }
        //}

        public static void LoadGlux(string filename)
        {
            
            lock (mLockObject)
            {
                if (mReloadIgnores == 0)
                {
                    mCurrentGlueFile = filename;

                    mShouldRefreshGlux = true;
                }
                else
                {
                    mReloadIgnores--;
                }
            }
        }

        private static void internalLoadGlux(string filename)
        {
			if (!string.IsNullOrEmpty(filename))
            {
				//int numberOfTries = 0;
				//const int maxNumberOfTries = 5;
				//bool succeeded = false;

				//Exception lastException = null;
				//ObjectFinder.GlueProject = null;
				//while (numberOfTries < maxNumberOfTries)
				//{
				//    try
				//    {
				//        GlueProjectSave = FileManager.XmlDeserialize<GlueProjectSave>(filename);
				//        succeeded = true;
				//        break;
				//    }
				//    catch (Exception e)
				//    {
				//        System.Threading.Thread.Sleep(25);
				//        numberOfTries++;
				//        lastException = e;
				//    }
				//}

				//if (!succeeded)
				//{
				//    MessageBox.Show("Error loading GLUX:\n\n" + lastException.ToString());
				//}


                ObjectFinder.GlueProject = GlueProjectSave;




                foreach (var screenSave in GlueProjectSave.Screens)
                {
                    screenSave.UpdateCustomProperties();
                }

                foreach (var entitySave in GlueProjectSave.Entities)
                {
                    entitySave.UpdateCustomProperties();
                }


                LocalizationManager.ClearDatabase();
                GlueProjectSave.UpdateIfTranslationIsUsed();




                // Need to find directory before doing the translation loading
                FileManager.RelativeDirectory = FileManager.GetDirectory(filename);
                
                
                
                mCurrentGlueFile = filename;

                Microsoft.Build.Evaluation.Project vsProj = new Microsoft.Build.Evaluation.Project();
                string csProjFileName = FileManager.RemoveExtension(mCurrentGlueFile) + ".csproj";

                vsProj = new Microsoft.Build.Evaluation.Project(csProjFileName, null, null, new ProjectCollection());


                FindContentDirectory(vsProj);


                var vsProjectState = new VsProjectState();
                vsProjectState.DefaultNamespace = null;
                EditorObjects.IoC.Container.Set<IVsProjectState>(vsProjectState);

                foreach (var bp in vsProj.Properties)
                {
                    if (bp.Name == "RootNamespace")
                    {
                        vsProjectState.DefaultNamespace = bp.EvaluatedValue;

                        break;
                    }
                }

                LoadLocalization();

                FlatRedBallServices.Game.Window.Title = filename;

                if (GluxLoaded != null)
                {
                    GluxLoaded();
                }
            }
        }

        private static void LoadLocalization()
        {
            if (GlueProjectSave.UsesTranslation)
            {
                List<ReferencedFileSave> allRfses = GlueProjectSave.GetAllReferencedFiles();

                foreach (ReferencedFileSave rfs in allRfses)
                {
                    if (rfs.IsDatabaseForLocalizing)
                    {
                        string fullFileName = ElementRuntime.ContentDirectory + rfs.Name;
                        try
                        {
                            LocalizationManager.AddDatabase(
                                fullFileName, rfs.CsvDelimiter.ToChar());
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Error loading localization database:\n\n" + fullFileName + "\n\n" + e.Message);
                        }
                        break;

                    }
                }


            }
        }

		/// <summary>
		///  Compares the previous GlueProjectSave with the current one.  
		/// </summary>
		/// <returns>True if the GlueProjectSaves are the same</returns>
		private static bool CompareGlueProjectSaves()
		{
			if (!string.IsNullOrEmpty(mCurrentGlueFile))
			{
				int numberOfTries = 0;
				const int maxNumberOfTries = 5;
				bool succeeded = false;

				GlueProjectSave newGlueProjectSave = null;
				Exception lastException = null;
				//ObjectFinder.GlueProject = null;
				while (numberOfTries < maxNumberOfTries)
				{
					try
					{
						newGlueProjectSave = FileManager.XmlDeserialize<GlueProjectSave>(mCurrentGlueFile);
						succeeded = true;
						break;
					}
					catch (Exception e)
					{
						System.Threading.Thread.Sleep(25);
						numberOfTries++;
						lastException = e;
					}
				}

				if (!succeeded)
				{
					MessageBox.Show("Error loading GLUX:\n\n" + lastException.ToString());
				}

				if (GlueProjectSave == null && newGlueProjectSave != null)
				{
                    // This method gets called on the primary
                    // thread so it's okay to call UnloadGluxActivity
                    // which is synchronous:


                    // October 23, 2012
                    // I don't understand
                    // why we call UnloadGluxActivity.
                    // If the GlueProjectSave is null then
                    // there is no need to unload it is there?
                    // Unloading wipes out mNextElement, which we
                    // don't want to do if we're loading a .glux for
                    // the first time.
                    //mShouldUnloadGlux = true;  
                    //UnloadGluxActivity();
					GlueProjectSave = newGlueProjectSave;

					return false;
				}

				CompareObjects compareObjects = new CompareObjects();

				compareObjects.ElementsToIgnore.Add("ContainerType");
				compareObjects.ElementsToIgnore.Add("ImageWidth");
				compareObjects.ElementsToIgnore.Add("ImageHeight");
				compareObjects.ElementsToIgnore.Add("EquilibriumParticleCount");
				compareObjects.ElementsToIgnore.Add("BurstParticleCount");
				compareObjects.ElementsToIgnore.Add("RuntimeType");
				compareObjects.ElementsToIgnore.Add("EventSave");
				compareObjects.ElementsToIgnore.Add("SharedCodeFullFileName");


                // This method gets called on the primary
                // thread so it's okay to call UnloadGluxActivity
                // which is synchronous:
                mShouldUnloadGlux = true;
				UnloadGluxActivity();

				GlueProjectSave = newGlueProjectSave;
			}
			return false;
		}


        public static string GetPreProcessorConstantsFromProject(Microsoft.Build.Evaluation.Project coreVisualStudioProject)
        {
            string preProcessorConstants = "";

            // Victor Chelaru October 20, 2012
            // We used to just look at the XML and had a broad way of determining the 
            // patterns.  I decided it was time to clean this up and make it more precise
            // so now we use the Properties from the project.
            foreach (var property in coreVisualStudioProject.Properties)
            {
                if (property.Name == "DefineConstants")
                {
                    preProcessorConstants += ";" + property.EvaluatedValue;
                }
            }
            return preProcessorConstants;
        }

        private static void FindContentDirectory(Microsoft.Build.Evaluation.Project project)
        {
            AlternativeContentDirectory = null;
            string projectName = FileManager.RemovePath(FileManager.RemoveExtension(mCurrentGlueFile));

            string directory = FileManager.GetDirectory(FileManager.GetDirectory(mCurrentGlueFile));




            string preProcessorConstants =  GetPreProcessorConstantsFromProject(project);

            bool wasFound = false;
            string foundDirectory;

            if (preProcessorConstants.Contains("ANDROID"))
            {
                // Check for this 
                wasFound = TryFindContentFolder(projectName, directory + projectName + "/Assets/content/", out foundDirectory);

                if(wasFound)
                {
                    AlternativeContentDirectory = foundDirectory;
                }

            }

            if (!wasFound)
            {
                // Not sure why I did this, but it shouldn't have / in front of Content
                //string directoryToLookFor = directory + projectName + "/Content/";
                // Now I know why.  On some platforms (like FRB XNA PC 4.0) the 
                // content directory is going to be c:\Project\ProjectContent\
                // While on others, the Content folder is in the main project, like
                // c:\Project\Project\Content\
                // Which it is depends on the type of project, so let's support both
                string directoryToLookFor = directory + projectName + "Content/";
                wasFound = TryFindContentFolder(projectName, directoryToLookFor, out foundDirectory);
                if (wasFound)
                {
                    AlternativeContentDirectory = foundDirectory;
                }
            }

            if(!wasFound)
            {
                string directoryToLookFor = directory + projectName + "/Content/";

                wasFound = TryFindContentFolder(projectName, directoryToLookFor, out foundDirectory);

                if (wasFound)
                {
                    AlternativeContentDirectory = foundDirectory;
                }
            }

        }

        private static bool TryFindContentFolder(string projectName, string directoryToLookFor, out string directoryName)
        {
            bool wasFound = false;
            directoryName = null;
            if (System.IO.Directory.Exists(directoryToLookFor))
            {
                //fileName = directoryToLookFor + projectName + "Content.contentproj";

                // Hold on a minute.
                // Why are we only using
                // the content directory if
                // the .contentproj file exists?
                // What if this is a project that
                // doesn't have a .contentproj?  We
                // should still use it.
                //if (FileManager.FileExists(fileName))
                {
                    directoryName = directoryToLookFor;
                    wasFound = true;
                }
            }
            return wasFound;
        }

        public static void UnloadGlux()
        {
            mShouldUnloadGlux = true;
        }

        private static void UnloadGluxActivity()
        {
            if(mShouldUnloadGlux == true)
            {
                mShouldUnloadGlux = false;

                mNextElement = null; 
                GlueProjectSave = null;
            
                if (!mShouldRefreshGlux && CurrentElement != null)
                {
            
                    CurrentElement.Destroy();
                    mCurrentElementHighlight.RemoveHighlights(); 
                    CurrentElement = null;
                }
            }
        }
       
        public static void ShowElement(string elementName)
        {
            lock (mLockObject)
            {
                if (!string.IsNullOrEmpty(elementName))
                {
                    string currentElementName = null;
                    if (CurrentElement != null)
                    {
                        currentElementName = CurrentElement.Name;
                    }

                    mNextElement = elementName;

                    if(currentElementName != elementName)
                    {
                        ElementToHighlight = null;
                    }
                }

            }
        }

        public static void ShowState(string stateName)
        {
            lock (mLockObject)
            {
                mNextState = stateName;
                mUpdateState = true;
            }
        }

        
        
        public static void Update()
        {
            lock (mLockObject)
            {
				RefreshActivities();

                RefreshStateActivity();

                CurrentElementActivity();

                mCurrentElementHighlight.Activity();
            }           
        }

		/// <summary>
		/// Checks to see if there is any ignores, then refreshes if not
		/// </summary>
		private static void RefreshActivities()
		{
            UnloadGluxActivity();

			if (mShouldRefreshGlux && CompareGlueProjectSaves() )
			{
				mShouldRefreshGlux = false;
				mNextElement = null;
				mShouldRefreshHighlight = false;
				return;
			}

			RefreshGluxActivity();
			LoadNextElementActivity();
			RefreshHighlightActivity();
		}

        private static bool HasElementChanged()
        {

            return 
                mNextElement != null &&
                
                (CurrentElement == null && !string.IsNullOrEmpty(mNextElement)) ||
                (CurrentElement != null && mNextElement != CurrentElement.Name);
        }

        private static void CurrentElementActivity()
        {
            if (CurrentElement != null)
            {
                CurrentElement.Activity();
            }
        }

        private static void RefreshGluxActivity()
        {
            if (mShouldRefreshGlux)
            {


                //UnloadGlux();
                internalLoadGlux(mCurrentGlueFile);

                mShouldRefreshGlux = false;
                mShouldRefreshHighlight = true;

                if (CurrentElement != null)
                {
                    if (mRefreshVariablesOnly)
                    {

                        CurrentElement.RefreshVariables();
                    }
                    else
                    {

                        ShowElement(CurrentElement.Name);
                    }
                }

            }
        }

        static void LoadNextElementActivity()
        {
            if (mNextElement != null)
            {

                IElement elementToShow = ObjectFinder.GetIElement(mNextElement);
                mNextElement = null;

                RemoveCurrentElement();

                ContentManagerName = StringFunctions.IncrementNumberAtEnd(ContentManagerName);

                try
                {
                    if (BeforeElementLoaded != null)
                    {
                        BeforeElementLoaded(elementToShow);
                    }


                    CurrentElement = new ElementRuntime(elementToShow, null, null, OnBeforeVariableSet, OnAfterVariableSet);

                    if (AfterElementLoaded != null)
                    {
                        SpriteManager.AddPositionedObject(CurrentElement);
                        AfterElementLoaded(CurrentElement.AssociatedIElement);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Error loading element " + elementToShow + ":\n\n" + e.ToString());
                }
            }
        }

        static void RefreshHighlightActivity()
        {
            if (mShouldRefreshHighlight)
            {
                if (mNextElementToHighlight  == null)
                {
                    if (mCurrentElementHighlight.CurrentElement != null)
                    {
                        mCurrentElementHighlight.CurrentElement = null;
                    }
                }
                else if(mCurrentElement != null)
                {

                    ElementRuntime nextElement = mCurrentElement.GetContainedElementRuntime(mNextElementToHighlight);
                    mCurrentElementHighlight.CurrentElement = nextElement;

                    mCurrentElementHighlight.Color =
                        mCurrentElementHighlight.GetColorVisibleAgainst(SpriteManager.Camera.BackgroundColor);
                }

                if (ElementHighlighted != null)
                {
                    ElementHighlighted(mCurrentElementHighlight.CurrentElement);
                }

                mShouldRefreshHighlight = false;
            }

            // We want to refresh every frame because elements may change due to plugins, interpolation, script, etc.
            // This is a little inefficient but it might be okay since we're on a PC.  Review this if we have performance
            // problems (I don't expect we will)
            mCurrentElementHighlight.CurrentElement = mCurrentElementHighlight.CurrentElement;
        }

        static void RefreshStateActivity()
        {
            if (mUpdateState)
            {
                mUpdateState = false;

                CurrentElement.SetState(mNextState);
            }
        }

        public static void RefreshGlueProject(bool refreshVariablesOnly)
        {
            if (mReloadIgnores == 0)
            {
                mShouldRefreshGlux = true;
                mRefreshVariablesOnly = refreshVariablesOnly;
            }
            else
            {
                mReloadIgnores--;
            }
        }

        public static void RefreshFile(string fileName)
        {
            // If it's part of global content, refresh it here.
            // We need to make this thing be relative to the project:
            string directoryToUse = AlternativeContentDirectory;
            if (string.IsNullOrEmpty(directoryToUse))
            {
                directoryToUse = ContentDirectory;
            }
            if (!string.IsNullOrEmpty(directoryToUse))
            {
                // Just in case, we're going to ToLower it.
                string relativeToProject = FileManager.MakeRelative(fileName, directoryToUse).ToLower() ;

                if (mGlobalContentFilesRuntime.LoadedRfses.ContainsKey(relativeToProject))
                {
                    mGlobalContentFilesRuntime.Destroy(relativeToProject);
                }
            }
        }

        private static void RemoveCurrentElement()
        {
            if (CurrentElement != null)
            {
                if(ElementRemoved != null)
                {
                    ElementRemoved(CurrentElement.AssociatedIElement);
                }
                CurrentElement.Destroy();

                SpriteManager.RemoveAllParticleSprites();
            }
        }

        static void OnBeforeVariableSet(object sender, VariableSetArgs args)
        {
            if (BeforeVariableSet != null)
            {
                BeforeVariableSet(sender, args);
            }
        }

        static void OnAfterVariableSet(object sender, VariableSetArgs args)
        {
            if (AfterVariableSet != null)
            {
                AfterVariableSet(sender, args);
            }
        }

        public static void ReceiveScript(string script)
        {
            if (ScriptReceived != null)
            {
                ScriptReceived(script);
            }
        }

        public static void ClearEngine()
        {
            while (ShapeManager.VisibleCircles.Count != 0)
            {
                ShapeManager.Remove(ShapeManager.VisibleCircles.Last());
            }
            while (ShapeManager.VisibleRectangles.Count != 0)
            {
                ShapeManager.Remove(ShapeManager.VisibleRectangles.Last());
            }
            while (ShapeManager.VisiblePolygons.Count != 0)
            {
                ShapeManager.Remove(ShapeManager.VisiblePolygons.Last());
            }

            while (ShapeManager.VisibleLines.Count != 0)
            {
                ShapeManager.Remove(ShapeManager.VisibleLines.Last());
            }

            while (SpriteManager.AutomaticallyUpdatedSprites.Count != 0)
            {
                SpriteManager.RemoveSprite(SpriteManager.AutomaticallyUpdatedSprites.Last());
            }

            while (TextManager.AutomaticallyUpdatedTexts.Count != 0)
            {
                TextManager.RemoveText(TextManager.AutomaticallyUpdatedTexts.Last());
            }
        }

    }
}
