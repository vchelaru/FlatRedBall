using System;
using System.IO;

using System.Collections.Generic;
using System.Text;

using System.Runtime.Serialization;
using Microsoft.Xna.Framework.Graphics;

using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Serialization;

using FlatRedBall;

using AnimationFrame = FlatRedBall.Graphics.Animation.AnimationFrame;

using AnimationChainList = FlatRedBall.Graphics.Animation.AnimationChainList;
using FileManager = FlatRedBall.IO.FileManager;
using FlatRedBall.IO;
using FlatRedBall.Graphics.Texture;
using FlatRedBall.Graphics;



namespace FlatRedBall.Content.AnimationChain
{
    [XmlType("AnimationChainArraySave")]
    public class AnimationChainListSave
    {
#if ANDROID || IOS
        public static bool ManualDeserialization = true;
#else
        public static bool ManualDeserialization = false;
#endif

        #region Fields

        private List<string> mToRuntimeErrors = new List<string>();

        [XmlIgnore]
        public string FileName
        {
            set { mFileName = value; }
            get { return mFileName; }
        }

        #endregion

        #region Properties

        /// <summary>
        /// The relative master project file that this is a part of. For exxample, the Glue (.glux) file.
        /// This can be used by tools to provide additional functionality.
        /// </summary>
        public string ProjectFile { get; set; }

        [XmlIgnore]
        public List<string> ToRuntimeErrors
        {
            get { return mToRuntimeErrors; }
        }

        [XmlIgnore]
        protected string mFileName;

        /// <summary>
        /// Whether files (usually image files) referenced by this object (and .achx) are
        /// relative to the .achx itself. If false, then file references will be stored as absolute. 
        /// If true, then file reference,s will be stored relative to the .achx itself. This value should
        /// be true so that a .achx can be moved to a different file system or computer and still
        /// have valid references.
        /// </summary>
        public bool FileRelativeTextures = true;

        public FlatRedBall.TimeMeasurementUnit TimeMeasurementUnit;
        public FlatRedBall.Graphics.TextureCoordinateType CoordinateType = Graphics.TextureCoordinateType.UV;

        [XmlElementAttribute("AnimationChain")]
        public List<AnimationChainSave> AnimationChains;

        #endregion

        #region Methods

        #region Constructor

        public AnimationChainListSave() 
        {
            AnimationChains = new List<AnimationChainSave>();
        }

        #endregion

        public static AnimationChainListSave FromFile(string fileName)
        {
            AnimationChainListSave toReturn = null;

            if (IsJsonFileName(fileName))
            {
                // JsonNode parsing does no reflection, so it works the same on Android/iOS
                // (ManualDeserialization) as anywhere else.
                using (Stream stream = FileManager.GetStreamForFile(fileName))
                {
                    toReturn = ParseJson(JsonNode.Parse(stream).AsObject());
                }
            }
            else if (ManualDeserialization)
            {
                toReturn = DeserializeManually(fileName);
            }
            else
            {
                toReturn =
                    FileManager.XmlDeserialize<AnimationChainListSave>(fileName);
            }

            if (FileManager.IsRelative(fileName))
                fileName = FileManager.MakeAbsolute(fileName);

            toReturn.mFileName = fileName;

            return toReturn;
        }

        /// <summary>
        /// Create a "save" object from a regular animation chain list
        /// </summary>
        public static AnimationChainListSave FromAnimationChainList(AnimationChainList chainList)
        {
            AnimationChainListSave achlist = new AnimationChainListSave();
            achlist.FileRelativeTextures = chainList.FileRelativeTextures;
            achlist.TimeMeasurementUnit = chainList.TimeMeasurementUnit;
            achlist.mFileName = chainList.Name;

            List<AnimationChainSave> newChains = new List<AnimationChainSave>(chainList.Count);
            for (int i = 0; i < chainList.Count; i++)
            {
                AnimationChainSave ach = AnimationChainSave.FromAnimationChain(chainList[i], achlist.TimeMeasurementUnit);
                newChains.Add(ach);
                
            }
            achlist.AnimationChains = newChains;

            return achlist;
        }


		public List<string> GetReferencedFiles(RelativeType relativeType)
		{
            
			List<string> referencedFiles = new List<string>();

			foreach (AnimationChainSave acs in this.AnimationChains)
			{

                foreach (AnimationFrameSave afs in acs.Frames)
                {
                    string texture = FileManager.Standardize( afs.TextureName, null, false );

                    if (FileManager.GetExtension(texture).StartsWith("gif"))
                    {
                        texture = FileManager.RemoveExtension(texture) + ".gif";
                    }

                    if (!string.IsNullOrEmpty(texture) && !referencedFiles.Contains(texture))
                    {
                        referencedFiles.Add(texture);
                    }
                }
			}


			if (relativeType == RelativeType.Absolute)
			{
				string directory = FileManager.GetDirectory(FileName);

				for (int i = 0; i < referencedFiles.Count; i++)
				{
					referencedFiles[i] = directory + referencedFiles[i];
				}
			}

			return referencedFiles;
		}


