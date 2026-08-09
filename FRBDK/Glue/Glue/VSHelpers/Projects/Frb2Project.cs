using Microsoft.Build.Evaluation;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    /// <summary>
    /// A FlatRedBall 2 game project. FRB2 builds its Screens and Entities from Glue's
    /// .gluj/.glsj/.glej JSON at runtime, so Glue authors only that JSON here: it generates no code
    /// and never writes the .csproj back to disk.
    /// </summary>
    /// <remarks>
    /// The .csproj is still fully loaded and read - it is what identifies the project as FRB2 (see
    /// <see cref="Frb2ProjectDetector"/>) and what resolves the project directory and Content folder
    /// that every relative file path in the JSON hangs off of. Only writes are suppressed, via
    /// <see cref="ProjectBase.IsMaintainedByGlue"/>.
    ///
    /// Getting those JSON files copied to the build output is the game project's own job: one static
    /// glob it carries in its .csproj, not something Glue maintains.
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
        /// Matches what FRB2 expects at runtime: <c>GlueContentSource</c> resolves every referenced
        /// file as <c>&lt;directory holding the .gluj&gt;/Content/&lt;name&gt;</c>.
        /// </summary>
        public override string ContentDirectory => "Content/";

        public override bool IsMaintainedByGlue => false;

        /// <summary>
        /// Self-contained, so the game copies it to output with one rule and no exclusions, and so it
        /// parallels the Gum project's own Content/GumProject/ folder.
        /// </summary>
        public override string GlueProjectSubdirectory => "Content/FrbEditor/";

        public override bool AllowContentCompile => false;

        protected override bool NeedToSaveContentProject => false;

        public Frb2Project(Project project) : base(project)
        {
            ContentProject = this;
            CodeProject = this;
        }
    }
}
