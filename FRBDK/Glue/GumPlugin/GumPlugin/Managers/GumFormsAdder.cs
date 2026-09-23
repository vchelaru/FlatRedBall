using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GumPlugin.DataGeneration;
using System.Threading.Tasks;

namespace GumPlugin.Managers;

// Single entry point for "add Forms controls to the current Gum project", used by both
// NewGumProjectCreationLogic (new project + forms) and GumControl.xaml.cs's three Forms buttons.
// Projects at GumDefaults2+ go through gumcli add-forms (see issue #2335); older projects keep
// using the legacy vendored FormsControlAdder template, which this class does not change.
public static class GumFormsAdder
{
    public static bool IsCurrentFormat =>
        GlueState.Self.CurrentGlueProject?.FileVersion >= (int)GlueProjectSave.GluxVersions.GumDefaults2;

    public static async Task<bool> AddFormsToCurrentProjectAsync(bool askToOverwrite)
    {
        if (IsCurrentFormat)
        {
            // gumcli add-forms only ever adds elements that are missing - it never overwrites an
            // existing file - so there is nothing for askToOverwrite to confirm here.
            var gumxFilePath = GumProjectManager.Self.GetGumProjectFileName();
            var succeeded = await GumCliRunner.AddFormsAsync(gumxFilePath);
            if (succeeded)
            {
                GumProjectManager.Self.ReloadGumProject();
            }
            return succeeded;
        }
        else
        {
            var assembly = typeof(FormsControlAdder).Assembly;

            if (askToOverwrite && !FormsControlAdder.AskToSaveIfOverwriting(assembly))
            {
                return false;
            }

            await FormsControlAdder.SaveElements(assembly);
            await FormsControlAdder.SaveBehaviors(assembly);
            return true;
        }
    }
}
