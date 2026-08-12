using System;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations
{
    /// <summary>
    /// Whether Glue may write C# into the loaded project. It may not for a project it does not own the
    /// code of - today that means a FlatRedBall 2 project, which reads Glue's .gluj/.glsj/.glej JSON
    /// directly at runtime and owns all of its own source.
    /// </summary>
    /// <remarks>
    /// This exists as one named rule rather than a condition repeated at each call site because code
    /// files are written from several unrelated places, and a missed one puts files into a user's
    /// project that Glue has no business creating. The chokepoints that consult it are
    /// <c>FileCommands.SaveIfDiffers</c> (where every generator that writes text ends up),
    /// <c>ProjectCommands.CreateAndAddCodeFile</c> (which creates an empty placeholder before anything
    /// fills it), and <c>ProjectCommands.CreateAndAddPartialFile</c>.
    ///
    /// It is a backstop, not the primary mechanism: <c>GlueCommands.GenerateCodeCommands</c> already
    /// resolves to a do-nothing implementation for these projects. What this catches is the generation
    /// that never goes through that interface - <c>CodeBuildItemAdder</c>'s embedded resource files and
    /// <c>CameraSetupCodeGenerator</c>, both of which write on glux load.
    /// </remarks>
    public static class CodeWritePolicy
    {
        /// <summary>
        /// True when Glue owns the loaded project's code. True with no project loaded, so nothing
        /// changes behavior before a project type is known.
        /// </summary>
        public static bool WritesCodeForCurrentProject =>
            GlueState.Self.CurrentMainProject?.IsMaintainedByGlue != false;

        /// <summary>
        /// True when the loaded project is an FRB2 project that has opted into typed accessors for its
        /// Screens and Entities (<see cref="GlueProjectSave.GenerateCode"/>).
        /// </summary>
        /// <remarks>
        /// Deliberately separate from <see cref="WritesCodeForCurrentProject"/> rather than turning it
        /// on, because that property answers "does Glue own this project's code and .csproj" - and
        /// every FRB1 generator, the csproj writer, and the tree view's Code/Events nodes read it.
        /// Making the opt-in flip it ran the entire FRB1 generator set against an FRB2 project
        /// (CameraSetup, FileAliases, the Performance/ pool types, the Tiled and TopDown plugins' output)
        /// and let Glue rewrite the .csproj, none of which an FRB2 game can compile or wants.
        ///
        /// Read live off the loaded <c>GlueProjectSave</c> rather than cached onto the project at load
        /// time, so there is no ordering requirement between "the JSON is loaded" and "the flag is
        /// applied", and no stale copy after the setting changes.
        /// </remarks>
        public static bool GeneratesFrb2Code =>
            GlueState.Self.CurrentMainProject is Frb2Project &&
            GlueState.Self.CurrentGlueProject?.GenerateCode == true;

        public static bool IsSuppressedCodeFile(FilePath filePath) =>
            !WritesCodeForCurrentProject &&
            string.Equals(filePath?.Extension, "cs", StringComparison.OrdinalIgnoreCase);

        public static bool IsSuppressedCodeFile(string fileName) =>
            !string.IsNullOrEmpty(fileName) && IsSuppressedCodeFile(new FilePath(fileName));
    }
}
