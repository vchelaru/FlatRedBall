using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
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

            var pathsVariableDefinition = new VariableDefinition();
            pathsVariableDefinition.Type = "string";
            pathsVariableDefinition.Name = "Paths";
            pathsVariableDefinition.UsesCustomCodeGeneration = true;
            pathsVariableDefinition.CustomGenerationFunc = GeneratePaths;
            pathsVariableDefinition.CustomPropertyGenerationFunc = GenerateProperty;

            pathsVariableDefinition.Category = "Path";
            pathsVariableDefinition.PreferredDisplayer = typeof(PathView);
            ati.VariableDefinitions.Add(pathsVariableDefinition);


            return ati;
        }

        private static void GenerateProperty(IElement arg1, CustomVariable customVariable, ICodeBlock codeBlock)
        {
            var prop = codeBlock.Property($"public string", customVariable.Name);

            var setter = prop.Set();

            setter.Line($"{customVariable.SourceObject}.FromJson(value);");
        }

        static string FloatToString(float value) => value.ToString(CultureInfo.InvariantCulture);

        private static string GeneratePaths(IElement element, NamedObjectSave nos, ReferencedFileSave rfs, string memberName)
        {
            StringBuilder toReturn = new StringBuilder();

            var variable = nos.GetCustomVariable(memberName ?? PathsVariableName);

            var nosElement = ObjectFinder.Self.GetElement(nos.SourceClassType);

            string ownerName = nos.InstanceName;

            if(nosElement != null)
            {
                // this is a tunneled variable
                var customVariable = nosElement.CustomVariables.Find(item => item.Name == memberName);

                if(!string.IsNullOrEmpty(customVariable?.SourceObject))
                {
                    ownerName += "." + customVariable.SourceObject;
                }
            }

            var variableValue = variable?.Value as string;

            toReturn.AppendLine($"{ownerName}.Clear();");

            if(!string.IsNullOrEmpty(variableValue))
            {
                var deserialized = JsonConvert.DeserializeObject<List<PathSegment>>(variableValue);

                foreach(var item in deserialized)
                {
                    var endX = FloatToString(item.EndX);
                    var endY = FloatToString(item.EndY);
                    if(item.SegmentType == SegmentType.Line)
                    {
                        toReturn.AppendLine($"{ownerName}.LineToRelative({endX}, {endY});");
                        //LineToRelative(float x, float y)
                    }
                    else if(item.SegmentType == SegmentType.Arc)
                    {
                        var signedAngle = FloatToString(item.ArcAngle);

                        //ArcToRelative(float endX, float endY, float signedAngle)
                        toReturn.AppendLine(
                            $"{ownerName}.ArcToRelative({endX}, {endY}, Microsoft.Xna.Framework.MathHelper.ToRadians({signedAngle}));");
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
