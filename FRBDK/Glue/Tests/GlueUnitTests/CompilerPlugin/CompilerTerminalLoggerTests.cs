using CompilerLibrary.ViewModels;
using CompilerPlugin.Managers;
using Shouldly;
using Xunit;

namespace GlueUnitTests.CompilerPlugin
{
    /// <summary>
    /// A user reported the Build tab filling with hundreds of raw terminal escape sequences
    /// (cursor-hide/move codes) - MSBuild's newer live-progress terminal logger, captured as
    /// literal text because Glue redirects StandardOutput instead of attaching a real console.
    /// CreateProcess never calls Start(), so this pins the ProcessStartInfo it builds without
    /// launching a real process.
    /// </summary>
    public class CompilerTerminalLoggerTests
    {
        [Fact]
        public void CreateProcess_SetsMsBuildTerminalLoggerOff()
        {
            var compiler = new Compiler(CompilerViewModel.Self);

            var process = compiler.CreateProcess("dotnet", "msbuild");

            process.StartInfo.EnvironmentVariables["MSBUILDTERMINALLOGGER"].ShouldBe("off");
        }
    }
}
