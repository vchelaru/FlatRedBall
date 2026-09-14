using System.Linq;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Split out of <c>ReferencedFileSaveExtensionMethods</c> (in <c>Glue.csproj</c>, net8.0-windows):
    /// these methods only touch a <see cref="ReferencedFileSave"/>'s own <see cref="PropertySave"/>
    /// list, with no singleton coupling. Lives here (net8.0, no WPF) so it and its tests can build and
    /// run on Linux/macOS. See #2276. Named differently from the original class (not a forwarding stub)
    /// to avoid a duplicate-type clash now that both assemblies are visible together via
    /// <c>Glue.csproj</c>'s <c>ProjectReference</c> to <c>GlueCommon</c>; extension method resolution
    /// doesn't care which class declares it, so existing call sites are unaffected.
    /// </summary>
    public static class ReferencedFileSavePropertyExtensions
    {
        public static T GetProperty<T>(this ReferencedFileSave referencedFileSave, string propertyName)
        {
            var propertySave = referencedFileSave.Properties.FirstOrDefault(
                item => item.Name == propertyName);

            if (propertySave?.Value != null)
            {
                return (T)propertySave.Value;
            }
            else
            {
                return default(T);
            }
        }

        public static void SetProperty(this ReferencedFileSave referencedFileSave, string propertyName, object value)
        {
            var propertySave = referencedFileSave.Properties.FirstOrDefault(
                item => item.Name == propertyName);

            if (propertySave != null)
            {
                propertySave.Value = value;
            }
            else
            {
                propertySave = new PropertySave();
                propertySave.Value = value;
                propertySave.Name = propertyName;

                referencedFileSave.Properties.Add(propertySave);
            }
        }
    }
}
