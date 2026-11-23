namespace FlatRedBall.Glue.IO
{
    /// <summary>
    /// How much of the editor's project-open work <see cref="ProjectLoader.LoadProject(string, ProjectLoadScope, GlueFormsCore.Controls.InitializationWindowWpf)"/> does.
    /// </summary>
    public enum ProjectLoadScope
    {
        /// <summary>
        /// What the editor's File > Load Project does: the model load plus every <see cref="ProjectLoadOperations"/>
        /// step (generate all code, save what the fix-ups changed, .csproj fix-ups, unreferenced files, editor UI).
        /// </summary>
        EditorSession,

        /// <summary>
        /// Reads the .csproj, the .gluj and its element files, applies the in-memory fix-ups, and notifies plugins
        /// that the project loaded. Glue itself writes nothing; plugin load reactions still run and can write
        /// (Gum and Tiled generate their code there).
        /// </summary>
        ModelOnly,
    }
}
