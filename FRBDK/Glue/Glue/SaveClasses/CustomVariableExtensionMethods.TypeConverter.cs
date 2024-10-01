using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using Microsoft.Xna.Framework;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Glue.GuiDisplay.Facades;
using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.SaveClasses
{
    public  static partial class CustomVariableExtensionMethods
    {

        public static TypeConverter GetTypeConverter(this CustomVariable customVariable, GlueElement containingElement)
        {
            return customVariable.GetTypeConverter(containingElement, null, null);
        }

        public static TypeConverter GetTypeConverter(this CustomVariable customVariable, GlueElement containingElementAsIElement, StateSave stateSave, FlatRedBall.Glue.Plugins.ExportedInterfaces.IGlueState glueState)
        {
            var containingElement = containingElementAsIElement as GlueElement;
            TypeConverter typeConverter = null;

            if (customVariable.GetIsVariableState())
            {
                typeConverter = new AvailableStates(
                    FacadeContainer.Self.GlueState.CurrentNamedObjectSave,
                    FacadeContainer.Self.GlueState.CurrentElement,
                    // I think this expected that we'd always be viewing the current variable, but we're not in the state data
                    //FacadeContainer.Self.GlueState.CurrentCustomVariable,
                    customVariable,
                    FacadeContainer.Self.GlueState.CurrentStateSave
                );
            }
            else
            {
                Type runtimeType = customVariable.GetRuntimeType();

                if (runtimeType != null)
                {
                    if (runtimeType.IsEnum)
                    {
                        typeConverter = new EnumConverter(runtimeType);
                    }
                    else if (runtimeType == typeof(Color))
                    {
                        return new AvailableColorTypeConverter();
                    }

                    else if ((runtimeType == typeof(string) || runtimeType == typeof(AnimationChainList)) &&
                        customVariable.SourceObjectProperty == "CurrentChainName")
                    {

                        typeConverter = new AvailableAnimationChainsStringConverter(customVariable, stateSave);
                    }
                    else if (customVariable.GetIsFile())
                    {
                        AvailableFileStringConverter converter = new AvailableFileStringConverter(containingElement);
                        converter.QualifiedRuntimeTypeNameFilter = runtimeType.FullName;
                        converter.ShowNewFileOption = false;
                        converter.RemovePathAndExtension = true;
                        typeConverter = converter;
                    }
                }
                else if(customVariable.GetIsBaseElementType(out GlueElement element))
                {
                    var derived= ObjectFinder.Self.GetAllDerivedElementsRecursive(element);

                    typeConverter = new AvailableDerivedElementsConverter(element);
                }
                else if (customVariable.GetIsCsv())
                {
                    var elements = ObjectFinder.Self.GetAllBaseElementsRecursively(containingElement).ToList();
                    elements.Add(containingElement);
                    

                    var rfsesUsingType = ObjectFinder.Self.GetAllReferencedFiles().Where(item =>
                    {
                        var isMatch = item.IsCsvOrTreatedAsCsv && item.GetTypeForCsvFile() == customVariable.Type;

                        if(isMatch)
                        {
                            // we include this if:
                            // 1 - it's in global content
                            // 2 - it's in this element
                            // 3 - it's in a base element
                            isMatch = GlueState.CurrentGlueProject.GlobalFiles.Contains(item) || elements.Any(element => element.ReferencedFiles.Contains(item));
                        }
                        return isMatch;
                    }).ToArray();



                    // May 3, 2020
                    // This currently
                    // uses only one RFS
                    // but maybe we want to 
                    // support setting the variable
                    // from a collection of RFS's? If
                    // so, then the variable will not only
                    // need to contain the CSV name, but the 
                    // fully-qualified CSV name

                    //var rfs = 
                    //    // prioritize this element...
                    //    rfsesUsingType.FirstOrDefault(item => containingElement.ReferencedFiles.Contains(item)) ??
                    //    // ... and if none found, fall back to any
                    //    rfsesUsingType.FirstOrDefault();

                    AvailableSpreadsheetValueTypeConverter converter = null;
                    if (rfsesUsingType.Length > 0)
                    {
                        var filePaths = rfsesUsingType.Select(item => new FilePath( GlueState.ContentDirectory + item.Name)).ToArray();
                        converter = new AvailableSpreadsheetValueTypeConverter(filePaths);
                    }
                    else
                    {
                        converter = new AvailableSpreadsheetValueTypeConverter(
                            GlueState.ContentDirectory + customVariable.Type);
                    }
                    converter.ShouldAppendFileName = true;

                    typeConverter = converter;
                }
                else if (customVariable.GetIsFile())
                {
                    // If we got here, that means that the
                    // CustomVariable is a file, but it doesn't
                    // have a System.Type, so it only knows its runtime
                    // type;
                    AvailableFileStringConverter converter = new AvailableFileStringConverter(containingElement);

                    var typeName = customVariable.Type;
                    if(typeName?.Contains('.') == true)
                    {
                        typeName = typeName.Substring(typeName.LastIndexOf('.') + 1);
                    }
                    converter.UnqualifiedRuntimeTypeNameFilter = typeName;
                    converter.ShowNewFileOption = false;
                    converter.RemovePathAndExtension = true;
                    typeConverter = converter;
                }

                if(typeConverter == null)
                {
                    var ati = containingElementAsIElement.GetAssetTypeInfo();
                    if(ati != null)
                    {
                        var variableDef = ati.VariableDefinitions.Find(item => item.Name == customVariable.Name);
                        // todo...
                        if(variableDef != null)
                        {
                            typeConverter = TypeConverterLogic.GetTypeConverter(null, containingElement, customVariable.Name, runtimeType, null, variableDef);
                        }
                    }
                }
            }


            return typeConverter;
        }
    }
}
