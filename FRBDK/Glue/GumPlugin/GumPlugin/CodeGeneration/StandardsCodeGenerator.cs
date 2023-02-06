using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;

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
                //codeBlock.If("this.WidthUnits == Gum.DataTypes.DimensionUnitType.RelativeToChildren")
                //    .Line("// make it have no line wrap width before assignign the text:")
                //    .Line("ContainedText.Width = 0;");

                //codeBlock.Line("ContainedText.RawText = value;");
                //codeBlock.Line("UpdateLayout();");

                codeBlock.Line("var widthBefore = ContainedText.WrappedTextWidth;");
                codeBlock.Line("var heightBefore = ContainedText.WrappedTextHeight;");

                codeBlock.Line("if (this.WidthUnits == Gum.DataTypes.DimensionUnitType.RelativeToChildren)");
                codeBlock.Line("{");
                codeBlock.Line("    // make it have no line wrap width before assignign the text:");
                codeBlock.Line("    ContainedText.Width = 0;");
                codeBlock.Line("}");


                codeBlock.Line("ContainedText.RawText = value;");

                codeBlock.Line("var shouldUpdate = widthBefore != ContainedText.WrappedTextWidth || heightBefore != ContainedText.WrappedTextHeight;");

                codeBlock.Line("if (shouldUpdate)");
                codeBlock.Line("{");
                if(GlueState.Self.CurrentGlueProject?.FileVersion >= (int)GluxVersions.GumTextObjectsUpdateTextWith0ChildDepth)
                {
                    codeBlock.Line("    UpdateLayout(Gum.Wireframe.GraphicalUiElement.ParentUpdateType.IfParentWidthHeightDependOnChildren | Gum.Wireframe.GraphicalUiElement.ParentUpdateType.IfParentStacks, 0);");
                }
                else
                {
                    codeBlock.Line("    UpdateLayout(true, int.MaxValue/2);");
                }
                codeBlock.Line("}");
            });

            mStandardSetterReplacements.Add("FontScale", (codeBlock) =>
            {
                codeBlock.Line("ContainedText.FontScale = value;");
                codeBlock.Line("UpdateLayout();");
            });

            mStandardSetterReplacements.Add("SourceFile", codeBlock =>
            {
                codeBlock.Line("this.Texture = value;");
            });

            mStandardSetterReplacements.Add("Texture", (codeBlock) =>
            {
                //codeBlock.Line("ContainedSprite.Texture = value;");
                //codeBlock.Line("UpdateLayout();");

                // This allows the object to prevent unnecessary layouts when texture changes:


                codeBlock.Line("var shouldUpdateLayout = false;");

                codeBlock.Line("int widthBefore = -1;");
                codeBlock.Line("int heightBefore = -1;");

                codeBlock.Line("var isUsingPercentageWidthOrHeight = WidthUnits == Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile || HeightUnits == Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;");
                codeBlock.Line("if (isUsingPercentageWidthOrHeight)");
                codeBlock.Line("{");
                codeBlock.Line("    if (ContainedSprite.Texture != null)");
                codeBlock.Line("    {");
                codeBlock.Line("        widthBefore = ContainedSprite.Texture.Width;");
                codeBlock.Line("        heightBefore = ContainedSprite.Texture.Height;");
                codeBlock.Line("    }");
                codeBlock.Line("}");
                codeBlock.Line("ContainedSprite.Texture = value;");

                codeBlock.Line("if (isUsingPercentageWidthOrHeight)");
                codeBlock.Line("{");
                codeBlock.Line("    int widthAfter = -1;");
                codeBlock.Line("    int heightAfter = -1;");
                codeBlock.Line("    if (ContainedSprite.Texture != null)");
                codeBlock.Line("    {");
                codeBlock.Line("        widthAfter = ContainedSprite.Texture.Width;");
                codeBlock.Line("        heightAfter = ContainedSprite.Texture.Height;");
                codeBlock.Line("    }");
                codeBlock.Line("    shouldUpdateLayout = widthBefore != widthAfter || heightBefore != heightAfter;");
                codeBlock.Line("}");

                codeBlock.Line("if (shouldUpdateLayout)");
                codeBlock.Line("{");
                codeBlock.Line("    UpdateLayout();");
                codeBlock.Line("}");
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

            // This could be any type, so don't force it to line rectangle
            //mStandardElementToQualifiedTypes.Add("Container", "RenderingLibrary.Math.Geometry.LineRectangle");
            mStandardElementToQualifiedTypes.Add("Container", null);

            mStandardElementToQualifiedTypes.Add("SolidRectangle", "RenderingLibrary.Graphics.SolidRectangle");
            mStandardElementToQualifiedTypes.Add("Sprite", "RenderingLibrary.Graphics.Sprite");
            mStandardElementToQualifiedTypes.Add("Text", "RenderingLibrary.Graphics.Text");
            mStandardElementToQualifiedTypes.Add("Circle", "RenderingLibrary.Math.Geometry.LineCircle");
            mStandardElementToQualifiedTypes.Add("Rectangle", "RenderingLibrary.Math.Geometry.LineRectangle");
            mStandardElementToQualifiedTypes.Add("Polygon", "RenderingLibrary.Math.Geometry.LinePolygon");

            mStandardElementToQualifiedTypes.Add("Arc", "SkiaGum.Renderables.RenderableArc");
            mStandardElementToQualifiedTypes.Add("ColoredCircle", "SkiaGum.Renderables.RenderableCircle");
            mStandardElementToQualifiedTypes.Add("LottieAnimation", "SkiaGum.Renderables.RenderableLottieAnimation");
            mStandardElementToQualifiedTypes.Add("RoundedRectangle", "SkiaGum.Renderables.RenderableRoundedRectangle");
            mStandardElementToQualifiedTypes.Add("Svg", "SkiaGum.Renderables.RenderableSvg");


            // What we will never support (as is)


            mVariableNamesToSkipForProperties.Add("Custom Texture Coordinates"); // replaced by texture address mode
            mVariableNamesToSkipForProperties.Add("Height Units");
            mVariableNamesToSkipForProperties.Add("Width Units");
            mVariableNamesToSkipForProperties.Add("Parent");
            mVariableNamesToSkipForProperties.Add("Guide");
            mVariableNamesToSkipForProperties.Add("IsItalic");
            mVariableNamesToSkipForProperties.Add("IsBold");

            mVariableNamesToSkipForProperties.Add("X Origin");
            mVariableNamesToSkipForProperties.Add("X Units");
            mVariableNamesToSkipForProperties.Add("Y Origin");
            mVariableNamesToSkipForProperties.Add("Y Units");
            mVariableNamesToSkipForProperties.Add("IgnoredByParentSize");

            mVariableNamesToSkipForProperties.Add("FlipHorizontal");


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
            mVariableNamesToSkipForProperties.Add("StackSpacing");

            // This restriction is only enforced Gum-side, not runtime-side (yet? ever?)
            mVariableNamesToSkipForProperties.Add("Contained Type");

            mVariableNamesToSkipForProperties.Add("Clips Children");
            mVariableNamesToSkipForProperties.Add("Wraps Children");

            mVariableNamesToSkipForProperties.Add("IsXamarinFormsControl");
            mVariableNamesToSkipForProperties.Add("IsOverrideInCodeGen");


            // properties to skip because they're handled in the GUE
            {
                mVariableNamesToSkipForProperties.Add("Rotation");
                mVariableNamesToSkipForProperties.Add("Wrap");
                mVariableNamesToSkipForProperties.Add("CurrentChainName");
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
            ICodeBlock classBodyBlock = codeBlock.Class("public partial", runtimeClassName, " : Gum.Wireframe.GraphicalUiElement");

            GueDerivingClassCodeGenerator.Self.GenerateConstructor(standardElementSave, classBodyBlock, runtimeClassName);

            string containedGraphicalObjectName = CreateContainedObjectMembers(classBodyBlock, standardElementSave);

            GenerateStates(standardElementSave, classBodyBlock);

            GenerateVariableProperties(standardElementSave, classBodyBlock, containedGraphicalObjectName);

            GenerateAssignDefaultState(standardElementSave, classBodyBlock);

            if(standardElementSave.Name == "Container")
            {
                GenerateGenericContainerCode(codeBlock);
            }

            GenerateAdditionalMethods(standardElementSave, classBodyBlock);

            return true;
        }

        private void GenerateAdditionalMethods(StandardElementSave standardElementSave, ICodeBlock classBodyBlock)
        {
            if(standardElementSave.Name == "Sprite")
            {
                var textureCoordinatesMethodBlock = classBodyBlock.Function("public void", "SetTextureCoordinatesFrom", "FlatRedBall.Graphics.Animation.AnimationFrame frbAnimationFrame");

                textureCoordinatesMethodBlock.Line("this.Texture = frbAnimationFrame.Texture;");
                textureCoordinatesMethodBlock.Line("this.TextureAddress = Gum.Managers.TextureAddress.Custom;");
                textureCoordinatesMethodBlock.Line("this.TextureLeft = FlatRedBall.Math.MathFunctions.RoundToInt(frbAnimationFrame.LeftCoordinate * frbAnimationFrame.Texture.Width);");
                textureCoordinatesMethodBlock.Line("this.TextureWidth = FlatRedBall.Math.MathFunctions.RoundToInt((frbAnimationFrame.RightCoordinate - frbAnimationFrame.LeftCoordinate) * frbAnimationFrame.Texture.Width);");
                textureCoordinatesMethodBlock.Line("this.TextureTop = FlatRedBall.Math.MathFunctions.RoundToInt(frbAnimationFrame.TopCoordinate * frbAnimationFrame.Texture.Height);");
                textureCoordinatesMethodBlock.Line("this.TextureHeight = FlatRedBall.Math.MathFunctions.RoundToInt((frbAnimationFrame.BottomCoordinate - frbAnimationFrame.TopCoordinate) * frbAnimationFrame.Texture.Height);");

                var sourceFileNameProperty = classBodyBlock.Property("public string", "SourceFileName");
                sourceFileNameProperty.Line("set => base.SetProperty(\"SourceFile\", value);");
            }
            else if(standardElementSave.Name == "Text")
            {
                var overrideTextRenderingPositionModeProperty = classBodyBlock.Property("public RenderingLibrary.Graphics.TextRenderingPositionMode?", "OverrideTextRenderingPositionMode");
                overrideTextRenderingPositionModeProperty.Line("get => mContainedText.OverrideTextRenderingPositionMode;");
                overrideTextRenderingPositionModeProperty.Line("set => mContainedText.OverrideTextRenderingPositionMode = value;");
            }
        }

        private void GenerateGenericContainerCode(ICodeBlock codeBlock)
        {
            codeBlock.Line(@"
    public class ContainerRuntime<T> : ContainerRuntime where T : Gum.Wireframe.GraphicalUiElement, new()
    {
        public new System.Collections.Generic.IEnumerable<T> Children
        {
            get
            {
                foreach(T item in base.Children)
                {
                    yield return item;
                }
            }
        }

        public ContainerRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(fullInstantiation, tryCreateFormsObject)
        {
        }

        public void AddChild(T newChild)
        {
            base.Children.Add(newChild);
        }

        public void RemoveChild(T childToRemove)
        {
            base.Children.Remove(childToRemove);
        }

        public void ClearChildren() => base.Children.Clear();


        public T AddChild()
        {
            var child = new T();

            AddChild(child);

            return child;
        }
    }
");
        }

        private void GenerateAssignDefaultState(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock = currentBlock.Function("public override void", "SetInitialState", "");
            {
                var suspendLayout = GlueState.Self.CurrentGlueProject.FileVersion >= (int)FlatRedBall.Glue.SaveClasses.GlueProjectSave.GluxVersions.GumHasMIsLayoutSuspendedPublic;
                if(suspendLayout)
                {
                    currentBlock.Line("var wasSuppressed = this.IsLayoutSuspended;");
                    currentBlock.Line("if(!wasSuppressed) this.SuspendLayout();");
                }
                currentBlock.Line("this.CurrentVariableState = VariableState.Default;");
                if(suspendLayout)
                {
                    currentBlock.Line("if(!wasSuppressed) this.ResumeLayout();");
                }

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
                    bool shouldGenerateVariable = GetIfShouldGenerateProperty(additionalVariable, standardElementSave);

                    if (shouldGenerateVariable)
                    {
                        GenerateVariable(currentBlock, containedGraphicalObjectName, additionalVariable, standardElementSave);
                    }
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
                    $"does not contain the key {standardElementSave.Name}. Look at the StandardsCodeGenerator constructor and add an entry there.");
            }
            string qualifiedBaseType = mStandardElementToQualifiedTypes[standardElementSave.Name];

            if(!string.IsNullOrEmpty(qualifiedBaseType))
            {
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
            else
            {
                return null;
            }
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

            if(standardElementSave.Name == "Container" && variable.Name == "Color")
            {
                return false;
            }
            if(standardElementSave.Name == "Svg")
            {
                if ( variable.Name == "Animate" || variable.Name == "SourceFile")
                {
                    return false;
                }
            }
            if(standardElementSave.Name == "LottieAnimation")
            {
                if (variable.Name == "SourceFile")
                {
                    return false;
                }
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

            if(containedGraphicalObjectName == null)
            {
                containedGraphicalObjectName = "((RenderingLibrary.Graphics.IRenderableIpso)this.RenderableComponent)";
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
                var noSpaceName = variable.Name?.Replace(" ", "");
                if (mStandardSetterReplacements.ContainsKey(noSpaceName))
                {
                    mStandardSetterReplacements[noSpaceName](setter);
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
                setter.Line("mWidth = value*2;");
                setter.Line("mHeight = value*2;");
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
