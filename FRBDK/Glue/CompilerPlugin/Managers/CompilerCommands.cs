using System;
using System.Threading.Tasks;
using CompilerLibrary.ViewModels;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.Managers;
using ToolsUtilities;

namespace CompilerPlugin.Managers
{
    /// <summary>
    /// The Compiler Plugin's <see cref="ICompilerCommands"/>. Each async command runs as a TaskManager task (inline
    /// when the caller is already in one), and an exception is printed as an error instead of reaching the caller.
    /// </summary>
    internal class CompilerCommands : ICompilerCommands
    {
        readonly Compiler compiler;
        readonly Runner runner;
        readonly CompilerViewModel viewModel;
        readonly Action<string> printOutput;
        readonly Action<string> printError;

        public CompilerCommands(Compiler compiler, Runner runner, CompilerViewModel viewModel,
            Action<string> printOutput, Action<string> printError)
        {
            this.compiler = compiler;
            this.runner = runner;
            this.viewModel = viewModel;
            this.printOutput = printOutput;
            this.printError = printError;
        }

        public void PrintOutput(string output) => printOutput(output);

        public Task<CompileGeneralResponse> CompileAsync(string configuration, bool printMsBuildCommand) =>
            RunAsTask(async () =>
            {
                try
                {
                    var result = await compiler.Compile(printOutput, printError,
                        !viewModel.HasDoneNugetRestore, configuration, printMsBuildCommand);

                    if (result.Succeeded)
                    {
                        viewModel.HasDoneNugetRestore = true;
                    }
                    // Copies Succeeded and Message only, so WasCancelled stays false, as it always has for
                    // callers outside this plugin. Passing it through changes RefreshManager's failure report.
                    var response = new CompileGeneralResponse();
                    response.SetFrom(result);
                    return response;
                }
                catch (Exception ex)
                {
                    return new CompileGeneralResponse { Succeeded = false, Message = ex.ToString() };
                }
            }, new CompileGeneralResponse(), "Compile");

        public Task<GeneralResponse> RunAsync(bool preventFocus, string runArguments) =>
            RunAsTask(async () =>
            {
                var response = new GeneralResponse();
                response.SetFrom(await runner.Run(preventFocus, runArguments, numberOfSecondsToWait: 20));
                return response;
            }, new GeneralResponse(), "Run");

        public Task KillGameProcessAsync() =>
            RunAsTask(async () =>
            {
                await runner.KillGameProcess();
                return true;
            }, false, "Kill game process");

        static async Task<T> RunAsTask<T>(Func<Task<T>> func, T resultOnException, string name)
        {
            var result = resultOnException;
            await TaskManager.Self.AddAsync(async () =>
            {
                try
                {
                    result = await func();
                }
                catch (Exception e)
                {
                    GlueCommands.Self.PrintError($"Compiler Plugin failed in {name}\n{e}");
                }
            }, $"Compiler Plugin: {name}");
            return result;
        }
    }
}
