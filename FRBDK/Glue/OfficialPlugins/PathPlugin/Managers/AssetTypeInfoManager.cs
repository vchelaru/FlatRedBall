using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Math.Paths;
using Newtonsoft.Json;
using OfficialPlugins.PathPlugin.Views;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OfficialPlugins.PathPlugin.Managers
{
    static class AssetTypeInfoManager
    {
        public static AssetTypeInfo PathAssetTypeInfo { get; private set; }

        static AssetTypeInfoManager()
        {
            PathAssetTypeInfo = CreatePathAssetTypeInfo();
        }

        public const string PathsVariableName = "Paths";

        static AssetTypeInfo CreatePathAssetTypeInfo()
        {
            var ati = new AssetTypeInfo();
            ati.FriendlyName = PathsVariableName;
            ati.QualifiedRuntimeTypeName = new PlatformSpecificType();
            ati.QualifiedRuntimeTypeName.QualifiedType = typeof(FlatRedBall.Math.Paths.Path).FullName;
            ati.CanBeObject = true;

            var segmentDefinition = new VariableDefinition();
            segmentDefinition.Type = "string";
            segmentDefinition.Name = "Paths";
            segmentDefinition.UsesCustomCodeGeneration = true;
            segmentDefinition.CustomGenerationFunc = GeneratePaths;
            segmentDefinition.Category = "Path";
            segmentDefinition.PreferredDisplayer = typeof(PathView);
            ati.VariableDefinitions.Add(segmentDefinition);


            return ati;
        }

        static string FloatToString(float value) => value.ToString(CultureInfo.InvariantCulture);

        private static string GeneratePaths(IElement element, NamedObjectSave nos, ReferencedFileSave rfs)
        {
            StringBuilder toReturn = new StringBuilder();
            var variable = nos.GetCustomVariable(PathsVariableName)?.Value as string;

            toReturn.AppendLine($"{nos.InstanceName}.Clear();");

            if(!string.IsNullOrEmpty(variable))
            {
                var deserialized = JsonConvert.DeserializeObject<List<PathSegment>>(variable);

                foreach(var item in deserialized)
                {
                    var endX = FloatToString(item.EndX);
                    var endY = FloatToString(item.EndY);
                    if(item.SegmentType == SegmentType.Line)
                    {
                        toReturn.AppendLine($"{nos.InstanceName}.LineToRelative({endX}, {endY});");
                        //LineToRelative(float x, float y)
                    }
                    else if(item.SegmentType == SegmentType.Arc)
                    {
                        var signedAngle = FloatToString(item.ArcAngle);

                        //ArcToRelative(float endX, float endY, float signedAngle)
                        toReturn.AppendLine(
                            $"{nos.InstanceName}.ArcToRelative({endX}, {endY}, Microsoft.Xna.Framework.MathHelper.ToRadians({signedAngle}));");
                    }
                    else
                    {
                        // Unknown segment type...
                    }
                }
            }

            return toReturn.ToString();
        }
    }
}
