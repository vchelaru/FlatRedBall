using FlatRedBall.Glue;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;

namespace OfficialPlugins.ElementInheritanceTypePlugin.CodeGenerators
{
    internal class ElementVariantCodeGenerator : ElementComponentCodeGenerator
    {
        public override void GenerateAdditionalClasses(ICodeBlock codeBlock, IElement element)
        {
            var derivedElements = ObjectFinder.Self.GetAllDerivedElementsRecursive(element as GlueElement);

            var glueProject = GlueState.Self.CurrentGlueProject;

            var shouldGenerate = derivedElements.Count > 0 &&
                glueProject.SuppressBaseTypeGeneration == false;


            /////////////////////Early Out////////////////////////////////
            if (!shouldGenerate)
            {
                return;
            }
            ///////////////////End Early Out//////////////////////////////

            string typeOrVariant = glueProject.FileVersion >= (int)GluxVersions.VariantsInsteadOfTypes
                ? "Variant"
                : "Type";

            var classBlock = codeBlock.Class("public partial", $"{element.ClassName}{typeOrVariant}");

            classBlock.Line("public string Name { get; set; }");
            classBlock.Line("public override string ToString() {return Name; }");

            classBlock.Line("public Type Type { get; set; }");
            classBlock.Line("public Performance.IEntityFactory Factory { get; set; }");
            classBlock.Line("public Func<string, object> GetFile {get; private set; }");
            classBlock.Line("public Action<string> LoadStaticContent { get; private set; }");

            FillCreateNew("Microsoft.Xna.Framework.Vector3 position", "position.X, position.Y", element, classBlock);
            FillCreateNew("float x = 0, float y = 0", "x, y", element, classBlock);
           
            var glueElement = (GlueElement)element;

            CreateVariableFields(element, classBlock);

            CreateFromNameMethod(glueElement, derivedElements, classBlock);

            foreach (var derivedElement in derivedElements)
            {
                CreateDerivedVariantClass(element, typeOrVariant, classBlock, derivedElement);
            }

            // The base class itself may need a variant so the user can access variables on it:
            CreateDerivedVariantClass(element, typeOrVariant, classBlock, element as GlueElement);

            classBlock.Line($"public static List<{element.ClassName}{typeOrVariant}> All = new List<{element.ClassName}{typeOrVariant}>{{");
            var innerList = classBlock.CodeBlockIndented();
            foreach (var derivedElement in derivedElements)
            {
                innerList.Line(derivedElement.ClassName + ",");
            }
            classBlock.Line($"}};");
        }

        private void CreateDerivedVariantClass(IElement element, string typeOrVariant, ICodeBlock classBlock, GlueElement derivedElement)
        {
            classBlock.Line($"public static {element.ClassName}{typeOrVariant} {derivedElement.ClassName} {{ get; private set; }} = new {element.ClassName}{typeOrVariant}");
            var block = classBlock.Block();
            block.Line($"Name = \"{derivedElement.ClassName}\",");
            block.Line($"Type = typeof({derivedElement.Name.Replace("/", ".").Replace("\\", ".")}),");
            var hasFactory = derivedElement is EntitySave derivedEntity && derivedEntity.CreatedByOtherEntities;
            if (hasFactory)
            {
                block.Line($"Factory = Factories.{derivedElement.ClassName}Factory.Self,");
            }
            block.Line($"GetFile = {QualifiedTypeName(derivedElement)}.GetFile,");
            block.Line($"LoadStaticContent = {QualifiedTypeName(derivedElement)}.LoadStaticContent,");

            foreach (var variable in element.CustomVariables
                //.Where(item => item.SetByDerived
            // Why don't we generate is shared? Now that we can inherit shared variables, let's do it so that the user can specify variables for the type, like a Display Name
            //&& !item.IsShared
            )
            {
                if (!ShouldSkipVariantField(variable))
                {
                    var matchingVariable = derivedElement.CustomVariables.FirstOrDefault(item => item.Name == variable.Name) ?? variable;
                    if (matchingVariable.DefaultValue != null)
                    {
                        // If it's null, just use whatever is defined on the base
                        var rightSide = CustomVariableCodeGenerator.GetRightSideOfEquals(matchingVariable, derivedElement);
                        if(IsTypeFileType(matchingVariable.Type) && !string.IsNullOrEmpty(rightSide))
                        {
                            // put quotes around it:
                            rightSide = $"\"{rightSide}\"";
                        }

                        // It seems like the variable.DefaultValue can be String.Empty
                        // If so, it bypasses the != null check, but still produces an empty
                        // right-slide assignment, so let's just put that here
                        if (!string.IsNullOrEmpty(rightSide))
                        {
                            block.Line($"{variable.Name} = {rightSide},");
                        }
                    }
                }
            }

            classBlock.Line(";");
        }

