using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.Content.Aseprite;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.AnimationChainPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OfficialPlugins.AnimationChainPlugin.Managers
{
    internal static class NamedObjectVariableChangeLogic
    {
        public static void HandleNamedObjectChangedValue(string changedMember, object oldValue, NamedObjectSave namedObject)
        {
            if (namedObject?.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.Sprite && changedMember == nameof(FlatRedBall.Sprite.AnimationChains))
            {
                var animationChains = namedObject.GetCustomVariable(nameof(FlatRedBall.Sprite.AnimationChains))?.Value as string;
                var elementOwner = ObjectFinder.Self.GetElementContaining(namedObject);
                var rfs = elementOwner?.GetReferencedFileSaveByInstanceNameRecursively(animationChains);
                var achxFullFile = rfs != null 
                    ? GlueCommands.Self.GetAbsoluteFilePath(rfs)
                    : null;

                var currentChainName = namedObject.GetCustomVariable(nameof(FlatRedBall.Sprite.CurrentChainName))?.Value as string;
                string firstAnimationName = null;
                if(string.IsNullOrEmpty(currentChainName) && achxFullFile?.Exists() == true)
                {
                    AnimationChainListSave achx = null;
                    var extension = achxFullFile.Extension;
                    try
                    {
                        if (extension == "aseprite")
                        {
                            achx = AsepriteAnimationChainLoader.ToAnimationChainListSave(achxFullFile.FullPath);
                        }
                        else
                        {
                            achx = AnimationChainListSave.FromFile(achxFullFile.FullPath);
                        }
                    }
                    catch(Exception e)
                    {
                        // The file could be corrupted. We don't want to have the entire plugin crash, so we catch
                        // the exception and output an error:
                        GlueCommands.Self.PrintError($"Error loading AnimationChain file {achxFullFile}:\n{e}");
                    }
                    firstAnimationName = achx?.AnimationChains.FirstOrDefault()?.Name;
                }

                if (firstAnimationName != null)
                {
                    // We're inside a plugin (and maybe task?)
                    //TaskManager.Self.Add
                    TaskManager.Self.Add(
                        () => GlueCommands.Self.GluxCommands.SetVariableOnAsync(namedObject, nameof(FlatRedBall.Sprite.CurrentChainName), firstAnimationName),
                        "Set AnimationChain name");
                }
            }

            if (changedMember == "CurrentChainName" || changedMember == "AnimationChains")
            {
                MainAnimationChainPlugin.Self.RefreshErrors();
            }
        }
    }
}
