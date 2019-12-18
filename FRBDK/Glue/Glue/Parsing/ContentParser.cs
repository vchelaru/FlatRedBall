using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO;

using FlatRedBall.Utilities;


using FlatRedBall.Content;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Content.Particle;
//using FlatRedBall.Content.SpriteRig;
using FlatRedBall.Content.AI.Pathfinding;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Splines;
//using FlatRedBall.Content.Model.WMELoader;

namespace FlatRedBall.Glue.Parsing
{
    public static class ContentParser
    {
        public static List<string> GetNamedObjectsIn(string fileName)
        {
            string extension = FileManager.GetExtension(fileName);
            List<string> listToReturn = new List<string>();
            switch (extension)
            {
                #region case Scene (scnx)
                case "scnx":

                    SpriteEditorScene ses = SpriteEditorScene.FromFile(fileName);

                    for (int i = 0; i < ses.PositionedModelSaveList.Count; i++)
                    {
                        listToReturn.Add(ses.PositionedModelSaveList[i].Name + " (PositionedModel)");
                    }

                    for(int i = 0; i < ses.SpriteFrameSaveList.Count; i++)
                    {
                        listToReturn.Add(ses.SpriteFrameSaveList[i].ParentSprite.Name + " (SpriteFrame)");
                    }

                    for(int i = 0; i < ses.SpriteGridList.Count; i++)
                    {
                        listToReturn.Add(ses.SpriteGridList[i].Name + " (SpriteGrid)");
                    }

                    for(int i = 0; i < ses.SpriteList.Count; i++)
                    {
                        listToReturn.Add(ses.SpriteList[i].Name + " (Sprite)");
                    }

                    for(int i = 0; i < ses.TextSaveList.Count; i++)
                    {
                        listToReturn.Add(ses.TextSaveList[i].Name + " (Text)");
                    }
                    break;
                #endregion

                #region case ShapeCollection (shcx)

                case "shcx":
                    ShapeCollectionSave scs = ShapeCollectionSave.FromFile(fileName);

                    for (int i = 0; i < scs.AxisAlignedCubeSaves.Count; i++)
                    {
                        listToReturn.Add(scs.AxisAlignedCubeSaves[i].Name + " (AxisAlignedCube)");
                    }
                    for (int i = 0; i < scs.AxisAlignedRectangleSaves.Count; i++)
                    {
                        listToReturn.Add(scs.AxisAlignedRectangleSaves[i].Name + " (AxisAlignedRectangle)");
                    }
                    for (int i = 0; i < scs.CircleSaves.Count; i++)
                    {
                        listToReturn.Add(scs.CircleSaves[i].Name + " (Circle)");
                    }
                    for (int i = 0; i < scs.PolygonSaves.Count; i++)
                    {
                        listToReturn.Add(scs.PolygonSaves[i].Name + " (Polygon)");
                    }
                    for (int i = 0; i < scs.SphereSaves.Count; i++)
                    {
                        listToReturn.Add(scs.SphereSaves[i].Name + " (Sphere)");
                    }
                    break;

                #endregion

				#region NodeNetwork (nntx)
				case "nntx":
					NodeNetworkSave nns = NodeNetworkSave.FromFile(fileName);

					for (int i = 0; i < nns.PositionedNodes.Count; i++)
					{
						listToReturn.Add(nns.PositionedNodes[i].Name + " (PositionedNode)");
					}

					break;
				#endregion

                #region EmitterList (emix)
                case "emix":
                    EmitterSaveList esl = EmitterSaveList.FromFile(fileName);

                    for (int i = 0; i < esl.emitters.Count; i++)
                    {
                        listToReturn.Add(esl.emitters[i].Name + " (Emitter)");
                    }

                    break;

                #endregion

                #region Case AnimationChainList (achx)

                case "achx":
					AnimationChainListSave acls = AnimationChainListSave.FromFile(fileName);

					for (int i = 0; i < acls.AnimationChains.Count; i++)
					{
						listToReturn.Add(acls.AnimationChains[i].Name + " (AnimationChain)");
					}
					break;
				#endregion

				#region Case SplineList (splx)
				case "splx":
					SplineSaveList ssl = SplineSaveList.FromFile(fileName);

					for (int i = 0; i < ssl.Splines.Count; i++)
					{
						listToReturn.Add(ssl.Splines[i].Name + " (Spline)");
					}

					break;

				#endregion
			}

            return listToReturn;
        }

		public static List<string> GetFilesReferencedByAsset(string file)
		{

			return GetFilesReferencedByAsset(file, readRecursively: false);
		}

