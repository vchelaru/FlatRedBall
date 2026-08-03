namespace BuildServerUploaderConsole.Data
{
    /// <summary>
    /// One NuGet package shipped as part of an engine. Every assembly a template-created project
    /// references is published this way, so the whole set moves together on restore rather than
    /// half of it coming from a Libraries folder baked into the template zip.
    /// </summary>
    public class PackageData
    {
        /// <summary>
        /// The .csproj that produces the package, relative to the checkout folder (the one holding
        /// both FlatRedBall\ and Gum\). Its &lt;Version&gt; is stamped at release time.
        /// </summary>
        public string CsProjLocation { get; set; }

        /// <summary>
        /// The NuGet package id. This is not always the assembly name: assemblies whose names read
        /// as belonging to another project (GumCore, SkiaInGum) get a FlatRedBall-scoped id so the
        /// package says who publishes it.
        /// </summary>
        public string PackageId { get; set; }

        public override string ToString() => PackageId;
    }
}
