using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using FlatRedBall.IO.Csv;
using FlatRedBall.IO;


using System.Windows.Forms;
using System.IO;
using FlatRedBall.Glue.Plugins;

namespace FlatRedBall.Glue.Elements
{
    #region GlobalOrProjectSpecific enum

    public enum GlobalOrProjectSpecific
    {
        Global,
        ProjectSpecific
    }

    #endregion

    public class AvailableAssetTypes
    {
        #region Fields

        static AvailableAssetTypes mSelf = new AvailableAssetTypes();

        string mStartupPath;
        string mCoreTypesFileLocation;

        List<AssetTypeInfo> mCoreAssetTypes = new List<AssetTypeInfo>();
        List<AssetTypeInfo> mCustomAssetTypes = new List<AssetTypeInfo>();

        List<AssetTypeInfo> mProjectSpecificAssetTypes = new List<AssetTypeInfo>();

        #endregion

        #region Properties

        public string GlobalCustomContentTypesFolder
        {
            get
            {
                return mStartupPath + @"\AdditionalContent\";
            }
        }

        public string ProjectSpecificContentTypesFolder
        {
            get
            {
#if GLUE
                string projectFileName = ProjectManager.GlueProjectFileName;
                string directory = FileManager.GetDirectory(projectFileName);

                return directory + @"GlueSettings\";
#else
                throw new NotImplementedException();
#endif
            }
        }


        public static AvailableAssetTypes Self
        {
            get
            {
                return mSelf;
            }
        }

        public List<AssetTypeInfo> CoreTypes
        {
            get
            {
                return mCoreAssetTypes;
            }
        }

        public List<string> AdditionalExtensionsToTreatAsAssets
        {
            get;
            private set;
        }

		public IEnumerable<AssetTypeInfo> AllAssetTypes
		{
			get
			{

                return mCoreAssetTypes.Concat(mCustomAssetTypes).ToList();
                
                // Project-specific ATIs will also
                // appear in the mCustomAssetTypes list,
                // so there's no need to loop through this
                // list.
                //foreach (var item in mProjectSpecificAssetTypes)
                //{
                //    yield return item;
                //}

            }
		}

        #endregion


        public void Initialize(string startupPath)
		{
            
            AdditionalExtensionsToTreatAsAssets = new List<string>();

            string fileName = startupPath + @"Content\ContentTypes.csv";

            Initialize(fileName, startupPath);
        }

        public void Initialize(string contentTypesFileLocation, string startupPath)
        {
            mStartupPath = startupPath;
            mCoreTypesFileLocation = contentTypesFileLocation;

            if (!File.Exists(contentTypesFileLocation))
            {
                throw new Exception("Could not find the file " + contentTypesFileLocation + " when trying to initialize the AvailableAssetTypes");
            }
            try
            {
                // We create a temporary
                // list to deserialize to
                // so that we can take the contents
                // and insert them at the front of the
                // list.  This makes the default implementations
                // show up first in the list so they can be found
                // first if searching by runtime type name.
                //List<AssetTypeInfo> tempList = new List<AssetTypeInfo>();
                // Update:  We now have a core asset types list
                CsvFileManager.CsvDeserializeList(typeof(AssetTypeInfo), contentTypesFileLocation, mCoreAssetTypes);
            }
            catch (Exception e)
            {

                string message = "Could not load the AssetTypes from\n" +
                    contentTypesFileLocation + "\nThis probably means your ContentTypes.csv is corrupt (the file was found).  You should re-install Glue.";


                throw new Exception(message, e);
            }



            if (Directory.Exists(GlobalCustomContentTypesFolder))
            {
                List<string> extraCsvs = FileManager.GetAllFilesInDirectory(GlobalCustomContentTypesFolder, "csv", 0);

                foreach (string file in extraCsvs)
                {
                    try
                    {
                        AddAssetTypes(file);
                    }
                    catch(Exception e)
                    {
                        MessageBox.Show("Failed to add additional asset types from file " + file + "\n\nError info:\n" + e);
                    }
                }
            }
		}

        public void ReactToProjectLoad(string projectFolder)
        {
            foreach (AssetTypeInfo ati in mProjectSpecificAssetTypes)
            {

                mCustomAssetTypes.Remove(ati);
#if GLUE
                PluginManager.ReceiveOutput("Removing known content type: " + ati);
#endif
            }

            mProjectSpecificAssetTypes.Clear();

            if(!string.IsNullOrEmpty(projectFolder))
            {

                List<string> allCsvs = FileManager.GetAllFilesInDirectory(projectFolder + "GlueSettings/", ".csv");

                foreach (string fileName in allCsvs)
                {
                    //string fileName = projectFolder + "GlueSettings/ProjectSpecificContent.csv";

                    if (File.Exists(fileName))
                    {
                        bool succeeded = AddAssetTypes(fileName, mProjectSpecificAssetTypes, tolerateFailure: true);

                    }
                }



                if (mProjectSpecificAssetTypes.Count != 0)
                {
#if GLUE
                    PluginManager.ReceiveOutput("Adding " + mProjectSpecificAssetTypes.Count + " content types");
#endif
                    this.mCustomAssetTypes.AddRange(mProjectSpecificAssetTypes);
                }
            }
        }

