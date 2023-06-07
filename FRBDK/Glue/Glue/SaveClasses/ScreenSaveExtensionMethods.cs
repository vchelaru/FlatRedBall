using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Elements;

namespace FlatRedBall.Glue.SaveClasses
{
    public static class ScreenSaveExtensionMethods
    {
        //public static void CleanUnusedVariablesFromStates(this ScreenSave screenSave)
        //{
        //    IElementHelper.CleanUnusedVariablesFromStates(screenSave);
        //}
        
        
        public static bool InheritsFrom(this ScreenSave instance, string screen)
        {
            if (instance.BaseScreen == screen)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(instance.BaseScreen))
            {
                var baseScreen = ObjectFinder.Self.GetScreenSave(instance.BaseScreen);

                if (baseScreen != null)
                {
                    return baseScreen.InheritsFrom(screen);
                }
            }

            return false;
        }

        public static List<ScreenSave> GetAllBaseScreens(this ScreenSave instance)
        {
            List<ScreenSave> listToReturn = new List<ScreenSave>();

            instance.GetAllBaseScreens(listToReturn);

            return listToReturn;
        }

        public static void GetAllBaseScreens(this ScreenSave instance, List<ScreenSave> listToFill)
        {
            if (!string.IsNullOrEmpty(instance.BaseScreen))
            {
                ScreenSave baseScreen = ObjectFinder.Self.GetScreenSave(instance.BaseScreen);

                if (baseScreen != null)
                {
                    listToFill.Add(baseScreen);

                    baseScreen.GetAllBaseScreens(listToFill);
                }
            }
        }



        public static ReferencedFileSave GetReferencedFileSaveRecursively(this ScreenSave instance, string fileName)
        {
            ReferencedFileSave rfs = FileReferencerHelper.GetReferencedFileSave(instance, fileName);

            if (rfs == null && !string.IsNullOrEmpty(instance.BaseScreen))
            {
                var baseElement = ObjectFinder.Self.GetElement(instance.BaseScreen);

                if (baseElement != null)
                {
                    rfs = baseElement.GetReferencedFileSaveRecursively(fileName);
                }
            }

            return rfs;
        }

        public static bool DoesMemberNeedToBeSetByContainer(this ScreenSave instance, string memberName)
        {
            return NamedObjectContainerHelper.DoesMemberNeedToBeSetByContainer(instance, memberName);
        }

    }
}
