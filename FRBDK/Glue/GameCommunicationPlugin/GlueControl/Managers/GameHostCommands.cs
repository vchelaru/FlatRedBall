using System;
using System.Threading.Tasks;
using CompilerLibrary.ViewModels;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;

namespace GameCommunicationPlugin.GlueControl.Managers;

/// <summary>The Glue Compiler plugin's <see cref="IGameHostCommands"/>.</summary>
internal class GameHostCommands : IGameHostCommands
{
    readonly CompilerViewModel viewModel;
    readonly Func<Task> buildAndRun;
    readonly RefreshManager refreshManager;

    public GameHostCommands(CompilerViewModel viewModel, Func<Task> buildAndRun, RefreshManager refreshManager)
    {
        this.viewModel = viewModel;
        this.buildAndRun = buildAndRun;
        this.refreshManager = refreshManager;
    }

    public bool IsGameRunning() => viewModel.IsRunning;

    public Task BuildAndRunAsync() =>
        TaskManager.Self.AddAsync(async () =>
        {
            try
            {
                await buildAndRun();
            }
            catch (Exception e)
            {
                GlueCommands.Self.PrintError($"Glue Compiler failed in BuildAndRun\n{e}");
            }
        }, "Glue Compiler: build and run");

    public async Task ShowStateAsync(string categoryName, string stateName)
    {
        try
        {
            await refreshManager.PushGlueSelectionToGame(categoryName, stateName);
        }
        catch (Exception e)
        {
            GlueCommands.Self.PrintError($"Glue Compiler failed showing state {stateName}\n{e}");
        }
    }

    public void SetIsLoggingSentCommands(bool isLogging) =>
        viewModel.IsPrintEditorToGameCheckboxChecked = isLogging;

    public void SetIsLoggingReceivedCommands(bool isLogging) =>
        viewModel.IsPrintGameToEditorCheckboxChecked = isLogging;
}
