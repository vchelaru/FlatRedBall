using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedGrinPlugin.CodeGenerators
{
    public static class CodeGeneratorCommonLogic
    {
        public static void SaveFile(string code, FilePath filePath)
        {
            System.IO.Directory.CreateDirectory(filePath.GetDirectoryContainingThis().FullPath);
            GlueCommands.Self.TryMultipleTimes(() => System.IO.File.WriteAllText(filePath.FullPath, code));
        }

        public static void AddCodeFileToProject(FilePath filePath)
        {
            GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(filePath);
        }

        public static void RemoveCodeFileFromProject(FilePath filePath)
        {
            GlueCommands.Self.ProjectCommands.RemoveFromProjects(filePath);
        }

        public static string GetElementNamespace(IElement element)
        {
            return GlueState.Self.ProjectNamespace +
                "." + element.Name.Replace("/", ".").Replace("\\", ".").Substring(
                0, element.Name.Length - (element.ClassName.Length + 1));
        }
        public static string GetElementFullName(IElement element)
        {
            return GetElementNamespace(element) + "." + element.GetStrippedName();
        }

        public static string GetNetStateNamespace(EntitySave entitySave)
        {
            string entityNamespace =
                entitySave.Name.Replace("/", ".").Replace("\\", ".").Substring(
                0, entitySave.Name.Length - (entitySave.ClassName.Length + 1));

            string removedEntities = entityNamespace.Substring("Entities".Length);

            if(removedEntities.StartsWith("."))
            {
                removedEntities = removedEntities.Substring(1);
            }

            if (string.IsNullOrEmpty(removedEntities))
            {
                entityNamespace = "NetStates";
            }
            else
            {
                entityNamespace = removedEntities + ".NetStates";
            }

            entityNamespace = "." + entityNamespace;
            entityNamespace = GlueState.Self.ProjectNamespace + entityNamespace;
            return entityNamespace;
        }
        public static string GetNetStateFullName(EntitySave entitySave)
        {
            var netStateFullName = $"{GetNetStateNamespace(entitySave)}.{entitySave.GetStrippedName()}NetState";

            return netStateFullName;
        }

        public static FilePath GetGeneratedElementNetworkFilePathFor(IElement element)
        {
            return GlueState.Self.CurrentGlueProjectDirectory +
                element.Name +
                ".Generated.Network.cs";
        }

        public static FilePath GetCustomElementNetworkFilePathFor(IElement element)
        {
            return GlueState.Self.CurrentGlueProjectDirectory +
                element.Name +
                ".Network.cs";
        }

        public static FilePath GetGeneratedNetStateFilePathFor(EntitySave entitySave)
        {
            return GlueState.Self.CurrentGlueProjectDirectory +
                entitySave.Name +
                "NetState.Generated.cs";
        }

        public static FilePath GetCustomNetStateFilePathFor(EntitySave entitySave)
        {
            return GlueState.Self.CurrentGlueProjectDirectory +
                entitySave.Name +
                "NetState.cs";
        }

        public static IEnumerable<FilePath> GetAllNetworkFilesFor(EntitySave entitySave)
        {
            yield return CodeGeneratorCommonLogic.GetGeneratedElementNetworkFilePathFor(entitySave);
            yield return CodeGeneratorCommonLogic.GetCustomElementNetworkFilePathFor(entitySave);
            yield return CodeGeneratorCommonLogic.GetGeneratedNetStateFilePathFor(entitySave);
            yield return CodeGeneratorCommonLogic.GetCustomNetStateFilePathFor(entitySave);
        }
    }
}
