using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.IO;
using FlatRedBall.IO;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using GumPlugin.Managers;
using GumPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Text;

namespace GumPlugin.ErrorReporting
{
    class AnimationCategoryNamingError : ErrorViewModel
    {
        string elementName;
        string categoryName;
        string animationName;

        public override string UniqueId => Details;

        public AnimationCategoryNamingError(string elementName, string categoryName, string animationName)
        {
            this.elementName = elementName;
            this.categoryName = categoryName;
            this.animationName = animationName;

            this.Details = $"{elementName} has a category named {categoryName} and an animation named {animationName} which will get generated to {animationName + "Animation"}";
        }

        public override bool GetIfIsFixed()
        {
            var gumProject = AppState.Self.GumProjectSave;
            if(gumProject == null)
            {
                return true;
            }

            var element = ObjectFinder.Self.GetElementSave(elementName);
            if(element == null)
            {
                return true;
            }

            var category = element.Categories.Find(item => item.Name == categoryName);
            if(category == null)
            {
                return true;
            }

            var animations = AnimationLogic.GetAnimationsFor(element);

            if(animations == null || animations.Animations.Count == 0)
            {
                return true;
            }

            var animation = animations.Animations.Find(item => item.Name == animationName);

            if(animation == null)
            {
                return true;
            }

            return false;
        }

        public override bool ReactsToFileChange(FilePath filePath)
        {
            var element = ObjectFinder.Self.GetElementSave(elementName);

            var animationFilePath = AnimationLogic.GetAnimationFile(element);

            if(animationFilePath == filePath)
            {
                return true;
            }


            string gumFolder = FileManager.GetDirectory(AppState.Self.GumProjectSave.FullFileName);

            string elementFileName = gumFolder + element.Subfolder + "/" + element.Name + "." + element.FileExtension;

            return elementFileName == filePath;
        }

    }
}
