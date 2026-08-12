using Microsoft.Build.Evaluation;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    /// <summary>
    /// A FlatRedBall 2 game project. FRB2 builds its Screens and Entities from Glue's
    /// .gluj/.glsj/.glej JSON at runtime, so Glue authors only that JSON here: none of FRB1's code
    /// generation runs, and the .csproj is never written back to disk.
    /// </summary>
    /// <remarks>
    /// The .csproj is still fully loaded and read - it is what identifies the project as FRB2 (see
    /// <see cref="Frb2ProjectDetector"/>) and what resolves the project directory and Content folder
    /// that every relative file path in the JSON hangs off of. Only writes are suppressed, via
    /// <see cref="ProjectBase.IsMaintainedByGlue"/>.
    ///
    /// Getting those JSON files copied to the build output is the game project's own job: one static
    /// glob it carries in its .csproj, not something Glue maintains.
    ///
    /// A project can opt into typed accessors for its Screens/Entities
    /// (<c>GlueProjectSave.GenerateCode</c>), which <c>Frb2CodeGenerator</c> writes beside the JSON
    /// without going through any of those suppressed seams. That opt-in deliberately leaves
    /// <see cref="IsMaintainedByGlue"/> false - see its own remarks.
    /// </remarks>
    public class Frb2Project : VisualStudioProject
    {
        public override string ProjectId => "Frb2";

        // FRB2 projects do not define a Glue-recognized preprocessor constant. This is the string
        // ProjectCreator's cascade keys off of, and nothing should ever match this project that way.
        public override string PrecompilerDirective => "";

        public override string FolderName => "Frb2";

        public override string NeededVisualStudioVersion => "17.0";

        /// <summary>
        /// The same folder the .gluj sits in, so referenced content lands inside the one directory
        /// that holds everything the editor authored rather than in the game's own content tree.
        /// </summary>
        /// <remarks>
        /// This is what makes the folder genuinely self-contained: a PNG dropped on a screen goes to
        /// Content/FrbEditor/Screens/&lt;screen&gt;/, so deleting Content/FrbEditor takes the art with
        /// it instead of orphaning it. It also gives FRB2 a single rule at runtime - every referenced
        /// file resolves relative to the .gluj.
        /// </remarks>
        public override string ContentDirectory => GlueProjectSubdirectory;

        public override bool IsMaintainedByGlue => false;

        /// <summary>
        /// Self-contained, so the game copies it to output with one rule and no exclusions, and so it
        /// parallels the Gum project's own Content/GumProject/ folder.
        /// </summary>
        public const string Frb2GlueProjectSubdirectory = "Content/FrbEditor/";

        public override string GlueProjectSubdirectory => Frb2GlueProjectSubdirectory;

        public override bool AllowContentCompile => false;

        protected override bool NeedToSaveContentProject => false;

        public Frb2Project(Project project) : base(project)
        {
            ContentProject = this;
            CodeProject = this;
        }
    }
}
