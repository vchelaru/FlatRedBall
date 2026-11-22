using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.Content.Aseprite;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OfficialPlugins.AnimationChainPlugin.Errors
{
    internal class AnimationReferenceErrorViewModel : ErrorViewModel
    {
        NamedObjectSave namedObject;
        GlueElement owner;
        public FilePath AchxFilePath { get; }
        public string ObjectName { get; }
        public string AnimationName { get; }

        public override string UniqueId => Details;

        public AnimationReferenceErrorViewModel(FilePath filePath, NamedObjectSave namedObject, string animation)
        {
            this.namedObject = namedObject;
            owner = ObjectFinder.Self.GetElementContaining(namedObject);
            AchxFilePath = filePath;
            ObjectName = namedObject.InstanceName;
            AnimationName = animation;

            // Use namedObject rather than ObjectName so the container is listed in case the user doesn't know to
            // double-click the view model
            Details = $"{namedObject} references animation {AnimationName} which is missing from {AchxFilePath}";
        }

        /// <summary>
        /// Reads an animation file. Tests replace it to count reads.
        /// </summary>
        internal static Func<FilePath, AnimationChainListSave> ReadAnimationFile = achxFilePath =>
            achxFilePath.Extension == "aseprite"
                ? AsepriteAnimationChainLoader.ToAnimationChainListSave(achxFilePath)
                : AnimationChainListSave.FromFile(achxFilePath.FullPath);

        /// <param name="animationNamesByFile">Animation names of each file already read in this scan, null for a
        /// file that couldn't be parsed. A file many objects reference is then parsed once per scan rather than
        /// once per object. Null reads the file every call.</param>
        public static bool GetIfHasError(GlueElement owner, NamedObjectSave namedObject, out FilePath filePath, out string animationName,
            Dictionary<string, HashSet<string>> animationNamesByFile = null)
        {
            filePath = null;
            animationName = null;
            foreach (var instruction in namedObject.InstructionSaves)
            {
                if (instruction.Member == "CurrentChainName" && !string.IsNullOrEmpty(instruction.Value as string))
                {
                    animationName = instruction.Value as string;
                    if (!string.IsNullOrEmpty(animationName))
                    {
                        var animationVariable = namedObject.InstructionSaves.FirstOrDefault(item => item.Member == "AnimationChains");
                        if (animationVariable != null)
                        {
                            var animationFileName = animationVariable.Value as string;
                            var rfs = owner.GetAllReferencedFileSavesRecursively()
                                .FirstOrDefault(item => item.GetInstanceName() == animationFileName);

                            if (rfs != null)
                            {
                                filePath = GlueCommands.Self.GetAbsoluteFilePath(rfs);

                                return GetIfHasError(owner, namedObject, filePath, animationName, animationNamesByFile);
                            }
                        }
                    }
                }
            }
            return false;
        }

        public static bool GetIfHasError(GlueElement owner, NamedObjectSave namedObject, FilePath achxFilePath, string animationName,
            Dictionary<string, HashSet<string>> animationNamesByFile = null)
        {
            // todo - need to handle a situation where the user changes which .achx file is referenced by the nos
            // Not doing that yet because it's more work and it can be added later

            // This is fixed if:
            // The element no longer exists:
            var project = GlueState.Self.CurrentGlueProject;
            if (owner is ScreenSave screenSave && project.Screens.Contains(screenSave) == false)
            {
                return true;
            }
            if (owner is EntitySave entitySave && project.Entities.Contains(entitySave) == false)
            {
                return true;
            }

            // If the NOS has been removed:
            if (owner.AllNamedObjects.Contains(namedObject) == false)
            {
                return true;
            }

            // If the file no longer exists:
            if (achxFilePath.Exists() == false)
            {
                return true;
            }

            // If the owner animation name is different
            var currentAnimationName = namedObject.InstructionSaves.FirstOrDefault(item => item.Member == "CurrentChainName");

            if (currentAnimationName == null)
            {
                return true;
            }
            if (currentAnimationName.Value as string != animationName)
            {
                return true;
            }

            // If the .achx now includes this animation:
            HashSet<string> animationNames = null;
            if (animationNamesByFile == null || !animationNamesByFile.TryGetValue(achxFilePath.FullPath, out animationNames))
            {
                animationNames = ReadAnimationNames(achxFilePath);
                animationNamesByFile?.Add(achxFilePath.FullPath, animationNames);
            }

            // A file that can't be parsed reports no missing animation; the parse error isn't this error's to report.
            return animationNames != null && animationNames.Contains(animationName) == false;
        }

        static HashSet<string> ReadAnimationNames(FilePath achxFilePath)
        {
            try
            {
                return ReadAnimationFile(achxFilePath).AnimationChains.Select(item => item.Name).ToHashSet();
            }
            catch
            {
                return null;
            }
        }

        public override bool GetIfIsFixed()
        {
            return GetIfHasError(owner, namedObject, AchxFilePath, AnimationName) == false;
        }

        public override void HandleDoubleClick()
        {
            GlueState.Self.CurrentNamedObjectSave = namedObject;
        }

        public override bool ReactsToFileChange(FilePath filePath)
        {
            return AchxFilePath == filePath;
        }
    }
}
