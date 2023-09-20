using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.IO;
using System.Windows;

using FlatRedBall.Utilities;


using FlatRedBall.Content;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Content.Particle;
using FlatRedBall.Content.AI.Pathfinding;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Splines;
using System.IO;
using System.Reflection;
using FlatRedBall.Math;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Content.Scene;

using FlatRedBall.Content.SpriteFrame;
using SourceReferencingFile = FlatRedBall.Glue.Content.SourceReferencingFile;

using Color = Microsoft.Xna.Framework.Color;

namespace EditorObjects.Parsing
{
    public enum ErrorBehavior
    {
        ThrowException,
        ContinueSilently
    }

    public enum TopLevelOrRecursive
    {
        TopLevel,
        Recursive
    }


    public static class ContentParser
    {
        public static bool GetNamedObjectsIn(string fileName, List<string> listToAddTo)
        {
            bool toReturn = true;

            string extension = FileManager.GetExtension(fileName);

            switch (extension)
            {
                #region case Scene (scnx)
                case "scnx":

                    SpriteEditorScene ses = SpriteEditorScene.FromFile(fileName);

                    for(int i = 0; i < ses.SpriteFrameSaveList.Count; i++)
                    {
                        listToAddTo.Add(ses.SpriteFrameSaveList[i].ParentSprite.Name + " (SpriteFrame)");
                    }

                    for(int i = 0; i < ses.SpriteGridList.Count; i++)
                    {
                        listToAddTo.Add(ses.SpriteGridList[i].Name + " (SpriteGrid)");
                    }

                    for(int i = 0; i < ses.SpriteList.Count; i++)
                    {
                        listToAddTo.Add(ses.SpriteList[i].Name + " (Sprite)");
                    }

                    for(int i = 0; i < ses.TextSaveList.Count; i++)
                    {
                        listToAddTo.Add(ses.TextSaveList[i].Name + " (Text)");
                    }
                    break;
                #endregion

                #region case ShapeCollection (shcx)

                case "shcx":
                    ShapeCollectionSave scs = ShapeCollectionSave.FromFile(fileName);

                    for (int i = 0; i < scs.AxisAlignedCubeSaves.Count; i++)
                    {
                        listToAddTo.Add(scs.AxisAlignedCubeSaves[i].Name + " (AxisAlignedCube)");
                    }
                    for (int i = 0; i < scs.AxisAlignedRectangleSaves.Count; i++)
                    {
                        listToAddTo.Add(scs.AxisAlignedRectangleSaves[i].Name + " (AxisAlignedRectangle)");
                    }
                    for (int i = 0; i < scs.CircleSaves.Count; i++)
                    {
                        listToAddTo.Add(scs.CircleSaves[i].Name + " (Circle)");
                    }
                    for (int i = 0; i < scs.PolygonSaves.Count; i++)
                    {
                        listToAddTo.Add(scs.PolygonSaves[i].Name + " (Polygon)");
                    }
                    for (int i = 0; i < scs.SphereSaves.Count; i++)
                    {
                        listToAddTo.Add(scs.SphereSaves[i].Name + " (Sphere)");
                    }
                    break;

                #endregion

				#region NodeNetwork (nntx)
				case "nntx":
					NodeNetworkSave nns = NodeNetworkSave.FromFile(fileName);

					for (int i = 0; i < nns.PositionedNodes.Count; i++)
					{
                        listToAddTo.Add(nns.PositionedNodes[i].Name + " (PositionedNode)");
					}

					break;
				#endregion

                #region EmitterList (emix)
                case "emix":
                    EmitterSaveList esl = EmitterSaveList.FromFile(fileName);

                    for (int i = 0; i < esl.emitters.Count; i++)
                    {
                        listToAddTo.Add(esl.emitters[i].Name + " (Emitter)");
                    }

                    break;

                #endregion

                #region Case AnimationChainList (achx)

                case "achx":
					AnimationChainListSave acls = AnimationChainListSave.FromFile(fileName);

					for (int i = 0; i < acls.AnimationChains.Count; i++)
					{
                        listToAddTo.Add(acls.AnimationChains[i].Name + " (AnimationChain)");
					}
					break;
				#endregion

				#region Case SplineList (splx)
				case "splx":
					SplineSaveList ssl = SplineSaveList.FromFile(fileName);

					for (int i = 0; i < ssl.Splines.Count; i++)
					{
                        listToAddTo.Add(ssl.Splines[i].Name + " (Spline)");
					}

					break;

				#endregion

                default:
                    toReturn = false;
                    break;
			}

            return toReturn;
        }