		public static List<string> GetFilesReferencedByAsset(string file, bool readRecursively)
		{
			string fileExtension = FileManager.GetExtension(file);

			List<string> referencedFiles = null;// = new List<string>();

			switch (fileExtension)
            {
                #region Scene (.scnx)

                case "scnx":

					SpriteEditorScene ses = SpriteEditorScene.FromFile(file);
					referencedFiles = ses.GetReferencedFiles(RelativeType.Absolute);
					break;

                #endregion

                #region Emitter List (.emix)

                case "emix":
					EmitterSaveList esl = EmitterSaveList.FromFile(file);
					referencedFiles = esl.GetReferencedFiles(RelativeType.Absolute);
					break;

                #endregion

                #region SpriteRig (.srgx)

                case "srgx":
                    SpriteRigSave srs = SpriteRigSave.FromFile(file);
                    referencedFiles = srs.GetReferencedFiles(RelativeType.Absolute);
                    break;

                #endregion

                #region AnimationChain List

                case "achx":
					AnimationChainListSave acls = AnimationChainListSave.FromFile(file);
					referencedFiles = acls.GetReferencedFiles(RelativeType.Absolute);
					break;

                #endregion

                #region Bitmap Font Generator Config File (.bmfc)

                case "bmfc":

                    referencedFiles = new List<string>();

                    // These are only referenced IF they actually exist
                    string referencedFileToAdd = FileManager.RemoveExtension(file) + ".png";
                    if (FileManager.FileExists(referencedFileToAdd))
                    {
                        referencedFiles.Add(referencedFileToAdd);
                    }

                    referencedFileToAdd = FileManager.RemoveExtension(file) + ".fnt";
                    if (FileManager.FileExists(referencedFileToAdd))
                    {
                        referencedFiles.Add(referencedFileToAdd);
                    }
                    break;

                #endregion

                #region X File (.x)

                case "x":
					referencedFiles = GetTextureReferencesInX(file);
					break;

                #endregion

                #region WME File (.wme)
                case "wme":
                    referencedFiles = new List<string>();
                    WMELoader.GetReferencedFiles(file, referencedFiles, RelativeType.Absolute);

                    break;

                #endregion

                #region Spline List (.slpx) - falls to default

                case "splx":

                #endregion
                default:
					referencedFiles = new List<string>();
					break;
			}

			if (readRecursively)
			{
				for (int i = referencedFiles.Count - 1; i > -1; i--)
				{
					referencedFiles.AddRange(GetFilesReferencedByAsset(referencedFiles[i], true));
				}

			}

            // Files may include "../", so let's get rid of that stuff
            for (int i = 0; i < referencedFiles.Count; i++)
            {
                referencedFiles[i] = FileManager.Standardize(referencedFiles[i], "", false);
            }


			return referencedFiles;
		}

        internal static string GetMemberNameForList(string extension, string memberType)
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
                            return "AxisAlignedRectangles";
                            //break;
                        case "AxisAlignedCube":
                            return "AxisAlignedCubes";
                            //break;
                        case "Circle":
                            return "Circles";
                            // break;
                        case "Line":
                            return "Lines";
                            break;
                        case "Polygon":
                            return "Polygons";
                            // break;
						case "ShapeCollection":
							return "";
							// break;
                        case "Sphere":
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

					break;

				#endregion

				#region NodeNetwork (nntx)
				case "nntx":
					return "";

					break;

				#endregion

                case "png":
                case "jpg":
                case "bmp":
                case "tga":
                    return "";
                    break;

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
			bool useCamel = false;
			const string TextureFileNameString = "TextureFilename {";
			string fileDirectory = FileManager.GetDirectory(fileName);

			int nextTextureFilename = contents.IndexOf(TextureFileNameString, currentIndex, StringComparison.OrdinalIgnoreCase);

			if (nextTextureFilename == -1)
			{

				return referencedFiles;
			}
			// The first "TextureFilename" is garbage - it's the template definition.  Let's do the next
			bool isFirstATemplate = false;
			if (contents.IndexOf("template TextureFilename", StringComparison.OrdinalIgnoreCase) == nextTextureFilename - "template ".Length)
			{
				isFirstATemplate = true;
			}
			if (isFirstATemplate)
			{
				currentIndex = nextTextureFilename + 1;
				nextTextureFilename = contents.IndexOf(TextureFileNameString, currentIndex, StringComparison.OrdinalIgnoreCase);
			}

			while (nextTextureFilename != -1)
			{
				// read the file

				string texture = StringFunctions.GetWordAfter(TextureFileNameString, contents, nextTextureFilename, StringComparison.OrdinalIgnoreCase);
				texture = fileDirectory + texture.Replace("\"", "").Replace(";", "").Replace(".\\\\", "").Replace("}", "");

				if (!referencedFiles.Contains(texture))
				{
					referencedFiles.Add(texture);
				}

				currentIndex = nextTextureFilename + 1;

				nextTextureFilename = contents.IndexOf(TextureFileNameString, currentIndex, StringComparison.OrdinalIgnoreCase);

			}


			

			return referencedFiles;
		}
    }
}
