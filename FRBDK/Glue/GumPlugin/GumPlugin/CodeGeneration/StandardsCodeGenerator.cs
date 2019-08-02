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

        // These are new variables that don't appear in the base definitioin of the standard element, but we support in code for convenience
        List<VariableSave> variableNamesToAddForProperties = new List<VariableSave>();

        #endregion

        #region Constructor

        public StandardsCodeGenerator()
        {
            mStandardSetterReplacements.Add("Text", (codeBlock) =>
            {
                codeBlock.If("this.WidthUnits == Gum.DataTypes.DimensionUnitType.RelativeToChildren")
                    .Line("// make it have no line wrap width before assignign the text:")
                    .Line("ContainedText.Width = 0;");

                codeBlock.Line("ContainedText.RawText = value;");
                codeBlock.Line("UpdateLayout();");
            });


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
            mStandardElementToQualifiedTypes.Add("Polygon", "RenderingLibrary.Math.Geometry.LinePolygon");


            // What we will never support (as is)


            mVariableNamesToSkipForProperties.Add("Custom Texture Coordinates"); // replaced by texture address mode
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
            mVariableNamesToSkipForProperties.Add("UseFontSmoothing");

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

            // This restriction is only enforced Gum-side, not runtime-side (yet? ever?)
            mVariableNamesToSkipForProperties.Add("Contained Type");

            mVariableNamesToSkipForProperties.Add("Clips Children");
            mVariableNamesToSkipForProperties.Add("Wraps Children");

            // properties to skip because they're handled in the GUE
            {
                mVariableNamesToSkipForProperties.Add("Rotation");
                mVariableNamesToSkipForProperties.Add("Wrap");
            }

            variableNamesToAddForProperties.Add(new VariableSave
            {
                IsFile = false,
                IsFont = false,

                Type = "Microsoft.Xna.Framework.Color",
                Name = "Color"
            });

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

            string runtimeClassName = GueDerivingClassCodeGenerator.Self.GetUnqualifiedRuntimeTypeFor(standardElementSave);

            // This needs to be public because it can be exposed as public in a public class
            //ICodeBlock currentBlock = codeBlock.Class("partial", runtimeClassName, " : Gum.Wireframe.GraphicalUiElement");
            ICodeBlock currentBlock = codeBlock.Class("public partial", runtimeClassName, " : Gum.Wireframe.GraphicalUiElement");

            GueDerivingClassCodeGenerator.Self.GenerateConstructor(standardElementSave, currentBlock, runtimeClassName);

            string containedGraphicalObjectName = CreateContainedObjectMembers(currentBlock, standardElementSave);

            GenerateStates(standardElementSave, currentBlock);

            GenerateVariableProperties(standardElementSave, currentBlock, containedGraphicalObjectName);

            GenerateAssignDefaultState(standardElementSave, currentBlock);

            return true;
        }


        private void GenerateAssignDefaultState(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock = currentBlock.Function("public override void", "SetInitialState", "");
            {
                currentBlock.Line("this.CurrentVariableState = VariableState.Default;");
            }
        }

        private void GenerateStates(StandardElementSave standardElementSave, ICodeBlock currentBlock)
        {
            StateCodeGenerator.Self.GenerateEverythingFor(standardElementSave, currentBlock);
        }

        private void GenerateVariableProperties(StandardElementSave standardElementSave, ICodeBlock currentBlock, string containedGraphicalObjectName)
        {
            // This can be null if the backing file is missing. Don't report an error, the error window
            // will do it through file tracking
            if(standardElementSave.DefaultState != null)
            {
                // generate properties on the default state:
                foreach (var variable in standardElementSave.DefaultState.Variables)
                {
                    bool shouldGenerateVariable = GetIfShouldGenerateProperty(variable, standardElementSave);

                    if (shouldGenerateVariable)
                    {

                        GenerateVariable(currentBlock, containedGraphicalObjectName, variable, standardElementSave);
                    }
                }

                foreach(var additionalVariable in variableNamesToAddForProperties)
                {
                    GenerateVariable(currentBlock, containedGraphicalObjectName, additionalVariable, standardElementSave);
                }
            }

            if(standardElementSave.Name == "Text")
            {
                // generate text-specific properties here:
                GenerateVariable(currentBlock, containedGraphicalObjectName, 
                    new VariableSave { Name = "BitmapFont", Type = "RenderingLibrary.Graphics.BitmapFont" }, 
                    standardElementSave);
                
                GenerateVariable(currentBlock, containedGraphicalObjectName,
                    new VariableSave { Name = "WrappedText", Type = "System.Collections.Generic.List<string>" },
                    standardElementSave,
                    generateSetter:false);

            }
            else if(standardElementSave.Name == "Sprite")
            {
                GenerateVariable(currentBlock, containedGraphicalObjectName,
                    new VariableSave { Name = "Texture", Type = "Microsoft.Xna.Framework.Graphics.Texture2D" },
                    standardElementSave);

            }
        }

        private string CreateContainedObjectMembers(ICodeBlock currentBlock, ElementSave standardElementSave)
        {
            if(mStandardElementToQualifiedTypes.ContainsKey(standardElementSave.Name) == false)
            {
                throw new InvalidOperationException($"The {nameof(mStandardElementToQualifiedTypes)} " +
                    $"does not contain the key {standardElementSave.Name}");
            }
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
            // Core Gum objets don't have states, so if it's a state then don't create a property for it - it'll be handled
            // by the code that handles states
            if(variable.IsState(standardElementSave))
            {
                return false;
            }

            return true;
        }

        private void GenerateVariable(ICodeBlock currentBlock, string containedGraphicalObjectName, Gum.DataTypes.Variables.VariableSave variable, ElementSave elementSave, 
            bool generateSetter = true)
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

            ICodeBlock propertyCodeBlock = currentBlock.Property("public " + variableType, variable.Name.Replace(" ", ""));


            string variableName = variable.Name;
            if (mStandardVariableNameAliases.ContainsKey(variableName.Replace(" ", "")))
            {
                variableName = mStandardVariableNameAliases[variableName.Replace(" ", "")];
            }

            string whatToGetOrSet = containedGraphicalObjectName + "." + variableName.Replace(" ", "");

            GenerateGetter(containedGraphicalObjectName, variable, propertyCodeBlock, variableName, whatToGetOrSet, elementSave);

            if(generateSetter)
            {
                GenerateSetter(containedGraphicalObjectName, variable, propertyCodeBlock, variableName, whatToGetOrSet, elementSave);
            }
        }

        private void GenerateSetter(string propertyName, VariableSave variable, ICodeBlock property, string variableName, string whatToGetOrSet, ElementSave elementSave)
        {
            var setter = property.Set();
            bool wasHandled = TryHandleCustomSetter(variable, elementSave, setter);
            if (!wasHandled)
            {
                if (mStandardSetterReplacements.ContainsKey(variable.Name))
                {
                    mStandardSetterReplacements[variable.Name](setter);
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
                setter.Line("ContainedNineSlice.SetSingleTexture(value);");
                return true;
            }

            if(elementSave.Name == "Circle" && variable.GetRootName() == "Radius")
            {
                setter.Line("mWidth = value/2;");
                setter.Line("mHeight = value/2;");
                setter.Line("ContainedCircle.Radius = value;");
                return true;
            }

            if (elementSave.Name == "Circle" || 
                elementSave.Name == "Rectangle" ||
                elementSave.Name == "Polygon")
            {
                string containedObject = null;

                if (elementSave.Name == "Circle")
                {
                    containedObject = "ContainedCircle";
                }
                else if(elementSave.Name == "Rectangle")
                {
                    containedObject = "ContainedRectangle";
                }
                else if(elementSave.Name == "Polygon")
                {
                    containedObject = "ContainedPolygon";
                }

                string colorComponent = null;

                if (variable.Name == "Alpha")
                {
                    colorComponent = "A";
                }
                else if (variable.Name == "Red")
                {
                    colorComponent = "R";
                }
                else if (variable.Name == "Green")
                {
                    colorComponent = "G";
                }
                else if (variable.Name == "Blue")
                {
                    colorComponent = "B";
                }

                if(!string.IsNullOrEmpty(colorComponent))
                {

                    setter.Line($"var color = {containedObject}.Color;");
                    setter.Line($"color.{colorComponent} = (byte)value;");
                    setter.Line($"{containedObject}.Color = color;");
                    return true;
                }
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
            // handle colors:

            if (elementSave.Name == "Circle" || elementSave.Name == "Rectangle"  ||
                elementSave.Name == "Polygon")
            {
                string containedObject;
                
                if(elementSave.Name == "Circle")
                {
                    containedObject = "ContainedCircle";
                }
                else if(elementSave.Name == "Rectangle")
                {
                    containedObject = "ContainedRectangle";
                }
                else
                {
                    containedObject = "ContainedPolygon";
                }
                if(variable.Name == "Alpha")
                {
                    getter.Line($"return {containedObject}.Color.A;");
                    return true;
                }
                else if (variable.Name == "Red")
                {
                    getter.Line($"return {containedObject}.Color.R;");
                    return true;
                }
                else if (variable.Name == "Green")
                {
                    getter.Line($"return {containedObject}.Color.G;");
                    return true;
                }
                else if (variable.Name == "Blue")
                {
                    getter.Line($"return {containedObject}.Color.B;");
                    return true;
                }
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