        public static List<SourceReferencingFile> GetSourceReferencingFilesReferencedByAsset(string fileName)
        {
            return GetSourceReferencingFilesReferencedByAsset(fileName, TopLevelOrRecursive.TopLevel);
        }

        public static List<SourceReferencingFile> GetSourceReferencingFilesReferencedByAsset(string fileName, TopLevelOrRecursive topLevelOrRecursive)
        {
            string throwAwayString = "";
            string throwawayVerboseString = "";

            return GetSourceReferencingFilesReferencedByAsset(fileName, topLevelOrRecursive, ErrorBehavior.ThrowException, ref throwAwayString, ref throwawayVerboseString);
        }

        public static List<SourceReferencingFile> GetSourceReferencingFilesReferencedByAsset(string fileName, TopLevelOrRecursive topLevelOrRecursive, ErrorBehavior errorBehavior, ref string error, ref string verboseError)
        {
            string fileExtension = FileManager.GetExtension(fileName);

            List<SourceReferencingFile> referencedFiles = null;

            switch (fileExtension)
            {
                //case "scnx":
                //    try
                //    {
                //        SpriteEditorScene ses = SpriteEditorScene.FromFile(fileName);

                //        referencedFiles = ses.GetSourceReferencingReferencedFiles(RelativeType.Absolute);
                //    }
                //    catch (Exception e)
                //    {
                //        error = "Error loading file " + fileName + ": " + e.Message;
                //        referencedFiles = new List<SourceReferencingFile>();
                //        verboseError = e.ToString();
                //    }
                //    break;
                //default:
                //    referencedFiles = new List<SourceReferencingFile>();
                //    break;
            }
/**/
            if (topLevelOrRecursive == TopLevelOrRecursive.Recursive)
            {
                //First we need to get a list of all referenced files
                List<FilePath> filesToSearch = new List<FilePath>();

                try
                {
                    // GetFilesReferencedByAsset can throw an error if the file doesn't
                    // exist.  But we don't really care if it's missing if it can't reference
                    // others.  I mean, sure we care, but it's not relevant here.  Other systems
                    // can check for that.
                    if (CanFileReferenceOtherFiles(fileName))
                    {
                        GetFilesReferencedByAsset(fileName, topLevelOrRecursive, filesToSearch);
                    }
                }
                catch (Exception e)
                {
                    if (errorBehavior == ErrorBehavior.ThrowException)
                    {
                        throw e;
                    }
                    else
                    {
                        error += e.Message + "\n";
                    }
                }



                if (filesToSearch != null)
                {
                    for (int i = filesToSearch.Count - 1; i > -1; i--)
                    {
                        string errorForThisFile = "";
                        string verboseErrorForThisFile = "";
                        List<SourceReferencingFile> subReferencedFiles = GetSourceReferencingFilesReferencedByAsset(filesToSearch[i].FullPath, topLevelOrRecursive,
                            errorBehavior, ref errorForThisFile, ref verboseErrorForThisFile);
                        // error may have already been set.  If it has already been set, we don't want to dump more errors (which may just be duplicates anyway)
                        if (string.IsNullOrEmpty(error))
                        {
                            error += errorForThisFile;
                        }

                        if (subReferencedFiles != null)
                        {
                            referencedFiles.AddRange(subReferencedFiles);
                        }
                    }
                }
            }
/**/
            return referencedFiles;

        }

        public static List<FilePath> GetFilesReferencedByAsset(FilePath filePath)
		{

            return GetFilesReferencedByAsset(filePath, TopLevelOrRecursive.TopLevel);
		}

        public static List<FilePath> GetFilesReferencedByAsset(FilePath filePath, TopLevelOrRecursive topLevelOrRecursive)
		{
            var referencedFiles = new List<FilePath>();

            GetFilesReferencedByAsset(filePath, topLevelOrRecursive, referencedFiles);

            return referencedFiles.Distinct().ToList();
        }

