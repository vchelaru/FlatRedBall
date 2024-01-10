using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Math.Paths;
using Newtonsoft.Json;
using OfficialPlugins.PathPlugin.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Controls;

namespace OfficialPlugins.PathPlugin.Managers
{
    static class AssetTypeInfoManager
    {
        public static AssetTypeInfo PathAssetTypeInfo { get; private set; }

        static PathView LastPathView;

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
            pathsVariableDefinition.UiCreated += HandleUiCreated;


            ati.VariableDefinitions.Add(pathsVariableDefinition);


            return ati;
        }

        private static void HandleUiCreated(object control)
        {
            var pathView = control as PathView;

            LastPathView = pathView;
        }

        public static void HighlightIndex(int index)
        {
            LastPathView?.FocusIndex(index);
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

    // July 26, 2023 
    // Initially I thought
    // it might be a good idea
    // to hold on to all PathView
    // instances, but I decided it
    // might be simpler to just hold
    // on to the last one. I'm not sure
    // if I'll need to hold on to the entire
    // list, so I'm keeping this here just in case.
    //public class WeakCollection<T> : ICollection<T> where T : class
    //{
    //    private readonly List<WeakReference<T>> list = new List<WeakReference<T>>();

    //    public void Add(T item) => list.Add(new WeakReference<T>(item));
    //    public void Clear() => list.Clear();
    //    public int Count => list.Count;
    //    public bool IsReadOnly => false;

    //    public bool Contains(T item)
    //    {
    //        foreach (var element in this)
    //            if (Equals(element, item))
    //                return true;
    //        return false;
    //    }

    //    public void CopyTo(T[] array, int arrayIndex)
    //    {
    //        foreach (var element in this)
    //            array[arrayIndex++] = element;
    //    }

    //    public bool Remove(T item)
    //    {
    //        for (int i = 0; i < list.Count; i++)
    //        {
    //            if (!list[i].TryGetTarget(out T target))
    //                continue;
    //            if (Equals(target, item))
    //            {
    //                list.RemoveAt(i);
    //                return true;
    //            }
    //        }
    //        return false;
    //    }

    //    public IEnumerator<T> GetEnumerator()
    //    {
    //        for (int i = list.Count - 1; i >= 0; i--)
    //        {
    //            if (!list[i].TryGetTarget(out T element))
    //            {
    //                list.RemoveAt(i);
    //                continue;
    //            }
    //            yield return element;
    //        }
    //    }

    //    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    //}




}