        public void Save(string fileName)
        {           
            

            if (FileRelativeTextures)
            {
                MakeRelative(fileName);
            }

            FileManager.XmlSerialize(this, fileName);
        }


        public AnimationChainList ToAnimationChainList(string contentManagerName)
        {

            return ToAnimationChainList(contentManagerName, true);
        }


        public AnimationChainList ToAnimationChainList(string contentManagerName, bool throwError)
        {
            mToRuntimeErrors.Clear();

            AnimationChainList list = new AnimationChainList();

            list.FileRelativeTextures = FileRelativeTextures;
            list.TimeMeasurementUnit = TimeMeasurementUnit;
            list.Name = mFileName;

            string oldRelativeDirectory = FileManager.RelativeDirectory;

            try
            {
                if (this.FileRelativeTextures)
                {
                    FileManager.RelativeDirectory = FileManager.GetDirectory(mFileName);
                }

                foreach (AnimationChainSave animationChain in this.AnimationChains)
                {
                    try
                    {
                        FlatRedBall.Graphics.Animation.AnimationChain newChain = null;

                        newChain = animationChain.ToAnimationChain(contentManagerName, this.TimeMeasurementUnit, this.CoordinateType);

                        newChain.mIndexInLoadedAchx = list.Count;

                        newChain.ParentAchxFileName = mFileName;

                        list.Add(newChain);

                    }
                    catch (Exception e)
                    {
                        mToRuntimeErrors.Add(e.ToString());
                        if (throwError)
                        {
                            throw new Exception("Error loading AnimationChain", e);
                        }
                    }
                }
            }
            finally
            {
                FileManager.RelativeDirectory = oldRelativeDirectory;
            }

            return list;
        }


        //AnimationChainList ToAnimationChainList(string contentManagerName, TextureAtlas textureAtlas, bool throwError)
        //{
        //    mToRuntimeErrors.Clear();

        //    AnimationChainList list = new AnimationChainList();

        //    list.FileRelativeTextures = FileRelativeTextures;
        //    list.TimeMeasurementUnit = TimeMeasurementUnit;
        //    list.Name = mFileName;

        //    string oldRelativeDirectory = FileManager.RelativeDirectory;

        //    try
        //    {
        //        if (this.FileRelativeTextures)
        //        {
        //            FileManager.RelativeDirectory = FileManager.GetDirectory(mFileName);
        //        }

        //        foreach (AnimationChainSave animationChain in this.AnimationChains)
        //        {
        //            try
        //            {
        //                FlatRedBall.Graphics.Animation.AnimationChain newChain = null;

        //                if (textureAtlas == null)
        //                {
        //                    newChain = animationChain.ToAnimationChain(contentManagerName, this.TimeMeasurementUnit, this.CoordinateType);
        //                }
        //                else
        //                {
        //                    newChain = animationChain.ToAnimationChain(textureAtlas, this.TimeMeasurementUnit);
        //                }
        //                newChain.mIndexInLoadedAchx = list.Count;

        //                newChain.ParentAchxFileName = mFileName;

        //                list.Add(newChain);

        //            }
        //            catch (Exception e)
        //            {
        //                mToRuntimeErrors.Add(e.ToString());
        //                if (throwError)
        //                {
        //                    throw new Exception("Error loading AnimationChain", e);
        //                }
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        FileManager.RelativeDirectory = oldRelativeDirectory;
        //    }

        //    return list;


        //}


        //public AnimationChainList ToAnimationChainList(Graphics.Texture.TextureAtlas textureAtlas)
        //{
        //    return ToAnimationChainList(null, textureAtlas, true);
        //}


        private void MakeRelative(string fileName)
        {
            string oldRelativeDirectory = FileManager.RelativeDirectory;

            string newRelativeDirectory = FileManager.GetDirectory(fileName);
            FileManager.RelativeDirectory = newRelativeDirectory;

            foreach (AnimationChainSave acs in AnimationChains)
            {
                acs.MakeRelative();

            }

            FileManager.RelativeDirectory = oldRelativeDirectory;
        }


        #region JSON (.achj)

        private static bool IsJsonFileName(string fileName) =>
            fileName.EndsWith(".achj", StringComparison.OrdinalIgnoreCase);

