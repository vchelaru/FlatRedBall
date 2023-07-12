using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;
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
            ati.FriendlyName = "Path";
            ati.QualifiedRuntimeTypeName = new PlatformSpecificType();
            ati.QualifiedRuntimeTypeName.QualifiedType = typeof(FlatRedBall.Math.Paths.Path).FullName;
            ati.CanBeObject = true;

            var pathsVariableDefinition = new VariableDefinition();
            pathsVariableDefinition.Type = "string";
            pathsVariableDefinition.Name = "Path";
            pathsVariableDefinition.UsesCustomCodeGeneration = true;
            pathsVariableDefinition.CustomGenerationFunc = GeneratePaths;
            pathsVariableDefinition.CustomPropertySetFunc= GenerateProperty;
            pathsVariableDefinition.HasGetter = false;

            pathsVariableDefinition.Category = "Path";
            pathsVariableDefinition.PreferredDisplayer = typeof(PathView);
            ati.VariableDefinitions.Add(pathsVariableDefinition);


            return ati;
        }

        private static string GenerateProperty(IElement arg1, CustomVariable customVariable)
        {
            return $"{customVariable.SourceObject}.FromJson(value);";
        }

        static string FloatToString(float value) => CodeParser.ConvertValueToCodeString(value);

        private static string GeneratePaths(IElement element, NamedObjectSave nos, ReferencedFileSave rfs, string memberName)
        {
            StringBuilder toReturn = new StringBuilder();

            string ownerName = GetOwnerName(nos, memberName);

            var variable = nos.GetCustomVariable(memberName ?? PathsVariableName);
            var variableValue = variable?.Value as string;

            toReturn.AppendLine($"{ownerName}.Clear();");

            if (!string.IsNullOrEmpty(variableValue))
            {
                var deserialized = JsonConvert.DeserializeObject<List<PathSegment>>(variableValue);

                foreach (var item in deserialized)
                {
                    GenerateCodeForSegment(toReturn, ownerName, item);
                }
            }

            return toReturn.ToString();
        }

        private static string GetOwnerName(NamedObjectSave nos, string memberName)
        {
            var nosElement = ObjectFinder.Self.GetElement(nos.SourceClassType);

            string ownerName = nos.InstanceName;

            if (nosElement != null)
            {
                // this is a tunneled variable
                var customVariable = nosElement.CustomVariables.Find(item => item.Name == memberName);

                if (!string.IsNullOrEmpty(customVariable?.SourceObject))
                {
                    ownerName += "." + customVariable.SourceObject;
                }
            }

            return ownerName;
        }

        private static void GenerateCodeForSegment(StringBuilder toReturn, string ownerName, PathSegment item)
        {
            var endX = FloatToString(item.EndX);
            var endY = FloatToString(item.EndY);
            if (item.SegmentType == SegmentType.Line)
            {
                toReturn.AppendLine($"{ownerName}.LineToRelative({endX}, {endY});");
                //LineToRelative(float x, float y)
            }
            else if (item.SegmentType == SegmentType.Arc)
            {
                var signedAngle = FloatToString(item.ArcAngle);

                //ArcToRelative(float endX, float endY, float signedAngle)
                toReturn.AppendLine(
                    $"{ownerName}.ArcToRelative({endX}, {endY}, Microsoft.Xna.Framework.MathHelper.ToRadians({signedAngle}));");
            }
            else if(item.SegmentType == SegmentType.Move)
            {
                toReturn.AppendLine($"{ownerName}.MoveToRelative({endX}, {endY});");
            }
            else if(item.SegmentType == SegmentType.Spline)
            {
                toReturn.AppendLine($"{ownerName}.SplineToRelative({endX}, {endY});");
            }
            else
            {
                // Unknown segment type...
            }
        }
    }
}
