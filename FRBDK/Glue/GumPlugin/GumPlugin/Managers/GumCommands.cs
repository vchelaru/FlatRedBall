using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;

namespace GumPlugin.Managers;

/// <summary>The Gum Plugin's <see cref="IGumCommands"/>.</summary>
internal class GumCommands : IGumCommands
{
    readonly NewGumProjectCreationLogic newGumProjectCreationLogic;
    readonly Action updateAfterProjectCreation;

    /// <param name="updateAfterProjectCreation">The plugin's UI update after it creates a Gum project.</param>
    public GumCommands(NewGumProjectCreationLogic newGumProjectCreationLogic, Action updateAfterProjectCreation)
    {
        this.newGumProjectCreationLogic = newGumProjectCreationLogic;
        this.updateAfterProjectCreation = updateAfterProjectCreation;
    }

    public bool HasGum() => AppState.Self.GumProjectSave != null;

    public bool IsAssetTypeInfoGum(AssetTypeInfo ati) =>
        ati != null && AssetTypeInfoManager.Self.IsAssetTypeInfoGum(ati);

    public IReadOnlyList<string> GetFormsGeneratedRelativePaths() =>
        CodeGeneratorManager.Self.GetFormsGeneratedRelativePaths().ToList();

    public Task AskToCreateGumProjectAsync() => newGumProjectCreationLogic.AskToCreateGumProject();

    public Task CreateGumProjectAsync(bool includeForms, bool askToOverwrite) =>
        TaskManager.Self.AddAsync(async () =>
        {
            try
            {
                await newGumProjectCreationLogic.CreateGumProjectInternal(includeForms, askToOverwrite);
                updateAfterProjectCreation();
            }
            catch (Exception e)
            {
                GlueCommands.Self.PrintError($"Gum Plugin failed creating a Gum project\n{e}");
            }
        }, "Gum Plugin: create Gum project");
}
