using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using GumPluginCore.CodeGeneration;
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
            TextCodeGenerator.Self.AddStandardGetterSetterReplacements(mStandardGetterReplacements, mStandardSetterReplacements);

            SpriteCodeGenerator.Self.AddStandardGetterSetterReplacements(mStandardGetterReplacements, mStandardSetterReplacements);
            
            mStandardSetterReplacements.Add("SourceFile", codeBlock =>
            {
                codeBlock.Line("this.Texture = value;");
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
            mStandardElementToQualifiedTypes.Add("Canvas", "SkiaGum.Renderables.RenderableCanvas");


            // What we will never support (as is)

            variableNamesToAddForProperties.Add(new VariableSave
            {
                IsFile = false,
                IsFont = false,

                Type = "Microsoft.Xna.Framework.Color",
                Name = "Color"
            });
        }

        public void RefreshVariableNamesToSkipForProperties()
        {
            mVariableNamesToSkipForProperties.Clear();

            TextCodeGenerator.Self.AddVariableNamesToSkipForProperties(mVariableNamesToSkipForProperties);

            void ExcludeIfVersionLessThan(string propertyName, GluxVersions gluxVersion)
            {
                if(GlueState.Self.CurrentGlueProject.FileVersion < (int)gluxVersion)
                {
                    mVariableNamesToSkipForProperties.Add(propertyName);
                }
            }

            mVariableNamesToSkipForProperties.Add("Custom Texture Coordinates"); // replaced by texture address mode
            mVariableNamesToSkipForProperties.Add("Height Units");
            mVariableNamesToSkipForProperties.Add("Width Units");
            mVariableNamesToSkipForProperties.Add("Parent");
            mVariableNamesToSkipForProperties.Add("Guide");


            mVariableNamesToSkipForProperties.Add("X Origin");
            mVariableNamesToSkipForProperties.Add("X Units");
            mVariableNamesToSkipForProperties.Add("Y Origin");
            mVariableNamesToSkipForProperties.Add("Y Units");

            mVariableNamesToSkipForProperties.Add("FlipHorizontal");



            mVariableNamesToSkipForProperties.Add("X");
            mVariableNamesToSkipForProperties.Add("Y");
            mVariableNamesToSkipForProperties.Add("Width");
            mVariableNamesToSkipForProperties.Add("Height");
            mVariableNamesToSkipForProperties.Add("Visible");


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

            // It turns out we dont' have a way to skip/include properties by version. To be safe we are going to exclude these for now. In the future we need to have version-based
            // include/exclude just like the StateCodeGenerator:
            ExcludeIfVersionLessThan("CustomFrameTextureCoordinateWidth", GluxVersions.GumUsesSystemTypes);
            ExcludeIfVersionLessThan("LineHeightMultiplier", GluxVersions.GumUsesSystemTypes);

            // Do not generate AutoGrid properties - they are already handled by GraphicalUiElement
            mVariableNamesToSkipForProperties.Add("AutoGridHorizontalCells");
            mVariableNamesToSkipForProperties.Add("AutoGridVerticalCells");

            // always ignore this because it's handled by GraphicalUiElement:
            mVariableNamesToSkipForProperties.Add("IgnoredByParentSize");
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
            ICodeBlock classBodyBlock = codeBlock.Class("public partial", runtimeClassName, " : global::Gum.Wireframe.GraphicalUiElement");

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
            SpriteCodeGenerator.Self.GenerateAdditionalMethods(standardElementSave, classBodyBlock);
            TextCodeGenerator.Self.GenerateAdditionalMethods(standardElementSave, classBodyBlock);
        }

        private void GenerateGenericContainerCode(ICodeBlock codeBlock)
        {
            codeBlock.Line(@"
    public class ContainerRuntime<T> : ContainerRuntime where T : Gum.Wireframe.GraphicalUiElement, new()
    {
        public int ItemCount
        {
            get => base.Children.Count;
            set
            {
                while(base.Children.Count < value)
                {
                    AddChild();
                }
                while(base.Children.Count > value)
                {
                    RemoveChild(base.Children.Last() as T);
                }
            }
        }

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

            TextCodeGenerator.Self.GenerateVariableProperties(standardElementSave, currentBlock, containedGraphicalObjectName);

            // Sprites handle this in GenerateAdditionalMethods
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

        public void GenerateVariable(ICodeBlock currentBlock, string containedGraphicalObjectName, Gum.DataTypes.Variables.VariableSave variable, ElementSave elementSave, 
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
                GenerateSetter(containedGraphicalObjectName, variable, propertyCodeBlock, whatToGetOrSet, elementSave);
            }
        }

        private void GenerateSetter(string propertyName, VariableSave variable, ICodeBlock property, string whatToGetOrSet, ElementSave elementSave)
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

                    if(GlueState.Self.CurrentGlueProject?.FileVersion >= (int)GlueProjectSave.GluxVersions.GraphicalUiElementINotifyPropertyChanged)
                    {
                        // notify it changing...
                        setter.Line("NotifyPropertyChanged();");
                    }
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
                    var version = GlueState.Self.CurrentGlueProject.FileVersion;
                    if(version >= (int)GluxVersions.GumUsesSystemTypes || GlueState.Self.CurrentMainProject.IsFrbSourceLinked())
                    {
                        //setter.Line($"var color = {containedObject}.Color;");
                        setter.Line("// The new version of Glue is moving away from XNA color values. This code converts color values. If this doesn't run, you need to upgrade your GLUX version.");
                        setter.Line("// More info here: https://flatredball.com/documentation/tools/glue-reference/glujglux/");

                        setter.Line($"var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.With{variable.Name}({containedObject}.Color, (byte)value);");
                        setter.Line($"{containedObject}.Color = color;");
                    }
                    else
                    {
                        setter.Line($"var color = {containedObject}.Color;");
                        setter.Line($"color.{colorComponent} = (byte)value;");
                        setter.Line($"{containedObject}.Color = color;");
                    }
                    return true;
                }
            }

            return false;
        }

        private void GenerateGetter(string containedGraphicalObjectName, VariableSave variable, 
            ICodeBlock property, string variableName, string whatToGetOrSet, ElementSave elementSave)
        {
            var getter = property.Get();

            bool wasHandled = TryHandleCustomGetter(variable, elementSave, getter);

            if (!wasHandled)
            {

                if (mStandardGetterReplacements.ContainsKey(variableName))
                {
                    mStandardGetterReplacements[variableName](getter);
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
            else if (variableSave.Type == "Microsoft.Xna.Framework.Color")
            {
                if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.GumUsesSystemTypes || GlueState.Self.CurrentMainProject.IsFrbSourceLinked())
                {
                    value = $"RenderingLibrary.Graphics.XNAExtensions.ToXNA({value})";
                }
            }
            return value;
        }

        string AdjustStandardElementVariableSetIfNecessary(VariableSave variableSave, string value)
        {
            if (variableSave.Type == "Blend")
            {
                value = "Gum.RenderingLibrary.BlendExtensions.ToBlendState(" + value + ")";
            }
            else if(variableSave.Type == "Microsoft.Xna.Framework.Color")
            {
                if (GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.GumUsesSystemTypes || GlueState.Self.CurrentMainProject.IsFrbSourceLinked())
                {
                    value = $"RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing({value})";
                }
            }


            return value;
        }
    }
}
