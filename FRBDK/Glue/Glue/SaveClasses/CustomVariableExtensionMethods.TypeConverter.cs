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

namespace FlatRedBall.Glue.SaveClasses
{
    public  static partial class CustomVariableExtensionMethods
    {

        public static TypeConverter GetTypeConverter(this CustomVariable customVariable, IElement containingElement)
        {
            return customVariable.GetTypeConverter(containingElement, null, null);
        }

        public static TypeConverter GetTypeConverter(this CustomVariable customVariable, IElement containingElement, StateSave stateSave, FlatRedBall.Glue.Plugins.ExportedInterfaces.IGlueState glueState)
        {
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
                        converter.QualifiedRuntimeTypeName = runtimeType.FullName;
                        converter.ShowNewFileOption = false;
                        converter.RemovePathAndExtension = true;
                        typeConverter = converter;
                    }
                }
                else if (customVariable.GetIsCsv())
                {
                    if (FacadeContainer.Self.ProjectValues == null)
                    {
                        throw new NullReferenceException("The ProjectValues property in FAcadeContainer.Self.ProjectValues must be set before trying to get the CSV type converter for the variable " + customVariable.ToString());
                    }

                    var rfsesUsingType = ObjectFinder.Self.GetAllReferencedFiles().Where(item =>
                        item.IsCsvOrTreatedAsCsv && item.GetTypeForCsvFile() == customVariable.Type);


                    // May 3, 2020
                    // This currently
                    // uses only one RFS
                    // but maybe we want to 
                    // support setting the variable
                    // from a collection of RFS's? If
                    // so, then the variable will not only
                    // need to contain the CSV name, but the 
                    // fully-qualified CSV name

                    var rfs = 
                        // prioritize this element...
                        rfsesUsingType.FirstOrDefault(item => containingElement.ReferencedFiles.Contains(item)) ??
                        // ... and if none found, fall back to any
                        rfsesUsingType.FirstOrDefault();

                    AvailableSpreadsheetValueTypeConverter converter = null;
                    if (rfs != null)
                    {
                        converter = new AvailableSpreadsheetValueTypeConverter(
                           FacadeContainer.Self.ProjectValues.ContentDirectory + rfs.Name, containingElement);
                    }
                    else
                    {
                        converter = new AvailableSpreadsheetValueTypeConverter(
                            FacadeContainer.Self.ProjectValues.ContentDirectory + customVariable.Type, containingElement);
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
                    converter.UnqualifiedRuntimeTypeName = typeName;
                    converter.ShowNewFileOption = false;
                    converter.RemovePathAndExtension = true;
                    typeConverter = converter;
                }
            }


            return typeConverter;
        }
    }
}