        // Property names and defaults match the FlatRedBall Animation Editor's .achj writer (camelCase,
        // AnimationChain.Common in the FlatRedBall2 repo). Keys this class doesn't model (chain "loop" and
        // "locked", frame "events") are simply not read. A missing key falls back to the same default the
        // XML path uses.
        private static AnimationChainListSave ParseJson(JsonObject root)
        {
            AnimationChainListSave result = new AnimationChainListSave();

            if (root["fileRelativeTextures"] is JsonValue fileRelativeTextures)
            {
                result.FileRelativeTextures = fileRelativeTextures.GetValue<bool>();
            }

            if (root["timeMeasurementUnit"] is JsonValue timeMeasurementUnit)
            {
                result.TimeMeasurementUnit = (FlatRedBall.TimeMeasurementUnit)Enum.Parse(
                    typeof(FlatRedBall.TimeMeasurementUnit), timeMeasurementUnit.GetValue<string>());
            }

            if (root["coordinateType"] is JsonValue coordinateType)
            {
                result.CoordinateType = (TextureCoordinateType)Enum.Parse(
                    typeof(TextureCoordinateType), coordinateType.GetValue<string>());
            }

            if (root["projectFile"] is JsonValue projectFile)
            {
                result.ProjectFile = projectFile.GetValue<string>();
            }

            if (root["animationChains"] is JsonArray chains)
            {
                foreach (JsonNode chainNode in chains)
                {
                    JsonObject chainObject = chainNode.AsObject();
                    AnimationChainSave chain = new AnimationChainSave();
                    chain.Name = chainObject["name"]?.GetValue<string>() ?? string.Empty;

                    if (chainObject["frames"] is JsonArray frames)
                    {
                        foreach (JsonNode frameNode in frames)
                        {
                            chain.Frames.Add(ParseFrameJson(frameNode.AsObject()));
                        }
                    }

                    result.AnimationChains.Add(chain);
                }
            }

            return result;
        }

        private static AnimationFrameSave ParseFrameJson(JsonObject frameObject)
        {
            AnimationFrameSave frame = new AnimationFrameSave();

            frame.TextureName = frameObject["textureName"]?.GetValue<string>() ?? string.Empty;
            frame.FrameLength = JsonFloat(frameObject, "frameLength");
            frame.LeftCoordinate = JsonFloat(frameObject, "leftCoordinate");
            frame.RightCoordinate = JsonFloat(frameObject, "rightCoordinate", 1);
            frame.TopCoordinate = JsonFloat(frameObject, "topCoordinate");
            frame.BottomCoordinate = JsonFloat(frameObject, "bottomCoordinate", 1);
            frame.FlipHorizontal = JsonBool(frameObject, "flipHorizontal");
            frame.FlipVertical = JsonBool(frameObject, "flipVertical");
            frame.FlipDiagonal = JsonBool(frameObject, "flipDiagonal");
            frame.RelativeX = JsonFloat(frameObject, "relativeX");
            frame.RelativeY = JsonFloat(frameObject, "relativeY");

            // Stored as signed ints on purpose: negative values subtract under the Add operation.
            frame.Red = JsonNullableInt(frameObject, "red");
            frame.Green = JsonNullableInt(frameObject, "green");
            frame.Blue = JsonNullableInt(frameObject, "blue");
            frame.Alpha = JsonNullableInt(frameObject, "alpha");

            if (frameObject["colorOperation"] is JsonValue colorOperation)
            {
                frame.ColorOperation = (AnimationFrameColorOperation)Enum.Parse(
                    typeof(AnimationFrameColorOperation), colorOperation.GetValue<string>());
            }

            if (frameObject["shapes"] is JsonObject shapes)
            {
                frame.ShapeCollectionSave = ParseShapesJson(shapes);
            }

            return frame;
        }

