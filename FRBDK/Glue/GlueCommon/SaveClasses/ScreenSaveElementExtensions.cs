using System.Collections.Generic;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Split out of <c>ScreenSaveExtensionMethods</c> (in <c>Glue.csproj</c>, net8.0-windows): these
    /// methods walk a <see cref="ScreenSave"/>'s base-screen chain, so they resolve screens by name through
    /// <see cref="IObjectFinderCore"/> (a narrow seam over <c>ObjectFinder.Self</c>, see that interface's
    /// doc comment) instead of reaching for <c>ObjectFinder.Self</c> directly. Lives here (net8.0, no WPF)
    /// so it and its tests can build and run on Linux/macOS. See issue #2276. Named differently from the
    /// original class (not a forwarding stub) to avoid a duplicate-type clash now that both assemblies are
    /// visible together via <c>Glue.csproj</c>'s <c>ProjectReference</c> to <c>GlueCommon</c>; extension
    /// method resolution doesn't care which class declares it, so existing call sites are unaffected.
    /// </summary>
    public static class ScreenSaveElementExtensions
    {
        public static bool InheritsFrom(this ScreenSave instance, string screen)
        {
            if (instance.BaseScreen == screen)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(instance.BaseScreen))
            {
                var baseScreen = ObjectFinderCore.Self.GetScreenSave(instance.BaseScreen);

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
                ScreenSave baseScreen = ObjectFinderCore.Self.GetScreenSave(instance.BaseScreen);

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
                // Static type is GlueElement, so this binds to the GlueElement overload in ElementExtensions
                // (same as it did in Glue.csproj), not back to this one.
                var baseElement = ObjectFinderCore.Self.GetElement(instance.BaseScreen);

                if (baseElement != null)
                {
                    rfs = baseElement.GetReferencedFileSaveRecursively(fileName);
                }
            }

            return rfs;
        }
    }
}
