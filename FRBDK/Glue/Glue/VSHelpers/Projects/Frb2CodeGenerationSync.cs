using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    /// <summary>
    /// Applies a loaded project's <see cref="GlueProjectSave.GenerateCode"/> setting to
    /// <see cref="ProjectBase.IsMaintainedByGlue"/>, which is what <c>GlueCommands.GenerateCodeCommands</c>
    /// and <c>CodeWritePolicy</c> actually key off. Only FRB2 projects are affected - FRB1 code
    /// generation is mandatory and this setting has no meaning for it.
    /// </summary>
    public static class Frb2CodeGenerationSync
    {
        public static void ApplyGenerateCodeSetting(ProjectBase project, GlueProjectSave glueProjectSave)
        {
            if (project is Frb2Project)
            {
                project.IsMaintainedByGlue = glueProjectSave?.GenerateCode == true;
            }
        }
    }
}