        private static global::FlatRedBall.Content.Math.Geometry.ShapeCollectionSave ParseShapesJson(JsonObject shapesObject)
        {
            var shapes = new global::FlatRedBall.Content.Math.Geometry.ShapeCollectionSave();

            if (shapesObject["rectangles"] is JsonArray rectangles)
            {
                foreach (JsonNode node in rectangles)
                {
                    JsonObject rectangleObject = node.AsObject();
                    var rectangle = new global::FlatRedBall.Content.Math.Geometry.AxisAlignedRectangleSave();
                    rectangle.Name = rectangleObject["name"]?.GetValue<string>() ?? string.Empty;
                    rectangle.X = JsonFloat(rectangleObject, "x");
                    rectangle.Y = JsonFloat(rectangleObject, "y");
                    rectangle.Z = JsonFloat(rectangleObject, "z");
                    rectangle.ScaleX = JsonFloat(rectangleObject, "scaleX", 16);
                    rectangle.ScaleY = JsonFloat(rectangleObject, "scaleY", 16);
                    rectangle.Alpha = JsonFloat(rectangleObject, "alpha", 1);
                    rectangle.Red = JsonFloat(rectangleObject, "red", 1);
                    rectangle.Green = JsonFloat(rectangleObject, "green", 1);
                    rectangle.Blue = JsonFloat(rectangleObject, "blue", 1);
                    shapes.AxisAlignedRectangleSaves.Add(rectangle);
                }
            }

            if (shapesObject["circles"] is JsonArray circles)
            {
                foreach (JsonNode node in circles)
                {
                    JsonObject circleObject = node.AsObject();
                    var circle = new global::FlatRedBall.Content.Math.Geometry.CircleSave();
                    circle.Name = circleObject["name"]?.GetValue<string>() ?? string.Empty;
                    circle.X = JsonFloat(circleObject, "x");
                    circle.Y = JsonFloat(circleObject, "y");
                    circle.Z = JsonFloat(circleObject, "z");
                    circle.Radius = JsonFloat(circleObject, "radius", 16);
                    circle.Alpha = JsonFloat(circleObject, "alpha", 1);
                    circle.Red = JsonFloat(circleObject, "red", 1);
                    circle.Green = JsonFloat(circleObject, "green", 1);
                    circle.Blue = JsonFloat(circleObject, "blue", 1);
                    shapes.CircleSaves.Add(circle);
                }
            }

            if (shapesObject["polygons"] is JsonArray polygons)
            {
                foreach (JsonNode node in polygons)
                {
                    JsonObject polygonObject = node.AsObject();
                    var polygon = new global::FlatRedBall.Content.Polygon.PolygonSave();
                    polygon.Name = polygonObject["name"]?.GetValue<string>() ?? string.Empty;
                    polygon.X = JsonFloat(polygonObject, "x");
                    polygon.Y = JsonFloat(polygonObject, "y");
                    polygon.Z = JsonFloat(polygonObject, "z");
                    polygon.Alpha = JsonFloat(polygonObject, "alpha", 1);
                    polygon.Red = JsonFloat(polygonObject, "red", 1);
                    polygon.Green = JsonFloat(polygonObject, "green", 1);
                    polygon.Blue = JsonFloat(polygonObject, "blue", 1);

                    var points = new List<global::FlatRedBall.Math.Geometry.Point>();
                    if (polygonObject["points"] is JsonArray pointsArray)
                    {
                        foreach (JsonNode pointNode in pointsArray)
                        {
                            JsonObject pointObject = pointNode.AsObject();
                            points.Add(new global::FlatRedBall.Math.Geometry.Point(
                                JsonFloat(pointObject, "x"), JsonFloat(pointObject, "y")));
                        }
                    }
                    polygon.Points = points.ToArray();

                    shapes.PolygonSaves.Add(polygon);
                }
            }

            return shapes;
        }

        private static float JsonFloat(JsonObject parent, string name, float defaultValue = 0) =>
            parent[name] is JsonValue value ? value.GetValue<float>() : defaultValue;

        private static bool JsonBool(JsonObject parent, string name) =>
            parent[name] is JsonValue value && value.GetValue<bool>();

        private static int? JsonNullableInt(JsonObject parent, string name) =>
            parent[name] is JsonValue value ? value.GetValue<int>() : (int?)null;

        #endregion

        private static AnimationChainListSave DeserializeManually(string fileName)
        {
            AnimationChainListSave toReturn = new AnimationChainListSave();
            System.Xml.Linq.XDocument xDocument = null;

            using (var stream = FileManager.GetStreamForFile(fileName))
            {
                xDocument = System.Xml.Linq.XDocument.Load(stream);
            }

            System.Xml.Linq.XElement foundElement = null;

            foreach (var element in xDocument.Elements())
            {
                if (element.Name.LocalName == "AnimationChainArraySave")
                {
                    foundElement = element;
                    break;
                }
            }

            LoadFromElement(toReturn, foundElement);

            return toReturn;
        }



        private static void LoadFromElement(AnimationChainListSave toReturn, System.Xml.Linq.XElement element)
        {
            foreach (var subElement in element.Elements())
            {
                switch (subElement.Name.LocalName)
                {
                    case "FileRelativeTextures":
                        toReturn.FileRelativeTextures = AsBool(subElement);
                        break;
                    case "TimeMeasurementUnit":
                        toReturn.TimeMeasurementUnit =
                            (TimeMeasurementUnit)Enum.Parse(typeof(TimeMeasurementUnit), subElement.Value, true);
                        break;


                    case "CoordinateType":
                        toReturn.CoordinateType =
                            (TextureCoordinateType)Enum.Parse(typeof(TextureCoordinateType), subElement.Value, true);
                        break;
                    case "AnimationChain":
                        toReturn.AnimationChains.Add(AnimationChainSave.FromXElement(subElement));
                        break;

                }
            }
        }

        internal static bool AsBool(System.Xml.Linq.XElement subElement)
        {
            return bool.Parse(subElement.Value);
        }

        #endregion
    }
}