        public static void GetFilesReferencedByAsset(FilePath filePath, TopLevelOrRecursive topLevelOrRecursive, List<FilePath> referencedFiles)
        {
            var newReferencedFiles = new List<FilePath>();
            if (!CanFileReferenceOtherFiles(filePath))
            {
                return;
            }
            else if (!filePath.Exists())
            {
                // We used to throw an error here but now we just let the error window handle it:
                //throw new FileNotFoundException("Could not find file " + fileName, fileName);

            }
            else
            {

                string fileExtension = filePath.Extension;

                switch (fileExtension)
                {
                    #region Scene (.scnx)

                    case "scnx":

                        SceneSave ses = SceneSave.FromFile(filePath.FullPath);
                        newReferencedFiles = ses.GetReferencedFiles(RelativeType.Absolute).Select(item => new FilePath(item)).ToList();

                        break;

                    #endregion

                    #region Emitter List (.emix)

                    case "emix":
                        EmitterSaveList esl = EmitterSaveList.FromFile(filePath.FullPath);
                        newReferencedFiles = esl.GetReferencedFiles(RelativeType.Absolute).Select(item => new FilePath(item)).ToList();
                        break;

                    #endregion

                    #region AnimationChain List

                    case "achx":

                        AnimationChainListSave acls = null;
                        try
                        {

                            acls = AnimationChainListSave.FromFile(filePath.FullPath);
                            newReferencedFiles = acls.GetReferencedFiles(RelativeType.Absolute).Select(item => new FilePath(item)).ToList();
                        }
                        catch(Exception e)
                        {
                            throw new Exception($"Error parsing file {filePath}:\n{e}");
                        }
                        break;

                    #endregion

                    #region X File (.x)

                    case "x":
                        newReferencedFiles = GetTextureReferencesInX(filePath.FullPath).Select(item => new FilePath(item)).ToList();
                        break;

                    #endregion

                    #region Spline List (.slpx) - falls to default

                    case "splx":

                    #endregion

                    #region Font File (.fnt)
                    case "fnt":
                        newReferencedFiles = GetTextureReferencesInFnt(filePath.FullPath).Select(item => new FilePath(item)).ToList();

                        break;
                    #endregion
                    default:
                        
                        break;
                }

                // We still want to construct as good of a reference structure as possible
                // even if there are missing files.  Therefore, we'll just keep track of errors and report them 
                // at the end of the method
                bool didErrorOccur = false;
                string errorMessage = "";
                if (topLevelOrRecursive == TopLevelOrRecursive.Recursive)
                {
                    for (int i = newReferencedFiles.Count - 1; i > -1; i--)
                    {
                        // If this file can't reference other files, no need to even do a file check or throw errors. 
                        if (CanFileReferenceOtherFiles(newReferencedFiles[i]) == true)
                        {
                            if (newReferencedFiles[i].Exists())
                            {

                                try
                                {
                                    GetFilesReferencedByAsset(newReferencedFiles[i], topLevelOrRecursive, newReferencedFiles);
                                }
                                catch (Exception e)
                                {
                                    didErrorOccur = true;
                                    errorMessage += e.Message;
                                }
                            }
                            else
                            {
                                didErrorOccur = true;
                                errorMessage += "Could not find the file " + newReferencedFiles[i] + 
                                    " which is referenced in the file " + filePath + "\n";
                            }
                        }
                    }

                }

                referencedFiles.AddRange(newReferencedFiles);

                if (didErrorOccur)
                {
                    throw new Exception(errorMessage);
                }

            }
		}

