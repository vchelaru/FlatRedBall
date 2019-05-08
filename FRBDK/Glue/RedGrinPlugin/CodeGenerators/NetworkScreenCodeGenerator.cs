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

        public static void GenerateCodeFor(ScreenSave screenSave, bool save = false)
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

                if(save)
                {
                    GlueCommands.Self.ProjectCommands.SaveProjects();
                }
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

            GenerateSendClaimEntity(codeBlock);

            GenerateReceiveClaimEntity(codeBlock, screenSave);

            return topBlock.ToString();

        }

        private static void GenerateSendClaimEntity(ICodeBlock codeBlock)
        {
            var function = codeBlock.Function("private void", "Claim", "RedGrin.INetworkEntity entity");

            var ifBlock = function.If("RedGrin.NetworkManager.Self.Role == RedGrin.NetworkRole.Server");

            ifBlock.Line("entity.OwnerId = RedGrin.NetworkManager.Self.NetworkId;");
            ifBlock.Line($"var claim = new {GlueState.Self.ProjectNamespace}.Messages.ClaimEntity();");
            ifBlock.Line($"entity.OwnerId = RedGrin.NetworkManager.Self.NetworkId;");
            ifBlock.Line("claim.EntityName = ((FlatRedBall.Utilities.INameable)entity).Name;");
            ifBlock.Line("RedGrin.NetworkManager.Self.AddToEntities(entity);");
            ifBlock.Line("claim.EntityId = entity.EntityId;");
            ifBlock.Line("RedGrin.NetworkManager.Self.RequestGenericMessage(claim);");

            var elseBlock = function.Else();

            elseBlock.Line("RedGrin.NetworkManager.Self.AddToEntities(entity);");
        }

        private static void GenerateReceiveClaimEntity(ICodeBlock codeBlock, ScreenSave screen)
        {
            var function = codeBlock.Function("private void", "HandleClaimEntity",
                $"{GlueState.Self.ProjectNamespace}.Messages.ClaimEntity claim");

            var switchBlock = function.Switch("claim.EntityName");
            foreach(var instance in screen.AllNamedObjects)
            {
                var entity = instance.GetReferencedElement() as EntitySave;

                if(entity != null && NetworkEntityViewModel.IsNetworked(entity))
                {
                    var caseBlock = switchBlock.Case($"\"{instance.FieldName}\"");

                    caseBlock.Line($"{instance.FieldName}.OwnerId = claim.OwnerId;");
                    caseBlock.Line($"{instance.FieldName}.EntityId = claim.EntityId;");
                    // break is automatically added
                }
            }
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

                // Even though the NetworkManager assigns the owner ID, we're going to do it here
                // to before calling UpdateFromState, so that any custom code that gets triggered from
                // UpdateFromState are guaranteed to have the right ID:

                ifBlock.Line("entity.OwnerId = ownerId;");

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