        public void AddAssetType(AssetTypeInfo assetTypeInfo)
        {
            mCustomAssetTypes.Add(assetTypeInfo);
        }

        public void AddAssetTypes(string fullFileName)
        {
            AddAssetTypes(fullFileName, mCustomAssetTypes);
        }

        public bool AddAssetTypes(string fullFileName, List<AssetTypeInfo> listToFill, bool tolerateFailure = false)
        {
            bool succeeded = true;

            List<AssetTypeInfo> newTypes = new List<AssetTypeInfo>();

            try
            {
                CsvFileManager.CsvDeserializeList(typeof(AssetTypeInfo), fullFileName, newTypes);
            }
            catch (Exception e)
            {
                succeeded = false;

                if (tolerateFailure)
                {
#if GLUE
                    // let's report it:
                    PluginManager.ReceiveError("Error loading CSV: " + fullFileName + "\n" + e.ToString());
#endif
                }
                else
                {
                    throw e;
                }
            }
            if (succeeded)
            {
#if GLUE
                PluginManager.ReceiveOutput("Loading content types from " + fullFileName + " and found " + newTypes.Count + " types");
#endif
                listToFill.AddRange(newTypes);
            }
            return succeeded;
        }

        public void RemoveAssetType(AssetTypeInfo assetTypeInfo)
        {
            mCustomAssetTypes.Remove(assetTypeInfo);
        }

		public AssetTypeInfo GetAssetTypeFromExtension(string extension)
		{
            return AllAssetTypes.FirstOrDefault(item => item.Extension == extension);
		}

        public AssetTypeInfo GetAssetTypeFromRuntimeType(string runtimeType,
            object callingObject, bool? isObject = null)
        {
            bool isQualified = runtimeType != null && runtimeType.Contains('.');

            var assetsToLoopThrough = AllAssetTypes;

            if(isObject.HasValue)
            {
                if (isObject == true)
                {
                    assetsToLoopThrough = assetsToLoopThrough.Where(item => item.CanBeObject);
                }
                else
                {
                    assetsToLoopThrough = assetsToLoopThrough.Where(item => !string.IsNullOrEmpty(item.Extension));
                }
            }

            if (isQualified)
            {
                foreach (var ati in assetsToLoopThrough)
                {
                    var effectiveQualified =
                        ati.QualifiedRuntimeTypeName.PlatformFunc?.Invoke(callingObject)
                        ?? ati.QualifiedRuntimeTypeName.QualifiedType;

                    if (effectiveQualified == runtimeType)
                    {
                        return ati;
                    }
                }
            }
            else
            {

                foreach (var ati in assetsToLoopThrough)
                {
                    if (ati.RuntimeTypeName == runtimeType)
                    {
                        return ati;
                    }
                }
            }
			return null;
        }

        public AssetTypeInfo GetAssetTypeFromExtensionAndQualifiedRuntime(string extension, string qualifiedRuntimeType)
        {
            foreach (var assetTypeInfo in AllAssetTypes)
            {
                if (assetTypeInfo.Extension == extension &&
                    assetTypeInfo.QualifiedRuntimeTypeName.QualifiedType == qualifiedRuntimeType)
				{
                    return assetTypeInfo;
				}
			}

			return null;


        }

        internal void CreateAdditionalCsvFile(string name, GlobalOrProjectSpecific globalOrProjectSpecific)
        {
            // We're going to load the full 
            // AvailableAssetTypes just to get
            // the headers.  Then we'll
            // create a new RCR, copy over the headers
            // then save off the RCR using the argument
            // fileName.  The new one will be empty except
            // for the headers.
            RuntimeCsvRepresentation rcr = CsvFileManager.CsvDeserializeToRuntime(
                mCoreTypesFileLocation);

            rcr.Records = new List<string[]>();


            string desiredFullPath = null;

            if (globalOrProjectSpecific == GlobalOrProjectSpecific.Global)
            {
                desiredFullPath = GlobalCustomContentTypesFolder + name + ".csv";
            }
            else // project-specific
            {
                desiredFullPath = ProjectSpecificContentTypesFolder + name + ".csv";
            }
            if (File.Exists(desiredFullPath))
            {
                MessageBox.Show("The CSV " + desiredFullPath + " already exists.");
            }
            else
            {
                try
                {
                    CsvFileManager.Serialize(rcr, desiredFullPath);
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Unable to save the CSV file.  You need to run Glue as an administrator");
                }
                catch (Exception e)
                {
                    MessageBox.Show("Unknown error attempting to create the CSV:\n\n" + e.ToString());
                }
            }
                
        }
    }
}