        private static void FillCreateNew(string methodHeaderParameters, string innerCallParameters, IElement element, ICodeBlock codeBlock)
        {
            codeBlock = codeBlock.Function($"public {element.ClassName}", "CreateNew", methodHeaderParameters);

            var ifBlock = codeBlock.If("this.Factory != null");
            {
                ifBlock.Line($"var toReturn = Factory.CreateNew({innerCallParameters}) as {element.ClassName};");
                ifBlock.Line("return toReturn;");
            }
            var elseBlock = ifBlock.End().Else();
            {
                var hasFactory = element is EntitySave asEntity && asEntity.IsAbstract == false && asEntity.CreatedByOtherEntities;
                if (hasFactory)
                {
                    elseBlock.Line($"var toReturn = Factories.{element.ClassName}Factory.CreateNew({innerCallParameters});");

                    foreach(var variable in element.CustomVariables)
                    {
                        var shouldGenerateVariable = 
                            !ShouldSkipVariantField(variable) &&
                            variable.IsShared == false &&
                            variable.Name != "X" && 
                            variable.Name != "Y" && 
                            variable.Name != "Z";

                        // don't assign position values - those aget set in the Create New method:
                        if(shouldGenerateVariable)
                        {
                            var rightSide = $"this.{variable.Name}";
                            if (IsTypeFileType(variable.Type))
                            {
                                var type = CustomVariableCodeGenerator.GetMemberTypeFor(variable, element);
                                rightSide = $"{element.ClassName}.GetFile(this.{variable.Name}) as {type}";
                            }


                            elseBlock.Line($"toReturn.{variable.Name} = {rightSide};");
                        }
                    }

                    elseBlock.Line("return toReturn;");
                }
                else
                {
                    elseBlock.Line("return null;");
                }

            }
        }


        private static void CreateFromNameMethod(GlueElement element, List<GlueElement> derivedElements, ICodeBlock classBlock)
        {
            var glueProject = GlueState.Self.CurrentGlueProject;

            string typeOrVariant = glueProject.FileVersion >= (int)GluxVersions.VariantsInsteadOfTypes
                ? "Variant"
                : "Type";
            var fromName = classBlock.Function($"public static {element.ClassName}{typeOrVariant}", "FromName", "string name");
            var switchBlock = fromName.Switch("name");
            foreach (var derivedElement in derivedElements)
            {
                // need to support both fully qualified and unqualified so let's add a line:
                
                var fullyQualified = $"{CodeWriter.GetGlueElementNamespace(element)}.{derivedElement.ClassName}";
                switchBlock.Line($"case \"{fullyQualified}\":");
                switchBlock.CaseNoBreak($"\"{derivedElement.ClassName}\"")
                    .Line($"return {derivedElement.ClassName};");
            }

            // In case the user wants to access variables on the base type:
            {
                var fullyQualified = $"{CodeWriter.GetGlueElementNamespace(element)}.{element.ClassName}";
                switchBlock.Line($"case \"{fullyQualified}\":");
                switchBlock.CaseNoBreak($"\"{element.ClassName}\"")
                    .Line($"return {element.ClassName};");
            }


            fromName.Line("return null;");
        }

        private void CreateVariableFields(IElement element, ICodeBlock classBlock)
        {
            CustomVariable tempVariable = new CustomVariable();
            foreach (var variable in element.CustomVariables.Where(item => item.SetByDerived))
            {
                if (!ShouldSkipVariantField(variable))
                {
                    // We want this to not be a property, and to not have any source class type so that it generates
                    // as a simple field
                    // In other words, we intentionally do NOT copy over the source object and source object property:
                    //tempVariable.SourceObject = variable.SourceObject;
                    //tempVariable.SourceObjectProperty = variable.SourceObjectProperty;
                    tempVariable.Name = variable.Name;

                    if(IsTypeFileType(variable.Type))
                    {
                        tempVariable.Type = "string";
                    }
                    else
                    {
                        // But since we aren't copying over the objects, we may need to explicitly
                        // set the type if it does have a source object and source object property
                        if(!string.IsNullOrEmpty(variable.SourceObject) && !string.IsNullOrEmpty(variable.SourceObjectProperty))
                        {
                            tempVariable.Type = CustomVariableCodeGenerator.GetMemberTypeFor(variable, element);
                        }
                        else
                        {
                            tempVariable.Type = variable.Type;
                        }
                    }
                    tempVariable.DefaultValue = variable.DefaultValue;
                    tempVariable.OverridingPropertyType = variable.OverridingPropertyType;

                    CustomVariableCodeGenerator.AppendCodeForMember(element as GlueElement, classBlock, tempVariable, forceGenerateExposed:true);
                }
            }
        }

        private static bool IsTypeFileType(string type)
        {
            return type == 
                // todo - add more here...
                "AnimationChainList"; 
        }

        static bool ShouldSkipVariantField(CustomVariable customVariable)
        {
            if(customVariable.SetByDerived == false)
            {
                return true;
            }
            var type = customVariable.Type;
            switch (type)
            {
                // Don't want to handle variables of certain types so we don't get unintentional loads:
                case "FlatRedBall.TileGraphics.LayeredTileMap":
                    return true;

            }

            if(type.EndsWith("DataTypes.TopDownValues"))
            {
                // This requires the content to have been loaded as well. We'll skip this for now, and come back to it later...
                return true;
            }

            return false;
        }

        string QualifiedTypeName(GlueElement element)
        {
            return "global::" + ProjectManager.ProjectNamespace + '.' + element.Name.Replace('\\', '.');
        }
    }
}
