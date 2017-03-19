using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumPlugin.CodeGeneration
{
    public class StandardsCodeGenerator : Singleton<StandardsCodeGenerator>
    {
        #region Fields

        Dictionary<string, Action<ICodeBlock>> mStandardGetterReplacements = new Dictionary<string, Action<ICodeBlock>>();
        Dictionary<string, Action<ICodeBlock>> mStandardSetterReplacements = new Dictionary<string, Action<ICodeBlock>>();

        Dictionary<string, string> mStandardElementToQualifiedTypes = new Dictionary<string, string>();

        Dictionary<string, string> mStandardVariableNameAliases = new Dictionary<string, string>();

        List<string> variablesToCallLayoutAfter = new List<string>();

        List<string> mVariableNamesToSkipForProperties = new List<string>();

        #endregion

        #region Constructor

        public StandardsCodeGenerator()
        {
            variablesToCallLayoutAfter.Add("Text");

            // This says what the property name is and what the contained variable name is.
            // For example:
            // public string Text
            // {
            //   get { return mContainedObject.RawText; }
            //   ...
            mStandardVariableNameAliases.Add("Text", "RawText");
            mStandardVariableNameAliases.Add("Blend", "BlendState");
            mStandardVariableNameAliases.Add("SourceFile", "Texture");

            mStandardElementToQualifiedTypes.Add("LineRectangle", "RenderingLibrary.Math.Geometry.LineRectangle");
            mStandardElementToQualifiedTypes.Add("NineSlice", "RenderingLibrary.Graphics.NineSlice");
            mStandardElementToQualifiedTypes.Add("ColoredRectangle", "RenderingLibrary.Graphics.SolidRectangle");
            mStandardElementToQualifiedTypes.Add("Container", "RenderingLibrary.Math.Geometry.LineRectangle");

            mStandardElementToQualifiedTypes.Add("SolidRectangle", "RenderingLibrary.Graphics.SolidRectangle");
            mStandardElementToQualifiedTypes.Add("Sprite", "RenderingLibrary.Graphics.Sprite");
            mStandardElementToQualifiedTypes.Add("Text", "RenderingLibrary.Graphics.Text");
            mStandardElementToQualifiedTypes.Add("Circle", "RenderingLibrary.Math.Geometry.LineCircle");
            mStandardElementToQualifiedTypes.Add("Rectangle", "RenderingLibrary.Math.Geometry.LineRectangle");


            // What we will never support (as is)
            mVariableNamesToSkipForProperties.Add("Custom Texture Coordinates");
            mVariableNamesToSkipForProperties.Add("Height Units");
            mVariableNamesToSkipForProperties.Add("Width Units");
            mVariableNamesToSkipForProperties.Add("Parent");
            mVariableNamesToSkipForProperties.Add("Guide");

            mVariableNamesToSkipForProperties.Add("X Origin");
            mVariableNamesToSkipForProperties.Add("X Units");
            mVariableNamesToSkipForProperties.Add("Y Origin");
            mVariableNamesToSkipForProperties.Add("Y Units");

            mVariableNamesToSkipForProperties.Add("Font");
            mVariableNamesToSkipForProperties.Add("FontSize");

            mVariableNamesToSkipForProperties.Add("X");
            mVariableNamesToSkipForProperties.Add("Y");
            mVariableNamesToSkipForProperties.Add("Width");
            mVariableNamesToSkipForProperties.Add("Height");
            mVariableNamesToSkipForProperties.Add("Visible");
            mVariableNamesToSkipForProperties.Add("OutlineThickness");

            mVariableNamesToSkipForProperties.Add("HasEvents");
            mVariableNamesToSkipForProperties.Add("ExposeChildrenEvents");

            // What may be supported at some point in the future:
            mVariableNamesToSkipForProperties.Add("Texture Height");
            mVariableNamesToSkipForProperties.Add("Texture Width");
            mVariableNamesToSkipForProperties.Add("Texture Left");
            mVariableNamesToSkipForProperties.Add("Texture Top");

            mVariableNamesToSkipForProperties.Add("Texture Address");
            mVariableNamesToSkipForProperties.Add("Texture Width Scale");
            mVariableNamesToSkipForProperties.Add("Texture Height Scale");


            mVariableNamesToSkipForProperties.Add("State");
            mVariableNamesToSkipForProperties.Add("CustomFontFile");
            mVariableNamesToSkipForProperties.Add("UseCustomFont");
            mVariableNamesToSkipForProperties.Add("Children Layout");
            mVariableNamesToSkipForProperties.Add("Clips Children");
            mVariableNamesToSkipForProperties.Add("Wraps Children");

        }

        #endregion

        public bool GenerateStandardElementSaveCodeFor(StandardElementSave standardElementSave, ICodeBlock codeBlock)
        {
            ///////////// EARLY OUT//////////////
            if (GueDerivingClassCodeGenerator.Self.ShouldGenerateRuntimeFor(standardElementSave) == false)
            {
                return false;
            }
            //////////////END EARLY OUT///////////

            string runtimeClassName = GueDerivingClassCodeGenerator.GetUnqualifiedRuntimeTypeFor(standardElementSave);

            // This needs to be public because it can be exposed as public in a public class
            //ICodeBlock currentBlock = codeBlock.Class("partial", runtimeClassName, " : Gum.Wireframe.GraphicalUiElement");
            ICodeBlock currentBlock = codeBlock.Class("public partial", runtimeClassName, " : Gum.Wireframe.GraphicalUiElement");

            GueDerivingClassCodeGenerator.Self.GenerateConstructor(standardElementSave, currentBlock, runtimeClassName);

            string propertyName = CreateContainedObjectMembers(currentBlock, standardElementSave);

            GenerateStates(standardElementSave, currentBlock);

            GenerateVariableProperties(standardElementSave, currentBlock, propertyName);

            return true;
        }

        private void GenerateStates(StandardElementSave standardElementSave, ICodeBlock currentBlock)
        {
            StateCodeGenerator.Self.GenerateEverythingFor(standardElementSave, currentBlock);
        }

        private void GenerateVariableProperties(StandardElementSave standardElementSave, ICodeBlock currentBlock, string propertyName)
        {
            foreach (var variable in standardElementSave.DefaultState.Variables)
            {
                bool shouldGenerateVariable = GetIfShouldGenerateProperty(variable, standardElementSave);

                if (shouldGenerateVariable)
                {

                    GenerateVariable(currentBlock, propertyName, variable, standardElementSave);
                }
            }
        }

        private string CreateContainedObjectMembers(ICodeBlock currentBlock, ElementSave standardElementSave)
        {

            string qualifiedBaseType = mStandardElementToQualifiedTypes[standardElementSave.Name];
            string unqualifiedBaseType = standardElementSave.Name;

            string fieldName = "mContained" + unqualifiedBaseType;

            currentBlock.Line(qualifiedBaseType + " " + fieldName + ";");

            string propertyName = "Contained" + unqualifiedBaseType;

            var containedProperty = currentBlock.Property(qualifiedBaseType, propertyName);
            {
                var get = containedProperty.Get();
                {
                    var ifBlock = get.If(fieldName + " == null");
                    {
                        ifBlock.Line(fieldName + " = this.RenderableComponent as " + qualifiedBaseType + ";");
                    }

                    get.Line("return " + fieldName + ";");
                }
            }
            return propertyName;
        }

        private bool GetIfShouldGenerateProperty(Gum.DataTypes.Variables.VariableSave variable, ElementSave standardElementSave)
        {
            string variableName = variable.GetRootName();

            if (mVariableNamesToSkipForProperties.Contains(variableName))
            {
                return false;
            }

            return true;
        }

        private void GenerateVariable(ICodeBlock currentBlock, string propertyName, Gum.DataTypes.Variables.VariableSave variable, ElementSave elementSave)
        {
            #region Get Variable Type
            string variableType = variable.Type;

            string unmodifiedVariableType = variableType;

            if (GueDerivingClassCodeGenerator.Self.TypeToQualifiedTypes.ContainsKey(variableType))
            {
                variableType = GueDerivingClassCodeGenerator.Self.TypeToQualifiedTypes[variableType];
            }

            if(variable.IsFile && variable.GetRootName() == "SourceFile")
            {
                variableType = "Microsoft.Xna.Framework.Graphics.Texture2D";
            }

            #endregion

            ICodeBlock property = currentBlock.Property("public " + variableType, variable.Name.Replace(" ", ""));


            string variableName = variable.Name;
            if (mStandardVariableNameAliases.ContainsKey(variableName.Replace(" ", "")))
            {
                variableName = mStandardVariableNameAliases[variableName.Replace(" ", "")];
            }

            string whatToGetOrSet = propertyName + "." + variableName.Replace(" ", "");

            GenerateGetter(propertyName, variable, property, variableName, whatToGetOrSet, elementSave);

            GenerateSetter(propertyName, variable, property, variableName, whatToGetOrSet, elementSave);
        }

        private void GenerateSetter(string propertyName, VariableSave variable, ICodeBlock property, string variableName, string whatToGetOrSet, ElementSave elementSave)
        {
            var setter = property.Set();
            bool wasHandled = TryHandleCustomSetter(variable, elementSave, setter);
            if (!wasHandled)
            {
                if (mStandardSetterReplacements.ContainsKey(variableName))
                {
                    mStandardSetterReplacements[propertyName](setter);
                }
                else
                {

                    string rightSide = "value";

                    rightSide = AdjustStandardElementVariableSetIfNecessary(variable, rightSide);

                    setter.Line(whatToGetOrSet + " = " + rightSide + ";");
                }

                if(variablesToCallLayoutAfter.Contains(variable.Name))
                {
                    setter.Line("UpdateLayout();");
                }
            }
        }

        private bool TryHandleCustomSetter(VariableSave variable, ElementSave elementSave, ICodeBlock setter)
        {
            if(variable.GetRootName() == "SourceFile" && elementSave.Name == "NineSlice")
            {

                //setter.Line("bool usePattern = RenderingLibrary.Graphics.NineSlice.GetIfShouldUsePattern(value);");
                
                //var ifBlock = setter.If("usePattern");
                //{
                //    ifBlock.Line("this.SetTexturesUsingPattern(value, null);");
                //}
                //var elseBlock = ifBlock.End().Else();
                //{
                //    var internalIf = elseBlock.If("if (!string.IsNullOrEmpty(value))");
                //    {
                //        internalIf.Line("this.SetSingleTexture(RenderingLibrary.Content.LoaderManager.Self.Load(value, RenderingLibrary.SystemManagers.Default));");
                //    }

                //}

                //return true;


                setter.Line("ContainedNineSlice.SetSingleTexture(value);");


                return true;

            }
            return false;
        }

        private void GenerateGetter(string propertyName, VariableSave variable, 
            ICodeBlock property, string variableName, string whatToGetOrSet, ElementSave elementSave)
        {
            var getter = property.Get();

            bool wasHandled = TryHandleCustomGetter(variable, elementSave, getter);

            if (!wasHandled)
            {

                if (mStandardGetterReplacements.ContainsKey(variableName))
                {
                    mStandardGetterReplacements[propertyName](getter);
                }
                else
                {
                    string whatToReturn = whatToGetOrSet;
                    whatToReturn = AdjustStandardElementVariableGetIfNecessary(variable, whatToReturn);

                    getter.Line("return " + whatToReturn + ";");
                }
            }
        }

        private bool TryHandleCustomGetter(VariableSave variable, ElementSave elementSave, ICodeBlock getter)
        {
            if (variable.GetRootName() == "SourceFile" && elementSave.Name == "NineSlice")
            {

                getter.Line("return ContainedNineSlice.TopLeftTexture;");

                return true;
            }

            return false;
        }

        string AdjustStandardElementVariableGetIfNecessary(VariableSave variableSave, string value)
        {
            if (variableSave.Type == "Blend")
            {
                value = "Gum.RenderingLibrary.BlendExtensions.ToBlend(" + value + ")";
            }

            return value;
        }

        string AdjustStandardElementVariableSetIfNecessary(VariableSave variableSave, string value)
        {
            if (variableSave.Type == "Blend")
            {
                value = "Gum.RenderingLibrary.BlendExtensions.ToBlendState(" + value + ")";
            }


            return value;
        }
    }
}
