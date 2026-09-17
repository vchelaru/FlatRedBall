using FlatRedBall.Glue.Elements;

namespace FlatRedBall.Glue.SaveClasses
{
    public static class ScreenSaveExtensionMethods
    {
        // InheritsFrom and GetAllBaseScreens moved to GlueCommon.SaveClasses.ScreenSaveElementExtensions (#2276).
        // GetReferencedFileSaveRecursively stays until the GlueElement overload it recurses into
        // (IElementExtensionMethods.GetReferencedFileSaveRecursively) lands in GlueCommon.

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

        // DoesMemberNeedToBeSetByContainer moved to GlueCommon.SaveClasses.ElementExtensions (#2276).

    }
}
