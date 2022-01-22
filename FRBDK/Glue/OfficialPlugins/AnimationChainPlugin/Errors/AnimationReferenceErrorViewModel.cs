using FlatRedBall.Content.AnimationChain;
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
        public FilePath AchxFilePath { get;}
        public string ObjectName {  get; }
        public string AnimationName { get;}

        public override string UniqueId => Details;

        public AnimationReferenceErrorViewModel(string filePath, NamedObjectSave namedObject, string animation)
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

        public override bool GetIfIsFixed()
        {
            // todo - need to handle a situation where the user changes which .achx file is referenced by the nos
            // Not doing that yet because it's more work and it can be added later

            // This is fixed if:
            // The element no longer exists:
            var project = GlueState.Self.CurrentGlueProject;
            if(owner is ScreenSave screenSave && project.Screens.Contains(screenSave) == false)
            {
                return true;
            }
            if(owner is EntitySave entitySave && project.Entities.Contains(entitySave) == false)
            {
                return true;
            }

            // If the NOS has been removed:
            if(owner.AllNamedObjects.Contains(namedObject) == false)
            {
                return true;
            }

            // If the file no longer exists:
            if(AchxFilePath.Exists() == false)
            {
                return true;
            }

            // If the owner animation name is different
            var currentAnimationName = namedObject.InstructionSaves.FirstOrDefault(item => item.Member == "CurrentChainName");

            if(currentAnimationName == null)
            {
                return true;
            }
            if(currentAnimationName.Value as string != AnimationName)
            {
                return true;
            }

            // If the .achx now includes this animation:
            try
            {
                var achSave = AnimationChainListSave.FromFile(AchxFilePath.FullPath);
                if(achSave.AnimationChains.Any(item => item.Name == AnimationName))
                {
                    return true;
                }
            }
            catch
            {
                // this could be a parse error, but that's not the responsibility of this error to report
            }


            return false;
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
