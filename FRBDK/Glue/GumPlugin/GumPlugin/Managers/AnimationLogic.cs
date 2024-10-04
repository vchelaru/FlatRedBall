using FlatRedBall.Glue.IO;
using FlatRedBall.IO;
using Gum.DataTypes;
using GumPlugin.Managers;
using StateAnimationPlugin.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace GumPlugin.Managers
{
    public static class AnimationLogic
    {
        public static ElementAnimationsSave GetAnimationsFor(ElementSave elementSave)
        {
            var animationFileName = GetAnimationFile(elementSave);

            ElementAnimationsSave animations = null;

            if (animationFileName.Exists())
            {
                try
                {
                    animations = FileManager.XmlDeserialize<ElementAnimationsSave>(animationFileName.FullPath);
                }
                catch (Exception e)
                {
                    throw new Exception($"Error trying to generate code for animation file {animationFileName}:\n{e}",
                        e);
                }
            }
            return animations;
        }

        public static FilePath GetAnimationFile(ElementSave elementSave)
        {
            var gumProjectName = AppState.Self.GumProjectSave.FullFileName;
            if(!string.IsNullOrEmpty(gumProjectName))
            {
                string gumFolder = FileManager.GetDirectory(AppState.Self.GumProjectSave.FullFileName);

                string fullAnimationName = null;
                fullAnimationName = gumFolder + elementSave.Subfolder + "/" + elementSave.Name + "Animations.ganx";
                return fullAnimationName;
            }
            else
            {
                return null;
            }
        }
    }
}
