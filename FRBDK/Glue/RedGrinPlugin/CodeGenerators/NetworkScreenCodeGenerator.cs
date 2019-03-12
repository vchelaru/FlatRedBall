using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using RedGrinPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedGrinPlugin.CodeGenerators
{
    class NetworkScreenCodeGenerator
    {
        public static void GenerateAllNetworkScreenCode()
        {
            var screensToRegenerate = GlueState.Self.CurrentGlueProject.Screens
                .Where(item => NetworkScreenViewModel.IsNetworked(item))
                .ToArray();

            foreach (var screen in screensToRegenerate)
            {
                NetworkScreenCodeGenerator.GenerateCodeFor(screen);
            }
        }

        public static void GenerateCodeFor(ScreenSave screenSave)
        {
            var isNetworkScreen = NetworkScreenViewModel.IsNetworked(screenSave);

            if(isNetworkScreen)
            {
                var screenGeneratedCode = GetGeneratedScreenCode(screenSave);
                var generatedScreenNetworkFilePath = CodeGeneratorCommonLogic.GetGeneratedElementNetworkFilePathFor(screenSave);

                CodeGeneratorCommonLogic.SaveFile(screenGeneratedCode, generatedScreenNetworkFilePath);
                CodeGeneratorCommonLogic.AddCodeFileToProject(generatedScreenNetworkFilePath);

                var customScreenNetworkFilePath = CodeGeneratorCommonLogic.GetCustomElementNetworkFilePathFor(screenSave);
                if(customScreenNetworkFilePath.Exists() == false)
                {
                    var customScreenNetworkCode = GenerateEmptyCustomScreenNetworkCode(screenSave);
                    CodeGeneratorCommonLogic.SaveFile(customScreenNetworkCode, customScreenNetworkFilePath);
                }
                CodeGeneratorCommonLogic.AddCodeFileToProject(customScreenNetworkFilePath);


                GlueCommands.Self.ProjectCommands.MakeGeneratedCodeItemsNested();
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }
        }

        #region Generated code methods

        private static string GetGeneratedScreenCode(ScreenSave screenSave)
        {
            ICodeBlock topBlock = new CodeBlockBaseNoIndent(null);

            string screenNamespace = CodeGeneratorCommonLogic.GetElementNamespace(screenSave);

            ICodeBlock codeBlock = topBlock.Namespace(screenNamespace);

            codeBlock = codeBlock.Class("public partial", screenSave.GetStrippedName(), " : RedGrin.INetworkArena");

            GenerateRequestCreateEntity(codeBlock);

            GenerateRequestDestroy(codeBlock);



            return topBlock.ToString();

        }

        private static void GenerateRequestDestroy(ICodeBlock codeBlock)
        {
            var requestDestroyMethod = codeBlock.Function(
                "public void", "RequestDestroyEntity", "RedGrin.INetworkEntity entity");
            requestDestroyMethod.Line(
                "(entity as FlatRedBall.Graphics.IDestroyable).Destroy();");
            requestDestroyMethod.Line(
                "CustomRequestDestroyNetworkEntity(entity);");
        }

        private static void GenerateRequestCreateEntity(ICodeBlock codeBlock)
        {
            var requestCreateMethod = codeBlock.Function(
                "public RedGrin.INetworkEntity", "RequestCreateEntity", "long ownerId, object entityData");

            requestCreateMethod.Line("RedGrin.INetworkEntity entity = null;");

            bool needsElseIf = false;

            var netEntities = GlueState.Self.CurrentGlueProject.Entities
                .Where(item => NetworkEntityViewModel.IsNetworked(item) );

            foreach (var entitySave in netEntities)
            {
                ICodeBlock ifBlock;

                var fullNetStateType = CodeGeneratorCommonLogic.GetNetStateFullName(entitySave);
                var fullEntityType = CodeGeneratorCommonLogic.GetElementFullName(entitySave);
                string ifcontents = $"entityData is {fullNetStateType}";

                if (needsElseIf == false)
                {
                    ifBlock = requestCreateMethod.If(ifcontents);
                }
                else
                {
                    ifBlock = requestCreateMethod.ElseIf(ifcontents);
                }

                var hasFactory = entitySave.CreatedByOtherEntities;

                if(hasFactory)
                {
                    var factoryName = $"{GlueState.Self.ProjectNamespace}.Factories.{entitySave.GetStrippedName()}Factory";
                    ifBlock.Line($"entity = {factoryName}.CreateNew();");
                }
                else
                {
                    ifBlock.Line($"entity = new {fullEntityType}();");
                }

                ifBlock.Line("entity.UpdateFromState(entityData, 0);");

                needsElseIf = true;
            }

            // At first I thought to have the CustomRequestCreateNetworkEntity
            // inside the if/else if so that the created entity could be modified
            // but it's possible the user may want to have their own totally custom
            // network entities and network entity states, in which case they will need
            // to instantiate the object fully in custom code. Therefore, we'll call the
            // method no matter what, even if the entity is null
            requestCreateMethod.Line("CustomRequestCreateNetworkEntity(ref entity, entityData);");

            requestCreateMethod.Line("return entity;");
        }

        #endregion

        private static string GenerateEmptyCustomScreenNetworkCode(ScreenSave screen)
        {
            ICodeBlock topBlock = new CodeBlockBaseNoIndent(null);

            string screenNamespace = CodeGeneratorCommonLogic.GetElementNamespace(screen);

            ICodeBlock codeBlock = topBlock.Namespace(screenNamespace);

            codeBlock = codeBlock.Class("public partial", screen.GetStrippedName());

            codeBlock.Function("void",
                "CustomRequestCreateNetworkEntity",
                "ref RedGrin.INetworkEntity entity, object entityData")
                .Line() ;

            codeBlock.Line();

            codeBlock.Function("void",
                "CustomRequestDestroyNetworkEntity",
                "RedGrin.INetworkEntity entity")
                .Line();

            return topBlock.ToString();
        }
    }
}
