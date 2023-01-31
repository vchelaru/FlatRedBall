using FlatRedBall.Glue;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ElementInheritanceTypePlugin.CodeGenerators
{
    internal class ElementInheritanceTypeCodeGenerator : ElementComponentCodeGenerator
    {
        public override void GenerateAdditionalClasses(ICodeBlock codeBlock, IElement element)
        {
            var derivedElements = ObjectFinder.Self.GetAllDerivedElementsRecursive(element as GlueElement);

            var shouldGenerate = derivedElements.Count > 0 &&
                GlueState.Self.CurrentGlueProject.SuppressBaseTypeGeneration == false;


            /////////////////////Early Out////////////////////////////////
            if (!shouldGenerate)
            {
                return;
            }
            ///////////////////End Early Out//////////////////////////////

            var classBlock = codeBlock.Class("public partial", $"{element.ClassName}Type");

            classBlock.Line("public string Name { get; set; }");
            classBlock.Line("public override string ToString() {return Name; }");

            classBlock.Line("public Type Type { get; set; }");
            classBlock.Line("public Performance.IEntityFactory Factory { get; set; }");
            classBlock.Line("public Func<string, object> GetFile {get; private set; }");
            classBlock.Line("public Action<string> LoadStaticContent { get; private set; }");


            classBlock.Line($"public {element.ClassName} CreateNew(Microsoft.Xna.Framework.Vector3 position) => Factory.CreateNew(position) as {element.ClassName};");
            classBlock.Line($"public {element.ClassName} CreateNew(float x = 0, float y = 0) => Factory.CreateNew(x, y) as {element.ClassName};");

            CreateVariableFields(element, classBlock);

            CreateFromNameMethod(element, derivedElements, classBlock);

            foreach (var derivedElement in derivedElements)
            {
                classBlock.Line($"public static {element.ClassName}Type {derivedElement.ClassName} {{ get; private set; }} = new {element.ClassName}Type");
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

                foreach (var variable in element.CustomVariables.Where(item => item.SetByDerived && !item.IsShared))
                {
                    if (!ShouldSkip(variable))
                    {
                        var matchingVariable = derivedElement.CustomVariables.FirstOrDefault(item => item.Name == variable.Name) ?? variable;
                        if (matchingVariable.DefaultValue != null)
                        {
                            // If it's null, just use whatever is defined on the base
                            var rightSide = CustomVariableCodeGenerator.GetRightSideOfEquals(matchingVariable, derivedElement);

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

            classBlock.Line($"public static List<{element.ClassName}Type> All = new List<{element.ClassName}Type>{{");
            var innerList = classBlock.CodeBlockIndented();
            foreach (var derivedElement in derivedElements)
            {
                innerList.Line(derivedElement.ClassName + ",");
            }
            classBlock.Line($"}};");


        }

        private static void CreateFromNameMethod(IElement element, List<GlueElement> derivedElements, ICodeBlock classBlock)
        {
            var fromName = classBlock.Function($"public static {element.ClassName}Type", "FromName", "string name");
            var switchBlock = fromName.Switch("name");
            foreach (var derivedElement in derivedElements)
            {
                switchBlock.CaseNoBreak($"\"{derivedElement.ClassName}\"")
                    .Line($"return {derivedElement.ClassName};");
            }
            fromName.Line("return null;");
        }

        private void CreateVariableFields(IElement element, ICodeBlock classBlock)
        {
            CustomVariable tempVariable = new CustomVariable();
            foreach (var variable in element.CustomVariables.Where(item => item.SetByDerived))
            {
                if (!ShouldSkip(variable))
                {
                    // We want this to not be a property, and to not have any source class type so that it generates
                    // as a simple field
                    tempVariable.Name = variable.Name;
                    tempVariable.Type = variable.Type;
                    tempVariable.DefaultValue = variable.DefaultValue;
                    tempVariable.OverridingPropertyType = variable.OverridingPropertyType;

                    CustomVariableCodeGenerator.AppendCodeForMember(element as GlueElement, classBlock, tempVariable, forceGenerateExposed:true);
                }
            }
        }

        bool ShouldSkip(CustomVariable customVariable)
        {
            switch(customVariable.Type)
            {
                // Don't want to handle variables of certain types so we don't get unintentional loads:
                case "FlatRedBall.TileGraphics.LayeredTileMap":
                    return true;
            }
            return false;
        }

        string QualifiedTypeName(GlueElement element)
        {
            return ProjectManager.ProjectNamespace + '.' + element.Name.Replace('\\', '.');
        }
    }
}
