namespace FlatRedBall.Glue.IO
{
    /// <summary>
    /// How much of the editor's project-open work <see cref="ProjectLoader.LoadProject(string, ProjectLoadScope, GlueFormsCore.Controls.InitializationWindowWpf)"/> does.
    /// </summary>
    public enum ProjectLoadScope
    {
        /// <summary>
        /// What the editor's File > Load Project does: the model load plus every <see cref="ProjectLoadOperations"/>
        /// step (plugin project code, generate all code, save what the fix-ups changed, .csproj fix-ups,
        /// unreferenced files, editor UI).
        /// </summary>
        EditorSession,

        /// <summary>
        /// Reads the .csproj, the .gluj and its element files, applies the in-memory fix-ups, and notifies plugins
        /// that the project loaded. Writes nothing: plugins write their project-level code in
        /// <see cref="ProjectLoadOperations.GeneratePluginProjectCode"/>, which this scope does not call.
        /// </summary>
        ModelOnly,
    }
}
