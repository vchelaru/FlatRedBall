using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.Content.Aseprite;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OfficialPlugins.AnimationChainPlugin.Errors
{
    internal class AnimationChainErrorReporter : ErrorReporterBase
    {
        public override ErrorViewModel[] GetAllErrors()
        {
            var errors = new List<ErrorViewModel>();

            var project = GlueState.Self.CurrentGlueProject;

            void AddBadReferencesFrom(GlueElement glueElement)
            {
                foreach(var namedObject in glueElement.AllNamedObjects)
                {
                    foreach(var instruction in namedObject.InstructionSaves)
                    {
                        // is it an animation reference?
                        // This is the VariableDefinition from Glue:
                        // Name=AnimationChains, Category=Animation, Type=AnimationChainList
                        // Name=CurrentChainName, Category=Animation, Type=string

                        if(instruction.Member == "CurrentChainName" && !string.IsNullOrEmpty(instruction.Value as string))
                        {
                            var animationChainName = instruction.Value as string;
                            // let's find the .achx:
                            var animationVariable = namedObject.InstructionSaves.FirstOrDefault(item => item.Member == "AnimationChains");
                            if(animationVariable != null)
                            {
                                var animationFileName = animationVariable.Value as string;
                                var rfs = glueElement.GetAllReferencedFileSavesRecursively()
                                    .FirstOrDefault(item => item.GetInstanceName() == animationFileName);

                                if(rfs != null)
                                {
                                    var filePath = GlueCommands.Self.GetAbsoluteFilePath(rfs);

                                    if(filePath.Exists())
                                    {
                                        AnimationChainListSave achSave = null;

                                        if(filePath.Extension == "aseprite")
                                        {
                                            achSave = AsepriteAnimationChainLoader.ToAnimationChainListSave(filePath);
                                        }
                                        else
                                        {
                                            achSave = AnimationChainListSave.FromFile(filePath.FullPath);
                                        }

                                        if(achSave.AnimationChains.Any(item => item.Name == animationChainName) == false)
                                        {
                                            var error = new AnimationReferenceErrorViewModel(
                                                filePath.FullPath, namedObject, animationChainName);

                                            errors.Add(error);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            foreach(var screen in project.Screens)
            {
                AddBadReferencesFrom(screen);
            }
            foreach(var entity in project.Entities)
            {
                AddBadReferencesFrom(entity);
            }

            return errors.ToArray();
        }
    }
}