        private static bool CanFileReferenceOtherFiles(FilePath filePath)
        {
            if(filePath == null)
            {
                return false;
            }
            else
            {
                string extension = filePath.Extension;

                if(extension == "png" || extension == "bmp" || extension == "jpg" || extension == "gif" || 
                    extension == "dds" || extension == "tga" ||
                    extension == "cs")
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        private static List<string> GetTextureReferencesInFnt(string fileName)
        {
            List<string> referencedFiles = new List<string>();
            string contents = FileManager.FromFileText(fileName);
            referencedFiles = new List<string>(BitmapFont.GetSourceTextures(contents));
            for(int x = 0; x < referencedFiles.Count; ++x)
            {
                string referencedFileName = referencedFiles[x];
                referencedFiles[x] = FileManager.GetDirectory(fileName) + referencedFileName;
            }
            return referencedFiles;
        }

        public static string GetMemberNameForList(string extension, string memberType)
        {
            switch (extension)
            {
                #region Scene (scnx)
                case "scnx":
                    switch (memberType)
                    {
                        case "Sprite":
                            return "Sprites";
                            //break;
                        case "SpriteFrame":
                            return "SpriteFrames";
                            //break;
						case "Text":
							return "Texts";
							// break;
                        case "PositionedModel":
                            return "PositionedModels";
                        case "SpriteGrid":
                            return "SpriteGrids";
						case "Scene":
							return "";
                    }
                    break;
                #endregion

                #region EmitterList (emix)
                case "emix":
                    return "";
                    //break;
                #endregion

                #region ShapeCollection (shcx)
                case "shcx":
                    switch (memberType)
                    {
                        case "AxisAlignedRectangle":
                        case "FlatRedBall.Math.Geometry.AxisAlignedRectangle":
                            return "AxisAlignedRectangles";
                            //break;
                        case "AxisAlignedCube":
                        case "FlatRedBall.Math.Geometry.AxisAlignedCube":
                            return "AxisAlignedCubes";
                            //break;
                        case "Circle":
                        case "FlatRedBall.Math.Geometry.Circle":
                            return "Circles";
                            // break;
                        case "Line":
                        case "FlatRedBall.Math.Geometry.Line":
                            return "Lines";
                            //break;
                        case "Polygon":
                        case "FlatRedBall.Math.Geometry.Polygon":
                            return "Polygons";
                            // break;
						case "ShapeCollection":
                        case "FlatRedBall.Math.Geometry.ShapeCollection":

                            return "";
							// break;
                        case "Sphere":
                        case "FlatRedBall.Math.Geometry.Sphere":
                            return "Spheres";
                            //break;
                    }
                    break;
                #endregion

				#region AnimationChainList (achx)

				case "achx":
					switch(memberType)
					{
						case "AnimationChain":
							return "";
							// break;

						case "AnimationChainList":
							return "";
							// break;

					}
					break;

				#endregion

				#region SplineList (splx)

				case "splx":

					switch (memberType)
					{
						case "Spline":
							return "";
							//break;
						case "SplineList":
							return "";
							//break;

					}

					break;

				#endregion

				#region SpriteRig (srgx)

				case "srgx":

					return "";

					//break;

				#endregion

				#region NodeNetwork (nntx)
				case "nntx":
					return "";

					//break;

				#endregion

                case "png":
                case "jpg":
                case "bmp":
                case "tga":
                    return "";
                    //break;

			}

            // Vic says - this used to throw an exception, but it is possible that the user sets object types
            // incorrectly.  This should just generate code that doesn't compile, not throw an exception.  Eventually
            // we will want to test for project errors and report this to the user.
            return "";
        }

		private static List<string> GetTextureReferencesInX(string fileName)
        {
            List<string> referencedFiles = new List<string>();
            string contents = FileManager.FromFileText(fileName);

            int currentIndex = 0;
            //bool useCamel = false;
            const string textureFileNameString = "TextureFilename {";
            string fileDirectory = FileManager.GetDirectory(fileName);

            int nextTextureFilename = contents.IndexOf(textureFileNameString, currentIndex, StringComparison.OrdinalIgnoreCase);

            if (nextTextureFilename == -1)
            {

                return referencedFiles;
            }
            // The first "TextureFilename" is garbage - it's the template definition.  Let's do the next
            bool isFirstATemplate = contents.IndexOf("template TextureFilename", StringComparison.OrdinalIgnoreCase) == nextTextureFilename - "template ".Length;

            if (isFirstATemplate)
            {
                currentIndex = nextTextureFilename + 1;
                nextTextureFilename = contents.IndexOf(textureFileNameString, currentIndex, StringComparison.OrdinalIgnoreCase);
            }

            while (nextTextureFilename != -1)
            {
                // read the file

                string texture = StringFunctions.GetWordAfter(textureFileNameString, contents, nextTextureFilename, StringComparison.OrdinalIgnoreCase);
                texture = fileDirectory + texture.Replace("\"", "").Replace(";", "").Replace(".\\\\", "").Replace("}", "");

                if (!referencedFiles.Contains(texture))
                {
                    referencedFiles.Add(texture);
                }

                currentIndex = nextTextureFilename + 1;

                nextTextureFilename = contents.IndexOf(textureFileNameString, currentIndex, StringComparison.OrdinalIgnoreCase);

            }


			

            return referencedFiles;
        }

        public static void EliminateDuplicateSourceReferencingFiles(List<SourceReferencingFile> sourceReferencingFileList)
        {
            for (int i = sourceReferencingFileList.Count - 1; i > -1; i--)
            {
//                bool hasFoundDuplicate = false;

                SourceReferencingFile firstSrf = sourceReferencingFileList[i];

                for (int j = i - 1; j > -1; j--)
                {
                    SourceReferencingFile otherSrf = sourceReferencingFileList[j];

                    if (otherSrf.HasTheSameFilesAs(firstSrf))
                    {
                        sourceReferencingFileList.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public static object GetValueForProperty(string fileName, string objectName, string propertyName)
        {
            string extension = FileManager.GetExtension(fileName);

            switch (extension)
            {
                #region case Scene (scnx)
                case "scnx":

                    var ses = SceneSave.FromFile(fileName);

                    string oldRelative = FileManager.RelativeDirectory;
                    FileManager.RelativeDirectory = FileManager.GetDirectory(fileName);

                    object returnValue = GetValueForPropertyInScene(ses, objectName, propertyName);

                    FileManager.RelativeDirectory = oldRelative;

                    return returnValue;

                    //break;
                #endregion

                #region case ShapeCollection (shcx)

                case "shcx":
                    ShapeCollectionSave scs = ShapeCollectionSave.FromFile(fileName);
                    return GetValueForPropertyInShapeCollection(scs, objectName, propertyName);
                    //break;

                #endregion
            }
            return null;
        }

        static object GetValueForPropertyInScene(SceneSave scene, string objectName, string property)
        {
            for (int i = 0; i < scene.SpriteFrameSaveList.Count; i++)
            {
                if (scene.SpriteFrameSaveList[i].ParentSprite.Name == objectName)
                {
                    return GetValueForPropertyOnObject(scene.SpriteFrameSaveList[i], property);
                }
            }

            for (int i = 0; i < scene.SpriteGridList.Count; i++)
            {
                if (scene.SpriteGridList[i].Name == objectName)
                {
                    return GetValueForPropertyOnObject(scene.SpriteGridList[i], property);
                }
            }

            for (int i = 0; i < scene.SpriteList.Count; i++)
            {
                if (scene.SpriteList[i].Name == objectName)
                {
                    return GetValueForPropertyOnObject(scene.SpriteList[i], property);
                }
            }

            for (int i = 0; i < scene.TextSaveList.Count; i++)
            {
                if (scene.TextSaveList[i].Name == objectName)
                {
                    return GetValueForPropertyOnObject(scene.TextSaveList[i], property);
                }
            }

            return null;
        }

        static object GetValueForPropertyInShapeCollection(ShapeCollectionSave shapeCollection, string objectName, string property)
        {
            for (int i = 0; i < shapeCollection.AxisAlignedCubeSaves.Count; i++)
            {
                if (shapeCollection.AxisAlignedCubeSaves[i].Name == objectName)
                {
                    return GetValueForPropertyOnObject(shapeCollection.AxisAlignedCubeSaves[i], property);
                }
            }
            for (int i = 0; i < shapeCollection.AxisAlignedRectangleSaves.Count; i++)
            {
                if (shapeCollection.AxisAlignedRectangleSaves[i].Name == objectName)
                {
                    return GetValueForPropertyOnObject(shapeCollection.AxisAlignedRectangleSaves[i], property);
                }
            }
            for (int i = 0; i < shapeCollection.CircleSaves.Count; i++)
            {
                if (shapeCollection.CircleSaves[i].Name == objectName)
                {
                    return GetValueForPropertyOnObject(shapeCollection.CircleSaves[i], property);
                }
            }
            for (int i = 0; i < shapeCollection.PolygonSaves.Count; i++)
            {
                if (shapeCollection.PolygonSaves[i].Name == objectName)
                {
                    return GetValueForPropertyOnObject(shapeCollection.PolygonSaves[i], property);
                }
            }
            for (int i = 0; i < shapeCollection.SphereSaves.Count; i++)
            {
                if (shapeCollection.SphereSaves[i].Name == objectName)
                {
                    return GetValueForPropertyOnObject(shapeCollection.SphereSaves[i], property);
                }
            }
            return null;
        }


        static object GetValueForPropertyOnObject(AxisAlignedRectangleSave rectangle, string fieldOrProperty)
        {
            if (fieldOrProperty == "Color")
            {
                // See if there are any matches
                Dictionary<string, Color> allColors = GetAllDefaultColors();
                int alphaAsInt = MathFunctions.RoundToInt(rectangle.Alpha * 255);
                int redAsInt = MathFunctions.RoundToInt(rectangle.Red * 255);
                int greenAsInt = MathFunctions.RoundToInt(rectangle.Green * 255);
                int blueAsInt = MathFunctions.RoundToInt(rectangle.Blue * 255);

                foreach(KeyValuePair<string, Color> kvp in allColors)
                {
                    Color color = kvp.Value;

                    if (color.A == alphaAsInt &&
                        color.R == redAsInt &&
                        color.G == greenAsInt &&
                        color.B == blueAsInt)
                    {
                        return kvp.Key;
                    }
                }

                return null;
            }

            return GetValueForPropertyOnObject<AxisAlignedRectangleSave>(rectangle, fieldOrProperty);
        }

        static object GetValueForPropertyOnObject(SpriteSave sprite, string fieldOrProperty)
        {
            if (fieldOrProperty == "Alpha")
            {
                float valueToDivideBy = 255 / GraphicalEnumerations.MaxColorComponentValue;

                return (255 - sprite.Fade) / valueToDivideBy;
            }
            else if (fieldOrProperty == "PixelSize")
            {
                return sprite.ConstantPixelSize;
            }
            else if (fieldOrProperty == "CurrentChainName")
            {
                // This thing should use an AnimationChain
                string animationChainRelative = sprite.AnimationChainsFile;

                string animationChainFile = FileManager.RelativeDirectory + sprite.AnimationChainsFile;

                AnimationChainListSave acls = AnimationChainListSave.FromFile(animationChainFile);

                int index = sprite.CurrentChain;

                if (index < acls.AnimationChains.Count)
                {
                    return acls.AnimationChains[index].Name;
                }
                else
                {
                    return null;
                }
            }

            return GetValueForPropertyOnObject<SpriteSave>(sprite, fieldOrProperty);

        }

        static object GetValueForPropertyOnObject(SpriteFrameSave spriteFrameSave, string fieldOrProperty)
        {
            object returnValue = GetValueForPropertyOnObject<SpriteFrameSave>(spriteFrameSave, fieldOrProperty);

            // Many of the properties on the SpriteFrame are actually on the Sprite itself.
            if (returnValue == null)
            {
                returnValue = GetValueForPropertyOnObject(spriteFrameSave.ParentSprite, fieldOrProperty);
            }

            return returnValue;
        }

        static object GetValueForPropertyOnObject<T>(T objectToGetValueFrom, string fieldOrProperty)
        {
            Type type = typeof(T);

            PropertyInfo[] properties = type.GetProperties();

            for (int i = 0; i < properties.Length; i++)
            {
                if (properties[i].Name == fieldOrProperty)
                {
                    return properties[i].GetValue(objectToGetValueFrom, null);
                }
            }

            FieldInfo[] fields = type.GetFields();

            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].Name == fieldOrProperty)
                {
                    return fields[i].GetValue(objectToGetValueFrom);
                }
            }

            return null;
        }

        static Dictionary<string, Color> GetAllDefaultColors()
        {
            Dictionary<string, Color> toReturn = new Dictionary<string, Color>();

            Type colorType = typeof(Color);

            PropertyInfo[] properties = colorType.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                if (property.PropertyType == colorType)
                {
                    toReturn.Add(property.Name, (Color)property.GetValue(null, null));
                }
            }

            return toReturn;
        }
    }
}
