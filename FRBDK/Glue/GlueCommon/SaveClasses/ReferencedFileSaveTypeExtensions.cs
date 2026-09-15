using FlatRedBall.IO;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Split out of <c>ReferencedFileSaveExtensionMethods</c> (in <c>Glue.csproj</c>, net8.0-windows):
    /// resolves the C# type name a referenced file's data deserializes into. Only reaches
    /// <c>ObjectFinder.Self.GlueProject</c> (already covered by <see cref="IObjectFinderCore"/>) and
    /// <c>GlueState.Self.ProjectNamespace</c> (covered by the new <see cref="IGlueStateCore"/>). Lives
    /// here (net8.0, no WPF) so it and its tests can build and run on Linux/macOS. See #2276. Named
    /// differently from the original class (not a forwarding stub) to avoid a duplicate-type clash now
    /// that both assemblies are visible together via <c>Glue.csproj</c>'s <c>ProjectReference</c> to
    /// <c>GlueCommon</c>; extension method resolution doesn't care which class declares it, so existing
    /// call sites are unaffected.
    /// </summary>
    public static class ReferencedFileSaveTypeExtensions
    {
        public static string GetUnqualifiedTypeForCsv(this ReferencedFileSave referencedFileSave, string alternativeFileName = null)
        {
            string toReturn = GetTypeForCsvFile(referencedFileSave, alternativeFileName);

            if (toReturn.Contains('.'))
            {
                int startOfUnqualified = toReturn.LastIndexOf('.') + 1;
                toReturn = toReturn.Substring(startOfUnqualified);
            }

            return toReturn;
        }

        public static string GetTypeForCsvFile(this ReferencedFileSave referencedFileSave, string alternativeFileName = null)
        {
            if (referencedFileSave == null)
            {
                throw new System.ArgumentNullException("ReferencedFileSave is null - it can't be.");
            }

            string fileName = referencedFileSave.Name;
            if (!string.IsNullOrEmpty(alternativeFileName))
            {
                fileName = alternativeFileName;
            }

            if (!string.IsNullOrEmpty(referencedFileSave.UniformRowType))
            {
                return referencedFileSave.UniformRowType;
            }
            else
            {
                string className;

                // Is this file using a custom class?
                CustomClassSave ccs = ObjectFinderCore.Self.GlueProject.GetCustomClassReferencingFile(fileName);
                if (ccs == null)
                {
                    className = FileManager.RemovePath(FileManager.RemoveExtension(fileName));
                    if (className.EndsWith("File"))
                    {
                        className = className.Substring(0, className.Length - "File".Length);
                    }

                    className = GlueStateCore.Self.ProjectNamespace + ".DataTypes." + className;
                }
                else
                {
                    if (!string.IsNullOrEmpty(ccs.CustomNamespace))
                    {
                        className = ccs.CustomNamespace + "." + ccs.Name;
                    }
                    else
                    {
                        className = GlueStateCore.Self.ProjectNamespace + ".DataTypes." + ccs.Name;
                    }
                }
                return className;
            }
        }
    }
}
